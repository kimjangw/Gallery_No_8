using System.Collections;
using UnityEngine;

public class ActionTrigger : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loopManager;
    public EnemyController enemyController;

    //트리거시 랜덤 대기 시간 설정.
    [Header("Random Delay")]
    public float minDelay = 0.5f;
    public float maxDelay = 2.5f;

    [Header("Options")]
    public bool isEnemy = true;

    // 기본은 잠금 상태
    bool locked = true;

    // 지연 실행 코루틴 핸들(전환/잠금 시 중단용)
    Coroutine delayedActionToken;

    // 레이어 문자열 변환 비용 최소화용 캐시
    int playerLayer;

    void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    void OnTriggerEnter(Collider other)
    {
        // 잠김이면 무시
        if (locked) return;

        // Player만 통과
        if (other.gameObject.layer != playerLayer) return;

        // Enemy가 있어야만 발동하는 트리거라면, 이번 루프에 Enemy가 없으면 스킵
        if (isEnemy)
        {
            if (loopManager == null) return;
            if (loopManager.isEnemyFlag == false) return;
        }

        // 이번 루프에서 1회만 발동(밟는 순간 잠금)
        locked = true;

        // 혹시 남아있는 코루틴이 있으면 정리 후 재시작
        if (delayedActionToken != null)
        {
            StopCoroutine(delayedActionToken);
            delayedActionToken = null;
        }

        delayedActionToken = StartCoroutine(RandomDelay());
    }

    IEnumerator RandomDelay()
    {
        float delayTime = Random.Range(minDelay, maxDelay);
        yield return new WaitForSeconds(delayTime);

        // 대기 중 Transition/리셋 등으로 상태가 바뀌었으면 실행하지 않음
        // (현재 구조: 밟는 순간 locked=true이므로 정상 케이스에서는 통과)
        if (!locked) yield break;

        if (enemyController != null)
            enemyController.StartSelectedEnemy();

        delayedActionToken = null;
    }

    // LoopManager에서 호출: 이번 루프에 사용하지 않는 트리거는 잠금
    public void ActionTriggerLock()
    {
        locked = true;

        // 대기 중이던 코루틴이 있으면 중단
        if (delayedActionToken != null)
        {
            StopCoroutine(delayedActionToken);
            delayedActionToken = null;
        }
    }

    // LoopManager에서 호출: 이번 루프에 선택된 트리거만 해금
    public void ActionTriggerUnlock()
    {
        locked = false;

        // 안전상 남아있는 코루틴이 있으면 중단
        if (delayedActionToken != null)
        {
            StopCoroutine(delayedActionToken);
            delayedActionToken = null;
        }
    }
}
