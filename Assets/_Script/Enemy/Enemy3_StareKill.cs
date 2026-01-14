using UnityEngine;
using UnityEngine.AI;

public class Enemy3_StareKill : MonoBehaviour
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
    public float stareTimeToCharge = 4f;
    public float killDistance = 1.1f;
    public float faceSpeed = 8f;

    float stare;
    bool charging;
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

        if (!agent || !sensor || player == null)
        {
            Debug.LogWarning("[Enemy3_StareKill] Activate failed: agent/sensor/player missing", this);
            enabled = false;
            return;
        }

        CancelInvoke();
        StopAllCoroutines();

        active = true;
        stare = 0f;
        charging = false;

        if (agent.isOnNavMesh)
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

        stare = 0f;
        charging = false;
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
        if (!agent || !agent.isOnNavMesh) return;

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
