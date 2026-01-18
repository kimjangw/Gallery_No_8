using UnityEngine;

public class FixLine : MonoBehaviour
{

    public LoopManager loopManager;
    bool locked = false;

    [Header("Side")]
    public bool sideA;

    //Fix이후 Lock하고 LoopManager에서 정답 판정 전까지 대기
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        if (locked) return;
        if (loopManager.fixCommitted) return;

        locked = true;
        loopManager.OnFix(sideA); // hub 대신 bool 전달
    }

    //정답 판정 후 UnLock
    public void ResetFix()
    {
        locked = false;
    }
}
