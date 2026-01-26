using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathUIController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;

    [Header("Refs")]
    public LoopManager loopManager;

    [Header("Scene")]
    public string mainSceneName = "Title"; // 빌드 세팅의 타이틀 씬 이름/경로에 맞게

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // LoopManager에서 죽었을 때 호출
    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Reset 버튼
    public void OnClickRestart()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (loopManager != null)
            loopManager.RestartAfterKill();
    }

    // Main 버튼
    public void OnClickMain()
    {
        // 타이틀로 나갈 땐 마우스는 보이게 두는 게 보통 안전
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainSceneName);
        // SceneLoader를 쓰고 있으면 여기만 SceneLoader.Instance.LoadScene(mainSceneName)로 교체
    }
}
