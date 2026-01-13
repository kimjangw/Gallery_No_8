using UnityEngine;

public class FixLine : MonoBehaviour
{
    public TransitionHub hub;          // 중요한 부분: 자신이 속한 Hub 직접 지정
    public LoopManager loopManager;
    bool locked = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        if (locked) return; // FixLine 자체 lock

        // LoopManager Fix lock 추가 체크
        if (loopManager.FixCommitted)
            return;

        locked = true;
        loopManager.OnFix(hub);
    }


    public void ResetFix()
    {
        locked = false;
    }
}
