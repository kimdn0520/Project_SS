using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 단일 활성 페이지 관리 및 UniTask 기반 페이지 전환 생명주기를 총괄하는 매니저.
/// IPageHandler 다형성을 통해 UI 및 2D 씬 페이지를 유연하게 관리하며,
/// IPageTransition 전략 패턴으로 전환 연출(페이드, 슬라이드, 즉시 전환 등)을 자유롭게 교체할 수 있습니다.
/// </summary>
public class PageManager : SingletonMonoBehaviour<PageManager>
{
    [Header("[Page Prefabs (SceneBase)]")]
    [SerializeField] private SceneBase mainPagePrefab;
    [SerializeField] private SceneBase playPagePrefab;

    [Header("[Dependencies]")]
    [SerializeField] private Camera mainCamera; // 페이지 캔버스에 주입할 Render Camera

    [Header("[Hierarchy Roots]")]
    [SerializeField] private Transform pagesRoot;
    [SerializeField] private Transform popupRoot;

    [Header("[Transition Settings]")]
    [SerializeField] private UIPageType initialPage = UIPageType.PlayPage;
    [SerializeField] private float pageFadeDuration = 0.2f;

    // 페이지 레지스트리 (페이지 이름 기반 관리)
    private readonly Dictionary<string, IPageHandler> pages = new Dictionary<string, IPageHandler>(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<PopupBase> popupStack = new Stack<PopupBase>();
    private CancellationTokenSource transitionCts;

    /// <summary>
    /// 현재 적용 중인 페이지 전환 트랜지션 (기본값: FadePageTransition)
    /// 런타임에 커스텀 트랜지션(슬라이드, 줌 등)으로 교체할 수 있습니다.
    /// </summary>
    public IPageTransition Transition { get; set; }

    /// <summary>
    /// 현재 페이지 전환이 진행 중인지 여부
    /// </summary>
    public bool IsChanging { get; private set; }
    public bool IsTransitioning => IsChanging; // 기존 코드 호환용 프로퍼티

    /// <summary>
    /// 현재 활성화된 페이지 핸들러
    /// </summary>
    public IPageHandler CurrentPage { get; private set; }
    public SceneBase CurrentScenePage => CurrentPage as SceneBase;
    public UIPageType CurrentPageType { get; private set; } = UIPageType.None;

    public int PageCount => pages.Count;

    // 외부 시스템에서 페이지 생명주기에 반응할 수 있는 이벤트 훅
    public event Action<IPageHandler, IPageHandler> OnPageWillChange; // (from, to)
    public event Action<IPageHandler> OnPageHandlerChanged;           // (currentPage)
    public event Action<UIPageType> OnPageChanged;                    // 하위 호환용 (pageType)

    protected override void Awake()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        base.Awake();

        if (pagesRoot == null)
            pagesRoot = transform;

        // 기본 트랜지션 설정 (CanvasGroup Fade)
        Transition = new FadePageTransition(pageFadeDuration);

        PreloadPages();
    }

    private void Start()
    {
        if (initialPage != UIPageType.None)
        {
            ChangeImmediate(initialPage);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackInput();
        }
    }

    /// <summary>
    /// 인스펙터에 등록된 기본 프리팹을 사전 인스턴스화하고 카메라를 주입(DI)하여 레지스트리에 등록합니다.
    /// </summary>
    private void PreloadPages()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        pages.Clear();

        // 1. MainPage
        Transform mainChild = pagesRoot != null ? pagesRoot.Find("MainPage") : null;
        SceneBase mainInstance = mainChild != null ? mainChild.GetComponent<SceneBase>() : null;
        if (mainInstance == null && mainPagePrefab != null)
        {
            mainInstance = Instantiate(mainPagePrefab, pagesRoot);
            mainInstance.gameObject.name = "MainPage";
        }
        if (mainInstance != null)
        {
            mainInstance.SetupRenderCamera(mainCamera);
            mainInstance.Hide();
            RegisterPage(UIPageType.MainPage, mainInstance);
        }

