using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    // 기준이 될 플레이어 Transform
    public Transform player;

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

    // 벽뚫기 방지(충돌 보정)
    [Header("Camera Collision")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.01f;
    public float collisionBuffer = 1f;

    // [CHANGE] 항상 빠르게 붙도록 고정 (인스펙터 노출 X)
    // - 100이면 거의 즉시 붙는 느낌
    [SerializeField] private float lerpSpeed = 100f;

    // 플레이어 좌/우 회전(yaw), 카메라 상/하(pitch)
    public float yaw;
    float pitch = 10f;

    void Start()
    {
        // 마우스 중앙 고정 및 감추기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 플레이어 최초 yaw 세팅
        if (player) yaw = player.eulerAngles.y;

        // 기본 마스크 세팅(필요하면 인스펙터에서 덮어써도 됨)
        collisionMask = LayerMask.GetMask("Wall", "Ceiling");
    }

    void LateUpdate()
    {
        if (!player) return;

        // 마우스 입력
        Vector2 mouse = Mouse.current.delta.ReadValue();

        // yaw/pitch 갱신
        yaw += mouse.x * sensitivity * Time.deltaTime;
        pitch -= mouse.y * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 플레이어 회전 적용
        player.rotation = Quaternion.Euler(0f, yaw, 0f);

        // 카메라 회전
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

        // 카메라 오프셋(등 뒤 + 어깨)
        Vector3 offset =
            Vector3.up * cameraHeight +
            Vector3.back * cameraBackDistance +
            Vector3.right * cameraShoulderOffset;

        // 기본 목표 위치
        Vector3 cameraNormalPos = player.position + cameraRotation * offset;

        // 충돌 체크 기준
        Vector3 cameraPivot = player.position + Vector3.up * cameraHeight;
        Vector3 cameraBackDir = cameraRotation * Vector3.back;

        Vector3 finalPos = cameraNormalPos;

        // SphereCast로 벽/천장 충돌 시 앞으로 당김
        if (Physics.SphereCast(cameraPivot, collisionRadius, cameraBackDir,
            out RaycastHit hit, cameraBackDistance, collisionMask))
        {
            finalPos = CorrectCameraPositionOnCollision(cameraPivot, cameraBackDir, hit);
        }

        // [CHANGE] 항상 빠른 Lerp로 추종(전환/평상시 동일)
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

    // 벽/천장 충돌 시 카메라 위치 보정(앞으로 당김)
    Vector3 CorrectCameraPositionOnCollision(Vector3 cameraPivot, Vector3 cameraBackDir, RaycastHit hit)
    {
        float safeDist = Mathf.Max(hit.distance - collisionBuffer, 0.05f);

        // 보정 위치 계산
        Vector3 correctedPos = cameraPivot + cameraBackDir * safeDist;

        // [권장] Y는 pivot 기준으로 고정(전환 직후 흔들림 완화)
        correctedPos.y = cameraPivot.y;

        return correctedPos;
    }

    // 전환 직후 “즉시” 맞추고 싶으면 호출(선택)
    // - 항상 100으로 붙을 거면 사실 없어도 됨.
    public void SnapToPlayerInstant()
    {
        if (!player) return;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 offset =
            Vector3.up * cameraHeight +
            Vector3.back * cameraBackDistance +
            Vector3.right * cameraShoulderOffset;

        transform.SetPositionAndRotation(player.position + rot * offset, rot);
    }
}
