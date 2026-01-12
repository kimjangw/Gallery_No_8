using UnityEngine;

public class TransitionController : MonoBehaviour
{
    [Header("References")]
    public Transform player;                  // 전환부에 이용될 플레이어의 Transform
    public CharacterController cc;            // 전환부에서 CC켜고 이동시 끼임 및 튀어나감 증상 제어를 위한 변수
    public CameraController cameraController; //

    [Header("Portal Hub")]
    public TransitionHub hub;               // 자기쪽 Hub
    public TransitionHub linkedHub;         // 반대쪽 Hub

    void OnTriggerEnter(Collider other)
    {
        // player 체크
        if (other.transform != player) return;

        // 이미 잠겨있으면 이동 금지
        if (hub.locked) return;

        Teleport();
    }

    void Teleport()
    {
        // 이동시 중복이동 방지를 위해 양쪽 Lock
        hub.locked = true;
        linkedHub.locked = true;

        // Player 겹침 및 튕김 방지(OFF)
        cc.enabled = false;

        // Z축 반전을 통한 캐릭터 좌표 이동
        Vector3 pos = player.position;
        pos.z = -pos.z;
        player.position = pos;

        // Player 겹침 및 튕김 방지(ON)
        cc.enabled = true;

        //카메라 재정렬
        cameraController.SnapToPlayerInstant();
    }
}
