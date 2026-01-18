using UnityEngine;

// Enemy 인터페이스
public interface EnemyPattern
{
    void Ready();             // 선택된 ActionTrigger 밟기 전 대기
    void StartAction();       // ActionTrigger 신호 오면 행동 시작
    void Deactivate();        // 비활성
    void ResetEnemy();        // 강제 복귀/초기화
    void OnTransitionReset(); // Transition 직후 내부 정보 초기화
}

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Patterns")]
    public MonoBehaviour[] enemies;

    // -1 = 선택 없음, 0~ = enemies 인덱스
    public int pickedIndex = -1;

    // 선택 여부(외부에서 단순 체크용)
    public bool hasSelectedEnemy = false;

    // Loop 시작: hasEnemy면 Pick + Ready, 아니면 DisableAll
    public void SetupForLoop(bool hasEnemy)
    {
        if (!hasEnemy)
        {
            DisableAllEnemies();
            return;
        }

        PickEnemyForLoop();

        // Pick 결과가 유효할 때만 Ready
        if (hasSelectedEnemy)
            ReadySelectedEnemy();
    }

    // 선택된 Enemy를 "대기 상태"로 세팅
    public void ReadySelectedEnemy()
    {
        EnemyPattern selectedEnemy = GetSelectedEnemy();
        if (selectedEnemy == null) return;

        selectedEnemy.Ready();
    }

    // 선택된 Enemy의 실제 행동 시작
    public void StartSelectedEnemy()
    {
        EnemyPattern selectedEnemy = GetSelectedEnemy();
        if (selectedEnemy == null) return;

        selectedEnemy.StartAction();
    }

    /* =========================================================
     *  Bulk Controls
     * ========================================================= */

    // Kill/게임리셋 등: 모든 Enemy 강제 초기화 + 선택 해제
    public void ResetAllEnemies()
    {
        if (enemies != null)
        {
            for (int enemyIndex = 0; enemyIndex < enemies.Length; enemyIndex++)
            {
                if (enemies[enemyIndex] is EnemyPattern enemyPattern)
                    enemyPattern.ResetEnemy();
            }
        }

        ClearSelectedEnemy();
    }

    // Transition 직후: 내부 상태만 리셋(선택 유지)
    public void ResetEnemiesAfterTransition()
    {
        if (enemies == null) return;

        for (int enemyIndex = 0; enemyIndex < enemies.Length; enemyIndex++)
        {
            if (enemies[enemyIndex] is EnemyPattern enemyPattern)
                enemyPattern.OnTransitionReset();
        }

        // pickedIndex/hasSelectedEnemy 유지
    }

    // 모든 Enemy 비활성화 + 선택 해제
    public void DisableAllEnemies()
    {
        if (enemies != null)
        {
            for (int enemyIndex = 0; enemyIndex < enemies.Length; enemyIndex++)
            {
                if (enemies[enemyIndex] is EnemyPattern enemyPattern)
                    enemyPattern.Deactivate();
            }
        }

        ClearSelectedEnemy();
    }

    // 이번 루프에서 사용할 Enemy 1개 랜덤 선정
    public void PickEnemyForLoop()
    {
        pickedIndex = -1;
        hasSelectedEnemy = false;

        if (enemies == null || enemies.Length == 0) return;

        pickedIndex = Random.Range(0, enemies.Length);
        hasSelectedEnemy = true;
    }

    // Enemy선택 초기화
    public void ClearSelectedEnemy()
    {
        pickedIndex = -1;
        hasSelectedEnemy = false;
    }

    // pickedIndex 기반으로 선택된 EnemyPattern 반환
    EnemyPattern GetSelectedEnemy()
    {
        if (!hasSelectedEnemy) return null;
        if (enemies == null) return null;
        if (pickedIndex < 0 || pickedIndex >= enemies.Length) return null;

        return enemies[pickedIndex] as EnemyPattern;
    }
}
