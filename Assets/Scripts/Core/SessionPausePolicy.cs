using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임플레이 일시정지(Pause) 정책 총괄 관리자.
/// Time.timeScale을 함부로 건드리지 않고, 팝업/모달/앱 백그라운드 전환 등 다양한 정지 소스를 카운팅하여
/// 전투(Battle)와 채굴(Mining) 게임플레이 클록의 일시정지 및 재개를 안전하게 조율합니다.
/// </summary>
public class SessionPausePolicy : SingletonMonoBehaviour<SessionPausePolicy>
{
    private readonly HashSet<string> pauseSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool IsPaused => pauseSources.Count > 0;
    public int ActivePauseCount => pauseSources.Count;

    public event Action<bool> OnPauseStateChanged; // (isPaused)

    /// <summary>
    /// 정지 요청 (예: "ModalPopup", "AppBackground", "Tutorial")
    /// </summary>
    public void RequestPause(string source)
    {
        if (string.IsNullOrEmpty(source)) source = "Default";

        bool wasPaused = IsPaused;
        pauseSources.Add(source);

        if (!wasPaused && IsPaused)
        {
            OnPauseStateChanged?.Invoke(true);
        }
    }

    /// <summary>
    /// 정지 해제
    /// </summary>
    public void ReleasePause(string source)
    {
        if (string.IsNullOrEmpty(source)) source = "Default";

        bool wasPaused = IsPaused;
        pauseSources.Remove(source);

        if (wasPaused && !IsPaused)
        {
            OnPauseStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// 모든 정지 소스 강제 클리어
    /// </summary>
    public void ClearAllPause()
    {
        bool wasPaused = IsPaused;
        pauseSources.Clear();

        if (wasPaused)
        {
            OnPauseStateChanged?.Invoke(false);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            RequestPause("AppLifecycle");
        }
        else
        {
            ReleasePause("AppLifecycle");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            RequestPause("AppFocusLoss");
        }
        else
        {
            ReleasePause("AppFocusLoss");
        }
    }
}
