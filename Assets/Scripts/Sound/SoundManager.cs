using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

/// <summary>
/// ScriptableObject(SoundData) 기반 오디오 매니저.
/// 2채널 BGM 부드러운 크로스페이드(DOTween), SFX 풀링 및 연타 스로틀링, PlayerPrefs 볼륨 저장을 완벽 지원합니다.
/// </summary>
public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    private const string PREFS_MASTER_VOL = "Sound_MasterVol";
    private const string PREFS_BGM_VOL = "Sound_BgmVol";
    private const string PREFS_SFX_VOL = "Sound_SfxVol";
    private const string PREFS_BGM_ON = "Sound_BgmOn";
    private const string PREFS_SFX_ON = "Sound_SfxOn";

    [Header("[Sound Data Asset]")]
    [SerializeField] private SoundData soundData;

    [Header("[Pool Settings]")]
    [SerializeField] private int initialSfxPoolSize = 12;

    // BGM 크로스페이드용 듀얼 소스
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private bool isBgmSourceAActive;
    private string currentBgmName;
    private Tween bgmFadeTweenA;
    private Tween bgmFadeTweenB;

    // SFX 풀링
    private readonly List<AudioSource> sfxPool = new List<AudioSource>();
    private Transform sfxRoot;

    // 볼륨 및 Mute 상태
    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;
    private bool isBgmOn = true;
    private bool isSfxOn = true;

    #region Properties

    public SoundData SoundData => soundData;
    public string CurrentBgmName => currentBgmName;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = masterVolume;
            PlayerPrefs.SetFloat(PREFS_MASTER_VOL, masterVolume);
            ApplyBgmVolume();
        }
    }

    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREFS_BGM_VOL, bgmVolume);
            ApplyBgmVolume();
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREFS_SFX_VOL, sfxVolume);
        }
    }

    public bool IsBGMOn
    {
        get => isBgmOn;
        set
        {
            isBgmOn = value;
            PlayerPrefs.SetInt(PREFS_BGM_ON, isBgmOn ? 1 : 0);
            if (!isBgmOn)
            {
                StopBGM(0.2f);
            }
            else if (!string.IsNullOrEmpty(currentBgmName))
            {
                PlayBGM(currentBgmName, 0.3f);
            }
        }
    }

    public bool IsSFXOn
    {
        get => isSfxOn;
        set
        {
            isSfxOn = value;
            PlayerPrefs.SetInt(PREFS_SFX_ON, isSfxOn ? 1 : 0);
            if (!isSfxOn)
            {
                StopAllSFX();
            }
        }
    }

    #endregion

    protected override void Awake()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        base.Awake();

        LoadPreferences();
        InitializeSources();

        if (soundData == null)
        {
            soundData = Resources.Load<SoundData>("SoundData");
        }

        if (soundData != null)
        {
            soundData.Initialize();
        }
    }

    private void LoadPreferences()
    {
        masterVolume = PlayerPrefs.GetFloat(PREFS_MASTER_VOL, 1f);
        bgmVolume = PlayerPrefs.GetFloat(PREFS_BGM_VOL, 1f);
        sfxVolume = PlayerPrefs.GetFloat(PREFS_SFX_VOL, 1f);
        isBgmOn = PlayerPrefs.GetInt(PREFS_BGM_ON, 1) == 1;
        isSfxOn = PlayerPrefs.GetInt(PREFS_SFX_ON, 1) == 1;

        AudioListener.volume = masterVolume;
    }

    private void InitializeSources()
    {
        // 1. BGM 듀얼 소스 생성
        GameObject bgmNodeA = new GameObject("BGM_Source_A");
        bgmNodeA.transform.SetParent(transform, false);
        bgmSourceA = bgmNodeA.AddComponent<AudioSource>();
        bgmSourceA.loop = true;
        bgmSourceA.playOnAwake = false;

        GameObject bgmNodeB = new GameObject("BGM_Source_B");
        bgmNodeB.transform.SetParent(transform, false);
        bgmSourceB = bgmNodeB.AddComponent<AudioSource>();
        bgmSourceB.loop = true;
        bgmSourceB.playOnAwake = false;

        // 2. SFX 풀 루트 및 초기 소스 생성
        GameObject sfxNode = new GameObject("SFX_Root");
        sfxNode.transform.SetParent(transform, false);
        sfxRoot = sfxNode.transform;

        for (int i = 0; i < initialSfxPoolSize; i++)
        {
            CreateNewSfxSource();
        }
    }

    private AudioSource CreateNewSfxSource()
    {
        GameObject go = new GameObject($"SFX_Source_{sfxPool.Count}");
        go.transform.SetParent(sfxRoot, false);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        sfxPool.Add(src);
        return src;
    }

    #region BGM Play & Stop

    /// <summary>
    /// BGM을 크로스페이드 트윈과 함께 재생합니다.
    /// </summary>
    public void PlayBGM(string clipName, float fadeDuration = 0.5f, bool loop = true)
    {
        if (soundData == null) return;
        var entry = soundData.GetEntry(clipName);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM 클립을 찾을 수 없습니다: {clipName}");
            return;
        }

        if (currentBgmName == clipName && GetActiveBgmSource().isPlaying)
        {
            return; // 이미 같은 BGM이 재생 중이면 무시
        }

        currentBgmName = clipName;
        if (!isBgmOn) return;

        AudioSource currentSource = isBgmSourceAActive ? bgmSourceA : bgmSourceB;
        AudioSource nextSource = isBgmSourceAActive ? bgmSourceB : bgmSourceA;
        isBgmSourceAActive = !isBgmSourceAActive;

        float targetVol = bgmVolume * entry.defaultVolume;

        // 기존 BGM 페이드 아웃
        bgmFadeTweenA?.Kill();
        bgmFadeTweenB?.Kill();

        if (currentSource.isPlaying && fadeDuration > 0f)
        {
            bgmFadeTweenA = currentSource.DOFade(0f, fadeDuration).OnComplete(() => currentSource.Stop());
        }
        else
        {
            currentSource.Stop();
        }

        // 새 BGM 설정 및 페이드 인
        nextSource.clip = entry.clip;
        nextSource.loop = loop;
        nextSource.pitch = entry.defaultPitch;

        if (fadeDuration > 0f)
        {
            nextSource.volume = 0f;
            nextSource.Play();
            bgmFadeTweenB = nextSource.DOFade(targetVol, fadeDuration);
        }
        else
        {
            nextSource.volume = targetVol;
            nextSource.Play();
        }
    }

    public void StopBGM(float fadeDuration = 0.5f)
    {
        AudioSource active = GetActiveBgmSource();
        if (active == null || !active.isPlaying) return;

        bgmFadeTweenA?.Kill();
        bgmFadeTweenB?.Kill();

        if (fadeDuration > 0f)
        {
            bgmFadeTweenA = active.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                active.Stop();
                currentBgmName = null;
            });
        }
        else
        {
            active.Stop();
            currentBgmName = null;
        }
    }

    public void PauseBGM()
    {
        GetActiveBgmSource()?.Pause();
    }

    public void ResumeBGM()
    {
        if (isBgmOn)
        {
            GetActiveBgmSource()?.UnPause();
        }
    }

    private AudioSource GetActiveBgmSource()
    {
        return isBgmSourceAActive ? bgmSourceA : bgmSourceB;
    }

    private void ApplyBgmVolume()
    {
        AudioSource active = GetActiveBgmSource();
        if (active != null && !string.IsNullOrEmpty(currentBgmName) && soundData != null)
        {
            var entry = soundData.GetEntry(currentBgmName);
            float baseVol = entry != null ? entry.defaultVolume : 1f;
            active.volume = bgmVolume * baseVol;
        }
    }

    #endregion

    #region SFX Play & Stop

    /// <summary>
    /// 단발성 SFX를 재생합니다. 쿨다운 스로틀링과 최대 보이스 제한이 자동으로 적용됩니다.
    /// </summary>
    public AudioSource PlaySFX(string clipName, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        if (!isSfxOn || soundData == null) return null;

        var entry = soundData.GetEntry(clipName);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립을 찾을 수 없습니다: {clipName}");
            return null;
        }

        // 1. 연타 방지 스로틀링 체크 (최소 인터벌)
        float curTime = Time.unscaledTime;
        if (entry.minInterval > 0f && (curTime - entry.lastPlayTime) < entry.minInterval)
        {
            return null;
        }
        entry.lastPlayTime = curTime;

        // 2. 최대 동시 재생 개수(maxInstances) 체크
        if (entry.maxInstances > 0)
        {
            ThrottleMaxInstances(entry.clip, entry.maxInstances);
        }

        // 3. 가용 AudioSource 확보
        AudioSource source = GetAvailableSfxSource();
        source.clip = entry.clip;
        source.volume = sfxVolume * entry.defaultVolume * Mathf.Clamp01(volumeMultiplier);
        source.pitch = entry.defaultPitch * pitchMultiplier;
        source.Play();

        return source;
    }

    /// <summary>
    /// 동일한 사운드가 이미 재생 중인 경우 추가 재생하지 않는 단발 재생.
    /// </summary>
    public AudioSource PlaySFXOnce(string clipName, float volumeMultiplier = 1f)
    {
        if (IsPlayingSFX(clipName)) return null;
        return PlaySFX(clipName, volumeMultiplier);
    }

    /// <summary>
    /// UI 전용 사운드 재생 (버튼 클릭 등)
    /// </summary>
    public AudioSource PlayUISFX(string clipName, float volumeMultiplier = 1f)
    {
        return PlaySFX(clipName, volumeMultiplier, 1f);
    }

    private void ThrottleMaxInstances(AudioClip clip, int maxAllowed)
    {
        int playingCount = 0;
        AudioSource oldest = null;

        for (int i = 0; i < sfxPool.Count; i++)
        {
            if (sfxPool[i].isPlaying && sfxPool[i].clip == clip)
            {
                playingCount++;
                if (oldest == null) oldest = sfxPool[i];
            }
        }

        if (playingCount >= maxAllowed && oldest != null)
        {
            oldest.Stop();
        }
    }

    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxPool.Count; i++)
        {
            if (!sfxPool[i].isPlaying)
            {
                return sfxPool[i];
            }
        }

        // 풀이 모두 사용 중일 때 자동 확장
        return CreateNewSfxSource();
    }

    public bool IsPlayingSFX(string clipName)
    {
        if (soundData == null) return false;
        var entry = soundData.GetEntry(clipName);
        if (entry == null || entry.clip == null) return false;

        for (int i = 0; i < sfxPool.Count; i++)
        {
            if (sfxPool[i].isPlaying && sfxPool[i].clip == entry.clip)
            {
                return true;
            }
        }
        return false;
    }

    public void StopSFX(string clipName)
    {
        if (soundData == null) return;
        var entry = soundData.GetEntry(clipName);
        if (entry == null || entry.clip == null) return;

        for (int i = 0; i < sfxPool.Count; i++)
        {
            if (sfxPool[i].isPlaying && sfxPool[i].clip == entry.clip)
            {
                sfxPool[i].Stop();
            }
        }
    }

    public void StopAllSFX()
    {
        for (int i = 0; i < sfxPool.Count; i++)
        {
            if (sfxPool[i].isPlaying)
            {
                sfxPool[i].Stop();
            }
        }
    }

    public void StopAll()
    {
        StopBGM(0f);
        StopAllSFX();
    }

    #endregion

    #region Static Convenience Facade

    public static void PlayBgm(string clipName, float fadeDuration = 0.5f, bool loop = true)
    {
        if (Instance != null) Instance.PlayBGM(clipName, fadeDuration, loop);
    }

    public static AudioSource PlaySfx(string clipName, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        return Instance != null ? Instance.PlaySFX(clipName, volumeMultiplier, pitchMultiplier) : null;
    }

    public static AudioSource PlaySfxOnce(string clipName, float volumeMultiplier = 1f)
    {
        return Instance != null ? Instance.PlaySFXOnce(clipName, volumeMultiplier) : null;
    }

    public static void StopBgm(float fadeDuration = 0.5f)
    {
        if (Instance != null) Instance.StopBGM(fadeDuration);
    }

    public static void StopSfx(string clipName)
    {
        if (Instance != null) Instance.StopSFX(clipName);
    }

    #endregion

    private void OnDestroy()
    {
        bgmFadeTweenA?.Kill();
        bgmFadeTweenB?.Kill();
    }
}
