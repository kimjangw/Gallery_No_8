// ============================================================================
//  Enemy5_Follower.cs (Layer 기반 Player 획득 / NavMesh 안전가드 / CommonStare 지원)
//  - Tag 사용 제거
//  - PlayerLayerName 과 playerLayerIndex 둘 중 하나만 맞춰도 동작
//  - (테스트용) OnTriggerEnter로 player를 먼저 잡는 방식 포함
// ============================================================================

using UnityEngine;
using UnityEngine.AI;

public class Enemy5_Follower : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop; // 없어도 됨(End만 쓰면)
    NavMeshAgent agent;
    Transform player;

    [Header("Player (Layer)")]
    public string playerLayerName = "Player"; // 네가 쓰는 Player 레이어 이름
    int playerLayerIndex = -1;

    [Header("Follow Settings")]
    public float keepDistance = 3.0f;     // 이 거리 이내면 정지
    public float resumeDistance = 4.5f;   // 이 거리 이상이면 다시 따라감 (히스테리시스)
    public float maxFollowTime = 0f;      // 0이면 무한, 값 있으면 시간 후 End

    float t;
    bool active;

    // 공통 Stare 패턴
    bool commonStareActive;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // LayerIndex 캐싱
        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);

        // (선택) 시작 시점에 한번 찾아보기 (씬이 단순하면 충분히 OK)
        // player는 트리거로 잡는 방식이 더 확실하지만, 테스트 편의상 함께 둠
        player = FindPlayerByLayerIndex(playerLayerIndex);

        if (!loop) loop = FindObjectOfType<LoopManager>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        Deactivate(); // 안전가드 들어있어서 Awake에서 호출해도 OK
    }

    // =====================================================================
    // [TEST] 플레이어가 Enemy 주변 트리거를 지나가면 player를 확정 등록
    // - 이 스크립트가 붙은 오브젝트에 Collider(IsTrigger) 있어야 함
    // =====================================================================
    void OnTriggerEnter(Collider other)
    {
        if (playerLayerIndex < 0) return;

        if (other.gameObject.layer != playerLayerIndex)
            return;

        // 플레이어 콜라이더가 자식일 수 있으니 root로 잡는 게 보통 안전
        player = other.transform.root;
    }

    public void Activate()
    {
        // player가 아직 없으면 한번 더 찾아본다 (테스트 편의)
        if (player == null && playerLayerIndex >= 0)
            player = FindPlayerByLayerIndex(playerLayerIndex);

        if (!agent || player == null)
        {
            Debug.LogWarning("[Enemy5_Follower] Activate failed: agent/player missing", this);
            enabled = false;
            return;
        }

        active = true;
        t = 0f;

        if (agent.isOnNavMesh)
            agent.isStopped = false;

        enabled = true;
    }

    public void Deactivate()
    {
        active = false;

        if (agent && agent.isOnNavMesh)
            agent.isStopped = true;

        // Follow도 꺼지고, Stare도 꺼짐
        commonStareActive = false;
        enabled = false;
    }

    public void ResetEnemy()
    {
        t = 0f;
        active = false;
        commonStareActive = false;

        if (agent && agent.isOnNavMesh)
            agent.isStopped = true;

        transform.SetPositionAndRotation(spawnPos, spawnRot);

        // Reset 후 NavMesh 위로 스냅(스폰이 약간 떠있거나, 피벗 문제로 벗어나는 케이스 방지)
        if (agent && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 1.0f, NavMesh.AllAreas))
                transform.position = hit.position;
        }

        enabled = false;
    }

    // ===== 공통패턴(랜덤 1개가 나를 쳐다봄) 지원 =====
    public void StartCommonStare()
    {
        // player가 아직 없으면 한번 확보 시도
        if (player == null && playerLayerIndex >= 0)
            player = FindPlayerByLayerIndex(playerLayerIndex);

        commonStareActive = true;
        enabled = true; // 비활성 상태여도 시선 연출은 가능
    }

    public void StopCommonStare()
    {
        commonStareActive = false;

        // Follow(active)가 아니라면 다시 꺼줌(불필요 Update 방지)
        if (!active) enabled = false;
    }

    void Update()
    {
        // player가 없으면 아무것도 못함
        if (player == null) return;

        // 공통 Stare가 우선권(회전만)
        if (commonStareActive)
        {
            FacePlayer(6f);
            return;
        }

        if (!active) return;
        if (!agent || !agent.isOnNavMesh) return;

        // 옵션: 일정 시간 후 종료(패턴 자동 종료)
        if (maxFollowTime > 0f)
        {
            t += Time.deltaTime;
            if (t >= maxFollowTime)
            {
                loop?.OnEnemyEnd();
                Deactivate();
                return;
            }
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (!agent.isStopped)
        {
            if (dist <= keepDistance)
                agent.isStopped = true;
        }
        else
        {
            if (dist >= resumeDistance)
                agent.isStopped = false;
        }

        if (!agent.isStopped)
            agent.SetDestination(player.position);
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

    // =====================================================================
    // Utils
    // =====================================================================
    Transform FindPlayerByLayerIndex(int layerIndex)
    {
        if (layerIndex < 0) return null;

        // 씬 전체 Transform 스캔 (테스트 용도. 나중에 다이어트 가능)
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject.layer == layerIndex)
                return all[i];
        }
        return null;
    }
}
