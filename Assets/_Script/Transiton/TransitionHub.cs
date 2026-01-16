using UnityEngine;

public class TransitionHub : MonoBehaviour
{
    [Header("Link")]
    public TransitionHub linkedHub; // 반대편 허브 인식용(동시 Lock/Unlock)

    [Header("Side (실제 선택 방향)")]
    public bool sideA; // A면 true, B면 false (Inspector에서 지정)

    [Header("State")]
    [SerializeField] private bool locked;

    // HubA,HubB (동시 잠금 or 동시 해제)
    public void SetLocked(bool value)
    {
        locked = value;
        if (linkedHub != null) linkedHub.locked = value;
    }

    // 외부에서 해당함수를 통해서 상태 확인.
    public bool IsLocked()
    {
        return locked;
    }

    //허브에 닿으면 플레이어 다시 Transition가능
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) { return; }

        SetLocked(false);
    }
}
