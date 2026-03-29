using UnityEngine;
using UnityEngine.AI;

public class Enemy1_Weeping : MonoBehaviour, EnemyPattern
{
    [Header("Refs (Inspector)")]
    public LoopManager loopManager;     // Kill 신호용
    public EnemySensol enemySensol;     // PlayerSensor가 써주는 데이터 컨테이너
    public Transform player;            // CC 루트(인스펙터 주입 권장)

    [Header("Visual States (Inspector)")]
    public GameObject idleObj;          // Idle(다른 동상이 선택중일 때)
    public GameObject pointObj;         // Point(선택되었을 때)
    public GameObject attackObj;         // Attack(추적 시작)

    [Header("Point")]
    public float pointDuration = 0.8f;  // Ready()에서 포인팅 유지 시간

    [Header("Attack Settings")]
    public float killTime = 10f;
    public float killDistance = 1.2f;
    public float repathInterval = 0.2f;

    [Header("NavMesh Safety")]
    public float navSampleRadius = 1.5f;
    public float spawnSampleRadius = 2.0f;

    [Header("Sound Settings")]
    public float moveSoundInterval = 0.2f;
    public float minPitch = 0.7f;
    public float maxPitch = 0.9f;
    private float moveSoundTimer;

    NavMeshAgent agent;

    // 상태
    bool actionStarted;     // StartAction 이후 true
    bool isPointing;        // Ready() 직후 pointDuration 동안 true
    float pointTimer;

    float t;
    float repathTimer;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!enemySensol) enemySensol = GetComponent<EnemySensol>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        ResetInternalState();
        SetStateIdle();
        StopAgent();
    }

    // =========================================================
    // EnemyPattern
    // =========================================================

    // 루프에서 선택되었을 때: "Point"를 1회 보여준다(행동 시작 아님)
    public void Ready()
    {
        ResetInternalState();

        // Point 시작
        isPointing = true;
        pointTimer = pointDuration;

        SetStatePoint();
        SnapFacePlayer();     // 요구: 로테이션은 즉시 플레이어를 바라보게

        EnsureOnNavMesh();
        StopAgent();
    }

    // ActionTrigger 신호: 실제 추적(Attack) 시작
    public void StartAction()
    {
        ResetInternalState();

        actionStarted = true;
        isPointing = false;

        SetStateAttack();
        SnapFacePlayer();

        if (agent == null || enemySensol == null || player == null)
        {
            Deactivate();
            return;
        }

        if (!EnsureOnNavMesh())
        {
            Deactivate();
            return;
        }

        agent.isStopped = false;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    // 패턴 중지(상태만 정리)
    public void Deactivate()
    {
        ResetInternalState();
        SetStateIdle();
        StopAgent();
    }

    // 강제 리셋(스폰 복귀 + Idle)
    public void ResetEnemy()
    {
        ResetToSpawn();
        Deactivate();
    }

    // 트랜지션 직후(스폰 복귀 + Idle)
    public void OnTransitionReset()
    {
        ResetToSpawn();
        Deactivate();
    }

    // =========================================================
    // Update
    // =========================================================

    void Update()
    {
        // 1) Ready()에서 Point를 보여주는 타이머
        if (isPointing)
        {
            pointTimer -= Time.deltaTime;
            if (pointTimer <= 0f)
            {
                isPointing = false;

                // 아직 StartAction 전이면 Idle 비주얼로 복귀
                if (!actionStarted)
                    SetStateIdle();
            }
        }

        // 2) StartAction 이후에만 추적 로직
        if (!actionStarted) return;

        if (agent == null || enemySensol == null || player == null) return;
        if (!EnsureOnNavMesh()) return;

        // Strong이면 정지(추적 금지)
        if (enemySensol.state == EnemySensol.State.Strong)
        {
            StopAgent();
        }
        else
        {
            // Weak/Blind이면 이동
            agent.isStopped = false;

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                agent.SetDestination(player.position);
                repathTimer = repathInterval;
            }

            t += Time.deltaTime;

            if (Vector3.Distance(transform.position, player.position) <= killDistance || t >= killTime)
            {
                if (loopManager != null) loopManager.OnEnemyKill();
                Deactivate();
            }
        }

        // [사운드 로직] 에이전트가 실제로 이동 중일 때만 사운드 재생
        if (agent != null && !agent.isStopped && agent.velocity.sqrMagnitude > 0.1f)
        {
            moveSoundTimer -= Time.deltaTime;
            if (moveSoundTimer <= 0f)
            {
                float randomPitch = Random.Range(minPitch, maxPitch);
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.Play3D("ston_Move", transform.position, 1f, randomPitch);
                }
                moveSoundTimer = moveSoundInterval;
            }
        }
        else
        {
            moveSoundTimer = 0f; // 멈추면 타이머 초기화 (출발 시 바로 소리 나도록)
        }
    }

    // =========================================================
    // Visual State
    // =========================================================

    void SetStateIdle()
    {
        if (idleObj) idleObj.SetActive(true);
        if (pointObj) pointObj.SetActive(false);
        if (attackObj) attackObj.SetActive(false);
    }

    void SetStatePoint()
    {
        if (idleObj) idleObj.SetActive(false);
        if (pointObj) pointObj.SetActive(true);
        if (attackObj) attackObj.SetActive(false);
    }

    void SetStateAttack()
    {
        if (idleObj) idleObj.SetActive(false);
        if (pointObj) pointObj.SetActive(false);
        if (attackObj) attackObj.SetActive(true);
    }

    // =========================================================
    // Rotation
    // =========================================================

    // 요구: 로테이션은 "즉시" 플레이어를 바라보도록 초기화
    void SnapFacePlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    // =========================================================
    // Internal / NavMesh
    // =========================================================

    void ResetInternalState()
    {
        actionStarted = false;
        isPointing = false;
        pointTimer = 0f;

        t = 0f;
        repathTimer = 0f;
    }

    void StopAgent()
    {
        if (agent == null || !agent.enabled) return;
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    bool EnsureOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
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
        StopAgent();
        transform.SetPositionAndRotation(spawnPos, spawnRot);

        if (agent != null && agent.enabled)
        {
            if (NavMesh.SamplePosition(spawnPos, out var hit, spawnSampleRadius, NavMesh.AllAreas))
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
