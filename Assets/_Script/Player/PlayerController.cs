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

    [Header("Death")]
    public bool isDead = false;

    int hashIsDead;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();

        hashMoveX = Animator.StringToHash("MoveX");
        hashMoveY = Animator.StringToHash("MoveY");
        hashIsDead = Animator.StringToHash("isDead");
    }

    void Update()
    {
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
    }

    // ===== 외부 공개 API: 딱 2개만 =====

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // 이동 파라미터 정리(항상 0)
        anim.SetFloat(hashMoveX, 0f);
        anim.SetFloat(hashMoveY, 0f);

        // Animator: AnyState -> Die 전이
        anim.SetBool(hashIsDead, true);

        // 이동 차단
        if (cc != null) cc.enabled = false;
        this.enabled = false;
    }

    public void Revive()
    {
        if (!isDead) return;
        isDead = false;

        // 애니/파라미터 정리
        anim.SetBool(hashIsDead, false);
        anim.SetFloat(hashMoveX, 0f);
        anim.SetFloat(hashMoveY, 0f);

        // 이동 복구
        if (cc != null) cc.enabled = true;
        this.enabled = true;
        // “재시작 시 원래 위치에서 시작” 같은 위치/루프 리셋은
        // 여기서 하지 말고 LoopManager(혹은 스폰/리셋 담당)에서 처리한 뒤
        // 마지막에 Revive()만 호출하는 쪽이 안전함.
    }
}
