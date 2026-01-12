using UnityEngine;

public class TransitionHub : MonoBehaviour
{
    public bool locked;

    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        locked = false;
    }
}