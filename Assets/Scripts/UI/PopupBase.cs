using System;
using UnityEngine;

/// <summary>
/// BasePopupHandler를 상속받은 하위 호환성 래퍼 클래스.
/// 기존 Open(param), Close(onClosed) 메서드를 유지하면서 BasePopupHandler 및 PopupManager와 연동됩니다.
/// </summary>
public abstract class PopupBase : BasePopupHandler
{
    public virtual void Open(object param = null)
    {
        OnWillEnter(param);
        Show();
        OnDidEnter(param);
    }

    public virtual void Close(Action onClosed)
    {
        Close();
        onClosed?.Invoke();
    }
}
