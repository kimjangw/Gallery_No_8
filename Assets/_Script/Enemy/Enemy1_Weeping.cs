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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<EnemySensol>();
        if (!loop) loop = FindObjectOfType<LoopManager>();

        // ✅ Player는 CharacterController 루트로 고정 (본 잡는 문제 제거)
        var cc = Object.FindFirstObjectByType<CharacterController>();
        player = cc ? cc.transform : null;

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

        // 시작은 일단 멈춤(Strong이면 계속 멈춤, Blind면 다음 Update에서 움직임)
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
        active = false;
        t = 0f;
        repathTimer = 0f;
        StopMove();
        Dump("Reset");
    }

    void Update()
    {
        // 디버그 주기
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

        // ✅ Strong일 때만 멈춤 (요구사항)
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

        string st = sensor ? sensor.state.ToString() : "NULL";
        string p = player ? player.name : "NULL";

        string nav =
            agent
            ? $"enabled={agent.enabled} onNavMesh={agent.isOnNavMesh} isStopped={agent.isStopped} speed={agent.speed} hasPath={agent.hasPath} pending={agent.pathPending} status={agent.pathStatus}"
            : "agent=NULL";

        //Debug.Log($"[Enemy1DBG] {name} reason={reason} active={active} state={st} player={p} | {nav}", this);
    }
}
