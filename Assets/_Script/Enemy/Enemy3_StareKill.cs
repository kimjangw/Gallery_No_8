using UnityEngine;
using UnityEngine.AI;

public class Enemy3_StareKill : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;
    NavMeshAgent agent;
    EnemySensor sensor;
    Transform player;

    [Header("Settings")]
    public float stareTimeToCharge = 4f;
    public float killDistance = 1.1f;
    public float faceSpeed = 8f;

    float stare;
    bool charging;
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
        stare = 0f;
        charging = false;
        agent.isStopped = true;
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
        stare = 0f;
        charging = false;
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

        FacePlayer(faceSpeed);

        bool seen = sensor.state == EnemySensor.State.Strong || sensor.state == EnemySensor.State.Weak;

        if (!charging)
        {
            if (seen) stare += Time.deltaTime;

            if (stare >= stareTimeToCharge)
            {
                charging = true;
                agent.isStopped = false;
            }
        }
        else
        {
            agent.SetDestination(player.position);

            if (Vector3.Distance(transform.position, player.position) <= killDistance)
                DoKill();
        }
    }

    void FacePlayer(float speed)
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
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
