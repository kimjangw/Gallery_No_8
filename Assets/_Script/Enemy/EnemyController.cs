using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemies (attach each enemy behaviour here)")]
    public MonoBehaviour[] enemies;

    int pickedIndex = -1;
    public bool HasPicked => pickedIndex >= 0;

    /* =========================================================
     * Legacy / Compatibility
     * ========================================================= */

    // 기존 LoopManager 호환용(즉시 선택+즉시 Activate)
    public void ActivateOne()
    {
        Debug.Log($"[EnemyController] ActivateOne on {name}", this);
        PickOne();
        ActivatePicked();
    }

    // ActionTrigger 등에서 의미 중립적으로 호출할 별칭(레거시: 즉시 행동)
    public void DoAction()
    {
        ActivateOne();
    }

    // 트리거 방식 준비용(선정만)
    public void PrepareOne()
    {
        PickOne();
        PreparePicked();
    }

    /* =========================================================
     * Pick / Prepare / Start
     * ========================================================= */

    public void PickOne()
    {
        if (enemies == null || enemies.Length == 0) return;

        // Pick 시점에 전부 꺼버리고 하나만 고르는 방식(기존 유지)
        DeactivateAll();

        pickedIndex = Random.Range(0, enemies.Length);
        Debug.Log($"[EnemyController] pickedIndex={pickedIndex}", this);
    }

    // (권장) 이번 턴 적을 "대기 상태"로만 세팅
    public void PreparePicked()
    {
        if (pickedIndex < 0) return;
        InvokeEnemy(enemies[pickedIndex], "Prepare"); // Enemy에 Prepare() 구현 권장
    }

    // (권장) 트리거 밟을 때 "실제 행동" 시작
    public void StartPicked()
    {
        if (pickedIndex < 0) return;
        InvokeEnemy(enemies[pickedIndex], "StartAction"); // Enemy에 StartAction() 구현 권장
    }

    // 기존: 즉시 Activate (Enemy가 Activate 즉시 움직이는 구조라면 StartAction 분리 권장)
    public void ActivatePicked()
    {
        if (pickedIndex < 0) return;
        InvokeEnemy(enemies[pickedIndex], "Activate");
    }

    /* =========================================================
     * Bulk Controls
     * ========================================================= */

    public void DeactivateAll()
    {
        if (enemies == null) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (!enemies[i]) continue;
            InvokeEnemy(enemies[i], "Deactivate");
        }

        pickedIndex = -1;
    }

    public void ResetAll()
    {
        if (enemies == null) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (!enemies[i]) continue;
            InvokeEnemy(enemies[i], "ResetEnemy");
        }

        pickedIndex = -1;
    }

    // Transition 직후 Enemy 내부 상태(타이머/경로/플래그)를 일괄 리셋
    // - Enemy에 OnTransitionReset()이 있으면 호출
    // - 없으면 조용히 스킵
    public void OnTransitionResetAll()
    {
        if (enemies == null) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e) continue;

            InvokeEnemyOptional(e, "OnTransitionReset");
        }

        // pickedIndex 유지: "이번 턴에 고른 Enemy"를 그대로 가져가고 싶을 때 유리
        // 만약 전환마다 완전 랜덤 재선정이 목적이면 pickedIndex = -1 로 초기화해도 됨
    }

    /* =========================================================
     * Common Pattern (Legacy 유지)
     * ========================================================= */

    public void StartRandomStareCommon(float duration)
    {
        if (enemies == null || enemies.Length == 0) return;

        int idx = Random.Range(0, enemies.Length);

        InvokeEnemyOptional(enemies[idx], "StartCommonStare");

        if (duration > 0f)
            InvokeEnemyOptional(enemies[idx], "StopCommonStare", delay: duration);
    }

    /* =========================================================
     * Invocation Utilities (Fixed: no double invoke)
     * ========================================================= */

    // 필수 메서드 호출: 없으면 Warning
    void InvokeEnemy(MonoBehaviour mb, string method)
    {
        if (!mb)
        {
            Debug.LogWarning($"[EnemyController] InvokeEnemy skipped. target=NULL method={method}", this);
            return;
        }

        var type = mb.GetType();
        var mi = type.GetMethod(method,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (mi == null)
        {
            Debug.LogWarning($"[EnemyController] '{method}' NOT FOUND on {type.Name} (GO={mb.gameObject.name})", mb);
            return;
        }

        mi.Invoke(mb, null);
        Debug.Log($"[EnemyController] Invoked {method} on {type.Name} (GO={mb.gameObject.name})", mb);
    }

    // 선택 메서드 호출: 없으면 스킵(조용히)
    void InvokeEnemyOptional(MonoBehaviour mb, string method, float delay = 0f)
    {
        if (!mb) return;

        // delay>0이면 Unity Invoke 사용 (메서드가 없을 수도 있으니 사전 검사)
        var type = mb.GetType();
        var mi = type.GetMethod(method,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (mi == null)
            return;

        if (delay <= 0f)
        {
            mi.Invoke(mb, null);
        }
        else
        {
            // Unity Invoke는 string 기반이지만, 우리는 이미 mi 검증을 했으니 안전하게 씀
            mb.Invoke(method, delay);
        }
    }
}
