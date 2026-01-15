using UnityEngine;
using UnityEngine.AI;

public class Enemy2_TeleportStalker : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;
    NavMeshAgent agent;
    EnemySensol sensor;
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

    // ✅ Spawn
    Vector3 spawnPos;
    Quaternion spawnRot;
    bool hasSpawn;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        sensor = GetComponent<EnemySensol>();

        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);
        player = FindPlayerByLayerIndex(playerLayerIndex);

        if (!loop) loop = FindObjectOfType<LoopManager>();

        // ✅ 최초 위치 저장
        spawnPos = transform.position;
        spawnRot = transform.rotation;
        hasSpawn = true;

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

        StopAgentHard();

        enabled = true;
    }

    public void Deactivate()
    {
        CancelInvoke();
        StopAllCoroutines();

        active = false;
        commonStareActive = false;

        StopAgentHard();

        enabled = false;
    }

    public void ResetEnemy()
    {
        // 완전 초기화(최초 위치까지)
        InternalFullReset(toSpawn: true);
    }

    // ✅ Transition 직후 호출(EnemyController.OnTransitionResetAll)
    public void OnTransitionReset()
    {
        // 전환 시점도 완전 초기화 + 스폰 복귀
        InternalFullReset(toSpawn: true);
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

        if (sensor.state == EnemySensol.State.Blind && cd <= 0f)
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
            if (agent && agent.enabled && agent.isOnNavMesh) agent.Warp(hit.position);
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

    // =========================
    // Reset / Agent helpers
    // =========================

    void InternalFullReset(bool toSpawn)
    {
        CancelInvoke();
        StopAllCoroutines();

        count = 0;
        cd = 0f;
        active = false;
        commonStareActive = false;

        StopAgentHard();

        if (toSpawn)
            ResetToSpawn();

        enabled = false;
    }

    void StopAgentHard()
    {
        if (!agent || !agent.enabled) return;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        else
        {
            // NavMesh 밖이면 여기서 path 조작 불가. Transform만 정리하고,
            // ResetToSpawn에서 SamplePosition/Warp로 복구함.
        }
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
                // Warp가 가장 확실하게 Agent 내부 좌표까지 동기화
                agent.Warp(hit.position);
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
        else
        {
            // agent 없으면 transform 복원으로 끝
        }
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
