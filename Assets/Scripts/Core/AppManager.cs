using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>
/// 앱 전역 생명주기 및 씬 전환(Splash ➔ Play)을 총괄하는 싱글톤 매니저.
/// 씬 내부 페이지 관리는 PageManager에게 위임하고, 앱 수준의 비동기 초기화 및 페이드 전환만을 전담합니다.
/// </summary>
public class AppManager : SingletonMonoBehaviour<AppManager>
{
    [Header("[Global Fade Overlay]")]
    [SerializeField] private CanvasGroup globalFadeCanvasGroup;
    [SerializeField] private float defaultFadeDuration = 0.4f;

    public bool IsTransitioning { get; private set; }
    public bool IsAppInitialized { get; private set; }

    protected override void Awake()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        base.Awake();
        EnsureGlobalFadeOverlay();
    }

    #region App Initialization Pipeline (Splash Scene)

    public async UniTask InitializeAppAsync(Action<float, string> onProgress = null, CancellationToken ct = default)
    {
        Debug.Log("[AppManager] 앱 초기화 파이프라인 시작...");

        onProgress?.Invoke(0.20f, "Configuring system settings...");
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);

        onProgress?.Invoke(0.50f, "Initializing resource managers...");
        if (SpriteManager.Instance != null)
        {
            SpriteManager.Instance.Initialize();
        }
        await UniTask.Delay(TimeSpan.FromSeconds(0.25f), cancellationToken: ct);

        onProgress?.Invoke(0.80f, "Loading user profile and data...");
        await UniTask.Delay(TimeSpan.FromSeconds(0.25f), cancellationToken: ct);

        onProgress?.Invoke(1.00f, "Ready!");
        IsAppInitialized = true;
        Debug.Log("[AppManager] 앱 초기화 완료.");
    }

    public void LoadPlayScene(float? customFadeDuration = null)
    {
        LoadPlaySceneAsync(customFadeDuration).Forget();
    }

    public async UniTask LoadPlaySceneAsync(float? customFadeDuration = null)
    {
        IsTransitioning = true;
        float duration = customFadeDuration ?? defaultFadeDuration;
        var ct = this.GetCancellationTokenOnDestroy();

        // 1. 페이드 아웃 (화면 암전)
        await FadeAsync(globalFadeCanvasGroup, 0f, 1f, duration, ct);

        // 2. Play 씬 비동기 로드
        await SceneManager.LoadSceneAsync("Play").ToUniTask(cancellationToken: ct);

        // 3. 페이드 인 (화면 밝아짐)
        await FadeAsync(globalFadeCanvasGroup, 1f, 0f, duration, ct);

        IsTransitioning = false;
        Debug.Log("[AppManager] Play 씬 로드 완료.");
    }

    #endregion

    #region Fade Routine

    public async UniTask FadeAsync(CanvasGroup group, float from, float to, float duration, CancellationToken ct = default)
    {
        CanvasGroup targetGroup = group ?? globalFadeCanvasGroup;
        if (targetGroup == null || duration <= 0f)
        {
            if (targetGroup != null) targetGroup.alpha = to;
            return;
        }

        targetGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            targetGroup.alpha = Mathf.Lerp(from, to, t);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        targetGroup.alpha = to;
        targetGroup.blocksRaycasts = (to > 0.01f);
    }

    private void EnsureGlobalFadeOverlay()
    {
        if (globalFadeCanvasGroup != null) return;

        GameObject fadeObj = new GameObject("GlobalFadeOverlay");
        fadeObj.transform.SetParent(transform, false);

        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        globalFadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
        globalFadeCanvasGroup.alpha = 0f;
        globalFadeCanvasGroup.blocksRaycasts = false;

        GameObject imgObj = new GameObject("BlackBackdrop");
        imgObj.transform.SetParent(fadeObj.transform, false);
        UnityEngine.UI.Image img = imgObj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;

        RectTransform rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    #endregion
}
