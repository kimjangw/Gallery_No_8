using UnityEngine;
using UnityEngine.AI;

public class Enemy2_TeleportStalker : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;
    NavMeshAgent agent;
    EnemySensor sensor;
    Transform player;

    [Header("Settings")]
    public int maxTeleports = 3;
    public float behindDistance = 2.2f;
    public float sampleRadius = 3f;
    public float blindCooldown = 0.4f;
    public bool killAfterCycles = true;

    int count;
    float cd;
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
        if (!sensor || player == null) { enabled = false; return; }

        active = true;
        count = 0;
        cd = 0f;
        if (agent) agent.isStopped = true;
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
        count = 0;
        cd = 0f;
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
        // 공통 Stare 우선권
        if (commonStareActive)
        {
            FacePlayer(6f);
            return;
        }

        if (!active) return;

        cd -= Time.deltaTime;

        if (sensor.state == EnemySensor.State.Blind && cd <= 0f)
        {
            cd = blindCooldown;
            TeleportBehind();

            count++;

            if (killAfterCycles && count >= maxTeleports)
                DoKill();
        }
        else
        {
            FacePlayer(6f);
        }
    }

    void TeleportBehind()
    {
        if (player == null) return;

        Vector3 desired = player.position - player.forward * behindDistance;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            if (agent && agent.isOnNavMesh) agent.Warp(hit.position);
            else transform.position = hit.position;

            FacePlayer(999f);
        }
    }

    void FacePlayer(float speed)
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        if (speed >= 900f) transform.rotation = target;
        else transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * speed);
    }

    void DoKill()
    {
        loop?.OnEnemyKill();
        Deactivate();
    }
}
