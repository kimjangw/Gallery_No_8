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

        // [ADD] 전환 구간: 카메라 보간 speed만 임시로 상승
        cameraController.BeginTransitionBoost();

        loopManager.OnTransition(hub);

        cc.enabled = false;

        Vector3 pos = player.position;
        pos.z = -pos.z;
        player.position = pos;

        cc.enabled = true;

        cameraController.SnapToPlayerInstant();

        loopManager.ResetFixLine();
        loopManager.enemyController?.OnTransitionResetAll();

        if (actionTriggerToReset != null)
            actionTriggerToReset.UnlockForNextSection();

        // [ADD] 다음 프레임부터는 원래 speed로 복귀 (즉시 끄면 같은 프레임 Lerp에 영향이 없을 수 있어 0프레임 딜레이)
        Invoke(nameof(EndCameraBoost), 0f);
    }

    void EndCameraBoost()
    {
        cameraController.EndTransitionBoost();
    }
}
