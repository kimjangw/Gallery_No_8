using UnityEngine;

public class Sensor : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public CameraController cameraController;
    public Light flashlight;

    [Header("Settings")]
    public float maxSightDistance = 20f;
    public LayerMask obstacleMask;

    [Header("Camera Hysteresis (Degrees)")]
    public float enterAngleDeg = 45f;
    public float exitAngleDeg = 60f;

    [Header("Outputs (Read Only)")]
    public bool flashSeen;
    public bool cameraSeen_hyst;
    public bool lostEvent;
    public bool existsInFOV;       // 요구 3번
    public VisState state;
    public float distancePlayer;
    public float distanceFlash;

    public enum VisState { Strong, Weak, Blind }

    bool prevCameraSeen;

    void LateUpdate()
    {
        ComputeSensor();
    }

    void ComputeSensor()
    {
        if (player == null || cameraController == null || flashlight == null)
        {
            ResetSensor();
            return;
        }

        Vector3 pPos = player.position;
        Vector3 ePos = transform.position;
        Vector3 fPos = flashlight.transform.position;

        distancePlayer = Vector3.Distance(pPos, ePos);
        distanceFlash = Vector3.Distance(fPos, ePos);

        if (distancePlayer > maxSightDistance)
        {
            ResetSensor();
            return;
        }

        //------------------------------------------------
        // FLASH (Strong)
        //------------------------------------------------
        flashSeen = false;

        Vector3 dirF = (ePos - fPos).normalized;
        float angF = Vector3.Angle(flashlight.transform.forward, dirF);

        if (angF <= flashlight.spotAngle * 0.5f && distanceFlash <= flashlight.range)
        {
            if (!Physics.Raycast(fPos, dirF, distanceFlash, obstacleMask))
                flashSeen = true;
        }

        //------------------------------------------------
        // CAMERA FOV (Yaw+Pitch)
        //------------------------------------------------
        Vector3 camPos = cameraController.transform.position;
        Vector3 camFwd = cameraController.transform.forward;
        Vector3 toEnemy = (ePos - camPos).normalized;

        float viewDot = Vector3.Dot(camFwd, toEnemy);
        float viewAngle = Mathf.Acos(Mathf.Clamp(viewDot, -1f, 1f)) * Mathf.Rad2Deg;

        bool camEnter = viewAngle <= enterAngleDeg;
        bool camExit = viewAngle >= exitAngleDeg;

        if (cameraSeen_hyst)
        {
            if (camExit) cameraSeen_hyst = false;
        }
        else
        {
            if (camEnter) cameraSeen_hyst = true;
        }

        //------------------------------------------------
        // LOST EVENT
        //------------------------------------------------
        lostEvent = prevCameraSeen && !cameraSeen_hyst;
        prevCameraSeen = cameraSeen_hyst;

        //------------------------------------------------
        // 존재판정(요구 3번)
        //------------------------------------------------
        existsInFOV = (viewAngle <= enterAngleDeg && distancePlayer <= maxSightDistance);

        //------------------------------------------------
        // FINAL STATE
        //------------------------------------------------
        if (flashSeen) state = VisState.Strong;
        else if (cameraSeen_hyst) state = VisState.Weak;
        else state = VisState.Blind;
    }

    void ResetSensor()
    {
        flashSeen = false;
        cameraSeen_hyst = false;
        lostEvent = false;
        prevCameraSeen = false;
        existsInFOV = false;
        state = VisState.Blind;
        distancePlayer = 0;
        distanceFlash = 0;
    }

    //------------------------------------------------
    // FOV BOX DRAW (요구 2번)
    //------------------------------------------------
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || cameraController == null) return;

        Vector3 camPos = cameraController.transform.position;
        Vector3 camFwd = cameraController.transform.forward;
        Vector3 camRight = cameraController.transform.right;
        Vector3 camUp = cameraController.transform.up;

        float dist = maxSightDistance;
        float half = Mathf.Tan(enterAngleDeg * Mathf.Deg2Rad) * dist;

        Vector3 center = camPos + camFwd * dist;

        Vector3 p1 = center + camRight * half + camUp * half;
        Vector3 p2 = center + camRight * half - camUp * half;
        Vector3 p3 = center - camRight * half - camUp * half;
        Vector3 p4 = center - camRight * half + camUp * half;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(camPos, p1);
        Gizmos.DrawLine(camPos, p2);
        Gizmos.DrawLine(camPos, p3);
        Gizmos.DrawLine(camPos, p4);

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
}
