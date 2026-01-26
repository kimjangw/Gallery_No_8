using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Animator anim;
    CharacterController cc;

    float walkSpeed = 3f;
    float runSpeed = 6f;

    int hashMoveX;
    int hashMoveY;

    // ===== Kill State =====
    bool isDead = false;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();

        hashMoveX = Animator.StringToHash("MoveX");
        hashMoveY = Animator.StringToHash("MoveY");
    }

    void Update()
    {
        // 죽은 상태면 이동/입력 무시
        if (isDead) return;

        PlayerMove(InputSystem.Input, InputSystem.IsSprint);
    }

    void PlayerMove(Vector2 input, bool isLeftShiftPressed)
    {
        if (!cc.enabled) return;

        if (input.magnitude > 0.1f)
        {
            Vector3 dir = transform.forward * input.y + transform.right * input.x;
            dir.Normalize();

            float curSpeed = isLeftShiftPressed ? runSpeed : walkSpeed;
            cc.Move(dir * curSpeed * Time.deltaTime);

            float animSpeed = isLeftShiftPressed ? 2f : 1f;
            anim.SetFloat(hashMoveX, input.x);
            anim.SetFloat(hashMoveY, input.y * animSpeed);
        }
        else
        {
            anim.SetFloat(hashMoveX, 0f);
            anim.SetFloat(hashMoveY, 0f);
        }

        Debug.DrawRay(transform.position, transform.forward * 10f, Color.blue);
    }

    // ===== LoopManager가 호출하는 Kill 진입점 =====
    public void OnKilled()
    {
        if (isDead) return; // 중복 방지
        isDead = true;

        // 이동 애니 정지
        if (anim != null)
        {
            anim.SetFloat(hashMoveX, 0f);
            anim.SetFloat(hashMoveY, 0f);

            // 필요하면 여기서 죽음 트리거/스테이트로 연결
            // anim.SetTrigger("Die");
        }

        // 가장 단순한 입력 차단: Update에서 return + CC 꺼도 됨
        // cc.enabled = false; // 원하면 사용 (다만 Transition에서 cc 토글을 이미 하므로 주의)
    }

    // 루프 리셋/재시작 시 호출(원하면 LoopManager에서 사용)
    public void OnRespawn()
    {
        isDead = false;

        // cc.enabled = true; // OnKilled에서 껐으면 켜기
    }

    public bool IsDead()
    {
        return isDead;
    }
}
