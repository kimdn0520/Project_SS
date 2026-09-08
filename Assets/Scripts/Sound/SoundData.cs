using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 모든 오디오 클립과 재생 기본 설정을 중앙 집중식으로 보관하는 ScriptableObject.
/// 인스펙터에서 손쉽게 사운드를 등록하고 볼륨, 피치, 동시 재생 제한을 설정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "SoundData", menuName = "ProjectSS/Sound Data", order = 100)]
public class SoundData : ScriptableObject
{
    [System.Serializable]
    public class SoundClipEntry
    {
        [Tooltip("사운드 호출 시 사용할 이름 (비어있으면 AudioClip 이름 자동 사용)")]
        public string clipName;
        public AudioClip clip;
        public SoundType soundType = SoundType.SFX;

        [Range(0f, 1f)] public float defaultVolume = 1f;
        [Range(0.5f, 2f)] public float defaultPitch = 1f;

        [Tooltip("동일 사운드 연타 방지 최소 간격 (초 단위)")]
        public float minInterval = 0.05f;

        [Tooltip("동시 재생 가능한 최대 인스턴스 개수 (0이면 무제한)")]
        public int maxInstances = 3;

        // 런타임 재생 시간 추적용
        [NonSerialized] public float lastPlayTime;
    }

    [Header("[Audio Clip Entries]")]
    [SerializeField] private List<SoundClipEntry> soundEntries = new List<SoundClipEntry>();

    private readonly Dictionary<string, SoundClipEntry> entryDict = new Dictionary<string, SoundClipEntry>(StringComparer.OrdinalIgnoreCase);
    private bool isInitialized;

    public void Initialize()
    {
        entryDict.Clear();
        for (int i = 0; i < soundEntries.Count; i++)
        {
            var entry = soundEntries[i];
            if (entry == null || entry.clip == null) continue;

            string key = string.IsNullOrEmpty(entry.clipName) ? entry.clip.name : entry.clipName;
            entry.clipName = key;
            entry.lastPlayTime = -999f;

            if (!entryDict.ContainsKey(key))
            {
                entryDict[key] = entry;
            }
            else
            {
                Debug.LogWarning($"[SoundData] 중복된 사운드 이름이 등록되어 있습니다: {key}");
            }
        }
        isInitialized = true;
    }

    public SoundClipEntry GetEntry(string name)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (string.IsNullOrEmpty(name)) return null;

        entryDict.TryGetValue(name, out SoundClipEntry entry);
        return entry;
    }

    public bool HasEntry(string name)
    {
        if (!isInitialized) Initialize();
        return !string.IsNullOrEmpty(name) && entryDict.ContainsKey(name);
    }

    public IReadOnlyList<SoundClipEntry> AllEntries => soundEntries;
}
