using UnityEngine;

public class LoopManager : MonoBehaviour
{
    [Header("Loop State")]
    public bool isEnemyFlag = false; // 이번 루프의 몬스터 존재 여부
    public int floor = 0;            // 현재 층수

    [Header("End Condition")]
    public int clearFloor = 8;

    [Header("Refs")]
    public EnemyController enemyController; // 루프 중 Enemy 제어를 위해 연결

    //화면의 층수 연동
    [Header("Floor Display")]
    public FloorDisplay floorDisplayA;
    public FloorDisplay floorDisplayB;

    //판정라인 연동
    [Header("FixLines")]
    public FixLine fixLineA;
    public FixLine fixLineB;

    //ActionTrigger
    [Header("ActionTriggers (3)")]
    public ActionTrigger[] actionTriggers;


    [Header("Player (Kill Handling)")]
    public PlayerController playerController;

    bool isKilling = false;


    //Fix확인용 변수
    public bool fixCommitted = false;
    //어느쪽 Fix인지 확인
    bool fixedSideA = false;


    [Header("Kill Test (Coroutine)")]
    public Transform playerRoot;          // 플레이어 루트 Transform(위치/회전 복귀용)
    public float restartDelay = 2.0f;     // 테스트용 대기 시간(나중에 버튼으로 교체)
    public DeathUIController deathUI;


    Vector3 playerSpawnPos;
    Quaternion playerSpawnRot;

    void Awake()
    {
        if (playerRoot == null && playerController != null)
            playerRoot = playerController.transform;

        if (playerRoot != null)
        {
            playerSpawnPos = playerRoot.position;
            playerSpawnRot = playerRoot.rotation;
        }
    }


    public void OnFix(bool sideA)
    {
        //이미 Fix되어있으면 2번은 작업하지 않음.
        if (fixCommitted) return;

        fixCommitted = true;
        fixedSideA = sideA;

        Debug.Log("[FIX] Fix위치=" + (sideA ? "A측" : "B측"));
    }

    public void OnTransition(TransitionHub transHub)
    {
        Debug.Log("[LOOP] OnTransition called. floor=" + floor + ", fixCommitted=" + fixCommitted);

        if (playerController != null && playerController.isDead) return;

        if (floor == 0)
        {
            fixCommitted = false;
            fixedSideA = false;

            floor = 1;
            NotifyFloorChanged(); // 층수 표시 초기화
            isEnemy();            // 패턴 존재 유무 세팅
            UpdateEnemyState();   // 패턴존재에 다른 Enemy세팅
            PickActionTrigger();  // ActionTrigger 선택
            return;
        }

        //Fix전 다시 Transition시 새로운 Enemy세팅
        if (!fixCommitted)
        {
            isEnemy();          // 패턴 존재 유무 세팅
            UpdateEnemyState(); // 패턴존재에 다른 Enemy세팅
            PickActionTrigger();// ActionTrigger 선택
            return;
        }

        //Fix된 위치 확인 변수(몬스터 O -> 나온곳, 몬스터 X ->반대편)
        bool usedSideA = transHub.sideA;
        bool usedFix = (usedSideA == fixedSideA);
        bool correct = isEnemyFlag ? usedFix : !usedFix;

        //정답 오답에 따른 층수 판정
        if (correct) floor++;
        else floor = 1;


        // 클리어시 EndScene으로
        if (floor > clearFloor)
        {
            SceneLoader.Instance.LoadEnd();
            return;
        }


        NotifyFloorChanged();   // 층 수 초기화

        fixCommitted = false;   // Fix초기화

        isEnemy();              // 정답 판정 후 새로운 Loop몬스터 존재유무
        UpdateEnemyState();     // 패턴존재에 다른 Enemy세팅
        PickActionTrigger();    // ActionTrigger 선택
    }

    // 패턴 존재 유무 세팅
    void isEnemy()
    {
        if (floor == 0)
        {
            isEnemyFlag = false;
            return;
        }

        //적 존재 유무
        isEnemyFlag = Random.value < 0.5f;
        Debug.Log("[패턴] " + floor + "층 → Enemy=" + (isEnemyFlag ? "있음" : "없음"));
    }

    // 패턴 존재 유무에 따라 Enemy세팅 or 전체 비활성화.
    void UpdateEnemyState()
    {
        if (enemyController == null) return;

        enemyController.SetupForLoop(isEnemyFlag);
    }

