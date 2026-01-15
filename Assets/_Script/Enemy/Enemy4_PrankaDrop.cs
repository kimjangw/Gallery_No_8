using UnityEngine;

public class Enemy3_FlyForward : MonoBehaviour
{
    [Header("Move Target")]
    public Transform moveRoot;   // ✅ 실제로 날릴 메쉬 루트(예: MCh_S_12... 또는 ZBrush_default_group)

    [Header("Move")]
    public float flySpeed = 18f;
    public float flyDuration = 0.7f; // 이 시간 후 종료

    bool active;
    Transform player;
    Vector3 dir;
    float t;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        // ✅ moveRoot 미지정이면 "첫 번째 Renderer"를 자동으로 잡아줌 (자식 메시 루트)
        if (!moveRoot)
        {
            var r = GetComponentInChildren<Renderer>();
            if (r) moveRoot = r.transform;
            else moveRoot = transform;
        }

        // ✅ 플레이어는 CC 루트로 고정
        var cc = Object.FindFirstObjectByType<CharacterController>();
        player = cc ? cc.transform : null;

        spawnPos = moveRoot.position;
        spawnRot = moveRoot.rotation;

        active = false;
    }

    public void Activate()
    {
        if (!player)
        {
            var cc = Object.FindFirstObjectByType<CharacterController>();
            player = cc ? cc.transform : null;
        }
        if (!player) { active = false; return; }

        active = true;
        t = 0f;

        // ✅ Player 진행 방향
        dir = player.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        // 연출: 날아가는 방향을 바라보게(원치 않으면 삭제)
        moveRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    public void Deactivate()
    {
        active = false;
    }

    // Transition 라인 넘어가면 ResetAll이 호출될 테니 이걸로 복귀
    public void ResetEnemy()
    {
        active = false;
        t = 0f;
        moveRoot.SetPositionAndRotation(spawnPos, spawnRot);
    }

    void Update()
    {
        if (!active) return;

        float step = flySpeed * Time.deltaTime;
        moveRoot.position += dir * step;

        t += Time.deltaTime;
        if (t >= flyDuration)
            Deactivate();
    }
}
