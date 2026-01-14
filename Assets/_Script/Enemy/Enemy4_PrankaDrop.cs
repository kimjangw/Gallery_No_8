using UnityEngine;

public class Enemy4_PrankaDrop : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;
    Transform player;

    [Header("Player (Layer)")]
    public string playerLayerName = "Player";
    int playerLayerIndex = -1;

    [Header("Settings")]
    public float existTime = 3f;

    float t;
    bool active;

    // ===== Common Stare =====
    bool commonStareActive;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        if (!loop) loop = FindObjectOfType<LoopManager>();

        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);
        player = FindPlayerByLayerIndex(playerLayerIndex);

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

        CancelInvoke();
        StopAllCoroutines();

        active = true;
        t = 0f;
        enabled = true;

        // 낙하/출현 연출 시작 지점
    }

    public void Deactivate()
    {
        CancelInvoke();
        StopAllCoroutines();

        active = false;
        commonStareActive = false;

        enabled = false;
    }

    public void ResetEnemy()
    {
        CancelInvoke();
        StopAllCoroutines();

        t = 0f;
        active = false;
        commonStareActive = false;

        transform.SetPositionAndRotation(spawnPos, spawnRot);
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

        t += Time.deltaTime;
        if (t >= existTime)
        {
            loop?.OnEnemyEnd();
            Deactivate();
        }
    }

    void FacePlayer(float speed)
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * speed);
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
