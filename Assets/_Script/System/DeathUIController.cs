using UnityEngine;

public class DeathUIController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot; // Panel 오브젝트

    [Header("Refs")]
    public LoopManager loopManager;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // LoopManager에서 죽었을 때 호출
    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        // 마우스 조작 필요하면 켜기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Restart 버튼에서 호출
    public void OnClickRestart()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        // 마우스 다시 잠금(게임 방식에 맞춰 조절)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (loopManager != null)
            loopManager.RestartAfterKill();
    }
}
