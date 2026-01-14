using UnityEngine;
using UnityEngine.AI;

public class SimpleChase : MonoBehaviour
{
    public Transform player;
    NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!player) return;
        agent.SetDestination(player.position);
    }
}
