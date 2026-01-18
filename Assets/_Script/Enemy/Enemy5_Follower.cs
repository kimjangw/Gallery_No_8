using UnityEngine;
using UnityEngine.AI;

public class Enemy5_Follower : MonoBehaviour, EnemyPattern
{
    [Header("Refs (Inspector)")]
    public LoopManager loopManager; // 루프 종료 신호용
    public Transform player;        // 추적 대상(인스펙터 주입)

    [Header("Follow")]
    public float keepDistance = 3.0f; // 이 거리까지 접근하면 정지/응시
    public float followSpeed = 2.8f;  // 이동 속도

    [Header("Stare (Option A)")]
    public float stareDuration = 2.0f;  // 응시 지속 시간
    public float stareTurnSpeed = 6.0f; // 응시 회전 속도

    [Header("NavMesh Safety")]
    public float sampleRadius = 1.5f; // NavMesh 이탈 시 보정 반경

    NavMeshAgent agent;

    bool actionStarted; // StartAction 이후 true
    bool isStaring;     // 근접 후 응시 상태
    float stareTimer;   // 응시 타이머

    Vector3 spawnPos;    // 최초 배치 위치
    Quaternion spawnRot; // 최초 배치 회전

    // 컴포넌트/스폰 정보 캐싱 + 초기 정지 상태 세팅
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        Deactivate(); // 스크립트는 꺼지지 않음(내부 상태만 정리)
    }

    // 루프에서 선택되었을 때: 등장/대기 상태 세팅
    public void Ready()
    {
        actionStarted = false;
        isStaring = false;
        stareTimer = 0f;

        if (agent != null)
        {
            agent.speed = followSpeed;
            EnsureOnNavMesh();
            StopAgent();
        }
    }

    // ActionTrigger 신호 시 행동 시작
    public void StartAction()
    {
        actionStarted = true;
        isStaring = false;
        stareTimer = 0f;

        if (agent == null || player == null)
        {
            Deactivate();
            return;
        }

        agent.speed = followSpeed;

        if (!EnsureOnNavMesh())
        {
            Deactivate();
            return;
        }

        agent.isStopped = false;
    }

    // 완전 비활성: 이동/상태만 정리 (스크립트는 끄지 않음)
    public void Deactivate()
    {
        actionStarted = false;
        isStaring = false;
        stareTimer = 0f;

        StopAgent();
    }

    // 강제 리셋: 스폰 복귀 후 비활성
    public void ResetEnemy()
    {
        ResetToSpawn();
        Deactivate();
    }

    // 트랜지션 직후: 스폰 복귀 후 비활성
    public void OnTransitionReset()
    {
        ResetToSpawn();
        Deactivate();
    }

    void Update()
    {
        if (!actionStarted) return;
        if (agent == null || player == null) return;
        if (!EnsureOnNavMesh()) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 근접하면 정지 + 응시 상태 진입
        if (!isStaring && dist <= keepDistance)
        {
            StopAgent();
            isStaring = true;
            stareTimer = 0f;
        }

        // 응시(회전) + 종료 처리
        if (isStaring)
        {
            FacePlayer(stareTurnSpeed);

            stareTimer += Time.deltaTime;
            if (stareTimer >= stareDuration)
            {
                if (loopManager != null) loopManager.OnEnemyEnd();
                Deactivate();
            }
            return;
        }

        // 플레이어에게 너무 붙지 않도록 keepDistance 지점으로 이동
        Vector3 toMe = transform.position - player.position;
        toMe.y = 0f;

        if (toMe.sqrMagnitude < 0.001f)
            toMe = -player.forward;

        toMe.Normalize();

        Vector3 target = player.position + toMe * keepDistance;
        agent.SetDestination(target);
    }

    // Agent 즉시 정지( NavMesh 위에서만 )
    void StopAgent()
    {
        if (agent == null || !agent.enabled) return;
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    // NavMesh 이탈 시 샘플 지점으로 Warp하여 예외/튕김 방지
    bool EnsureOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out var hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    // 최초 배치 위치/회전으로 복귀 + Agent 좌표까지 Warp로 동기화
    void ResetToSpawn()
    {
        StopAgent();
        transform.SetPositionAndRotation(spawnPos, spawnRot);

        if (agent != null && agent.enabled)
        {
            if (NavMesh.SamplePosition(spawnPos, out var hit, sampleRadius, NavMesh.AllAreas))
                agent.Warp(hit.position);

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
    }

    // 플레이어를 바라보도록 Y축 회전만 부드럽게 보간
    void FacePlayer(float turnSpeed)
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed);
    }
}
