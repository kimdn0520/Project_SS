using System;
using UnityEngine;

/// <summary>
/// 팝업의 표시, 숨김, 생명주기 및 닫기 이벤트를 정의하는 인터페이스.
/// </summary>
public interface IPopupHandler
{
    string PopupName { get; }
    Canvas Canvas { get; }
    CanvasGroup CanvasGroup { get; }
    bool IsOpen { get; }

    void Show();
    void Hide();

    void OnWillEnter(object param);
    void OnDidEnter(object param);
    void OnWillLeave();
    void OnDidLeave();

    void SetSortingOrder(int order);
    void OnEscape();
    void Close(object result = null);

    /// <summary>
    /// 팝업 개별 애니메이션 (null일 경우 PopupManager의 기본 애니메이션 사용)
    /// </summary>
    IPopupAnimation Animation { get; }
}
