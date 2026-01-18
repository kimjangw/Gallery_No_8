using UnityEngine;

public class TransitionController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public CharacterController cc;
    public CameraController cameraController;
    public LoopManager loopManager;

    [Header("ActionTrigger Reset")]
    public ActionTrigger[] actionTriggers;

    [Header("Portal Hub")]
    public TransitionHub hub;

    int playerLayer;
    bool isTransitioning;

    void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    void OnTriggerEnter(Collider other)
    {
        // Player 체크
        if (other.gameObject.layer != playerLayer) return;

        // 전환 중이면 무시 (중복 진입 방지)
        if (isTransitioning) return;

        // 허브가 잠겨있으면 이동 금지
        if (hub.IsLocked()) return;

        Teleport();
    }

    void Teleport()
    {
        // 중복 방지 가드
        isTransitioning = true;

        // 양쪽 Hub동시 Lock (Hub터치해야 UnLock)
        hub.SetLocked(true);

        // 캐릭터 위치 꼬임 방지
        cc.enabled = false;

        Vector3 pos = player.position;
        pos.z = -pos.z;
        player.position = pos;

        cc.enabled = true;

        // 전환 직후 카메라 재정렬
        cameraController.SnapAfterTransition();

        // loopManager를 통해 세팅 리셋(Fix, Enemy상태, ActionTrigger)
        loopManager.TransitionReset();

        // loopManager 루프 판정/상태 갱신 
        loopManager.OnTransition(hub);

        // 가드 해제
        isTransitioning = false;
    }
}
