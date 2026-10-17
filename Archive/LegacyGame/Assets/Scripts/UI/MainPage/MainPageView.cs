using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 로비 화면을 담당하는 MainPage 프리팹 컨트롤러.
/// SceneBase를 상속받아 씬 수준의 완전한 4단계 생명주기(OnWillEnter, OnDidEnter, OnWillLeave, OnDidLeave)를 가집니다.
/// </summary>
public class MainPageView : SceneBase
{
    [Header("[MainPage Controllers]")]
    [SerializeField] private SwipeTabController swipeTabController;
    [SerializeField] private BottomTabBar bottomTabBar;

    [Header("[Action Buttons]")]
    [SerializeField] private Button startGameButton;

    private void Awake()
    {
        BindEvents();
    }

    public override void OnWillEnter(object param)
    {
        if (Canvas != null && (Canvas.worldCamera == null || Canvas.renderMode != RenderMode.ScreenSpaceCamera))
        {
            SetupRenderCamera(Camera.main);
        }

        // 페이드 인 직전: 화면 암전 상태에서 탭 위치 초기화
        if (CanvasGroup != null)
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }

        // 로비 진입 시 기본 포커스는 [1] 홈(Home) 탭
        if (swipeTabController != null)
        {
            swipeTabController.SnapToTab(1, instant: true);
        }
        if (bottomTabBar != null)
        {
            bottomTabBar.SetSelectedTab(1);
        }
    }

    public override void OnDidEnter()
    {
        // 페이드 인 완료: 화면 완전 노출, 입력 활성화
        if (CanvasGroup != null)
        {
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }
    }

    public override void OnWillLeave()
    {
        // 페이드 아웃 직전: 터치 입력 차단
        if (CanvasGroup != null)
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }
    }

    public override void OnDidLeave()
    {
        // 페이드 아웃 완료 후 정리
    }

    private void BindEvents()
    {
        if (swipeTabController != null && bottomTabBar != null)
        {
            swipeTabController.OnTabChanged += bottomTabBar.SetSelectedTab;
            bottomTabBar.OnTabButtonClicked += HandleTabButtonClicked;
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnClickStartGame);
        }
    }

    private void HandleTabButtonClicked(int index)
    {
        if (swipeTabController != null)
        {
            swipeTabController.SnapToTab(index);
        }
    }

    private void OnClickStartGame()
    {
        Debug.Log("[MainPageView] START GAME 클릭 -> PlayPage 전환 요청");
        if (PageManager.Instance != null)
        {
            PageManager.Instance.ShowPage(UIPageType.PlayPage);
        }
    }

    private void OnDestroy()
    {
        if (swipeTabController != null && bottomTabBar != null)
        {
            swipeTabController.OnTabChanged -= bottomTabBar.SetSelectedTab;
            bottomTabBar.OnTabButtonClicked -= HandleTabButtonClicked;
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnClickStartGame);
        }
    }
}
