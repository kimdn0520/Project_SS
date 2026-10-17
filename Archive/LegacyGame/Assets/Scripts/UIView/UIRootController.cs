using System;
using UnityEngine;

/// <summary>
/// Screen Space - Camera 캔버스를 총괄하는 UI 루트 컨트롤러.
/// PageManager를 통해 MainPage(로비)와 PlayPage(인게임)를 스위칭하며,
/// 인게임 상태 데이터를 하위 뷰로 라우팅합니다.
/// </summary>
public class UIRootController : MonoBehaviour
{
    [Header("[Page Architecture]")]
    [SerializeField] private PageManager pageManager;
    [SerializeField] private MainPageView mainPageView;
    [SerializeField] private PlayPageView playPageView;

    [Header("[UI Components]")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    public Canvas Canvas => canvas;
    public CanvasGroup CanvasGroup => canvasGroup;
    public PageManager PageManager => pageManager;
    public MainPageView MainPage => mainPageView;
    public PlayPageView PlayPage => playPageView;

    // UI에서 발생한 사용자 입력 브로드캐스트 (Decoupled)
    public event Action<string, object> OnCommandTriggered;

    private void Awake()
    {
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (pageManager == null) pageManager = GetComponentInChildren<PageManager>(true);
        if (mainPageView == null) mainPageView = GetComponentInChildren<MainPageView>(true);
        if (playPageView == null) playPageView = GetComponentInChildren<PlayPageView>(true);

        if (playPageView != null && playPageView.BottomPanel != null)
        {
            playPageView.BottomPanel.OnButtonClicked += HandleButtonClicked;
        }
    }

    public void Initialize(object param)
    {
        SetInteractable(false);

        // 초기 시작 페이지: MainPage(로비)
        if (pageManager != null)
        {
            pageManager.ShowPage(UIPageType.MainPage);
        }
    }

    public void SetInteractable(bool isInteractable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = isInteractable;
        }
    }

    public void UpdateHp(int cur, int max)
    {
        if (playPageView != null && playPageView.TopHUD != null)
            playPageView.TopHUD.SetHp(cur, max);
    }

    public void UpdateGold(long gold)
    {
        if (playPageView != null && playPageView.TopHUD != null)
            playPageView.TopHUD.SetGold(gold);
    }

    public void UpdateStage(int stage)
    {
        if (playPageView != null && playPageView.TopHUD != null)
            playPageView.TopHUD.SetStage(stage);
    }

    public void ShowLog(string log)
    {
        if (playPageView != null && playPageView.BottomPanel != null)
            playPageView.BottomPanel.AppendLog(log);
    }

    private void HandleButtonClicked(string cmd, object payload)
    {
        OnCommandTriggered?.Invoke(cmd, payload);
    }

    private void OnDestroy()
    {
        if (playPageView != null && playPageView.BottomPanel != null)
        {
            playPageView.BottomPanel.OnButtonClicked -= HandleButtonClicked;
        }
    }
}
