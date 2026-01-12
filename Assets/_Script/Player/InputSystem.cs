using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputSystem : MonoBehaviour
{
    private PlayerInput playerInput;
   
    // 연속 입력되는 액션 - 프로퍼티로 노출
    public static Vector2 Input { get; private set; }               //이동 프로퍼티
    public static bool IsSprint { get; private set; }              //왼쪽쉬프트 프로퍼티


    // Input System의 액션들 (외부에서 몰라도 됨)
    private InputAction moveAction;
    private InputAction sprintAction;

    // 콜백 함수들을 저장할 변수들
    private Action<InputAction.CallbackContext> onMovePerformed;
    private Action<InputAction.CallbackContext> onMoveCanceled;
    private Action<InputAction.CallbackContext> onSprintPerformed;
    private Action<InputAction.CallbackContext> onSprintCanceled;


    void OnEnable()
    {
        // PlayerInput 컴포넌트 초기화
        playerInput = GetComponent<PlayerInput>();
        playerInput.defaultActionMap = "Player";
        playerInput.defaultControlScheme = "Keyboard&Mouse"; //"Default"
        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

        // 각각의 액션들 찾기
        moveAction = playerInput.actions.FindAction("Move");
        sprintAction = playerInput.actions.FindAction("Sprint");

        // Move Action 콜백등록
        if (moveAction != null)
        {
            onMovePerformed = ctx => Input = ctx.ReadValue<Vector2>();
            onMoveCanceled = ctx => Input = Vector2.zero;
            moveAction.performed += onMovePerformed;
            moveAction.canceled += onMoveCanceled;
        }

        // Sprint Action 콜백등록
        if (sprintAction != null)
        {
            onSprintPerformed = ctx => IsSprint = true;
            onSprintCanceled = ctx => IsSprint = false;
            sprintAction.performed += onSprintPerformed;
            sprintAction.canceled += onSprintCanceled;
        }
    }

    void OnDisable()
    {
        // Move Action 콜백해제
        if (moveAction != null)
        {
            if (onMovePerformed != null)
            {
                moveAction.performed -= onMovePerformed;
                onMovePerformed = null;
            }

            if (onMoveCanceled != null)
            {
                moveAction.canceled -= onMoveCanceled;
                onMoveCanceled = null;
            }
        }

        // Sprint Action 콜백해제
        if (sprintAction != null)
        {
            if (onSprintPerformed != null)
            {
                sprintAction.performed -= onSprintPerformed;
                onSprintPerformed = null;
            }

            if (onSprintCanceled != null)
            {
                sprintAction.canceled -= onSprintCanceled;
                onSprintCanceled = null;
            }
        }
    }
}
