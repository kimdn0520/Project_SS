using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 페이지 전환 연출(페이드, 슬라이드, 줌 등)을 전략 패턴으로 분리하는 인터페이스.
/// PageManager에 주입하여 전환 연출을 손쉽게 교체할 수 있습니다.
/// </summary>
public interface IPageTransition
{
    /// <summary>
    /// 새 페이지가 화면에 나타날 때 실행되는 연출 (예: 페이드 인, 슬라이드 인)
    /// </summary>
    UniTask TransitionInAsync(IPageHandler page, CancellationToken ct = default);

    /// <summary>
    /// 기존 페이지가 화면에서 퇴장할 때 실행되는 연출 (예: 페이드 아웃, 슬라이드 아웃)
    /// </summary>
    UniTask TransitionOutAsync(IPageHandler page, CancellationToken ct = default);
}
