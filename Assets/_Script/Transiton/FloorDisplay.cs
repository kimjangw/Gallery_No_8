using TMPro;
using UnityEngine;

public class FloorDisplay : MonoBehaviour
{
    public TMP_Text text;

    void Start()
    {
        SetFloor(0);
    }

    // LoopManager가 층 변경 시 직접 호출하는 함수
    public void SetFloor(int floor)
    {
        if (text == null) return;

        // 층수 표시
        text.text = floor.ToString();
    }
}