    //층수 변경 적용 함수
    void NotifyFloorChanged()
    {
        if (floorDisplayA != null)
            floorDisplayA.SetFloor(floor);

        if (floorDisplayB != null)
            floorDisplayB.SetFloor(floor);
    }

    //LoopManager에서 전환시 다른 함수들에 초기화를 중계
    public void TransitionReset()
    {
        if(playerController != null && playerController.isDead) return;

        // Fix라인 Lock해제.
        if (fixLineA != null) fixLineA.ResetFix();
        if (fixLineB != null) fixLineB.ResetFix();



        if (enemyController != null)
            enemyController.ResetEnemiesAfterTransition();

        if (actionTriggers != null)
        {
            for (int actionTriggerCount = 0; actionTriggerCount < actionTriggers.Length; actionTriggerCount++)
                if (actionTriggers[actionTriggerCount] != null)
                    actionTriggers[actionTriggerCount].ActionTriggerUnlock();
        }

    }

    // Enemy에의한 사망시 쓰이는 함수.
    public void OnEnemyKill()
    {
        if (isKilling) return; // 중복 Kill 방지
        isKilling = true;

        Debug.Log("[LOOP] Kill 발생 → Loop Reset");

        // 1) Player 연출/입력락
        if (playerController != null)
            playerController.Die();

        // 2) Enemy 초기화
        if (enemyController != null)
            enemyController.ResetAllEnemies();

        // 3) 판정 상태 초기화(다음 루프 꼬임 방지)
        fixCommitted = false;
        fixedSideA = false;

        // 4) 트리거는 전부 잠그는 게 안전 (죽은 상태에서 밟아도 발동 안하게)
        if (actionTriggers != null)
        {
            for (int actionTriggerCount = 0; actionTriggerCount < actionTriggers.Length; actionTriggerCount++)
                if (actionTriggers[actionTriggerCount] != null)
                    actionTriggers[actionTriggerCount].ActionTriggerLock();
        }

        // 5) UI 표시(버튼 대기)
        if (deathUI != null)
            deathUI.Show();
    }


    public void RestartAfterKill()
    {
        if (playerRoot != null)
        {
            CharacterController playerCC = playerRoot.GetComponent<CharacterController>();
            bool wasEnabled = false;

            if (playerCC != null)
            {
                wasEnabled = playerCC.enabled;
                playerCC.enabled = false;
            }

            playerRoot.SetPositionAndRotation(playerSpawnPos, playerSpawnRot);

            if (playerCC != null)
                playerCC.enabled = wasEnabled;
        }



        if (actionTriggers != null)
        {
            for (int actionTriggerCount = 0; actionTriggerCount < actionTriggers.Length; actionTriggerCount++)
                if (actionTriggers[actionTriggerCount] != null)
                    actionTriggers[actionTriggerCount].ActionTriggerUnlock();
        }


        if (playerController != null) playerController.Revive();

        // 상태 초기화
        fixCommitted = false;
        fixedSideA = false;

        // 1층부터 재시작
        floor = 1;
        NotifyFloorChanged();

        // 새 루프 패턴 세팅
        isEnemy();
        UpdateEnemyState();
        PickActionTrigger();

        isKilling = false; // Kill 가드 해제

    }

    //범용 Enemy리셋 함수
    public void OnEnemyEnd()
    {
        Debug.Log("[LOOP] 패턴 종료 → Loop 진행");

        if (enemyController != null)
            enemyController.ResetAllEnemies();
    }

    void PickActionTrigger()
    {
        // Enemy가 없으면 트리거는 전부 잠가두는 쪽이 안전
        if (actionTriggers == null || actionTriggers.Length == 0) return;

        // 전부 Lock
        for (int actionTriggerCount = 0; actionTriggerCount < actionTriggers.Length; actionTriggerCount++)
        {
            if (actionTriggers[actionTriggerCount] != null)
                actionTriggers[actionTriggerCount].ActionTriggerLock();
        }

        // Enemy가 있을 때만 1개를 열어줌
        if (!isEnemyFlag) return;

        int selectActionTrigger = Random.Range(0, actionTriggers.Length);
        if (actionTriggers[selectActionTrigger] != null)
            actionTriggers[selectActionTrigger].ActionTriggerUnlock();
    }

}
