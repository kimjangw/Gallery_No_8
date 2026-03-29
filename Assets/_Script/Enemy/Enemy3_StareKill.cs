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
    public float killDistance = 2.5f;    // 이 거리 이내면 Kill
    public float repathInterval = 0.15f; // 목적지 갱신 주기

    [Header("Rush Speed")]
    public float rushSpeed = 6.0f;       // 차지 완료 후 돌진 속도

    [Header("Face While Charging")]
    public float faceTurnSpeed = 6.0f;   // 차지 중 Y축 회전 응시 속도

    [Header("NavMesh Safety")]
    public float navSampleRadius = 1.5f;   // NavMesh 이탈 보정 반경
    public float spawnSampleRadius = 2.0f; // 스폰 복귀 보정 반경

    [Header("Sound Settings")]
    public float moveSoundInterval = 0.1f; // 돌진하므로 소리 간격을 짧게!
    public float minPitch = 0.8f;
    public float maxPitch = 1.0f;
    private float moveSoundTimer;

    [Header("Debug")]
    public bool debugLog = true;

    NavMeshAgent agent;

    bool actionStarted;   // StartAction 이후 true
    bool chargingDone;    // charge 완료 여부
    float chargeAccum;    // Strong 누적(끊겨도 유지)
    float repathTimer;    // 경로 갱신 타이머

    Vector3 spawnPos;        // 최초 배치 위치
    Quaternion spawnRot;     // 최초 배치 회전

    // Kill 중복 방지(연동 체크 시 중요)
    bool killSent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        ResetInternalState();
        StopAgent();

        if (debugLog)
        {
            Debug.Log(
                "[Enemy3] Awake\n" +
                " - agent=" + (agent != null) + "\n" +
                " - loopManager=" + (loopManager != null) + "\n" +
                " - player=" + (player != null) + "\n" +
                " - sensor=" + (sensor != null)
            );
        }
    }

    // 루프에서 선택되었을 때: 대기 상태
    public void Ready()
    {
        ResetInternalState();
        StopAgent();
        EnsureOnNavMesh();

        if (debugLog) Debug.Log("[Enemy3] Ready()");
    }

    // ActionTrigger 신호: 차지 시작(Strong 누적을 기다림)
    public void StartAction()
    {
        ResetInternalState();
        actionStarted = true;

        if (debugLog) Debug.Log("[Enemy3] StartAction() called");

        if (agent == null || sensor == null || player == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(
                    "[Enemy3] StartAction() FAIL: missing refs\n" +
                    " - agent=" + (agent != null) +
                    " sensor=" + (sensor != null) +
                    " player=" + (player != null)
                );
            }
            Deactivate();
            return;
        }

        if (!EnsureOnNavMesh())
        {
            if (debugLog) Debug.LogWarning("[Enemy3] StartAction() FAIL: agent not on NavMesh");
            Deactivate();
            return;
        }

        StopAgent(); // 차지 완료 전에는 정지 유지
    }

    // 패턴 중지(상태만 정리)
    public void Deactivate()
    {
        if (debugLog) Debug.Log("[Enemy3] Deactivate()");
        ResetInternalState();
        StopAgent();
    }

    // 강제 리셋(스폰 복귀 + 상태 리셋)
    public void ResetEnemy()
    {
        if (debugLog) Debug.Log("[Enemy3] ResetEnemy()");
        ResetToSpawn();
        Deactivate();
    }

    // 트랜지션 직후(스폰 복귀 + 상태 리셋)
    public void OnTransitionReset()
    {
        if (debugLog) Debug.Log("[Enemy3] OnTransitionReset()");
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
            FacePlayer(faceTurnSpeed);

            if (sensor.state == EnemySensol.State.Strong)
            {
                chargeAccum += Time.deltaTime;

                if (debugLog)
                {
                    Debug.Log("[Enemy3] Charging... accum=" + chargeAccum.ToString("F2") +
                              " / " + chargeTime.ToString("F2"));
                }

                if (chargeAccum >= chargeTime)
                {
                    chargingDone = true;

                    if (debugLog) Debug.Log("[Enemy3] Charge DONE → Rush start");

                    agent.speed = rushSpeed;
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    repathTimer = repathInterval;
                    return;
                }
            }

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

        float dist = Vector3.Distance(transform.position, player.position);

        if (debugLog)
        {
            Debug.Log("[Enemy3] Rushing... dist=" + dist.ToString("F2") +
                      " killDistance=" + killDistance.ToString("F2"));
        }

        if (dist <= killDistance)
        {
            SendKillOnce("distance");
            Deactivate();
        }
        if (agent != null && !agent.isStopped && agent.velocity.sqrMagnitude > 0.1f)
        {
            moveSoundTimer -= Time.deltaTime;
            if (moveSoundTimer <= 0f)
            {
                float randomPitch = Random.Range(minPitch, maxPitch);
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.Play3D("ston_Move", transform.position, 1f, randomPitch);
                }
                moveSoundTimer = moveSoundInterval;
            }
        }
        else
        {
            moveSoundTimer = 0f;
        }
    }

    void SendKillOnce(string reason)
    {
        if (killSent) return;
        killSent = true;

        if (debugLog) Debug.Log("[Enemy3] KILL TRIGGERED (" + reason + ") → loopManager.OnEnemyKill()");

        if (loopManager != null)
        {
            loopManager.OnEnemyKill();
        }
        else
        {
            if (debugLog) Debug.LogWarning("[Enemy3] loopManager is NULL. Kill not delivered.");
        }
    }

    void ResetInternalState()
    {
        actionStarted = false;
        chargingDone = false;
        chargeAccum = 0f;
        repathTimer = 0f;
        killSent = false;
    }

    void StopAgent()
    {
        if (agent == null || !agent.enabled) return;
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

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

    void FacePlayer(float turnSpeed)
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed);
    }
}
