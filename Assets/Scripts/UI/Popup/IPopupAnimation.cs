using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 팝업의 등장(AnimateIn) 및 퇴장(AnimateOut) 애니메이션을 전략 패턴으로 분리하는 인터페이스.
/// </summary>
public interface IPopupAnimation
{
    UniTask AnimateInAsync(IPopupHandler popup, CancellationToken ct = default);
    UniTask AnimateOutAsync(IPopupHandler popup, CancellationToken ct = default);
}
