using System.Collections;
using UnityEngine;

public class ActionTrigger : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loopManager;
    public EnemyController enemyController;

    [Header("Random Delay")]
    public float minDelay = 0.5f;
    public float maxDelay = 2.5f;

    [Header("Options")]
    public bool requireMonster = true;

    // ===== flag 잠금 =====
    bool locked;
    Coroutine co;

    void Awake()
    {
        if (!loopManager) loopManager = FindObjectOfType<LoopManager>();
        if (!enemyController) enemyController = FindObjectOfType<EnemyController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (locked) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        if (requireMonster && loopManager != null && loopManager.hasMonster == false)
            return;

        locked = true;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoDelayThenAction());
    }

    IEnumerator CoDelayThenAction()
    {
        float d = Random.Range(minDelay, maxDelay);
        yield return new WaitForSeconds(d);

        // Transition에서 잠금이 풀렸으면(즉, 구간이 바뀌었으면) 실행하지 않음
        if (!locked) yield break;

        Debug.Log($"[ActionTrigger] DoAction on {enemyController.name}", enemyController);
        enemyController?.DoAction();
    }

    // TransitionController에서 호출: 구간 전환 시 잠금 해제 + 대기 코루틴 제거
    public void UnlockForNextSection()
    {
        locked = false;

        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }
}
