using UnityEngine;
using UnityEngine.AI;

public class Enemy3_StareKill : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;
    NavMeshAgent agent;
    EnemySensol sensor;
    Transform player;

    [Header("Settings")]
    public float chargeTime = 1.0f;       // Strong 연속 유지 시간
    public float killDistance = 1.1f;
    public float repathInterval = 0.15f;

    bool active;
    bool chargingDone;
    float chargeAccum;
    float repathTimer;

    // ✅ Spawn
    Vector3 spawnPos;
    Quaternion spawnRot;
    bool hasSpawn;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<EnemySensol>();
        if (!loop) loop = FindObjectOfType<LoopManager>();

        // Player는 CharacterController 루트로 고정
        var cc = Object.FindFirstObjectByType<CharacterController>();
        player = cc ? cc.transform : null;

        // ✅ 최초 위치 저장
        spawnPos = transform.position;
        spawnRot = transform.rotation;
        hasSpawn = true;

        active = false;
        chargingDone = false;
        StopMove();
    }

    public void Activate()
    {
        if (!player)
        {
            var cc = Object.FindFirstObjectByType<CharacterController>();
            player = cc ? cc.transform : null;
        }

        if (!agent || !sensor || !player)
        {
            active = false;
            chargingDone = false;
            StopMove();
            return;
        }

        EnsureOnNavMesh();

        active = true;
        chargingDone = false;
        chargeAccum = 0f;
        repathTimer = 0f;

        StopMove();
    }

    public void Deactivate()
    {
        active = false;
        chargingDone = false;
        StopMove();
    }

    public void ResetEnemy()
    {
        InternalFullReset(toSpawn: true);
    }

    // ✅ Transition 직후 호출(EnemyController.OnTransitionResetAll)
    public void OnTransitionReset()
    {
        InternalFullReset(toSpawn: true);
    }

    void Update()
    {
        if (!active) return;
        if (!agent || !sensor || !player) return;
        if (!EnsureOnNavMesh()) return;

        // 1) charge가 아직 안 끝났으면 Strong 연속 유지로만 누적
        if (!chargingDone)
        {
            if (sensor.state == EnemySensol.State.Strong)
            {
                chargeAccum += Time.deltaTime;

                if (chargeAccum >= chargeTime)
                {
                    chargingDone = true;

                    // 달성 순간 즉시 돌진 시작
                    ResumeMove();
                    agent.SetDestination(player.position);
                    repathTimer = repathInterval;
                }
                else
                {
                    StopMove();
                }
            }
            else
            {
                // Strong이 끊기면 charge 리셋(연속 조건)
                chargeAccum = 0f;
                StopMove();
            }

            return;
        }

        // 2) 돌진 상태: Strong/Weak/Blind 무시하고 계속 추적
        ResumeMove();

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            agent.SetDestination(player.position);
            repathTimer = repathInterval;
        }

        if (Vector3.Distance(transform.position, player.position) <= killDistance)
        {
            loop?.OnEnemyKill();
            Deactivate();
        }
    }

    // =========================
    // Reset / Spawn
    // =========================

    void InternalFullReset(bool toSpawn)
    {
        active = false;
        chargingDone = false;
        chargeAccum = 0f;
        repathTimer = 0f;

        StopMoveHard();

        if (toSpawn)
            ResetToSpawn();
    }

    void ResetToSpawn()
    {
        if (!hasSpawn) return;

        // 1) Transform 복원
        transform.SetPositionAndRotation(spawnPos, spawnRot);

        // 2) NavMesh 위로 스냅 + Warp 동기화
        if (agent && agent.enabled)
        {
            if (NavMesh.SamplePosition(spawnPos, out var hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
    }

    // =========================
    // NavMesh Move Control
    // =========================

    void StopMove()
    {
        if (!agent || !agent.enabled) return;
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    void StopMoveHard()
    {
        if (!agent || !agent.enabled) return;

        // NavMesh 위면 확실히 정리
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        // NavMesh 밖이면 ResetToSpawn에서 SamplePosition/Warp로 회복
    }

    void ResumeMove()
    {
        if (!agent || !agent.enabled) return;
        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;
    }

    bool EnsureOnNavMesh()
    {
        if (!agent || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out var hit, 1.5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }
        return false;
    }
}
