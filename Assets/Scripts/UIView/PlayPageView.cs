using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 화면을 담당하는 PlayPage 프리팹 컨트롤러.
/// SceneBase를 상속받아 인게임 월드(Game_Root)와 인게임 UI(UI_Canvas)를 한 번에 오케스트레이션합니다.
/// 최상위 루트는 일반 GameObject(Transform)이며, RectTransform 스케일 왜곡 없이 순수 2D 월드를 완벽 보존합니다.
/// </summary>
public class PlayPageView : SceneBase
{
    [Header("[Parallel Roots (Siblings)]")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameRootController gameRoot;
    [SerializeField] private WorldAreaLayoutBinder layoutBinder;

    [Header("[UI Components]")]
    [SerializeField] private TopHUDView topHUDView;
    [SerializeField] private BottomPanelView bottomPanelView;
    [SerializeField] private SidebarController sidebarController;
    [SerializeField] private Button lobbyButton;

    public Canvas UICanvas => uiCanvas;
    public GameRootController GameRoot => gameRoot;
    public WorldAreaLayoutBinder LayoutBinder => layoutBinder;
    public TopHUDView TopHUD => topHUDView;
    public BottomPanelView BottomPanel => bottomPanelView;
    public SidebarController Sidebar => sidebarController;

    private void Awake()
    {
        if (lobbyButton != null)
        {
            lobbyButton.onClick.AddListener(OnClickLobby);
        }

        if (layoutBinder == null)
        {
            layoutBinder = GetComponentInChildren<WorldAreaLayoutBinder>(true);
        }

        BindGameEvents();
    }

    public override void SetupRenderCamera(Camera cam)
    {
        base.SetupRenderCamera(cam);
        if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCanvas.worldCamera = cam;
        }

        if (layoutBinder != null)
        {
            layoutBinder.ForceUpdateLayout();
        }
    }

    public override void OnWillEnter(object param)
    {
        if (uiCanvas != null && (uiCanvas.worldCamera == null || uiCanvas.renderMode != RenderMode.ScreenSpaceCamera))
        {
            SetupRenderCamera(Camera.main);
        }

        layoutBinder?.ForceUpdateLayout();

        // 1. UI 암전 및 입력 차단
        if (CanvasGroup != null)
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }

        // 2. 2D 월드 활성화 및 초기화
        if (gameRoot != null)
        {
            gameRoot.gameObject.SetActive(true);
            gameRoot.Initialize(param);
        }
    }

    public override void OnDidEnter()
    {
        // 1. UI 페이드 인 완료: 노출 및 인터랙션 활성화
        if (CanvasGroup != null)
        {
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }

        // 2. 2D 월드 전투/게임 루프 가동
        if (gameRoot != null)
        {
            gameRoot.StartPlay();
        }
    }

    public override void OnWillLeave()
    {
        // 1. UI 인터랙션 차단
        if (CanvasGroup != null)
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }

        // 2. 2D 월드 일시 정지
        if (gameRoot != null)
        {
            gameRoot.PausePlay();
        }
    }

    public override void OnDidLeave()
    {
        // 1. 2D 월드 루프 정지 및 완전 비활성화
        if (gameRoot != null)
        {
            gameRoot.StopPlay();
            gameRoot.gameObject.SetActive(false);
        }
    }

    private void BindGameEvents()
    {
        if (gameRoot != null)
        {
            if (topHUDView != null)
            {
                gameRoot.OnHpChanged += topHUDView.SetHp;
                gameRoot.OnGoldChanged += topHUDView.SetGold;
                gameRoot.OnStageChanged += topHUDView.SetStage;
                gameRoot.OnDepthChanged += topHUDView.SetDepth;
                gameRoot.OnGemsChanged += topHUDView.SetGems;
            }

            if (bottomPanelView != null)
            {
                gameRoot.OnLogMessage += bottomPanelView.AppendLog;
                bottomPanelView.OnButtonClicked += gameRoot.DispatchCommand;
            }
        }

        if (sidebarController != null && bottomPanelView != null)
        {
            sidebarController.OnSidebarAction += HandleSidebarAction;
        }
    }

    private void UnbindGameEvents()
    {
        if (gameRoot != null)
        {
            if (topHUDView != null)
            {
                gameRoot.OnHpChanged -= topHUDView.SetHp;
                gameRoot.OnGoldChanged -= topHUDView.SetGold;
                gameRoot.OnStageChanged -= topHUDView.SetStage;
                gameRoot.OnDepthChanged -= topHUDView.SetDepth;
                gameRoot.OnGemsChanged -= topHUDView.SetGems;
            }

            if (bottomPanelView != null)
            {
                gameRoot.OnLogMessage -= bottomPanelView.AppendLog;
                bottomPanelView.OnButtonClicked -= gameRoot.DispatchCommand;
            }
        }

        if (sidebarController != null)
        {
            sidebarController.OnSidebarAction -= HandleSidebarAction;
        }
    }

    private void HandleSidebarAction(string action)
    {
        bottomPanelView?.AppendLog($"[{action}] 메뉴가 선택되었습니다.");
    }

    private void OnClickLobby()
    {
        Debug.Log("[PlayPageView] LOBBY 클릭 -> MainPage 전환 요청");
        if (PageManager.Instance != null)
        {
            PageManager.Instance.ShowPage(UIPageType.MainPage);
        }
    }

    private void OnDestroy()
    {
        if (lobbyButton != null)
        {
            lobbyButton.onClick.RemoveListener(OnClickLobby);
        }

        UnbindGameEvents();
    }
}
