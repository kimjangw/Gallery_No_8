using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public MonoBehaviour[] enemies;

    int pickedIndex = -1;
    public bool HasPicked => pickedIndex >= 0;

    // [ADD] 기존 LoopManager 호환용
    public void ActivateOne()
    {
        Debug.Log($"[EnemyController] ActivateOne on {name}", this);
        print("ActiveOne");
        PickOne();
        ActivatePicked();
    }

    // [ADD] 트리거 방식 준비용 (선정만)
    public void PrepareOne()
    {
        PickOne();
        print("선택");
    }

    public void PickOne()
    {
        if (enemies == null || enemies.Length == 0) return;

        DeactivateAll();
        pickedIndex = Random.Range(0, enemies.Length);
        print("현재 패턴" + pickedIndex);
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
    }


    // =====================================================================
    // [ADD] 공통패턴: 랜덤 Enemy가 나를 쳐다봄
    // - duration초 동안만 실행
    // - Enemy 스크립트에 StartCommonStare / StopCommonStare 메서드가 있어야 함
    // =====================================================================
    public void StartRandomStareCommon(float duration)
    {
        if (enemies == null || enemies.Length == 0) return;

        int idx = Random.Range(0, enemies.Length);

        // 공통패턴 시작
        enemies[idx].Invoke("StartCommonStare", 0f);

        // duration 후 종료
        if (duration > 0f)
            enemies[idx].Invoke("StopCommonStare", duration);
    }
}
