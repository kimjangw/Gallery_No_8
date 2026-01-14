using UnityEngine;

public class Enemy4_PrankaDrop : MonoBehaviour
{
    [Header("Refs")]
    public LoopManager loop;

    [Header("Settings")]
    public float existTime = 3f;

    float t;
    bool active;

    // ===== Common Stare (공통패턴) =====
    bool commonStareActive;

    Transform player;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        if (!loop) loop = FindObjectOfType<LoopManager>();
        player = GameObject.FindWithTag("Player")?.transform;

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        Deactivate();
    }

    public void Activate()
    {
        active = true;
        t = 0f;
        enabled = true;

        // 낙하/출현 연출은 여기서 구현
    }

    public void Deactivate()
    {
        active = false;
        enabled = false;
    }

    public void ResetEnemy()
    {
        t = 0f;
        commonStareActive = false;
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

        t += Time.deltaTime;
        if (t >= existTime)
        {
            loop?.OnEnemyEnd();
            Deactivate();
        }
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
}
