using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    // 기준이 될 플레이어 Transform
    public Transform player;

    // TPS 카메라 오프셋
    [Header("CameraOffset")]
    public float cameraBackDistance = 1.1f;   // 뒤
    public float cameraHeight = 1.12f;        // 위
    public float cameraShoulderOffset = 0.2f; // 우측 어깨

    // 민감도 + 피치 제한
    [Header("CameraRotation")]
    public float sensitivity = 20f;
    public float minPitch = -25f;
    public float maxPitch = 25f;

    // 벽뚫기 방지(충돌 보정용)
    [Header("Camera Collision")]
    [SerializeField] private LayerMask collisionMask;   // [CHANGE] private로 내림
    public float collisionRadius = 0.01f;
    public float collisionBuffer = 1f;
    // 카메라 추적 스피드
    private float lerpSpeed = 100f;

    // 플레이어 좌/우 회전(yaw), 카메라 상/하(pitch)
    public float yaw;
    float pitch = 10f;

    // 카메라 측에서 보는 값을 다른 스크립트로 전달.
    public CameraView CurrentView { get; private set; }
    public struct CameraView
    {
        public float pitch;
        public float yaw;
        public Vector3 forward;
        public Vector3 right;
        public Vector3 up;
    }


    void Start()
    {
        // 마우스 중앙 고정 및 감추기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 플레이어 최초 yaw 세팅
        if (player) yaw = player.eulerAngles.y;

        // 기본 마스크 세팅
        if (collisionMask == 0)
        { collisionMask = LayerMask.GetMask("Wall", "Ceiling", "Ground"); }
          
    }

    void LateUpdate()
    {
        if (!player) return;

        // 마우스 입력
        Vector2 mouse = Mouse.current.delta.ReadValue();

        // yaw, pitch 갱신
        yaw += mouse.x * sensitivity * Time.deltaTime;
        pitch -= mouse.y * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 플레이어 회전 적용
        player.rotation = Quaternion.Euler(0f, yaw, 0f);

        // 카메라 회전
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

        // 목표 위치
        Vector3 finalPos = player.position + cameraRotation * GetBaseCameraOffset();

        // 충돌 체크 기준
        Vector3 cameraPivot = player.position + Vector3.up * cameraHeight;
        Vector3 cameraBackDir = cameraRotation * Vector3.back;

        // SphereCast로 벽/천장 충돌 시 앞으로 당김
        if (Physics.SphereCast(cameraPivot, collisionRadius, cameraBackDir, out RaycastHit hit, cameraBackDistance, collisionMask))
        {
            finalPos = CorrectCameraPositionOnCollision(cameraRotation, hit);
        }

        //카메라의 포지션을 보간을 이용해 부드럽게 플레이어 추적
        transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * lerpSpeed);
        transform.rotation = cameraRotation;

        // 외부 제공 값 갱신
        CurrentView = new CameraView
        {
            pitch = pitch,
            yaw = yaw,
            forward = transform.forward,
            right = transform.right,
            up = transform.up
        };
    }

    // 카메라의 기본 TPS 오프셋(오른쪽 어깨 위에서 조금 뒤)
    Vector3 GetBaseCameraOffset()
    {
        return Vector3.up * cameraHeight
             + Vector3.back * cameraBackDistance
             + Vector3.right * cameraShoulderOffset;
    }

    // 벽, 천장 충돌 시 카메라 위치 보정(앞으로 당김)
    Vector3 CorrectCameraPositionOnCollision(Quaternion cameraRotation, RaycastHit hit)
    {
        // 벽까지 거리에서 buffer를 뺀 "가능한 뒤로 거리"
        float safeBack = Mathf.Max(hit.distance - collisionBuffer, 0.05f);

        // 카메라를 안정거리까지 당길수 있는 거리 계산
        Vector3 correctedOffset =
            Vector3.up * cameraHeight +
            Vector3.right * cameraShoulderOffset +
            Vector3.back * safeBack;

        //충돌시 당기는 거리 보정.
        return player.position + cameraRotation * correctedOffset;
    }

    // Transition직후 카메라 재위치(TransitionController.cs에서 사용)
    public void SnapAfterTransition()
    {
        if (!player) return;

        Quaternion cameraTargetRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 cameraTargetPosition = player.position + cameraTargetRotation * GetBaseCameraOffset();

        transform.SetPositionAndRotation(cameraTargetPosition, cameraTargetRotation);
    }
}
