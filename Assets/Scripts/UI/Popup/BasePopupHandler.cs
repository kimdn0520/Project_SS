using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 모든 팝업의 기반 클래스.
/// Canvas / CanvasGroup 캐싱, 소팅 오더 관리, 딤드(커튼) 터치 및 백버튼(Escape) 통일 처리,
/// 그리고 닫기 시 결과 파라미터 전달을 담당합니다.
/// </summary>
public abstract class BasePopupHandler : MonoBehaviour, IPopupHandler
{
    [Header("[Canvas & Group]")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("[Buttons]")]
    [Tooltip("배경 반투명 딤드(커튼) 영역 버튼. 클릭 시 OnClickCurtain()을 통해 팝업이 닫힙니다.")]
    [SerializeField] private Button curtainButton;

    [Tooltip("팝업 우상단 X 닫기 버튼. 클릭 시 OnClickClose()를 통해 팝업이 닫힙니다.")]
    [SerializeField] private Button closeButton;

    [Header("[Sorting Order Settings]")]
    [SerializeField] private bool autoSetSortingOrder = true;
    [SerializeField] private int baseSortingOrder = 1000;

    public virtual string PopupName => string.IsNullOrEmpty(gameObject.name) ? GetType().Name : gameObject.name;
    public bool IsOpen { get; private set; }
    private GameObject transitionInputBlocker;

    private void SetTransitionInputBlocked(bool blocked)
    {
        // CanvasGroup.interactable changes every descendant Selectable's tint.
        // Absorb pointer input without changing the buttons' visual states.
        if (blocked && transitionInputBlocker == null && Canvas != null)
        {
            transitionInputBlocker = new GameObject("TransitionInputBlocker", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)transitionInputBlocker.transform;
            rect.SetParent(Canvas.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = transitionInputBlocker.GetComponent<Image>();
            image.color = Color.clear; image.raycastTarget = true;
        }
        if (transitionInputBlocker != null)
        {
            transitionInputBlocker.transform.SetAsLastSibling();
            transitionInputBlocker.SetActive(blocked);
        }
        if (blocked && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// 개별 팝업만의 커스텀 등장/퇴장 연출. null이면 PopupManager의 기본 애니메이션 사용
    /// </summary>
    public virtual IPopupAnimation Animation => null;

    public Canvas Canvas
    {
        get
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>(true);
            }
            return canvas;
        }
    }

    public CanvasGroup CanvasGroup
    {
        get
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>() ?? GetComponentInChildren<CanvasGroup>(true);
            }
            return canvasGroup;
        }
    }

    /// <summary>
    /// 딤드 터치 또는 백버튼으로 닫힐 때 반환할 기본 파라미터
    /// </summary>
    protected virtual object DefaultCloseParam => null;

    protected virtual void Awake()
    {
        if (curtainButton != null)
            curtainButton.onClick.AddListener(OnClickCurtain);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        IsOpen = true;
    }

    public virtual void Hide()
    {
        IsOpen = false;
        gameObject.SetActive(false);
    }

    public virtual void OnWillEnter(object param)
    {
        if (Canvas != null)
        {
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            if(Canvas.worldCamera == null)Canvas.worldCamera = PopupManager.Instance != null ? PopupManager.Instance.RenderCamera : Camera.main;
            Canvas.planeDistance = 10f;
        }

        // 트랜지션 연출 중에는 터치 입력 방지
        if (CanvasGroup != null)
        {
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }
        SetTransitionInputBlocked(true);
    }

    public virtual void OnDidEnter(object param)
    {
        SetTransitionInputBlocked(false);
        // 트랜지션 완료 후 터치 입력 활성화
        if (CanvasGroup != null)
        {
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }
    }

    public virtual void OnWillLeave()
    {
        // 퇴장 시작 시 즉시 터치 입력 차단
        if (CanvasGroup != null)
        {
            CanvasGroup.blocksRaycasts = true;
        }
        SetTransitionInputBlocked(true);
    }

    public virtual void OnDidLeave()
    {
    }

    public virtual void SetSortingOrder(int order)
    {
        if (autoSetSortingOrder && Canvas != null)
        {
            Canvas.overrideSorting = true;
            Canvas.sortingOrder = order;
        }
    }

    /// <summary>
    /// 팝업을 현재 닫을 수 있는 상태인지 검사 (연출 진행 중이거나 락 상태일 때 차단)
    /// </summary>
    protected virtual bool CanEscape()
    {
        if (!IsOpen) return false;
        if (PopupManager.IsChanging) return false;
        if (PopupManager.CurrentPopup != (IPopupHandler)this) return false;
        return true;
    }

    /// <summary>
    /// 커튼(딤드) 터치는 백버튼(Escape)과 동일한 경로를 탄다.
    /// </summary>
    public virtual void OnClickCurtain()
    {
        OnEscape();
    }

    public virtual void OnClickClose()
    {
        Close(DefaultCloseParam);
    }

    /// <summary>
    /// 안드로이드/키보드 뒤로가기 및 딤드 터치 공통 진입점
    /// </summary>
    public virtual void OnEscape()
    {
        if (!CanEscape()) return;
        Close(DefaultCloseParam);
    }

    /// <summary>
    /// 이 팝업을 닫고 결과값을 반환합니다.
    /// </summary>
    public virtual void Close(object result = null)
    {
        PopupManager.CloseByHandler(this, result);
    }

    protected virtual void OnDestroy()
    {
        if (curtainButton != null)
            curtainButton.onClick.RemoveListener(OnClickCurtain);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);
    }
}
