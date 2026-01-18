// ============================================================================
//  Enemy4_PrankaDrop.cs (EnemyPattern / A안: NavMesh 미사용, Raycast로 바닥 스냅)
//  - 목표 지점: "플레이어 forward" 기준 앞쪽
//  - StartAction(): 원래 위치 -> 목표지점으로 짧게 이동(점프스퀘어)
//  - 이후 가만히 있음
//  - 다음 루프(TransitionReset)에서 스폰 복귀
//  - 스크립트/오브젝트 비활성화(OFF) 하지 않음
// ============================================================================

using UnityEngine;

public class Enemy4_PrankaDrop : MonoBehaviour, EnemyPattern
{
    [Header("Refs (Inspector)")]
    public Transform player;     // 플레이어 루트(인스펙터 주입)
    public Transform moveRoot;   // 실제로 움직일 메쉬 루트(미지정이면 자동 탐색)

    [Header("Ground")]
    public LayerMask groundMask; // Ground 레이어 지정(인스펙터)
    public float raycastHeight = 2.0f;
    public float raycastDistance = 10f;

    [Header("Target (Player Forward)")]
    public float targetForward = 1.2f;  // 플레이어 앞 거리
    public float targetUp = 0.2f;       // 레이캐스트 전 약간 띄움

    [Header("Fly")]
    public float flyDuration = 0.7f;    // 0이면 즉시
    public float flyArcUp = 0.0f;       // 0이면 직선, >0이면 포물선 느낌
    public float rotateToTarget = 1f;   // 0이면 회전 안 함, 값 클수록 빨리 회전

    bool active;
    float timer;

    Vector3 startPos;
    Quaternion startRot;

    Vector3 spawnPos;
    Quaternion spawnRot;

    void Awake()
    {
        // moveRoot 미지정이면 첫 Renderer로 자동 설정
        if (moveRoot == null)
        {
            Renderer r = GetComponentInChildren<Renderer>();
            moveRoot = (r != null) ? r.transform : transform;
        }

        // 스폰(최초 배치 위치) 저장
        spawnPos = moveRoot.position;
        spawnRot = moveRoot.rotation;

        // 초기화
        active = false;
        timer = 0f;

        ResetToSpawn();
    }

    // 루프에서 선택되었을 때: 대기 상태(아무 것도 안 함)
    public void Ready()
    {
        active = false;
        timer = 0f;
    }

    // ActionTrigger 신호 시: 플레이어 앞쪽으로 점프스퀘어 연출 시작
    public void StartAction()
    {
        if (player == null || moveRoot == null)
        {
            active = false;
            timer = 0f;
            return;
        }

        startPos = moveRoot.position;
        startRot = moveRoot.rotation;

        active = true;
        timer = 0f;
    }

    // 행동 중단(스크립트/오브젝트 OFF 안 함)
    public void Deactivate()
    {
        active = false;
        timer = 0f;
    }

    // 강제 리셋(킬 등): 스폰 복귀 + 행동 중단
    public void ResetEnemy()
    {
        ResetToSpawn();
        Deactivate();
    }

    // 트랜지션 직후: 스폰 복귀 + 행동 중단
    public void OnTransitionReset()
    {
        ResetToSpawn();
        Deactivate();
    }

    void Update()
    {
        if (!active) return;
        if (player == null || moveRoot == null) { Deactivate(); return; }

        // 1) 목표 지점 계산: 플레이어 forward 기준
        Vector3 forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 target = player.position + forward * targetForward;
        target += Vector3.up * targetUp;

        // 2) 바닥 스냅(Raycast)
        Vector3 rayStart = target + Vector3.up * raycastHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            target = hit.point;
        }

        // 3) 시간 보간
        timer += Time.deltaTime;
        float t = (flyDuration <= 0.0001f) ? 1f : Mathf.Clamp01(timer / flyDuration);

        // 4) 이동(직선 or 간단 아크)
        Vector3 pos = Vector3.Lerp(startPos, target, t);
        if (flyArcUp > 0f)
        {
            // 0~1~0 형태로 위로 살짝 튀는 곡선
            float arc = Mathf.Sin(t * Mathf.PI) * flyArcUp;
            pos += Vector3.up * arc;
        }
        moveRoot.position = pos;

        // 5) (선택) 목표를 바라보도록 회전
        if (rotateToTarget > 0f)
        {
            Vector3 lookDir = target - moveRoot.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                moveRoot.rotation = Quaternion.Slerp(moveRoot.rotation, targetRot, Time.deltaTime * rotateToTarget);
            }
        }

        // 6) 연출 종료: 그 자리에서 멈추고 가만히(루프 종료 신호 없음)
        if (t >= 1f)
        {
            Deactivate();
        }
    }

    // 최초 배치 위치/회전으로 복귀
    void ResetToSpawn()
    {
        if (moveRoot == null) return;
        moveRoot.SetPositionAndRotation(spawnPos, spawnRot);
    }
}
