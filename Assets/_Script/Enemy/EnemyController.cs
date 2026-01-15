using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public MonoBehaviour[] enemies;

    int pickedIndex = -1;
    public bool HasPicked => pickedIndex >= 0;

    // 기존 LoopManager 호환용 (그대로 유지)
    public void ActivateOne()
    {
        Debug.Log($"[EnemyController] ActivateOne on {name}", this);
        PickOne();
        ActivatePicked();
    }

    // [ADD] ActionTrigger에서 의미 중립적으로 호출할 별칭
    public void DoAction()
    {
        ActivateOne();
    }

    // (기존 유지) 트리거 방식 준비용 (선정만)
    public void PrepareOne()
    {
        PickOne();
    }

    public void PickOne()
    {
        if (enemies == null || enemies.Length == 0) return;

        DeactivateAll();
        pickedIndex = Random.Range(0, enemies.Length);
        Debug.Log($"[EnemyController] pickedIndex={pickedIndex}", this);
    }

    public void ActivatePicked()
    {
        if (pickedIndex < 0) return;
        InvokeEnemy(enemies[pickedIndex], "Activate");
    }

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

    void InvokeEnemy(MonoBehaviour mb, string method)
    {
        if (!mb)
        {
            Debug.LogWarning($"[EnemyController] InvokeEnemy skipped. target=NULL method={method}", this);
            return;
        }
        mb.Invoke(method, 0f);

        // ✅ 추가: 해당 컴포넌트에 메서드가 실제 있는지 검사
        var mi = mb.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (mi == null)
        {
            Debug.LogWarning($"[EnemyController] '{method}' NOT FOUND on {mb.GetType().Name} (GO={mb.gameObject.name})", mb);
            return;
        }

        mb.Invoke(method, 0f);
        Debug.Log($"[EnemyController] Invoked {method} on {mb.GetType().Name} (GO={mb.gameObject.name})", mb);

    }

    // (기존 유지) 공통패턴
    public void StartRandomStareCommon(float duration)
    {
        if (enemies == null || enemies.Length == 0) return;

        int idx = Random.Range(0, enemies.Length);

        enemies[idx].Invoke("StartCommonStare", 0f);

        if (duration > 0f)
            enemies[idx].Invoke("StopCommonStare", duration);
    }
}
