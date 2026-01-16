using UnityEngine;

public class FixLine : MonoBehaviour
{
    public LoopManager loopManager;
    bool locked = false;

    [Header("Side")]
    public bool sideA;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        if (locked) return;
        if (loopManager.FixCommitted) return;

        locked = true;
        loopManager.OnFix(sideA); // hub 대신 bool 전달
    }

    public void ResetFix()
    {
        locked = false;
    }
}
