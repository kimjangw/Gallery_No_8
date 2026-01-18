using UnityEngine;

public class CurtainPair : MonoBehaviour
{
    public SkinnedMeshRenderer original; //오리지널 커튼
    public Transform cloneTransform; // 위치만 담당
    Mesh baked; //클론의 메쉬용 baked변수 생성

    void Awake()
    {
        baked = new Mesh();//객체 생성
        baked.MarkDynamic();//복제 메쉬 최적화
    }

    void LateUpdate()
    {
        original.BakeMesh(baked); //오리지널의 값을 클론에 복사

        //복사된 값 클론에 그대로 그리기
        Graphics.DrawMesh(baked,cloneTransform.localToWorldMatrix,original.sharedMaterial, 0);
    }
}
