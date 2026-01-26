using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환을 관리하는 싱글톤 매니저.
/// 다른 스크립트에서 SceneLoader.Instance로 접근 가능.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // 싱글톤으로 사용할 인스턴스(정적)
    private static SceneLoader instance;
    public static SceneLoader Instance
    {
        get
        {
            // 씬에 인스턴스가 없다면 찾아오거나 생성해야 한다.
            if (instance == null)
            {
                // 현재 씬에 존재하는 오브젝트에서 찾아온다.
                instance = FindAnyObjectByType<SceneLoader>();

                // 그래도 없다면 새 게임오브젝트를 만들고 컴포넌트를 추가한다.
                if (instance == null)
                {
                    GameObject obj = new GameObject("SceneLoader");
                    instance = obj.AddComponent<SceneLoader>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 이미 인스턴스가 있고, 그 인스턴스가 자신(this)이 아니면 중복이므로 제거
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 씬 로드하는 방법은 2가지 (1. 이름  2. 인덱스)
    /// 호출할 쪽(예: 버튼)의 필요에 따라 선택해서 사용한다.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int sceneId)
    {
        SceneManager.LoadScene(sceneId);
    }

    /// <summary>
    /// 현재 씬을 다시 로드
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadScene(currentSceneName);
    }

    // (참고) 더 단순한 싱글톤 매니저 구현 예시
    /*
    public static SceneLoader Instance { get; private set; }

    private void Awake()
    {
        // 싱글톤 초기화
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    */

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            SceneManager.LoadScene("Title"); // 인덱스 번호로 호출 가능
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            SceneManager.LoadScene("GameScene"); // 이름으로 호출 가능
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            SceneManager.LoadScene("End");
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            ReloadCurrentScene();
        }
    }
}
