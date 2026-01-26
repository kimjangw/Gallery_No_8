using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 디버그/참조용 컴포넌트:
/// - EnemyController의 pickedIndex/HasSelectedEnemy를 읽어 현재 내가 선택된 Enemy인지 표시
/// - PlayerSensor가 써주는 센서 데이터(flashSeen/cameraSeen/state 등)를 SceneView에 라벨로 출력
///
/// 중요: 게임 로직에 영향 주지 않음(읽기 전용)
/// </summary>
public class EnemySensol : MonoBehaviour
{
    /* =========================================================
     *  EnemyController Pick Debug
     * ========================================================= */
    [Header("Pick Debug")]
    public EnemyController enemyController; // 비워두면 자동 탐색

    public bool isPicked;
    public int myIndex = -1;
    public int pickedIndex = -1;

    // 캐시: enemies 배열이 바뀌지 않는 한 myIndex는 고정
    MonoBehaviour[] cachedEnemiesArray;

    /* =========================================================
     *  Sensor Data (PlayerSensor가 써주는 값)
     * ========================================================= */
    public enum State { Strong, Weak, Blind }

    [Header("Sensor Data (Written by PlayerSensor)")]
    public bool flashSeen;
    public bool cameraSeen;
    public bool prevCameraSeen;
    public bool lostEvent;
    public float distance;
    public State state = State.Blind;

    /* =========================================================
     *  Label
     * ========================================================= */
    [Header("Label")]
    public float labelHeight = 4.5f;

    void Awake()
    {
        ResolveEnemyController();
        CacheMyIndexIfNeeded();
        UpdatePick();
    }

    void OnEnable()
    {
        ResolveEnemyController();
        CacheMyIndexIfNeeded();
        UpdatePick();
    }

    void Update()
    {
        // 센서 데이터는 PlayerSensor가 갱신한다고 가정.
        // 여기서는 선택 상태만 갱신.
        ResolveEnemyController();
        CacheMyIndexIfNeeded();
        UpdatePick();
    }

    /* =========================================================
     *  Internal
     * ========================================================= */

    void ResolveEnemyController()
    {
        if (enemyController != null) return;
        enemyController = FindObjectOfType<EnemyController>();
    }

    void CacheMyIndexIfNeeded()
    {
        if (enemyController == null)
        {
            cachedEnemiesArray = null;
            myIndex = -1;
            return;
        }

        // enemies 배열 참조가 바뀌었을 때만 다시 찾기
        if (ReferenceEquals(cachedEnemiesArray, enemyController.enemies))
            return;

        cachedEnemiesArray = enemyController.enemies;
        myIndex = FindMyIndex(enemyController);
    }

    void UpdatePick()
    {
        if (enemyController == null || enemyController.enemies == null || enemyController.enemies.Length == 0)
        {
            pickedIndex = -1;
            isPicked = false;
            return;
        }

        // EnemyController가 제공하는 기준으로 pickedIndex 읽기
        if (!enemyController.hasSelectedEnemy)
        {
            pickedIndex = -1;
            isPicked = false;
            return;
        }

        pickedIndex = enemyController.pickedIndex;

        // myIndex가 아직 없으면(비정상) 한번 더 계산
        if (myIndex < 0) myIndex = FindMyIndex(enemyController);

        isPicked = (myIndex >= 0 && pickedIndex >= 0 && myIndex == pickedIndex);
    }

    int FindMyIndex(EnemyController controller)
    {
        if (controller == null || controller.enemies == null) return -1;

        for (int i = 0; i < controller.enemies.Length; i++)
        {
            var mb = controller.enemies[i];
            if (mb == null) continue;

            // EnemyController.enemies에 들어간 MonoBehaviour가 "같은 게임오브젝트"에 붙어있는 경우를 기본 전제로 함
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
          $"Picked: {isPicked} (my={myIndex}, cur={pickedIndex})\n" +  // 타겟 선정 정보 (내 Index, 선택 Index)
          $"State: {state}\n" +                                        // 현재 인식 상태
          $"Flash: {flashSeen}\n" +                                    // 손전등(Flashlight) 감지 여부
          $"Cam:   {cameraSeen}\n" +                                   // 카메라 시야(FOV)에 인지되었는지 여부
          $"Lost:  {lostEvent}\n" +                                    // 인지 → 손실 전환 이벤트 발생 여부
          $"Dist:  {distance:F1}\n";                                   // Player와 거리


        Handles.Label(pos, txt);
    }
#endif
}
