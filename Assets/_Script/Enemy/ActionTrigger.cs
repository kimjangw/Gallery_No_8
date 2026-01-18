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

    bool locked = true;      // LoopManager가 PickActionTrigger()로 열어주기 전엔 잠겨있다고 가정
    Coroutine co;
    int playerLayer;

    void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    void OnTriggerEnter(Collider other)
    {
        if (locked) return;
        if (other.gameObject.layer != playerLayer) return;

        // Enemy가 있어야만 작동하는 트리거라면: 이번 루프에 Enemy 없으면 실행 안 함
        if (requireMonster)
        {
            if (loopManager == null) return;
            if (loopManager.isEnemyFlag == false) return;
        }

        // 한 번 밟으면 이 트리거는 잠금(이번 루프 1회 발동)
        locked = true;

        // 기존 코루틴이 있으면 정리(안전)
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }

        co = StartCoroutine(CoDelayThenAction());
    }

    IEnumerator CoDelayThenAction()
    {
        float d = Random.Range(minDelay, maxDelay);
        yield return new WaitForSeconds(d);

        // Transition 등으로 Unlock/Lock 상태가 바뀌었으면 실행하지 않음
        if (!locked) yield break;

        if (enemyController != null)
            enemyController.DoAction();

        co = null;
    }

    // LoopManager에서 호출: 이번 루프에 사용하지 않는 트리거는 잠가둠
    public void Lock()
    {
        locked = true;

        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }

    // LoopManager에서 호출: 이번 루프에 선택된 트리거만 열어줌
    public void Unlock()
    {
        locked = false;

        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }
}
