using UnityEngine;
using UnityEngine.AI;

public class Enemy2_TeleportStalker : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;
    NavMeshAgent agent;
    EnemySensor sensor;
    Transform player;

    [Header("Player (Layer)")]
    public string playerLayerName = "Player";
    int playerLayerIndex = -1;

    [Header("Settings")]
    public int maxTeleports = 3;
    public float behindDistance = 2.2f;
    public float sampleRadius = 3f;
    public float blindCooldown = 0.4f;
    public bool killAfterCycles = true;

    int count;
    float cd;
    bool active;

    // ===== Common Stare =====
    bool commonStareActive;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<EnemySensor>();

        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);
        player = FindPlayerByLayerIndex(playerLayerIndex);

        if (!loop) loop = FindObjectOfType<LoopManager>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        Deactivate();
    }

    void OnTriggerEnter(Collider other)
    {
        if (playerLayerIndex < 0) return;
        if (other.gameObject.layer != playerLayerIndex) return;

        player = other.transform.root;
    }

    public void Activate()
    {
        if (player == null && playerLayerIndex >= 0)
            player = FindPlayerByLayerIndex(playerLayerIndex);

        if (!sensor || player == null)
        {
            Debug.LogWarning("[Enemy2_TeleportStalker] Activate failed: sensor/player missing", this);
            enabled = false;
            return;
        }

        CancelInvoke();
        StopAllCoroutines();

        active = true;
        count = 0;
        cd = 0f;

        if (agent && agent.isOnNavMesh)
            agent.isStopped = true;

        enabled = true;
    }

    public void Deactivate()
    {
        CancelInvoke();
        StopAllCoroutines();

        active = false;
        commonStareActive = false;

        if (agent && agent.isOnNavMesh)
            agent.isStopped = true;

        enabled = false;
    }

    public void ResetEnemy()
    {
        CancelInvoke();
        StopAllCoroutines();

        count = 0;
        cd = 0f;
        active = false;
        commonStareActive = false;

        if (agent && agent.isOnNavMesh)
            agent.isStopped = true;

        transform.SetPositionAndRotation(spawnPos, spawnRot);

        if (agent && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 1.0f, NavMesh.AllAreas))
                transform.position = hit.position;
        }

        enabled = false;
    }

    public void StartCommonStare()
    {
        if (player == null && playerLayerIndex >= 0)
            player = FindPlayerByLayerIndex(playerLayerIndex);

        commonStareActive = true;
        enabled = true;
    }

    public void StopCommonStare()
    {
        commonStareActive = false;
        if (!active) enabled = false;
    }

    void Update()
    {
        if (player == null) return;

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

    Transform FindPlayerByLayerIndex(int layerIndex)
    {
        if (layerIndex < 0) return null;

        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject.layer == layerIndex)
                return all[i];
        }
        return null;
    }
}
