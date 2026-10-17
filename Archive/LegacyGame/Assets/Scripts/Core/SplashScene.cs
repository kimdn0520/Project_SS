using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// 앱 기동 시 최초 진입하는 스플래시 씬 컨트롤러.
/// 코루틴을 배제하고 async UniTask 기반으로 초기화 진행률을 갱신하고 Play 씬으로 전환합니다.
/// </summary>
public class SplashScene : MonoBehaviour
{
    [Header("[UI Components]")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;

    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        UpdateProgress(0.05f, "Configuring app environment...");
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);

        // AppManager UniTask 초기화 파이프라인 호출
        if (AppManager.Instance != null)
        {
            await AppManager.Instance.InitializeAppAsync((progress, desc) =>
            {
                UpdateProgress(progress, desc);
            }, ct);
        }

        UpdateProgress(1.0f, "Ready! Launching game...");
        await UniTask.Delay(TimeSpan.FromSeconds(0.3f), cancellationToken: ct);

        // Play 씬 로드
        if (AppManager.Instance != null)
        {
            AppManager.Instance.LoadPlayScene();
        }
        else
        {
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Play").ToUniTask(cancellationToken: ct);
        }
    }

    private void UpdateProgress(float value, string status)
    {
        if (progressSlider != null)
            progressSlider.value = Mathf.Clamp01(value);

        if (progressText != null)
            progressText.text = $"{Mathf.RoundToInt(value * 100f)}%";

        if (statusText != null)
            statusText.text = status;
    }
}
