using UnityEngine;
using UnityEngine.AI;

// Strong 누적(끊겨도 유지) → 누적이 chargeTime 도달하면 rushSpeed로 돌진 → 근접 시 Kill
public class Enemy3_StareKill : MonoBehaviour, EnemyPattern
{
    [Header("Refs (Inspector)")]
    public LoopManager loopManager;  // Kill 신호용
    public Transform player;         // CC 루트(인스펙터 주입)
    public EnemySensol sensor;       // PlayerSensor가 써주는 데이터 컨테이너(인스펙터 주입)

    [Header("Charge / Kill")]
    public float chargeTime = 1.0f;      // Strong 누적 목표 시간
    public float killDistance = 1.1f;    // 이 거리 이내면 Kill
    public float repathInterval = 0.15f; // 목적지 갱신 주기

    [Header("Rush Speed")]
    public float rushSpeed = 6.0f;       // 차지 완료 후 돌진 속도

    [Header("Face While Charging")]
    public float faceTurnSpeed = 6.0f;   // 차지 중 Y축 회전 응시 속도

    [Header("NavMesh Safety")]
    public float navSampleRadius = 1.5f;   // NavMesh 이탈 보정 반경
    public float spawnSampleRadius = 2.0f; // 스폰 복귀 보정 반경

    NavMeshAgent agent;

    bool actionStarted;   // StartAction 이후 true
    bool chargingDone;    // charge 완료 여부
    float chargeAccum;    // Strong 누적(끊겨도 유지)
    float repathTimer;    // 경로 갱신 타이머

    Vector3 spawnPos;        // 최초 배치 위치
    Quaternion spawnRot;     // 최초 배치 회전

    // 컴포넌트/스폰 캐싱 + 초기 정지
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        ResetInternalState();
        StopAgent();
    }

    // 루프에서 선택되었을 때: 대기 상태
    public void Ready()
    {
        ResetInternalState();
        StopAgent();
        EnsureOnNavMesh();
    }

    // ActionTrigger 신호: 차지 시작(Strong 누적을 기다림)
    public void StartAction()
    {
        ResetInternalState();
        actionStarted = true;

        if (agent == null || sensor == null || player == null)
        {
            Deactivate();
            return;
        }

        if (!EnsureOnNavMesh())
        {
            Deactivate();
            return;
        }

        StopAgent(); // 차지 완료 전에는 정지 유지
    }

    // 패턴 중지(상태만 정리)
    public void Deactivate()
    {
        ResetInternalState();
        StopAgent();
    }

    // 강제 리셋(스폰 복귀 + 상태 리셋)
    public void ResetEnemy()
    {
        ResetToSpawn();
        Deactivate();
    }

    // 트랜지션 직후(스폰 복귀 + 상태 리셋)
    public void OnTransitionReset()
    {
        ResetToSpawn();
        Deactivate();
    }

    void Update()
    {
        if (!actionStarted) return;
        if (agent == null || sensor == null || player == null) return;
        if (!EnsureOnNavMesh()) return;

        // 1) 차지 단계: Strong이면 누적 증가, Strong이 끊겨도 누적 유지
        if (!chargingDone)
        {
            // 차지 중에는 항상 응시(회전만)
            FacePlayer(faceTurnSpeed);

            if (sensor.state == EnemySensol.State.Strong)
            {
                chargeAccum += Time.deltaTime;

                if (chargeAccum >= chargeTime)
                {
                    chargingDone = true;

                    // ✅ 누적 완료 순간에 돌진 시작
                    agent.speed = rushSpeed;
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    repathTimer = repathInterval;

                    return;
                }
            }

            // Strong이 아니면: 정지 유지 + 누적값은 그대로 둠
            StopAgent();
            return;
        }

        // 2) 돌진 상태: 계속 추적
        agent.isStopped = false;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            agent.SetDestination(player.position);
            repathTimer = repathInterval;
        }

        if (Vector3.Distance(transform.position, player.position) <= killDistance)
        {
            if (loopManager != null) loopManager.OnEnemyKill();
            Deactivate();
        }
    }

    // 내부 상태 리셋(스폰 복귀는 별도)
    void ResetInternalState()
    {
        actionStarted = false;
        chargingDone = false;
        chargeAccum = 0f;
        repathTimer = 0f;
    }

    // Agent 즉시 정지(NavMesh 위에서만)
    void StopAgent()
    {
        if (agent == null || !agent.enabled) return;
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    // NavMesh 이탈 시 Warp로 예외 방지
    bool EnsureOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out var hit, navSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    // 최초 배치 위치/회전으로 복귀 + Agent 좌표 Warp 동기화
    void ResetToSpawn()
    {
        StopAgent();
        transform.SetPositionAndRotation(spawnPos, spawnRot);

        if (agent != null && agent.enabled)
        {
            if (NavMesh.SamplePosition(spawnPos, out var hit, spawnSampleRadius, NavMesh.AllAreas))
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
