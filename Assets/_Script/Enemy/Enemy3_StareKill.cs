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
    public float chargeTime = 1.0f;       // ✅ Strong 연속 유지 시간
    public float killDistance = 1.1f;
    public float repathInterval = 0.15f;

    bool active;
    bool chargingDone;   // chargeTime 달성 후 돌진 상태
    float chargeAccum;
    float repathTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<EnemySensol>();
        if (!loop) loop = FindObjectOfType<LoopManager>();

        // Player는 CharacterController 루트로 고정
        var cc = Object.FindFirstObjectByType<CharacterController>();
        player = cc ? cc.transform : null;

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

        // 처음엔 멈춤(Strong로 charge 쌓일 때까지 대기)
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
        active = false;
        chargingDone = false;
        chargeAccum = 0f;
        repathTimer = 0f;
        StopMove();
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

                    // ✅ 달성 순간 즉시 돌진 시작
                    ResumeMove();
                    agent.SetDestination(player.position);
                    repathTimer = repathInterval;
                }
                else
                {
                    // Strong 유지 중에는 계속 대기(정지)
                    StopMove();
                }
            }
            else
            {
                // Strong이 끊기면 charge 리셋(“연속” 조건)
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
