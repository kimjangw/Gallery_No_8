using UnityEngine;

public class TransitionHub : MonoBehaviour
{
    public bool locked;

    public bool isA; // A 사이드인지 B 사이드인지 Inspector에서 설정
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        locked = false;
    }
}