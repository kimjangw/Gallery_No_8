using System.Collections;
using UnityEngine;

public class ActionTrigger: MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loopManager;           // 씬의 LoopManager 연결
    public EnemyController enemyController;   // Enemy 빈부모의 EnemyController 연결

    [Header("Random Delay (No Pattern)")]
    public float minDelay = 0.5f;
    public float maxDelay = 2.5f;

    [Header("Options")]
    public bool oneShotPerLoop = true;  // 루프당 1회만 발동
    public bool requireMonster = true; // 이번 루프에 몬스터 없으면 무시

    bool fired;

    void Awake()
    {
        // 인스펙터 연결 권장. 비워두면 자동 탐색.
        if (!loopManager) loopManager = FindObjectOfType<LoopManager>();
        if (!enemyController) enemyController = FindObjectOfType<EnemyController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (oneShotPerLoop && fired) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        if (requireMonster && loopManager != null && loopManager.hasMonster == false)
            return;

        fired = true;

        StopAllCoroutines(); // [ADD] 중복 코루틴 방지
        StartCoroutine(CoRandomDelayThenActivate());
    }

    IEnumerator CoRandomDelayThenActivate()
    {
        float d = Random.Range(minDelay, maxDelay);
        yield return new WaitForSeconds(d);

        Debug.Log($"[ActionTrigger] calling ActivateOne on {enemyController.name}", enemyController);
        // Enemy 쪽은 기존대로: 랜덤 Enemy 선택 + 즉시 활성
        enemyController?.ActivateOne();
    }

    // 루프 리셋/층 변경 시 다시 트리거 사용 가능하게 하고 싶으면 호출
    public void ResetTrigger()
    {
        fired = false;
        StopAllCoroutines();
    }
}
