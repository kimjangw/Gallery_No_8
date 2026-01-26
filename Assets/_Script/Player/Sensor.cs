using UnityEngine;

public class PlayerSensor : MonoBehaviour
{
    [Header("References")]
    public CameraController cameraController;
    public Light flashlight;

    [Header("Scan Targets")]
    public Transform[] enemies;

    [Header("Settings")]
    public LayerMask obstacleMask;
    public float enterAngleDeg = 33f;
    public float exitAngleDeg = 55f;

    // 내부 변환값 (외부 수정 불가)
    private float sightDistance;

    void LateUpdate()
    {
        if (!flashlight || enemies == null) return;

        // 손전등 Range == 인식 거리
        sightDistance = flashlight.range;

        ScanAll();
    }

    void ScanAll()
    {
        Vector3 camPos = cameraController.transform.position;
        Vector3 camFwd = cameraController.transform.forward;
        Vector3 lightPos = flashlight.transform.position;
        Vector3 lightFwd = flashlight.transform.forward;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            EnemySensol  es = enemy.GetComponent<EnemySensol>();
            if (!es) continue;

            Vector3 ePos = enemy.position;
            Vector3 toE_cam = (ePos - camPos).normalized;
            Vector3 toE_light = ePos - lightPos;
            float dist = toE_light.magnitude;

            // 1. 기본 거리 
            bool inDistance = dist <= sightDistance;

            // 2. 카메라 기반 FOV (히스테리시스)
            float camAngle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(camFwd, toE_cam), -1, 1)) * Mathf.Rad2Deg;
            bool camSeenNow = camAngle <= enterAngleDeg;

            if (es.cameraSeen)
            {
                if (camAngle >= exitAngleDeg) es.cameraSeen = false;
            }
            else
            {
                if (camSeenNow) es.cameraSeen = true;
            }

            es.lostEvent = es.prevCameraSeen && !es.cameraSeen;
            es.prevCameraSeen = es.cameraSeen;

            // 3. Flash Strong 조건
            es.flashSeen = false;

            if (inDistance && es.cameraSeen) // Strong은 반드시 FOV 안
            {
                Vector3 dirFlash = toE_light.normalized;

                float dotLight = Vector3.Dot(lightFwd, dirFlash);
                bool inFront = dotLight > 0f;
                bool inCone = Mathf.Acos(dotLight) * Mathf.Rad2Deg <= flashlight.spotAngle * 0.5f;
                bool clear = !Physics.Raycast(lightPos, dirFlash, dist, obstacleMask);

                if (inFront && inCone && clear)
                    es.flashSeen = true;
            }

            // 상태 결정
            if (es.flashSeen) es.state = EnemySensol.State.Strong;
            else if (es.cameraSeen) es.state = EnemySensol.State.Weak;
            else es.state = EnemySensol.State.Blind;

            es.distance = dist;
        }
    }
}
