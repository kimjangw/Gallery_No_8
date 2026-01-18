using UnityEngine;
using UnityEngine.AI;

public class Enemy2_TeleportStalker : MonoBehaviour, EnemyPattern
{
    [Header("Refs (Inspector)")]
    public LoopManager loopManager;
    public Transform player;
    public EnemySensol sensor;

    [Header("Settings")]
    public float behindDistance = 2.2f;   // 기준 거리(100%)
    public float sampleRadius = 3.0f;     // NavMesh 샘플 반경
    public bool killAfterThirdTeleport = true;

    [Header("Teleport Steps (percent of behindDistance)")]
    [Range(0f, 1f)] public float farPercent = 1.00f;   
    [Range(0f, 1f)] public float midPercent = 0.50f;   
    [Range(0f, 1f)] public float nearPercent = 0.20f;  

    [Header("Face")]
    public float faceTurnSpeed = 6.0f;

    NavMeshAgent agent;

    bool active;
    int teleportStep; // 0,1,2
    EnemySensol.State prevState;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        // 초기값
        active = false;
        teleportStep = 0;
        prevState = EnemySensol.State.Blind;

        StopAgentHard();
        // enabled는 끄지 않음(씬에서 지속 사용)
    }

    // -----------------------------
    // EnemyPattern
    // -----------------------------

    // 루프에서 선택되었을 때: 대기 상태(즉시 행동 X)
    public void Ready()
    {
        active = false;
        teleportStep = 0;
        prevState = (sensor != null) ? sensor.state : EnemySensol.State.Blind;

        StopAgentHard();
        ResetToSpawn();
    }

    // ActionTrigger 신호: 패턴 활성화
    public void StartAction()
    {
        if (player == null || sensor == null)
        {
            active = false;
            return;
        }

        active = true;
        teleportStep = 0;
        prevState = sensor.state;

        StopAgentHard();
    }

    // 패턴 중지(상태만 정리)
    public void Deactivate()
    {
        active = false;
        teleportStep = 0;

        StopAgentHard();
        // enabled는 끄지 않음
    }

    // 강제 리셋(스폰 복귀 + 비활성)
    public void ResetEnemy()
    {
        ResetToSpawn();
        Deactivate();
    }

    // 트랜지션 직후(스폰 복귀 + 비활성)
    public void OnTransitionReset()
    {
        ResetEnemy(); // 완전 동일 동작이므로 공용 처리
    }

    void Update()
    {
        if (!active) return;
        if (player == null || sensor == null) return;

        EnemySensol.State currentState = sensor.state;

        // B안 핵심: Weak/Strong -> Blind "전환 순간"에만 1회 워프
        bool becameBlindThisFrame = (prevState != EnemySensol.State.Blind && currentState == EnemySensol.State.Blind);

        if (becameBlindThisFrame)
        {
            DoTeleportStep();

            // 3번째(teleportStep==2) 이후 kill 조건
            if (killAfterThirdTeleport && teleportStep >= 3)
            {
                DoKill();
                return;
            }
        }
        else
        {
            // 워프가 없을 땐 기본 연출: 플레이어 바라보기 정도만
            FacePlayer(faceTurnSpeed);
        }

        prevState = currentState;
    }

    // -----------------------------
    // Teleport Step Logic
    // -----------------------------

    void DoTeleportStep()
    {
        float distance = GetStepDistance(teleportStep);

        Vector3 desired = player.position - player.forward * distance;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.Warp(hit.position);
            else
                transform.position = hit.position;

            // 워프 직후는 즉시 바라보게(점프스퀘어 연출)
            FacePlayer(999f);
        }
        else
        {
            // 샘플 실패 시: 그냥 Transform 이동(최후 안전장치)
            transform.position = desired;
            FacePlayer(999f);
        }

        teleportStep += 1;

        // 3번째 워프 직후 Kill을 원하면 여기서 처리해도 됨
        if (killAfterThirdTeleport && teleportStep >= 3)
        {
            DoKill();
        }
    }

    float GetStepDistance(int step)
    {
        if (step == 0) return behindDistance * farPercent; // 1st: 100
        if (step == 1) return behindDistance * midPercent; // 2nd: 70
        return behindDistance * nearPercent;               // 3rd: 30
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    void DoKill()
    {
        if (loopManager != null) loopManager.OnEnemyKill();
        Deactivate();
    }

    void FacePlayer(float speed)
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (speed >= 900f) transform.rotation = target;
        else transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * speed);
    }

    void StopAgentHard()
    {
        if (agent == null || !agent.enabled) return;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    void ResetToSpawn()
    {
        StopAgentHard();
        transform.SetPositionAndRotation(spawnPos, spawnRot);

        if (agent != null && agent.enabled)
        {
            if (NavMesh.SamplePosition(spawnPos, out var hit, 2.0f, NavMesh.AllAreas))
                agent.Warp(hit.position);

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
    }
}
