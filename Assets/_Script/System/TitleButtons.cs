using UnityEngine;

public class TitleButtons : MonoBehaviour
{
    [Header("Target Scene")]
    public string gameSceneName = "GameScene";

    [Header("Panels")]
    public GameObject soundPanel;   // 사운드 옵션 패널(슬라이더 들어있는 Panel) 드래그

    public void OnClickGameStart()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();       
             SceneLoader.Instance.LoadScene(gameSceneName);
        }

    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===== Sound Button: Panel Toggle =====
    public void OnClickSoundPanelToggle()
    {
        if (soundPanel == null)
        {
            Debug.LogWarning("[TitleButtons] soundPanel is null.");
            return;
        }

        soundPanel.SetActive(!soundPanel.activeSelf);
    }
}
