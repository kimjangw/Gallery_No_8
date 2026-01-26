using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSettingUI : MonoBehaviour
{

    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    public TextMeshProUGUI masterText;
    public TextMeshProUGUI bgmText;
    public TextMeshProUGUI sfxText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //SoundManager.Instance.PlayBGM("Bgm1");

        masterSlider.value = SoundManager.Instance.masterVolume;
        bgmSlider.value = SoundManager.Instance.bgmVolume;
        sfxSlider.value = SoundManager.Instance.sfxVolume;

        //�̺�Ʈ ����
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);

        SoundManager.Instance.PlayBGM("Title_BGM");
        masterText.text = $"{(masterSlider.value * 100):F0}%";
        bgmText.text = $"{(bgmSlider.value * 100):F0}%";
        sfxText.text = $"{(sfxSlider.value * 100):F0}%";
    }

    void OnMasterChanged(float value)
    {
        SoundManager.Instance.SetMasterVolume(value);
        UpdateTexts();
    }

    void OnBGMChanged(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
        UpdateTexts();
    }

    void OnSFXChanged(float value)
    {
        SoundManager.Instance.SetSFXVolume(value);
        UpdateTexts();
    }

    void UpdateTexts()
    {
        masterText.text = $"{(masterSlider.value * 100):F0}%";
        bgmText.text = $"{(bgmSlider.value * 100):F0}%";
        sfxText.text = $"{(sfxSlider.value * 100):F0}%";
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    SoundManager.Instance.Play("Jump");

        //    //SoundManager.Instance.StopBGM();
        //    //SoundManager.Instance.PlayBGM("Bgm2");

        //}
        //if (Input.GetMouseButtonDown(1))
        //{
        //    SoundManager.Instance.Play("Die");
        //    //SoundManager.Instance.CrossFadeBGM("Bgm1");
        //}
    }
}
