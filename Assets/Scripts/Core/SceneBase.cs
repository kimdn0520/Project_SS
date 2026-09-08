using UnityEngine;

/// <summary>
/// 각 씬 및 Sub-Scene(MainPage, PlayPage)의 공통 생명주기 및 캔버스 인터페이스를 정의하는 기반 추상 클래스.
/// IPageHandler 인터페이스를 구현하여 PageManager에서 다형적으로 관리됩니다.
/// </summary>
public abstract class SceneBase : MonoBehaviour, IPageHandler
{
    [Header("[Canvas References]")]
    [SerializeField] protected Canvas canvas;
    public Canvas Canvas
    {
        get
        {
            if (canvas == null)
                canvas = GetComponentInChildren<Canvas>(true);
            return canvas;
        }
    }

    [SerializeField] protected CanvasGroup canvasGroup;
    public CanvasGroup CanvasGroup
    {
        get
        {
            if (canvasGroup == null)
                canvasGroup = GetComponentInChildren<CanvasGroup>(true);
            return canvasGroup;
        }
    }

    /// <summary>
    /// Screen Space - Camera 캔버스에 Render Camera를 주입 (Dependency Injection)
    /// </summary>
    public virtual void SetupRenderCamera(Camera cam)
    {
        if (cam == null) return;

        Canvas targetCanvas = Canvas;
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (targetCanvas != null)
        {
            targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            targetCanvas.worldCamera = cam;
            targetCanvas.planeDistance = 10f;
        }
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public abstract void OnWillEnter(object param);
    public abstract void OnDidEnter();
    public abstract void OnWillLeave();
    public abstract void OnDidLeave();
}

public enum GAME_MODE
{
    Title,
    MainGame,
    Lobby,
}
