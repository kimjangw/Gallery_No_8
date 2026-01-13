using UnityEngine;

public class CurtainPair : MonoBehaviour
{
    public SkinnedMeshRenderer original;
    public Transform cloneTransform; // 위치만 담당
    Mesh baked;

    void Awake()
    {
        baked = new Mesh();
        baked.MarkDynamic();
    }

    void LateUpdate()
    {
        original.BakeMesh(baked);

        // Render simulation cloth mesh on clone position
        Graphics.DrawMesh(
            baked,
            cloneTransform.localToWorldMatrix,
            original.sharedMaterial,
            0
        );
    }
}
