using UnityEngine;

public class LoopManager : MonoBehaviour
{
    [Header("Pattern State")]
    public bool hasMonster = false;
    public int floor = 0;

    [Header("Refs")]
    public EnemyController enemyController;

    [Header("Floor Display (Inspector Assign)")]
    public FloorDisplay floorDisplayA;
    public FloorDisplay floorDisplayB;

    [Header("FixLines (Inspector Assign)")]
    public FixLine fixLineA;
    public FixLine fixLineB;

    bool fixCommitted = false;
    public bool FixCommitted { get { return fixCommitted; } }

    bool fixedSideA = false;

    public void OnFix(bool sideA)
    {
        if (fixCommitted) return;

        fixCommitted = true;
        fixedSideA = sideA;

        Debug.Log("[FIX] Fix위치=" + (sideA ? "A측" : "B측"));
    }

    public void OnTransition(TransitionHub transHub)
    {
        if (floor == 0)
        {
            floor = 1;
            SetupPattern();
            NotifyFloorChanged();
            ApplyEnemyByPattern();
            return;
        }

        if (!fixCommitted)
        {
            SetupPattern();
            ApplyEnemyByPattern();
            return;
        }

        bool usedSideA = transHub.sideA;
        bool usedFix = (usedSideA == fixedSideA);
        bool correct = hasMonster ? usedFix : !usedFix;

        if (correct) floor++;
        else floor = 1;

        NotifyFloorChanged();

        fixCommitted = false;

        SetupPattern();
        ApplyEnemyByPattern();
    }

    void NotifyFloorChanged()
    {
        // Display A
        if (floorDisplayA != null)
            floorDisplayA.SetFloor(floor);

        // Display B
        if (floorDisplayB != null)
            floorDisplayB.SetFloor(floor);
    }

    void ApplyEnemyByPattern()
    {
        if (enemyController == null) return;

        if (hasMonster) enemyController.ActivateOne();
        else enemyController.DeactivateAll();
    }

    public void ResetFixLine()
    {
        if (fixLineA != null) fixLineA.ResetFix();
        if (fixLineB != null) fixLineB.ResetFix();

        fixCommitted = false;
    }

    void SetupPattern()
    {
        if (floor == 0)
        {
            hasMonster = false;
            return;
        }

        hasMonster = Random.value < 0.5f;
        Debug.Log("[패턴] " + floor + "층 → 몬스터=" + (hasMonster ? "있음" : "없음"));
    }

    public void OnEnemyKill()
    {
        Debug.Log("[LOOP] Kill 발생 → Loop Reset");

        if (enemyController != null)
            enemyController.ResetAll();

        floor = 1;
        SetupPattern();
        NotifyFloorChanged();
        ApplyEnemyByPattern();
    }

    public void OnEnemyEnd()
    {
        Debug.Log("[LOOP] 패턴 종료 → Loop 진행");

        if (enemyController != null)
            enemyController.ResetAll();
    }

    public void AfterTransitionReset()
    {
        ResetFixLine();

        if (enemyController != null)
            enemyController.OnTransitionResetAll();
    }
}
