using TMPro;
using UnityEngine;

public class FloorDisplay : MonoBehaviour
{
    public TMP_Text text;
    public LoopManager loop;

    void Start()
    {
        loop.OnFloorChanged += (floor) =>
        {
            text.text = $"{floor}";
        };

        // 초기 UI 반영
        text.text = $"{loop.floor}";
    }
}
