using System.Collections;
using UnityEngine;

public class ActionTrigger : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loopManager;
    public EnemyController enemyController;

    //랜덤 딜레이 설정
    [Header("Random Delay")]
    public float minDelay = 0.5f;
    public float maxDelay = 2.5f;

    [Header("Options")]
    public bool requireMonster = true;

    // ===== flag 잠금 =====
    bool locked;
    Coroutine co;

    // Player Layer Cache
    int playerLayer;

    void Awake()
    {
        // 레이어는 문자열 변환 비용이 있으니 한 번만
        playerLayer = LayerMask.NameToLayer("Player");
    }

    void OnTriggerEnter(Collider other)
    {
        // 잠겨있으면 무시
        if (locked) return;

        // Player 체크
        if (other.gameObject.layer != playerLayer) return;

        // Monster(Enemy) 필요한 트리거라면: 현재 루프에 Enemy가 없으면 실행하지 않음
        // (LoopManager 최신 기준: isEnemyFlag 사용)
        if (requireMonster)
        {
            if (loopManager == null) return;
            if (loopManager.isEnemyFlag == false) return;
        }

        // 통과하면 이 트리거는 잠금
        locked = true;

        // 기존 코루틴 정리 후 새로 시작
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

        // Transition에서 잠금이 풀렸으면(즉, 구간이 바뀌었으면) 실행하지 않음
        if (!locked) yield break;

        if (enemyController != null)
        {
            Debug.Log("[ActionTrigger] DoAction on " + enemyController.name, enemyController);
            enemyController.DoAction();
        }

        // 여기서 locked를 풀지 않는 이유:
        // - 한 구간에서 1회만 발동하는 트리거로 사용
        // - 다음 구간(Transition)에서 UnlockForNextSection()으로 다시 열어줌
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

    // LoopManager에서 호출: 이번 루프에서 사용하지 않는 트리거는 잠가둠
    public void LockForThisLoop()
    {
        locked = true;

        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }
}
