using UnityEngine;

public class Enemy4_PrankaDrop : MonoBehaviour, EnemyPattern
{
    [Header("Refs (Inspector)")]
    public Transform player;      // 플레이어(인스펙터 주입)
    public Transform moveRoot;    // 실제로 이동시킬 루트(인스펙터 주입)

    [Header("Jump Scare Fly")]
    public float targetForward = 2.5f; // 플레이어 기준 앞으로 떨어질 거리
    public float startHeight = 1.2f;   // 시작 높이(위에서 내려오는 느낌)
    public float flyDuration = 0.25f;  // 날아오는 시간

    bool isFlying;
    float timer;

    // 루프당 1회만 발동 가드
    bool hasFiredThisLoop;

    // 최초 배치 위치/회전(트랜지션 시 복귀)
    Vector3 spawnPos;
    Quaternion spawnRot;

    // 1회 목표 확정
    Vector3 flyStartPos;
    Vector3 flyTargetPos;

    // 컴포넌트/스폰 캐싱
    void Awake()
    {
        if (!moveRoot) moveRoot = transform;

        spawnPos = moveRoot.position;
        spawnRot = moveRoot.rotation;

        isFlying = false;
        hasFiredThisLoop = false;
    }

    // 루프에서 선택되었을 때(대기 상태): 루프당 1회 발동 가능하도록 가드 초기화
    public void Ready()
    {
        isFlying = false;
        timer = 0f;
        hasFiredThisLoop = false;
    }

    // ActionTrigger 신호: 루프당 1회만 목표 확정 후 날아옴
    public void StartAction()
    {
        if (hasFiredThisLoop) return;
        if (player == null) return;

        hasFiredThisLoop = true;

        // 목표 1회 확정(플레이어 기준 앞으로)
        Vector3 fwd = player.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();

        flyTargetPos = player.position + fwd * targetForward;

        // 시작 위치는 목표 위쪽(startHeight)
        flyStartPos = moveRoot.position;
        flyStartPos.y = flyTargetPos.y + startHeight;

        moveRoot.position = flyStartPos;

        timer = 0f;
        isFlying = true;
    }

    // 비활성: 상태만 정지
    public void Deactivate()
    {
        isFlying = false;
        timer = 0f;
    }

    // 강제 리셋: 스폰 복귀 + 상태 정리
    public void ResetEnemy()
    {
        ResetToSpawn();
        Deactivate();
        hasFiredThisLoop = false;
    }

    // 트랜지션 직후: 스폰 복귀 + 상태 정리
    public void OnTransitionReset()
    {
        ResetToSpawn();
        Deactivate();
        hasFiredThisLoop = false;
    }

    void Update()
    {
        if (!isFlying) return;

        timer += Time.deltaTime;
        float t = (flyDuration <= 0f) ? 1f : Mathf.Clamp01(timer / flyDuration);

        moveRoot.position = Vector3.Lerp(flyStartPos, flyTargetPos, t);

        // 도착 후에는 그 자리에서 멈춰서 그대로 존재
        if (t >= 1f)
            isFlying = false;
    }

    // 최초 배치 위치/회전으로 복귀
    void ResetToSpawn()
    {
        moveRoot.SetPositionAndRotation(spawnPos, spawnRot);
    }
}
