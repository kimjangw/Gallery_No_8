using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemySensor : MonoBehaviour
{
    public enum State { Strong, Weak, Blind }

    [HideInInspector] public bool flashSeen;
    [HideInInspector] public bool cameraSeen;
    [HideInInspector] public bool prevCameraSeen;
    [HideInInspector] public bool lostEvent;
    [HideInInspector] public float distance;
    [HideInInspector] public State state = State.Blind;

    public float labelHeight = 2.0f;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 pos = transform.position + Vector3.up * labelHeight;

        string txt =
            $"[{name}]\n" +
            $"State: {state}\n" +
            $"Flash: {flashSeen}\n" +
            $"Cam:   {cameraSeen}\n" +
            $"Lost:  {lostEvent}\n" +
            $"Dist:  {distance:F1}\n";

        Handles.Label(pos, txt);
    }
#endif
}
