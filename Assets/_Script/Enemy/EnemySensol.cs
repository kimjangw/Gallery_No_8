using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemySensol : MonoBehaviour
{
    public Sensor sensor;   // 같은 Enemy가 들고있는 Sensor 연결

    [Header("Label Offset")]
    public float heightOffset = 2.0f;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || sensor == null)
            return;

        Vector3 pos = transform.position + Vector3.up * heightOffset;

        string text =
            $"[{name}]\n" +
            $"State: {sensor.state}\n" +
            $"Flash: {sensor.flashSeen}\n" +
            $"Cam:   {sensor.cameraSeen_hyst}\n" +
            $"Lost:  {sensor.lostEvent}\n";

        Handles.Label(pos, text);
    }
#endif
}
