using UnityEngine;
using UnityEngine.AI;

public class Enemy2_TeleportStalker : MonoBehaviour, EnemyPattern
{
    [Header("Refs (Inspector)")]
    public LoopManager loopManager;
    public Transform player;
    public EnemySensol sensor;

    [Header("Settings")]
    public float behindDistance = 2.2f;   // "플레이어~스폰 라인"에서 플레이어 기준 거리(스텝 기준)
    public float sampleRadius = 3.0f;     // NavMesh 샘플 반경
    public bool killAfterThirdTeleport = true;

    [Header("Teleport Steps (percent of behindDistance)")]
    [Range(0f, 1f)] public float farPercent = 1.00f;   // 1st
    [Range(0f, 1f)] public float midPercent = 0.50f;   // 2nd
    [Range(0f, 1f)] public float nearPercent = 0.20f;  // 3rd

    [Header("Start Look (one-shot)")]
    public bool snapLookOnStart = true;   // 시작 시 1회 "쨘" 연출
    public float startLookDelay = 0.0f;   // 필요하면 0.05 같은 딜레이로 연출 가능(기본 0)

    NavMeshAgent agent;

    bool active;
    int teleportStep; // 0,1,2 ...
    EnemySensol.State prevState;

    Vector3 spawnPos;
    Quaternion spawnRot;

    // 시작 인지용 1회 회전 가드
    bool didStartLook;
    float startLookTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        active = false;
        teleportStep = 0;
        prevState = EnemySensol.State.Blind;

        didStartLook = false;
        startLookTimer = 0f;

        StopAgentHard();
        // enabled는 끄지 않음(씬에서 지속 사용)
    }

    // -----------------------------
    // EnemyPattern
    // -----------------------------

    public void Ready()
    {
        active = false;
        teleportStep = 0;
        prevState = (sensor != null) ? sensor.state : EnemySensol.State.Blind;

        didStartLook = false;
        startLookTimer = 0f;

        StopAgentHard();
        ResetToSpawn();
    }

    public void StartAction()
    {
        if (player == null || sensor == null)
        {
            active = false;
            return;
        }

        active = true;
        teleportStep = 0;
        prevState = sensor.state;

        didStartLook = false;
        startLookTimer = startLookDelay;

        StopAgentHard();

        // (요구 1) 시작 인지: 최초 1회만 플레이어 바라보기(즉시 or 딜레이 후)
        if (snapLookOnStart && startLookDelay <= 0f)
        {
            SnapFacePlayerOnce();
        }
    }

    public void Deactivate()
    {
        active = false;
        teleportStep = 0;

        didStartLook = false;
        startLookTimer = 0f;

        StopAgentHard();
        // enabled는 끄지 않음
    }

    public void ResetEnemy()
    {
        ResetToSpawn();
        Deactivate();
    }

    public void OnTransitionReset()
    {
        ResetEnemy();
    }

    void Update()
    {
        if (!active) return;
        if (player == null || sensor == null) return;

        // 시작 연출 딜레이 처리(필요 시)
        if (snapLookOnStart && !didStartLook && startLookDelay > 0f)
        {
            startLookTimer -= Time.deltaTime;
            if (startLookTimer <= 0f)
            {
                SnapFacePlayerOnce();
            }
        }

        EnemySensol.State currentState = sensor.state;

        // Weak/Strong -> Blind "전환 순간"에만 1회 워프
        bool becameBlindThisFrame = (prevState != EnemySensol.State.Blind && currentState == EnemySensol.State.Blind);

        if (becameBlindThisFrame)
        {
            bool killed = DoTeleportStep();
            if (killed) return;
        }

        // (요구 1) 지속 추적 회전 제거:
        // - 워프가 없을 때도 계속 바라보지 않음
        // - FacePlayer() 호출은 StartAction의 1회 Snap만 사용

        prevState = currentState;
    }

    // -----------------------------
    // Teleport Step Logic
    // -----------------------------

    // return: killed?
    bool DoTeleportStep()
    {
        float stepDistanceFromPlayer = GetStepDistance(teleportStep);

        // (요구 2) "플레이어 뒤"가 아니라 "스폰 <-> 플레이어" 사이 선분 위로
        Vector3 playerPosition = player.position;
        Vector3 playerToSpawn = spawnPos - playerPosition;

        float playerToSpawnDistance = playerToSpawn.magnitude;
        if (playerToSpawnDistance < 0.001f)
        {
            // 스폰과 플레이어가 거의 같은 위치면 안전 처리: 그냥 스폰 기준으로 이동하지 않음
            teleportStep += 1;
            return CheckKillAfterStep();
        }

        Vector3 playerToSpawnDir = playerToSpawn / playerToSpawnDistance;

        // 선분 위를 보장하려면, 플레이어 기준 거리 = min(요청거리, 스폰까지 거리-여유)
        float safetyMargin = 0.05f;
        float clampedDistance = stepDistanceFromPlayer;
        if (clampedDistance > playerToSpawnDistance - safetyMargin)
            clampedDistance = Mathf.Max(0f, playerToSpawnDistance - safetyMargin);

        Vector3 desired = playerPosition + playerToSpawnDir * clampedDistance;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.Warp(hit.position);
            else
                transform.position = hit.position;
        }
        else
        {
            // 샘플 실패 시: Transform 이동(최후 안전장치)
            transform.position = desired;
        }

        teleportStep += 1;

        return CheckKillAfterStep();
    }

    bool CheckKillAfterStep()
    {
        if (killAfterThirdTeleport && teleportStep >= 3)
        {
            DoKill();
            return true;
        }
        return false;
    }

    float GetStepDistance(int step)
    {
        if (step == 0) return behindDistance * farPercent;
        if (step == 1) return behindDistance * midPercent;
        return behindDistance * nearPercent;
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    void DoKill()
    {
        if (loopManager != null) loopManager.OnEnemyKill();
        Deactivate();
    }

    // 시작 인지용 1회만 사용
    void SnapFacePlayerOnce()
    {
        if (didStartLook) return;
        didStartLook = true;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = targetRotation;
    }

    void StopAgentHard()
    {
        if (agent == null || !agent.enabled) return;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    void ResetToSpawn()
    {
        StopAgentHard();
        transform.SetPositionAndRotation(spawnPos, spawnRot);

        if (agent != null && agent.enabled)
        {
            if (NavMesh.SamplePosition(spawnPos, out var hit, 2.0f, NavMesh.AllAreas))
                agent.Warp(hit.position);

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
    }
}
