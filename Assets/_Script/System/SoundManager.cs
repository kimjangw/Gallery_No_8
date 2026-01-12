using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 클래스는 인스펙터에서 보이게 한다
[System.Serializable]
public class Sound
{
    public string name;         // 사운드 이름(키값)
    public AudioClip clip;      // 실제 재생할 사운드 파일 (MP3 등)

    [Range(0f, 1f)]             // 0 ~ 1로 조절 (인스펙터에서 드래그하여 조절)
    public float volume = 1f;   // 기본 볼륨
    [Range(0f, 2f)]
    public float pitch = 1f;    // 기본 피치 (높낮이)

    public bool loop = false;   // 반복 재생 여부

    [HideInInspector]
    public AudioSource source;  // 재생용 AudioSource
}

public class SoundManager : MonoBehaviour
{
    // 전역 인스턴스: 어디서든 SoundManager.Instance로 접근 가능
    public static SoundManager Instance { get; private set; }

    // 인스펙터에서 등록할 사운드 목록
    [Header("사운드 목록")]
    [Tooltip("여기에 사운드를 추가하세요!")]
    public Sound[] sounds;                   // 인스펙터에서 추가한 사운드들

    // 빠른 검색을 위한 딕셔너리
    Dictionary<string, Sound> soundDictionary;

    [Header("BGM 관련")]
    AudioSource bgmSource;
    string currentBGM = "";

    [Header("마스터 볼륨")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;


    void Awake()
    {
        // 싱글턴 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 딕셔너리 초기화
        soundDictionary = new Dictionary<string, Sound>();

        // 각 사운드에 AudioSource 추가
        foreach (Sound s in sounds)
        {
            GameObject soundObject = new GameObject("Sound_" + s.name);
            soundObject.transform.SetParent(transform);

            s.source = soundObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;

            // 딕셔너리에 추가
            soundDictionary.Add(s.name, s);
        }

        // BGM 재생용 AudioSource 생성
        GameObject bgmObject = new GameObject("BGM");
        bgmObject.transform.SetParent(transform);
        bgmSource = bgmObject.AddComponent<AudioSource>();
        bgmSource.loop = true;

        print($"사운드 매니저 초기화 완료! 총 {sounds.Length}개 로드됨");
    }

    /// <summary>
    /// SFX 재생 (기본 재생)
    /// </summary>
    public void Play(string name, float volumeScale = 1f)
    {
        if (!soundDictionary.ContainsKey(name))
        {
            print($"사운드 '{name}'을 찾을 수 없습니다");
            return;
        }

        Sound sound = soundDictionary[name];

        sound.source.volume = masterVolume * sfxVolume * sound.volume * volumeScale;
        sound.source.Play();

        print($"효과음 재생: {name}");
    }

    /// <summary>
    /// BGM 재생
    /// </summary>
    public void PlayBGM(string name, float volumeScale = 1f)
    {
        if (!soundDictionary.ContainsKey(name))
        {
            print($"사운드 '{name}'을 찾을 수 없습니다");
            return;
        }

        if (currentBGM == name && bgmSource.isPlaying)
        {
            print($"BGM '{name}'은 이미 재생중");
            return;
        }

        Sound bgm = soundDictionary[name];

        bgmSource.clip = bgm.clip;
        bgmSource.volume = masterVolume * bgmVolume * bgm.volume * volumeScale;
        bgmSource.Play();

        currentBGM = name;
        print($"BGM 재생: {name}");
    }

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
        currentBGM = "";
        print("BGM 정지");
    }

    /// <summary>
    /// BGM 페이드 아웃 후 정지
    /// </summary>
    public void StopFadeOutBGM(float duration = 1f)
    {
        StartCoroutine(FadeOut(duration));
    }

    IEnumerator FadeOut(float duration)
    {
        float startVolume = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = startVolume;
        currentBGM = "";

        print("BGM 페이드 아웃 완료");
    }

    /// <summary>
    /// BGM 페이드 인
    /// </summary>
    public void PlayFadeInBGM(string name, float duration = 1f)
    {
        StartCoroutine(FadeInBGM(name, duration));
    }

    IEnumerator FadeInBGM(string name, float duration)
    {
        if (!soundDictionary.ContainsKey(name))
        {
            print($"BGM {name}을 찾을 수 없음");
            yield break;
        }

        Sound bgm = soundDictionary[name];
        bgmSource.clip = bgm.clip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float targetVolume = masterVolume * bgmVolume * bgm.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        currentBGM = name;
        print($"BGM 페이드 인 완료: {name}");
    }

    /// <summary>
    /// BGM 크로스 페이드 (A -> B)
    /// </summary>
    public void CrossFadeBGM(string name, float duration = 2f)
    {
        StartCoroutine(CrossFade(name, duration));
    }

    IEnumerator CrossFade(string name, float duration)
    {
        if (!soundDictionary.ContainsKey(name))
        {
            print($"BGM {name}을 찾을 수 없음");
            yield break;
        }

        float elapsed = 0f;
        float startVolume = bgmSource.volume;

        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        Sound newSound = soundDictionary[name];
        tempSource.clip = newSound.clip;
        tempSource.volume = 0f;
        tempSource.loop = true;
        tempSource.Play();

        float targetVolume = masterVolume * bgmVolume * newSound.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            tempSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);

            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = tempSource.clip;
        bgmSource.volume = tempSource.volume;
        bgmSource.Play();

        Destroy(tempSource);

        currentBGM = name;
        print($"BGM 크로스페이드 완료: {name}");
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        if (bgmSource.isPlaying && !string.IsNullOrEmpty(currentBGM))
        {
            Sound bgm = soundDictionary[currentBGM];
            bgmSource.volume = masterVolume * bgmVolume * bgm.volume;
        }
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (bgmSource.isPlaying && !string.IsNullOrEmpty(currentBGM))
        {
            Sound bgm = soundDictionary[currentBGM];
            bgmSource.volume = masterVolume * bgmVolume * bgm.volume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public bool IsPlaying(string name)
    {
        if (!soundDictionary.ContainsKey(name)) return false;

        return soundDictionary[name].source.isPlaying;
    }

    public string GetCurrentBGM()
    {
        return currentBGM;
    }
}
