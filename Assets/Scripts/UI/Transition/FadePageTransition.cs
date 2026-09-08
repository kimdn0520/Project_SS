using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// CanvasGroup의 Alpha 값을 조정하여 부드러운 페이드 인/아웃을 처리하는 기본 트랜지션.
/// </summary>
public class FadePageTransition : IPageTransition
{
    private readonly float duration;

    public FadePageTransition(float duration = 0.2f)
    {
        this.duration = duration;
    }

    public async UniTask TransitionInAsync(IPageHandler page, CancellationToken ct = default)
    {
        CanvasGroup group = ResolveCanvasGroup(page);
        if (group == null || duration <= 0f) return;

        await FadeAsync(group, 0f, 1f, duration, ct);
    }

    public async UniTask TransitionOutAsync(IPageHandler page, CancellationToken ct = default)
    {
        CanvasGroup group = ResolveCanvasGroup(page);
        if (group == null || duration <= 0f) return;

        await FadeAsync(group, group.alpha, 0f, duration, ct);
    }

    private static CanvasGroup ResolveCanvasGroup(IPageHandler page)
    {
        if (page is SceneBase sb && sb.CanvasGroup != null)
            return sb.CanvasGroup;

        if (page is Component comp)
            return comp.GetComponentInChildren<CanvasGroup>(true);

        return null;
    }

    private static async UniTask FadeAsync(CanvasGroup group, float from, float to, float duration, CancellationToken ct)
    {
        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        group.alpha = to;
    }
}
