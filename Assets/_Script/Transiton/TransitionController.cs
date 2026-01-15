using UnityEngine;

public class TransitionController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public CharacterController cc;
    public CameraController cameraController;
    public LoopManager loopManager;

    // [ADD] 전환 시 ActionTrigger의 flag를 풀어주기 위한 참조
    [Header("ActionTrigger Reset")]
    public ActionTrigger actionTriggerToReset;

    [Header("Portal Hub")]
    public TransitionHub hub;
    public TransitionHub linkedHub;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        if (hub.locked) return;

        Teleport();
    }

    void Teleport()
    {
        hub.locked = true;
        linkedHub.locked = true;

        loopManager.OnTransition(hub);

        cc.enabled = false;

        Vector3 pos = player.position;
        pos.z = -pos.z;
        player.position = pos;

        cc.enabled = true;

        cameraController.SnapToPlayerInstant();

        loopManager.ResetFixLine();

        // [ADD] 전환 완료 시점에 ActionTrigger 잠금 해제(다음 구간에서 다시 발동 가능)
        if (actionTriggerToReset != null)
            actionTriggerToReset.UnlockForNextSection();
    }
}
