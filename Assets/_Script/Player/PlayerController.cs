using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Animator anim;
    CharacterController cc;

    float walkSpeed = 3f;
    float runSpeed = 6f;

    // Animator Hash ID
    int hashMoveX;
    int hashMoveY;


    private void Awake()
    {
        //컴포넌트 참조 초기화
        anim = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();

        // Animator Hash Init
        hashMoveX = Animator.StringToHash("MoveX");
        hashMoveY = Animator.StringToHash("MoveY");

    }
    void Update()
    {
        //플레이어 이동
        PlayerMove(InputSystem.Input, InputSystem.IsSprint);

    }

    /// <summary>
    /// 플레이어 이동처리
    /// </summary>
    /// <param name="input">InputManager.Input 사용하기</param>
    /// <param name="isLeftShiftPressed">InputManager.IsSprint 사용하기</param>
    void PlayerMove(Vector2 input, bool isLeftShiftPressed)
    {
        if (!cc.enabled) return; // 추가

        if (input.magnitude > 0.1f)
        {
            //이동 처리
            Vector3 dir = transform.forward * input.y + transform.right * input.x;
            dir.Normalize();
            float curSpeed = isLeftShiftPressed ? runSpeed : walkSpeed;
            cc.Move(dir * curSpeed * Time.deltaTime);

            //이동 애니메이션
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

}
