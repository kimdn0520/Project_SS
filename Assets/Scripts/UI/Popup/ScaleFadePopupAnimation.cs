using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 팝업이 뜰 때 부드러운 스케일 업(0.85 -> 1.0)과 페이드 인,
/// 닫힐 때 페이드 아웃을 수행하는 기본 팝업 애니메이션.
/// </summary>
public class ScaleFadePopupAnimation : IPopupAnimation
{
    private readonly float duration;

    public ScaleFadePopupAnimation(float duration = 0.15f)
    {
        this.duration = duration;
    }

    public async UniTask AnimateInAsync(IPopupHandler popup, CancellationToken ct = default)
    {
        if (popup is not Component comp || duration <= 0f) return;

        Transform t = comp.transform;
        CanvasGroup group = popup.CanvasGroup;

        float elapsed = 0f;
        Vector3 fromScale = Vector3.one * 0.85f;
        Vector3 toScale = Vector3.one;

        t.localScale = fromScale;
        if (group != null) group.alpha = 0f;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // Ease-out Quad curve
            float ease = 1f - (1f - progress) * (1f - progress);

            t.localScale = Vector3.LerpUnclamped(fromScale, toScale, ease);
            if (group != null) group.alpha = progress;

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        t.localScale = toScale;
        if (group != null) group.alpha = 1f;
    }

    public async UniTask AnimateOutAsync(IPopupHandler popup, CancellationToken ct = default)
    {
        if (popup is not Component comp || duration <= 0f) return;

        Transform t = comp.transform;
        CanvasGroup group = popup.CanvasGroup;

        float elapsed = 0f;
        Vector3 fromScale = t.localScale;
        Vector3 toScale = Vector3.one * 0.9f;
        float fromAlpha = group != null ? group.alpha : 1f;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            t.localScale = Vector3.Lerp(fromScale, toScale, progress);
            if (group != null) group.alpha = Mathf.Lerp(fromAlpha, 0f, progress);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        t.localScale = toScale;
        if (group != null) group.alpha = 0f;
    }
}
