// ============================================================================
//  Enemy5_Follower.cs (EnemyPattern / Option A: 근접 시 정지 + 응시)
//  - Player Layer: Awake에서 LayerMask.NameToLayer("Player") 캐싱 방식
// ============================================================================

using UnityEngine;
using UnityEngine.AI;

public class Enemy5_Follower : MonoBehaviour, EnemyPattern
{
    [Header("Refs")]
    public LoopManager loop;

    NavMeshAgent agent;
    Transform player;

    // Player 레이어 캐시(Awake에서 1회 계산)
    int playerLayer = -1;

    [Header("Follow Settings")]
    public float keepDistance = 3.0f;      // 이 거리 이내면 정지 + 응시 전환
    public float resumeDistance = 4.5f;    // 이 거리 이상이면 다시 추적(히스테리시스)
    public float followSpeed = 2.8f;       // 추적 속도

    [Header("Stare Settings (Option A)")]
    public float stareDuration = 2.0f;     // 근접 후 응시 지속 시간
    public float stareTurnSpeed = 6.0f;    // 응시 회전 속도

    [Header("NavMesh Safety")]
    public float navSampleRadius = 1.5f;   // NavMesh 밖일 때 보정 반경
    public float spawnSampleRadius = 2.0f; // 스폰 복귀 시 보정 반경

    // 상태
    bool actionStarted = false;            // StartAction 이후 true
    bool isStaring = false;                // 근접 후 응시 상태
    float stareTimer = 0f;

    // 스폰(최초 배치 위치)
    Vector3 spawnPos;
    Quaternion spawnRot;
    bool hasSpawn = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Player 레이어 캐싱
        playerLayer = LayerMask.NameToLayer("Player");

        // (테스트/안정) 시작 시점에 한번 확보
        player = FindPlayerByLayer(playerLayer);

        if (!loop) loop = FindObjectOfType<LoopManager>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;
        hasSpawn = true;

        Deactivate();
    }

    void OnTriggerEnter(Collider other)
    {
        if (playerLayer < 0) return;
        if (other.gameObject.layer != playerLayer) return;

        // 플레이어 콜라이더가 자식일 수 있으니 root로
        player = other.transform.root;
    }

    // =========================================================
    // EnemyPattern
    // =========================================================

    public void Ready()
    {
        AcquirePlayerIfNeeded();

        actionStarted = false;
        isStaring = false;
        stareTimer = 0f;

        if (agent)
        {
            agent.speed = followSpeed;
            EnsureOnNavMesh();
            StopMoveHard();
        }

        enabled = true;
    }

    public void StartAction()
    {
        AcquirePlayerIfNeeded();

        actionStarted = true;
        isStaring = false;
        stareTimer = 0f;

        if (!agent || player == null)
        {
            Deactivate();
            return;
        }

        agent.speed = followSpeed;

        if (!EnsureOnNavMesh())
        {
            Deactivate();
            return;
        }

        agent.isStopped = false;
        enabled = true;
    }

    public void Deactivate()
    {
        actionStarted = false;
        isStaring = false;
        stareTimer = 0f;

        StopMoveHard();
        enabled = false;
    }

    public void ResetEnemy()
    {
        actionStarted = false;
        isStaring = false;
        stareTimer = 0f;

        ResetToSpawn();
        enabled = false;
    }

    public void OnTransitionReset()
    {
        actionStarted = false;
        isStaring = false;
        stareTimer = 0f;

        ResetToSpawn();   // 트랜지션 시 스폰 복귀(필수)
        enabled = false;
    }

    // =========================================================
    // Update
    // =========================================================

    void Update()
    {
        if (!actionStarted) return;
        if (!agent) return;
        if (player == null) return;

        if (!EnsureOnNavMesh()) return;

        // 응시 상태
        if (isStaring)
        {
            FacePlayer(stareTurnSpeed);

            stareTimer += Time.deltaTime;
            if (stareTimer >= stareDuration)
            {
                if (loop != null) loop.OnEnemyEnd();
                Deactivate();
            }
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        // 근접 도달 → 정지 + 응시
        if (!agent.isStopped && dist <= keepDistance)
        {
            StopMoveHard();
            isStaring = true;
            stareTimer = 0f;
            return;
        }

        // 멀어지면 재추적
        if (agent.isStopped && dist >= resumeDistance)
        {
            agent.isStopped = false;
        }

        // 추적 중이면 목적지 갱신(붙지 않게 keepDistance 지점)
        if (!agent.isStopped)
        {
            Vector3 followTarget = GetFollowTarget();
            agent.SetDestination(followTarget);
        }
    }

    Vector3 GetFollowTarget()
    {
        Vector3 awayDir = transform.position - player.position;
        awayDir.y = 0f;

        if (awayDir.sqrMagnitude < 0.0001f)
            awayDir = -player.forward;

        awayDir.Normalize();

        return player.position + awayDir * keepDistance;
    }

    // =========================================================
    // NavMesh / Movement Safety
    // =========================================================

    void StopMoveHard()
    {
        if (!agent || !agent.enabled) return;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    bool EnsureOnNavMesh()
    {
        if (!agent || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out var hit, navSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    void ResetToSpawn()
    {
        if (!hasSpawn) return;

        StopMoveHard();

        transform.SetPositionAndRotation(spawnPos, spawnRot);

        if (agent && agent.enabled)
        {
            if (NavMesh.SamplePosition(spawnPos, out var hit, spawnSampleRadius, NavMesh.AllAreas))
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

    // =========================================================
    // Facing
    // =========================================================

    void FacePlayer(float turnSpeed)
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed);
    }

    // =========================================================
    // Player Acquire
    // =========================================================

    void AcquirePlayerIfNeeded()
    {
        if (player != null) return;
        player = FindPlayerByLayer(playerLayer);
    }

    Transform FindPlayerByLayer(int layerIndex)
    {
        if (layerIndex < 0) return null;

        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int enemyIndex = 0; enemyIndex < all.Length; enemyIndex++)
        {
            if (all[enemyIndex].gameObject.layer == layerIndex)
                return all[enemyIndex];
        }
        return null;
    }
}
