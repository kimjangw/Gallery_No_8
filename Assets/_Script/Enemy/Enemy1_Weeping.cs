using UnityEngine;
using UnityEngine.AI;

public class Enemy1_Weeping : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;
    NavMeshAgent agent;
    EnemySensol sensor;
    Transform player;

    [Header("Settings")]
    public float killTime = 10f;
    public float killDistance = 1.2f;
    public float repathInterval = 0.2f;

    bool active;
    float t;
    float repathTimer;

    [Header("Debug")]
    public bool debug = true;
    public float debugInterval = 0.5f;
    float dbgTimer;

    // ✅ 최초 위치/회전 저장
    Vector3 spawnPos;
    Quaternion spawnRot;
    bool hasSpawn;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<EnemySensol>();
        if (!loop) loop = FindObjectOfType<LoopManager>();

        // ✅ Player는 CharacterController 루트로 고정
        var cc = Object.FindFirstObjectByType<CharacterController>();
        player = cc ? cc.transform : null;

        // ✅ 스폰 위치 저장(씬에 배치된 최초 위치)
        spawnPos = transform.position;
        spawnRot = transform.rotation;
        hasSpawn = true;

        active = false;
        StopMove();
    }

    public void Activate()
    {
        // 혹시 Start/Awake 순서로 못 잡았으면 재획득
        if (!player)
        {
            var cc = Object.FindFirstObjectByType<CharacterController>();
            player = cc ? cc.transform : null;
        }

        active = (agent && sensor && player);
        if (!active)
        {
            Dump("ActivateFailed");
            StopMove();
            return;
        }

        EnsureOnNavMesh();

        t = 0f;
        repathTimer = 0f;

        // 시작은 일단 멈춤(Strong이면 계속 멈춤, Weak/Blind면 Update에서 이동)
        StopMove();

        Dump("ActivateOK");
    }

    public void Deactivate()
    {
        active = false;
        StopMove();
        Dump("Deactivate");
    }

    public void ResetEnemy()
    {
        // ✅ 완전 초기화: 최초 위치까지 되돌림
        active = false;
        t = 0f;
        repathTimer = 0f;

        ResetToSpawn();

        Dump("Reset");
    }

    // ✅ 전환 직후 호출되는 리셋 훅(EnemyController.OnTransitionResetAll에서 호출)
    public void OnTransitionReset()
    {
        active = false;
        t = 0f;
        repathTimer = 0f;

        ResetToSpawn();

        Dump("OnTransitionReset");
    }

    void Update()
    {
        dbgTimer -= Time.deltaTime;

        if (!active)
        {
            if (dbgTimer <= 0f) { Dump("Return:!active"); dbgTimer = debugInterval; }
            return;
        }

        if (!agent || !sensor || !player)
        {
            if (dbgTimer <= 0f) { Dump("Return:missingRef"); dbgTimer = debugInterval; }
            return;
        }

        if (!EnsureOnNavMesh())
        {
            if (dbgTimer <= 0f) { Dump("Return:!onNavMesh"); dbgTimer = debugInterval; }
            return;
        }

        // ✅ Strong일 때만 멈춤
        if (sensor.state == EnemySensol.State.Strong)
        {
            StopMove();
            if (dbgTimer <= 0f) { Dump("Stop:Strong"); dbgTimer = debugInterval; }
            return;
        }

        // ✅ Weak/Blind 이동
        ResumeMove();

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            bool ok = agent.SetDestination(player.position);
            repathTimer = repathInterval;

            if (dbgTimer <= 0f)
            {
                Dump(ok ? "SetDest:OK" : "SetDest:FAILED");
                dbgTimer = debugInterval;
            }
        }

        t += Time.deltaTime;

        if (Vector3.Distance(transform.position, player.position) <= killDistance || t >= killTime)
        {
            loop?.OnEnemyKill();
            Deactivate();
        }
    }

    // ========================================================================
    // Spawn Reset
    // ========================================================================

    void ResetToSpawn()
    {
        if (!hasSpawn) return;

        // 1) NavMeshAgent 안전 정지/경로 제거
        if (agent && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        // 2) Transform 복원
        transform.SetPositionAndRotation(spawnPos, spawnRot);

        // 3) Agent 위치 동기화(가능하면 Warp)
        if (agent && agent.enabled)
        {
            // Warp는 NavMesh 위에서 가장 안정적. 스폰이 NavMesh 밖일 수도 있으니 샘플 후 Warp.
            if (NavMesh.SamplePosition(spawnPos, out var hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                // NavMesh 샘플 실패하면 Transform만 복원된 상태로 두고,
                // 다음 Activate 시 EnsureOnNavMesh()가 보정하도록 둠.
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        // 센서 상태가 전환 후 남아 문제가 된다면, 여기서 별도 리셋을 추가할 수 있음
        // 예: sensor?.ResetState(); (해당 메서드가 있을 경우)
    }

    // ========================================================================
    // Movement helpers
    // ========================================================================

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

    void Dump(string reason)
    {
        if (!debug) return;

        // 필요 시 로그 활성화
        // string st = sensor ? sensor.state.ToString() : "NULL";
        // string p = player ? player.name : "NULL";
        // string nav =
        //     agent
        //     ? $"enabled={agent.enabled} onNavMesh={agent.isOnNavMesh} isStopped={agent.isStopped} speed={agent.speed} hasPath={agent.hasPath} pending={agent.pathPending} status={agent.pathStatus}"
        //     : "agent=NULL";
        // Debug.Log($"[Enemy1DBG] {name} reason={reason} active={active} state={st} player={p} | {nav}", this);
    }
}