        // 2. PlayPage (씬 내 인스턴스가 존재할 경우 우선 재사용하여 위지윅 에디터 편집 완벽 지원)
        Transform playChild = pagesRoot != null ? pagesRoot.Find("PlayPage") : null;
        SceneBase playInstance = playChild != null ? playChild.GetComponent<SceneBase>() : null;
        if (playInstance == null && playPagePrefab != null)
        {
            playInstance = Instantiate(playPagePrefab, pagesRoot);
            playInstance.gameObject.name = "PlayPage";
        }
        if (playInstance != null)
        {
            playInstance.SetupRenderCamera(mainCamera);
            playInstance.Hide();
            RegisterPage(UIPageType.PlayPage, playInstance);
        }
    }

    #region Page Registry (Add / Remove / Get)

    public void RegisterPage(string pageName, IPageHandler page)
    {
        if (string.IsNullOrEmpty(pageName) || page == null)
        {
            Debug.LogWarning("[PageManager] RegisterPage 실패: 이름 또는 페이지 핸들러가 유효하지 않습니다.");
            return;
        }

        pages[pageName] = page;
    }

    public void RegisterPage(UIPageType pageType, IPageHandler page)
    {
        RegisterPage(pageType.ToString(), page);
    }

    public void UnregisterPage(string pageName)
    {
        if (!string.IsNullOrEmpty(pageName))
        {
            pages.Remove(pageName);
        }
    }

    public void UnregisterPage(UIPageType pageType)
    {
        UnregisterPage(pageType.ToString());
    }

    public bool HasPage(string pageName) => pages.ContainsKey(pageName);
    public bool HasPage(UIPageType pageType) => pages.ContainsKey(pageType.ToString());

    public IPageHandler GetPage(string pageName)
    {
        pages.TryGetValue(pageName, out IPageHandler page);
        return page;
    }

    public T GetPage<T>(string pageName) where T : class, IPageHandler
    {
        return GetPage(pageName) as T;
    }

    public T GetPage<T>(UIPageType pageType) where T : class, IPageHandler
    {
        return GetPage(pageType.ToString()) as T;
    }

    #endregion

    #region Page Navigation (Change / ChangeImmediate / Clear)

    /// <summary>
    /// 트랜지션 연출과 함께 페이지를 전환합니다. (Fire-and-forget 비동기)
    /// </summary>
    public void Change(string pageName, object param = null)
    {
        ChangeAsync(pageName, param, enableTransition: true).Forget();
    }

    public void Change(UIPageType pageType, object param = null)
    {
        Change(pageType.ToString(), param);
    }

    /// <summary>
    /// 트랜지션 없이 즉시 페이지를 전환합니다. (Fire-and-forget 비동기)
    /// </summary>
    public void ChangeImmediate(string pageName, object param = null)
    {
        ChangeAsync(pageName, param, enableTransition: false).Forget();
    }

    public void ChangeImmediate(UIPageType pageType, object param = null)
    {
        ChangeImmediate(pageType.ToString(), param);
    }

    /// <summary>
    /// 기존 코드와의 하위 호환성을 위한 ShowPage 메서드
    /// </summary>
    public void ShowPage(UIPageType pageType, object param = null)
    {
        Change(pageType, param);
    }

    public UniTask<bool> ShowPageAsync(UIPageType pageType, object param = null)
    {
        return ChangeAsync(pageType.ToString(), param, enableTransition: true);
    }

    /// <summary>
    /// 비동기 페이지 전환 핵심 파이프라인.
    /// 불필요한 예외 포획을 배제하고, 취소(CancellationToken)와 상태 무결성(finally)을 단일 블록으로 관리합니다.
    /// </summary>
    public async UniTask<bool> ChangeAsync(string pageName, object param = null, bool enableTransition = true)
    {
        if (IsChanging)
        {
            Debug.LogWarning($"[PageManager] 이미 페이지 전환이 진행 중입니다. 요청 무시: {pageName}");
            return false;
        }

        if (!pages.TryGetValue(pageName, out IPageHandler targetPage))
        {
            Debug.LogError($"[PageManager] 등록되지 않은 페이지입니다: {pageName}");
            return false;
        }

        if (CurrentPage == targetPage)
        {
            Debug.LogWarning($"[PageManager] 이미 활성화된 페이지입니다: {pageName}");
            return false;
        }

        // 이전 전환 작업 취소 및 새 CancellationToken 생성
        transitionCts?.Cancel();
        transitionCts?.Dispose();
        transitionCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken ct = transitionCts.Token;

        IsChanging = true;
        OnPageWillChange?.Invoke(CurrentPage, targetPage);

        try
        {
            // 1. 기존 페이지 퇴장 시퀀스
            if (CurrentPage != null)
            {
                CurrentPage.OnWillLeave();

                if (enableTransition && Transition != null)
                {
                    await Transition.TransitionOutAsync(CurrentPage, ct);
                }

                CurrentPage.OnDidLeave();
                CurrentPage.Hide();
            }

            // 2. 새 페이지 진입 시퀀스
            CurrentPage = targetPage;
            if (Enum.TryParse(pageName, out UIPageType parsedType))
            {
                CurrentPageType = parsedType;
            }
            else
            {
                CurrentPageType = UIPageType.None;
            }

            CurrentPage.Show();
            CurrentPage.OnWillEnter(param);

            if (enableTransition && Transition != null)
            {
                await Transition.TransitionInAsync(CurrentPage, ct);
            }

            CurrentPage.OnDidEnter();

            // 3. 전환 완료 이벤트 통지
            OnPageHandlerChanged?.Invoke(CurrentPage);
            if (CurrentPageType != UIPageType.None)
            {
                OnPageChanged?.Invoke(CurrentPageType);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            // 의도된 비동기 취소 (빠른 연속 클릭 또는 오브젝트 파괴)
            return false;
        }
        finally
        {
            IsChanging = false;
        }
    }

    /// <summary>
    /// 현재 활성화된 페이지를 즉시 퇴장 처리하고 비활성화합니다.
    /// </summary>
    public void Clear()
    {
        if (CurrentPage != null)
        {
            CurrentPage.OnWillLeave();
            CurrentPage.OnDidLeave();
            CurrentPage.Hide();
            CurrentPage = null;
            CurrentPageType = UIPageType.None;
        }

        IsChanging = false;
    }

    #endregion

    #region Popup Management & Back Input

    public void PushPopup(PopupBase popup, object param = null)
    {
        if (popup == null) return;

        if (popupRoot != null && popup.transform.parent != popupRoot)
        {
            popup.transform.SetParent(popupRoot, false);
        }

        popup.Open(param);
        popupStack.Push(popup);
    }

    public void PopPopup()
    {
        if (popupStack.Count > 0)
        {
            PopupBase top = popupStack.Pop();
            top.Close();
        }
    }

    private void HandleBackInput()
    {
        // 1. PopupManager에 열린 팝업이 있으면 팝업 먼저 닫기
        if (PopupManager.IsOpenAny)
        {
            PopupManager.HandleBackInput();
            return;
        }

        // 2. 레거시 팝업 스택 체크
        if (popupStack.Count > 0)
        {
            PopPopup();
            return;
        }

        // 3. 인게임 화면일 때 로비로 돌아가기
        if (CurrentPageType == UIPageType.PlayPage)
        {
            Change(UIPageType.MainPage);
        }
    }

    #endregion

    private void OnDestroy()
    {
        transitionCts?.Cancel();
        transitionCts?.Dispose();
    }
}
