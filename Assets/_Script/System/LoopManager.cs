using UnityEngine;

public class LoopManager : MonoBehaviour
{
    [Header("Pattern State")]
    public bool hasMonster = false; // 층별 몬스터 존재 여부
    public int floor = 0;           // 0층 = 둘러보기층

    [Header("Refs")]
    public EnemyController enemyController;
    public System.Action<int> OnFloorChanged;

    [Header("FixLines (Inspector Assign)")]
    public FixLine[] fixLines;  // FindObjectsByType 금지 -> 인스펙터로 넣기

    // Fix state
    bool fixCommitted = false;
    public bool FixCommitted => fixCommitted;

    bool fixedSideA = false;
    public bool FixedSideA => fixedSideA;

    public void OnFix(bool sideA)
    {
        if (fixCommitted) return;

        fixCommitted = true;
        fixedSideA = sideA;

        Debug.Log($"[FIX] Fix위치={(sideA ? "A측" : "B측")}");
    }

    public void OnTransition(TransitionHub transHub)
    {
        // 0층 처리(유지)
        if (floor == 0)
        {
            floor = 1;
            SetupPattern();
            OnFloorChanged?.Invoke(floor);
            ApplyEnemyByPattern();
            return;
        }

        // Fix 전이라면: 판정하지 말고 매 Transition마다 새 패턴 세팅(당신 설계 유지)
        if (!fixCommitted)
        {
            SetupPattern();
            ApplyEnemyByPattern();
            return;
        }

        // Fix 후에만 정답/오답 판정
        bool usedSideA = transHub.sideA;          // 실제 선택
        bool usedFix = (usedSideA == fixedSideA); // Fix와 일치했는가

        bool correct = hasMonster ? usedFix : !usedFix;

        if (correct) floor++;
        else floor = 1;

        OnFloorChanged?.Invoke(floor);

        // Fix는 한 번 판정했으면 해제
        fixCommitted = false;

        // 다음 패턴 세팅
        SetupPattern();
        ApplyEnemyByPattern();
    }

    void ApplyEnemyByPattern()
    {
        if (hasMonster) enemyController?.ActivateOne();
        else enemyController?.DeactivateAll();
    }

    public void ResetFixLine()
    {
        // FindObjectsByType 금지 -> 인스펙터 배열만 순회
        if (fixLines != null)
        {
            for (int i = 0; i < fixLines.Length; i++)
            {
                if (fixLines[i] != null)
                    fixLines[i].ResetFix();
            }
        }

        // Fix 상태도 초기화
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

        enemyController?.ResetAll();

        floor = 1;
        SetupPattern();
        ApplyEnemyByPattern();
    }

    public void OnEnemyEnd()
    {
        Debug.Log("[LOOP] 패턴 종료 → Loop 진행");
        enemyController?.ResetAll();
    }

    public void AfterTransitionReset()
    {
        // Transition 직후 공통 리셋
        ResetFixLine();
        enemyController?.OnTransitionResetAll();
        // ActionTrigger도 나중에 여기로 합치면 됨
    }
}
