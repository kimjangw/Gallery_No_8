using UnityEngine;
using UnityEngine.AI;

public class Enemy1_Weeping : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;
    NavMeshAgent agent;
    EnemySensor sensor;
    Transform player;

    [Header("Settings")]
    public float killTime = 10f;
    public float killDistance = 1.2f;
    public bool weakCountsAsUnseen = true;

    float t;
    bool active;

    // ===== Common Stare (공통패턴) =====
    bool commonStareActive;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<EnemySensor>();
        player = GameObject.FindWithTag("Player")?.transform;
        if (!loop) loop = FindObjectOfType<LoopManager>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        Deactivate();
    }

    public void Activate()
    {
        if (!agent || !sensor || player == null) { enabled = false; return; }

        active = true;
        t = 0f;
        agent.isStopped = false;
        enabled = true;
    }

    public void Deactivate()
    {
        active = false;
        if (agent) agent.isStopped = true;
        enabled = false;
    }

    public void ResetEnemy()
    {
        t = 0f;
        commonStareActive = false;
        if (agent) agent.isStopped = true;

        transform.SetPositionAndRotation(spawnPos, spawnRot);
    }

    // ===== 공통패턴(랜덤 1개가 나를 쳐다봄) 지원 =====
    public void StartCommonStare()
    {
        commonStareActive = true;
        enabled = true;
    }

    public void StopCommonStare()
    {
        commonStareActive = false;
    }

    void Update()
    {
        // 공통 Stare 우선권: 회전만 수행하고 자기 패턴은 멈춤
        if (commonStareActive)
        {
            FacePlayer(6f);
            return;
        }

        if (!active) return;

        var st = sensor.state;

        if (st == EnemySensor.State.Strong)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);

        bool unseen = (st == EnemySensor.State.Blind) || (weakCountsAsUnseen && st == EnemySensor.State.Weak);
        if (unseen) t += Time.deltaTime;

        if (t >= killTime || Vector3.Distance(transform.position, player.position) <= killDistance)
        {
            DoKill();
        }
    }

    void FacePlayer(float speed)
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * speed);
    }

    void DoKill()
    {
        loop?.OnEnemyKill();
        Deactivate();
    }
}
