using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    //기준이될 플레이어 Transform
    public Transform player;

    //카메라 측에서 보는 값을 다른 스크립트로 전달.
    public CameraView CurrentView { get; private set; }
    public struct CameraView
    {
        public float pitch;
        public float yaw;
        public Vector3 forward;
        public Vector3 right;
        public Vector3 up;
    }

    //TPS구현을 위한 카메라의 등뒤 위치 세팅.
    [Header("CameraOffset")]
    public float cameraBackDistance = 1.1f;        // 뒤
    public float cameraHeight = 1.12f;          // 위
    public float cameraShoulderOffset = 0.2f;  // 우측 어깨

    //민감도 조절 및 최대 시야각(위,아래)조절
    [Header("CameraRotation")]
    public float sensitivity = 20f;
    public float minPitch = -25f;
    public float maxPitch = 25f;

    //카메라 벽뚫기 관련 충돌 제어
    [Header("Camera Collision")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.01f;
    public float collisionBuffer = 1f;
    public float cameraLerpSpeed = 12f;

    //플레이어 좌,우 회전 변수
    public float yaw;
    //카메라 위, 아래 각도 변수
    float pitch = 10f;

    // 보간 임시 비활성 모드 플래그 추가
    bool snapMode = false;


    void Start()
    {
        //마우스 중앙 고정 및 감추기.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //플레이어 최초 Y값 세팅
        yaw = player.eulerAngles.y;
        collisionMask = LayerMask.GetMask("Wall", "Ceiling");
    }

    //카메라 흔들림 제어를 위해 update다음 실행되는 LateUpde 이용
    void LateUpdate()
    {
        //마우스의 현재 값 읽기
        Vector2 mouse = Mouse.current.delta.ReadValue();


        //마우스 X좌표 이동 -> yaw제어(캐릭터 좌우 회전값)
        yaw += mouse.x * sensitivity * Time.deltaTime;
        //마우스 Y좌표이동 -> Pitch제어(캐릭터 상하 회전값)
        pitch -= mouse.y * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        //마우스 좌우 회전값 적용
        player.rotation = Quaternion.Euler(0f, yaw, 0f);
        // 카메라 각도 계산
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

        // 카메라 등뒤 위치 계산.
        Vector3 offset = Vector3.up * cameraHeight + Vector3.back * cameraBackDistance + Vector3.right * cameraShoulderOffset;

        // 기본 위치 저장
        Vector3 cameraNormalPos = player.position + cameraRotation * offset;

        // ===== 충돌 체크용 기준 =====
        Vector3 cameraPivot = player.position + Vector3.up * cameraHeight;
        Vector3 cameraBackDir = cameraRotation * Vector3.back;

        Vector3 finalPos = cameraNormalPos;

        // 카메라에 구형레이를 넣어 마스크 목록에 있는 것과 충돌 체크
        if (Physics.SphereCast(cameraPivot, collisionRadius, cameraBackDir, out RaycastHit hit, cameraBackDistance, collisionMask))
        {
            //벽, 천장에 닿았을 때만 함수 실행
            finalPos = CorrectCameraPositionOnCollision(cameraPivot, cameraBackDir, hit);
        }

        //스냅 여부 최종 카메라 위치 조정 
        if (snapMode)
        {
            //Transition을 통한 위치 이동시 위치 강제 이동
            transform.position = finalPos;
        }
        else
        {   //기존 Lerp보간을 이용한 부드러운 카메라 연출.
            transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * cameraLerpSpeed); // 기존 보간
        }

        transform.rotation = cameraRotation;

        //다른 스크립트로 보낼 변수 새로 초기화.
        CurrentView = new CameraView
        {
            pitch = pitch,
            yaw = yaw,
            forward = transform.forward,
            right = transform.right,
            up = transform.up
        };

    }

    // 마스크(벽,천장) 충돌시 시 Z축 보정 전용 함수
    Vector3 CorrectCameraPositionOnCollision(Vector3 cameraPivot, Vector3 cameraBackDir, RaycastHit hit)
    {
        float safeDist = Mathf.Max(hit.distance - collisionBuffer, 0.05f);

        //카메라 보정위치 계산
        Vector3 correctedPos = cameraPivot + cameraBackDir * safeDist;
        //Y축은 고정(앞뒤로만)
        correctedPos.y = transform.position.y;

        return correctedPos;
    }

    //전환부 이동시 카메라 보간 OFF하고 직접 초기화. (TransitionController.cs에서 호출)
    public void SnapToPlayerInstant()
    {

        yaw = player.eulerAngles.y;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);

        Vector3 offset =
            Vector3.up * cameraHeight +
            Vector3.back * cameraBackDistance +
            Vector3.right * cameraShoulderOffset;

        transform.rotation = rot;
        transform.position = player.position + rot * offset;
        snapMode = true;
    }

    // snapMode 해제용 (TransitionController.cs에서 호출)
    public void ReleaseSnap()
    {
        snapMode = false;
    }

}
