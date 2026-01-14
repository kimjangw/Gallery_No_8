using UnityEngine;

public class LoopManager : MonoBehaviour
{
    public bool hasMonster = false; // 층별 몬스터 존재 여부
    public int floor = 0;           // 0층 = 둘러보기층
    public EnemyController enemyController;
    public System.Action<int> OnFloorChanged;

    TransitionHub fixedHub = null;
    bool fixCommitted = false;
    public bool FixCommitted => fixCommitted;
    public void OnFix(TransitionHub hub)
    {
        if (fixCommitted) return; // 이미 Fix 됐다면 덮어쓰기 금지

        fixedHub = hub;
        fixCommitted = true;

        Debug.Log($"[FIX] Fix위치={(hub.isA ? "A측" : "B측")}");
    }

    public void OnTransition(TransitionHub transHub)
    {
        // 0층일 때는 정답 판정 없이 1층 진입
        if (floor == 0)
        {
            floor = 1;
            SetupPattern();
            Debug.Log("[TRANS] 0층 종료 → 1층 시작");
            OnFloorChanged?.Invoke(floor);

            if (hasMonster)
                enemyController?.ActivateOne(); // ← 단일 Enemy 활성
            else
                enemyController?.DeactivateAll(); // ← Idle

            return;
        }

        // 1층 이상인데 Fix 없는 경우는 설계상 비정상
        if (fixedHub == null)
        {
            Debug.LogWarning("[TRANS] Fix 없이 Transition (설계 확인 필요)");
            return;
        }

        bool usedFix = (transHub == fixedHub);

        Debug.Log(
            $"[TRANS] 위치={(transHub.isA ? "A측" : "B측")}" +
            $" | Fix={(fixedHub.isA ? "A측" : "B측")}" +
            $" | usedFix={(usedFix ? "사용됨" : "사용안됨")}"
        );

        // 정답 공식:
        // 몬스터 있으면 → Fix가 정답
        // 몬스터 없으면 → Non-Fix가 정답
        bool correct = hasMonster ? usedFix : !usedFix;

        Debug.Log($"[판정] 몬스터={hasMonster} → 결과={(correct ? "정답" : "오답")}");

        if (correct)
        {
            floor++;
            Debug.Log($"[층 갱신] 정답 → floor={floor}");
            OnFloorChanged?.Invoke(floor);
        }
        else
        {
            floor = 1; // 틀리면 1층으로 리셋
            Debug.Log("[층 갱신] 오답 → 1층으로 리셋");
            OnFloorChanged?.Invoke(floor);
        }

        // Fix 초기화 후 다음 패턴 설정
        fixedHub = null;
        SetupPattern();

        if (hasMonster)
            enemyController?.ActivateOne();
        else
            enemyController?.DeactivateAll();

    }

    public void ResetFixLine()
    {
        foreach (var fix in Object.FindObjectsByType<FixLine>(FindObjectsSortMode.None))
            fix.ResetFix();

        fixedHub = null;
        fixCommitted = false;
    }

    void SetupPattern()
    {
        if (floor == 0)
        {
            hasMonster = false;
            return;
        }

        // 예시: 50% 확률로 등장/비등장
        hasMonster = Random.value < 0.5f;

        Debug.Log($"[패턴] {floor}층 → 몬스터={(hasMonster ? "있음" : "없음")}");
    }


    public void OnEnemyKill()
    {
        Debug.Log("[LOOP] Kill 발생 → Loop Reset");

        // Enemy 초기화
        enemyController?.ResetAll();

        // Kill은 무조건 1층으로 리셋 (설계 반영 가능)
        floor = 1;
        SetupPattern();

        // 새 Loop 시작
        if (hasMonster)
            enemyController?.ActivateOne();
        else
            enemyController?.DeactivateAll();
    }


    public void OnEnemyEnd()
    {
        Debug.Log("[LOOP] 패턴 종료 → Loop 진행");

        enemyController?.ResetAll();

        //floor++;
        //SetupPattern();

        //if (hasMonster)
        //    enemyController?.ActivateOne();
        //else
        //    enemyController?.DeactivateAll();
    }

}