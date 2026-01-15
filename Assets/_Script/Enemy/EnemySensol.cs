using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Enemy(1~5)에 붙이는 단일 컴포넌트:
/// - PlayerSensor가 계산한 센서 결과를 저장/노출 (cameraSeen/flashSeen/state/lostEvent/distance)
/// - EnemyController에서 현재 선택(pickedIndex)을 읽어 isPicked 세팅
/// - SceneView 라벨 출력
/// 
/// 중요: 센서 판정 로직은 PlayerSensor가 "유일하게" 담당한다.
/// EnemySensol은 계산하지 않는다(덮어쓰기/충돌 방지).
/// </summary>
public class EnemySensol : MonoBehaviour
{
    /* =========================================================
     *  Pick Debug (EnemyController 연동)
     * ========================================================= */
    [Header("Pick (optional)")]
    public EnemyController enemyController; // 비워두면 자동 탐색

    [Header("Pick Debug")]
    public bool isPicked;
    public int myIndex = -1;
    public int pickedIndex = -1;

    FieldInfo pickedIndexField;

    /* =========================================================
     *  Sensor Data (PlayerSensor가 써주는 값)
     * ========================================================= */
    public enum State { Strong, Weak, Blind }

    [Header("Sensor Data (Written by PlayerSensor)")]
    public bool flashSeen;        // PlayerSensor가 세팅
    public bool cameraSeen;       // PlayerSensor가 세팅
    public bool prevCameraSeen;   // PlayerSensor가 세팅/사용
    public bool lostEvent;        // PlayerSensor가 세팅
    public float distance;        // PlayerSensor가 세팅
    public State state = State.Blind; // PlayerSensor가 세팅

    /* =========================================================
     *  Label
     * ========================================================= */
    [Header("Label")]
    public float labelHeight = 4.5f; // 기본값을 위로 올림(원하면 인스펙터에서 조절)

    void Awake()
    {
        ResolveEnemyController();
        CacheReflection();
        UpdatePick();
    }

    void OnEnable()
    {
        ResolveEnemyController();
        CacheReflection();
        UpdatePick();
    }

    void Update()
    {
        // 센서 값은 PlayerSensor가 갱신한다.
        // 여기서는 Pick 상태만 갱신.
        UpdatePick();
    }

    /* =========================================================
     *  Pick
     * ========================================================= */
    void UpdatePick()
    {
        if (!enemyController)
        {
            ResolveEnemyController();
            CacheReflection();
        }

        if (!enemyController || enemyController.enemies == null)
        {
            isPicked = false;
            myIndex = -1;
            pickedIndex = -1;
            return;
        }

        myIndex = FindMyIndex(enemyController);
        pickedIndex = ReadPickedIndex(enemyController);
        isPicked = (myIndex >= 0 && pickedIndex >= 0 && myIndex == pickedIndex);
    }

    bool ResolveEnemyController()
    {
        if (enemyController) return true;
        enemyController = FindObjectOfType<EnemyController>();
        return enemyController != null;
    }

    void CacheReflection()
    {
        if (!enemyController) return;
        if (pickedIndexField != null) return;

        pickedIndexField = typeof(EnemyController).GetField(
            "pickedIndex",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (pickedIndexField == null)
            Debug.LogWarning("[EnemySensol] Cannot find private field 'pickedIndex' in EnemyController.", this);
    }

    int ReadPickedIndex(EnemyController controller)
    {
        if (!controller) return -1;
        if (!controller.HasPicked) return -1;
        if (pickedIndexField == null) return -1;

        object v = pickedIndexField.GetValue(controller);
        return (v is int i) ? i : -1;
    }

    int FindMyIndex(EnemyController controller)
    {
        for (int i = 0; i < controller.enemies.Length; i++)
        {
            var mb = controller.enemies[i];
            if (!mb) continue;
            if (mb.gameObject == gameObject) return i;
        }
        return -1;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 pos = transform.position + Vector3.up * labelHeight;

        string txt =
            $"[{name}]\n" +
            $"Picked: {isPicked} (my={myIndex}, cur={pickedIndex})\n" +
            $"State: {state}\n" +
            $"Flash: {flashSeen}\n" +
            $"Cam:   {cameraSeen}\n" +
            $"Lost:  {lostEvent}\n" +
            $"Dist:  {distance:F1}\n";

        Handles.Label(pos, txt);
    }
#endif
}
