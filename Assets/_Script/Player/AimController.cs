using UnityEngine;

public class AimController : MonoBehaviour
{
    //인스펙터 창을 통해 연결
    [Header("References")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform flashlight;      // 손전등 (회전만)
    [SerializeField] private Transform headAimTarget;   // Head Multi-Aim Target

    //손전등 에임 세팅
    [Header("Light Aim")]
    public float lightPitchMultiplier = 1f;
    public float lightMinPitch = -25f;
    public float lightMaxPitch = 25f;
    public float lightSmooth = 8f;

    //Light 쪽 쳐다보기 위한 머리 에임 세팅.
    [Header("Head Aim")]
    public float headPitchMultiplier = 1f;
    public float headSmooth = 6f;
    public float headAimDistance = 1.5f;

    //Base 정보 보관
    Quaternion baselightRotation;
    Vector3 baseHeadAimLocalPos;


    void Start()
    {
        //최초 정보 초기화
        if (flashlight != null)
            baselightRotation = flashlight.localRotation;
        if (headAimTarget != null)
            baseHeadAimLocalPos = headAimTarget.localPosition;
    }

    void LateUpdate()
    {
        if (cameraController == null) return;
        
        //CameraController.cs에서 카메라 각도 호출
        float camPitch = cameraController.CurrentView.pitch;

        //손전등과 머리 각도 에임 지속 연출.
        ApplylightAim(camPitch);
        ApplyHeadAim(camPitch);
    }

    //손전등 각도 조정
    void ApplylightAim(float camPitch)
    {
        if (flashlight == null) return;

        //손전등 불빛 최소, 최대 각도 제한
        float pitchOffset = Mathf.Clamp(camPitch * lightPitchMultiplier, lightMinPitch, lightMaxPitch);
        
        //계산된 각도를 baselightRotation에 곱해 목표 각도를 구하고.
        Quaternion targetRotation = baselightRotation * Quaternion.Euler(-pitchOffset, 0f, 0f);
        
        //Slerp구면 보간을 통해서목표 위치까지 Smooth*시간동안 이동.
        flashlight.localRotation = Quaternion.Slerp(flashlight.localRotation, targetRotation,Time.deltaTime * lightSmooth);
    }

    //머리 각도 조정
    void ApplyHeadAim(float camPitch)
    {
        if (headAimTarget == null) return;

        //현재Animation Rigging을 통해서 Multi-Aim Constranint를 통해 머리 각도 조정을 진행 중 이다.
        //기본 헤드 Aim + 일정거리+ 각도 값을 계산.
        Vector3 targetLocalPos = baseHeadAimLocalPos + Vector3.forward * headAimDistance + Vector3.up * (-camPitch * headPitchMultiplier * 0.01f);
        //타겟을 머리 앞 직선거리 targetLocalPos만큼 앞에 두고 계속 이동시켜 손전등 불빛을 바라보는 연출.
        headAimTarget.localPosition = Vector3.Lerp( headAimTarget.localPosition,targetLocalPos,Time.deltaTime * headSmooth);
    }
}
