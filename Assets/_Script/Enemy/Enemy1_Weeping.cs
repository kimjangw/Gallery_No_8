using UnityEngine;
using UnityEngine.AI;

public class Enemy1_Weeping : MonoBehaviour
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

        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);
        player = FindPlayerByLayerIndex(playerLayerIndex);

        if (!loop) loop = FindObjectOfType<LoopManager>();

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        Deactivate();
    }

    // (선택) Trigger Collider가 있다면 플레이어 확정 등록
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
            Debug.LogWarning("[Enemy1_Weeping] Activate failed: agent/sensor/player missing", this);
            enabled = false;
            return;
        }

        CancelInvoke();
        StopAllCoroutines();

        active = true;
        t = 0f;

        if (agent.isOnNavMesh)
            agent.isStopped = false;

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

        t = 0f;
        active = false;
        commonStareActive = false;

        if (agent && agent.isOnNavMesh)
            agent.isStopped = true;

        transform.SetPositionAndRotation(spawnPos, spawnRot);

        // 스폰이 NavMesh 밖으로 밀리는 경우 방지 (선택)
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
        if (player == null && playerLayerIndex >= 0)
            player = FindPlayerByLayerIndex(playerLayerIndex);

        commonStareActive = true;
        enabled = true;
    }

    public void StopCommonStare()
    {
        commonStareActive = false;

        if (!active)
            enabled = false;
    }

    void Update()
    {
        if (player == null) return;

        // 공통 Stare 우선권: 회전만
        if (commonStareActive)
        {
            FacePlayer(6f);
            return;
        }

        if (!active) return;
        if (!agent || !agent.isOnNavMesh) return;

        var st = sensor.state;

        // Strong(확실히 보임)일 때 멈춤
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
            DoKill();
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
