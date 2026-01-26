using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleEndingCredits : MonoBehaviour
{
    [Header("UI")]
    public RectTransform viewport; // Panel
    public RectTransform content;  // Content

    [Header("Scroll")]
    public float speed = 120f;        // px/sec
    public float startPadding = 200f; // 시작 아래 여유
    public float endPadding = 800f;   // 종료 위 여유

    [Header("Finish")]
    public string mainSceneName = "Title";

    float endY;
    bool isPlaying;

    void OnEnable()
    {
        StartCoroutine(InitAndPlay());
    }

    IEnumerator InitAndPlay()
    {
        // Layout 안정화
        yield return null;
        Canvas.ForceUpdateCanvases();

        float viewportHeight = viewport.rect.height;
        float contentHeight = content.rect.height;

        // content 피벗이 Top(0.5,1)인 상태 기준으로 계산
        float startY = -viewportHeight - startPadding;
        endY = contentHeight + endPadding;

        Vector2 pos = content.anchoredPosition;
        pos.y = startY;
        content.anchoredPosition = pos;

        isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying) return;

        Vector2 pos = content.anchoredPosition;
        pos.y += speed * Time.deltaTime;
        content.anchoredPosition = pos;

        if (pos.y >= endY)
        {
            isPlaying = false;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            SceneManager.LoadScene(mainSceneName);
        }
    }
}
