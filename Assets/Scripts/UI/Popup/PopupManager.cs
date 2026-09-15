using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 스택(중첩 팝업) 및 큐(순차 팝업) 기반 모달 팝업 매니저.
/// IPopupHandler 다형성과 IPopupAnimation 전략 패턴을 적용하였으며,
/// UniTaskCompletionSource를 통해 팝업 닫힘 및 결과값(Result) 반환을 비동기로 완벽 지원합니다.
/// </summary>
public class PopupManager : SingletonMonoBehaviour<PopupManager>
{
    [Header("[Hierarchy Root]")]
    [SerializeField] private Transform popupRoot;
    [SerializeField] private Camera renderCamera;
    public Camera RenderCamera => renderCamera != null ? renderCamera : (renderCamera = Camera.main);

    public void SetRenderCamera(Camera camera)
    {
        renderCamera = camera;
        foreach (var popup in popups.Values) BindCamera(popup);
    }

    private void BindCamera(IPopupHandler popup)
    {
        if (popup.Canvas == null) return;
        popup.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
        popup.Canvas.worldCamera = RenderCamera;
        popup.Canvas.planeDistance = 10f;
    }

    [Header("[Sorting Order Settings]")]
    [SerializeField] private int baseSortingOrder = 1000;
    [SerializeField] private int sortingOrderStep = 10;

    [Header("[Pre-registered Popup Prefabs]")]
    [SerializeField] private List<BasePopupHandler> popupPrefabs = new List<BasePopupHandler>();

    // 팝업 캐시 및 프리팹 딕셔너리
    private readonly Dictionary<string, IPopupHandler> popups = new Dictionary<string, IPopupHandler>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BasePopupHandler> prefabRegistry = new Dictionary<string, BasePopupHandler>(StringComparer.OrdinalIgnoreCase);

    // 열려있는 팝업 스택 (최상단 팝업이 Last)
    private readonly List<PopupItem> popupStack = new List<PopupItem>();

    // 대기 중인 팝업 큐 (현재 팝업이 닫힌 뒤 순차적으로 뜸)
    private readonly Queue<PopupItem> popupQueue = new Queue<PopupItem>();

    private CancellationTokenSource transitionCts;
    private readonly HashSet<IPopupHandler> closing = new HashSet<IPopupHandler>();

    private void ReleasePopup(IPopupHandler handler)
    {
        if (popups.TryGetValue(handler.PopupName, out var registered) && ReferenceEquals(registered, handler))
            popups.Remove(handler.PopupName);
        if (handler is Component component && component != null)
            Destroy(component.gameObject);
    }

    /// <summary>
    /// 전역 기본 팝업 연출 (개별 팝업에 Animation이 없을 때 사용)
    /// </summary>
    public IPopupAnimation GlobalAnimation { get; set; } = new ScaleFadePopupAnimation(0.15f);

    private bool isChanging;
    public bool IsChangingInstance => isChanging;
    public static bool IsChanging => Instance != null && Instance.isChanging;

    public static bool IsOpenAny => Instance != null && Instance.popupStack.Count > 0;
    public static int OpenedCount => Instance != null ? Instance.popupStack.Count : 0;
    public static IPopupHandler CurrentPopup => Instance != null && Instance.popupStack.Count > 0 ? Instance.popupStack[^1].Handler : null;

    // 외부 연동용 이벤트 훅
    public static event Action<IPopupHandler> OnPopupWillOpen;
    public static event Action<IPopupHandler> OnPopupOpened;
    public static event Action<IPopupHandler> OnPopupWillClose;
    public static event Action<IPopupHandler> OnPopupClosed;

    protected override void Awake()
    {
        if (persistAcrossScenes && transform.parent != null)
        {
            transform.SetParent(null);
        }
        base.Awake();

        if (popupRoot == null)
            popupRoot = transform;

        // 인스펙터에 등록된 프리팹 등록
        foreach (var prefab in popupPrefabs)
        {
            if (prefab != null)
            {
                RegisterPrefab(prefab.PopupName, prefab);
            }
        }
    }

    private void Update()
    {
        // 안드로이드 / PC Escape(뒤로가기) 입력 처리
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackInput();
        }
    }

    public static bool HandleBackInput()
    {
        if (CurrentPopup != null)
        {
            CurrentPopup.OnEscape();
            return true;
        }
        return false;
    }

    #region Registration & Prefab Management

    public static void RegisterPrefab(string popupName, BasePopupHandler prefab)
    {
        if (Instance == null || string.IsNullOrEmpty(popupName) || prefab == null) return;
        Instance.prefabRegistry[popupName] = prefab;
    }

    public static void RegisterPopup(string popupName, IPopupHandler handler)
    {
        if (Instance == null || string.IsNullOrEmpty(popupName) || handler == null) return;
        Instance.popups[popupName] = handler;
    }

    public static void UnregisterPopup(string popupName)
    {
        if (Instance == null || string.IsNullOrEmpty(popupName)) return;
        Instance.popups.Remove(popupName);
    }

    public static bool Contains(string popupName)
    {
        if (Instance == null || string.IsNullOrEmpty(popupName)) return false;
        return Instance.popups.ContainsKey(popupName) || Instance.prefabRegistry.ContainsKey(popupName);
    }

    private async UniTask<IPopupHandler> GetOrCreatePopupAsync(string popupName)
    {
        if (!prefabRegistry.ContainsKey(popupName) && popups.TryGetValue(popupName, out IPopupHandler cached))
        {
            return cached;
        }

        // 1. 등록된 프리팹에서 생성
        if (prefabRegistry.TryGetValue(popupName, out BasePopupHandler prefab) && prefab != null)
        {
            BasePopupHandler instance = Instantiate(prefab, popupRoot);
            instance.name = popupName;
            instance.Hide();
            popups[popupName] = instance;
            return instance;
        }

        // 2. Resources 폴더 폴백 로드
        ResourceRequest request = Resources.LoadAsync<BasePopupHandler>($"Prefabs/Popups/{popupName}");
        await request;

        if (request.asset is BasePopupHandler resourcePrefab)
        {
            prefabRegistry[popupName] = resourcePrefab;
            var handler = Instantiate(resourcePrefab, popupRoot);
            handler.name = popupName;
            handler.Hide();
            popups[popupName] = handler;
            return handler;
        }

        Debug.LogError($"[PopupManager] 팝업을 찾을 수 없습니다: {popupName}");
        return null;
    }

    #endregion

    #region Show & Queue API

    /// <summary>
    /// 팝업을 즉시 띄웁니다. (Fire-and-forget)
    /// </summary>
    public static void Show(string popupName, object param = null, Action<object> closeCallback = null)
    {
        ShowAsync(popupName, param, closeCallback).Forget();
    }

    /// <summary>
    /// 팝업을 띄우고 닫힐 때까지 대기하여 결과 파라미터를 반환받습니다.
    /// </summary>
    public static async UniTask<T> ShowAsync<T>(string popupName, object param = null)
    {
        object result = await ShowAsync(popupName, param);
        return result is T casted ? casted : default;
    }

    public static async UniTask<object> ShowAsync(string popupName, object param = null, Action<object> closeCallback = null)
    {
        if (Instance == null) return null;
        var existing = Instance.popupStack.Find(p => string.Equals(p.Handler.PopupName, popupName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            var existingResult = await existing.CompletionSource.Task;
            closeCallback?.Invoke(existingResult);
            return existingResult;
        }

        IPopupHandler handler = await Instance.GetOrCreatePopupAsync(popupName);
        if (handler == null) return null;

        var tcs = new UniTaskCompletionSource<object>();
        var item = new PopupItem(handler, param, closeCallback, tcs);

        await Instance.ShowInternalAsync(item);
        return await tcs.Task;
    }

    /// <summary>
    /// 팝업을 큐에 등록합니다. 현재 열려있는 팝업이 없으면 즉시 열리고, 있으면 닫힌 뒤 순차적으로 열립니다.
    /// </summary>
    public static void Queue(string popupName, object param = null, Action<object> closeCallback = null)
    {
        QueueAsync(popupName, param, closeCallback).Forget();
    }

    public static async UniTask<T> QueueAsync<T>(string popupName, object param = null)
    {
        object result = await QueueAsync(popupName, param);
        return result is T casted ? casted : default;
    }

    public static async UniTask<object> QueueAsync(string popupName, object param = null, Action<object> closeCallback = null)
    {
        if (Instance == null) return null;

        IPopupHandler handler = await Instance.GetOrCreatePopupAsync(popupName);
        if (handler == null) return null;

        var tcs = new UniTaskCompletionSource<object>();
        var item = new PopupItem(handler, param, closeCallback, tcs);

        if (Instance.popupStack.Count == 0 && !Instance.isChanging)
        {
            await Instance.ShowInternalAsync(item);
        }
        else
        {
            Instance.popupQueue.Enqueue(item);
        }

        return await tcs.Task;
    }

    private async UniTask ShowInternalAsync(PopupItem item)
    {
        if (item == null || item.Handler == null) return;

        if (item.Handler.IsOpen)
        {
            Debug.LogWarning($"[PopupManager] 이미 열려있는 팝업입니다: {item.Handler.PopupName}");
            return;
        }

        transitionCts?.Cancel();
        transitionCts?.Dispose();
        transitionCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken ct = transitionCts.Token;

        isChanging = true;
        OnPopupWillOpen?.Invoke(item.Handler);

        try
        {
            // 스택에 등록 및 소팅 오더 설정
            popupStack.Add(item);
            int newOrder = baseSortingOrder + (popupStack.Count * sortingOrderStep);
            item.Handler.SetSortingOrder(newOrder);

            BindCamera(item.Handler);
            item.Handler.Show();
            item.Handler.OnWillEnter(item.Param);

            // 애니메이션 연출 (개별 우선, 없으면 글로벌)
            IPopupAnimation anim = item.Handler.Animation ?? GlobalAnimation;
            if (anim != null)
            {
                await anim.AnimateInAsync(item.Handler, ct);
            }

            item.Handler.OnDidEnter(item.Param);
            OnPopupOpened?.Invoke(item.Handler);
        }
        catch (OperationCanceledException)
        {
            // 의도된 취소
        }
        finally
        {
            isChanging = false;
        }
    }

    #endregion

    #region Close API

    /// <summary>
    /// 최상단 팝업을 닫습니다.
    /// </summary>
    public static void Close(object result = null)
    {
        if (Instance == null || Instance.popupStack.Count == 0) return;
        CloseByItemAsync(Instance.popupStack[^1], result).Forget();
    }

    public static async UniTask CloseAsync(object result = null)
    {
        if (Instance == null || Instance.popupStack.Count == 0) return;
        await CloseByItemAsync(Instance.popupStack[^1], result);
    }

    /// <summary>
    /// 특정 이름의 팝업을 닫습니다.
    /// </summary>
    public static void CloseByName(string popupName, object result = null)
    {
        if (Instance == null) return;
        PopupItem item = Instance.popupStack.Find(p => string.Equals(p.Handler.PopupName, popupName, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            CloseByItemAsync(item, result).Forget();
        }
    }

    /// <summary>
    /// 특정 팝업 핸들러 인스턴스를 닫습니다.
    /// </summary>
    public static void CloseByHandler(IPopupHandler handler, object result = null)
    {
        if (Instance == null || handler == null) return;
        PopupItem item = Instance.popupStack.Find(p => p.Handler == handler);
        if (item != null)
        {
            CloseByItemAsync(item, result).Forget();
        }
    }

    /// <summary>
    /// 열려있는 모든 팝업을 순차적으로 닫습니다.
    /// </summary>
    public static void CloseAll(object result = null)
    {
        CloseAllAsync(result).Forget();
    }

    public static async UniTask CloseAllAsync(object result = null)
    {
        if (Instance == null) return;

        while (Instance.popupStack.Count > 0)
        {
            PopupItem top = Instance.popupStack[^1];
            await CloseByItemAsync(top, result);
        }

        while (Instance.popupQueue.Count > 0)
        {
            var pending = Instance.popupQueue.Dequeue();
            Instance.ReleasePopup(pending.Handler);
            pending.Complete(null);
        }
    }

    /// <summary>
    /// 연출 없이 즉시 최상단 팝업을 닫습니다.
    /// </summary>
    public static void CloseImmediate(object result = null)
    {
        if (Instance == null || Instance.popupStack.Count == 0) return;

        PopupItem item = Instance.popupStack[^1];
        Instance.transitionCts?.Cancel();
        Instance.popupStack.RemoveAt(Instance.popupStack.Count - 1);

        item.Handler.OnWillLeave();
        item.Handler.OnDidLeave();
        item.Handler.Hide();
        Instance.ReleasePopup(item.Handler);
        item.Complete(result);
        OnPopupClosed?.Invoke(item.Handler);

        Instance.CheckNextQueuedPopup();
    }

    private static async UniTask CloseByItemAsync(PopupItem item, object result)
    {
        if (Instance == null || item == null || !Instance.popupStack.Contains(item) || !Instance.closing.Add(item.Handler)) return;

        Instance.transitionCts?.Cancel();
        Instance.transitionCts?.Dispose();
        Instance.transitionCts = CancellationTokenSource.CreateLinkedTokenSource(Instance.GetCancellationTokenOnDestroy());
        CancellationToken ct = Instance.transitionCts.Token;

        Instance.isChanging = true;
        OnPopupWillClose?.Invoke(item.Handler);

        try
        {
            item.Handler.OnWillLeave();

            IPopupAnimation anim = item.Handler.Animation ?? Instance.GlobalAnimation;
            if (anim != null)
            {
                await anim.AnimateOutAsync(item.Handler, ct);
            }

            item.Handler.OnDidLeave();
            item.Handler.Hide();

            Instance.popupStack.Remove(item);
            Instance.ReleasePopup(item.Handler);
            item.Complete(result);
            OnPopupClosed?.Invoke(item.Handler);
        }
        catch (OperationCanceledException)
        {
            // 비동기 취소
        }
        finally
        {
            Instance.closing.Remove(item.Handler);
            Instance.isChanging = false;
            Instance.CheckNextQueuedPopup();
        }
    }

    private void CheckNextQueuedPopup()
    {
        if (popupQueue.Count > 0 && popupStack.Count == 0 && !isChanging)
        {
            PopupItem next = popupQueue.Dequeue();
            ShowInternalAsync(next).Forget();
        }
    }

    /// <summary>
    /// 스택과 큐에 등록된 모든 팝업 상태를 초기화합니다.
    /// </summary>
    public static void Clear()
    {
        if (Instance == null) return;
        Instance.transitionCts?.Cancel();

        foreach (var item in Instance.popupStack)
        {
            item.Handler.OnWillLeave();
            item.Handler.OnDidLeave();
            item.Handler.Hide();
            Instance.ReleasePopup(item.Handler);
            item.Complete(null);
        }

        Instance.popupStack.Clear();
        while (Instance.popupQueue.Count > 0)
        {
            var pending = Instance.popupQueue.Dequeue();
            Instance.ReleasePopup(pending.Handler);
            pending.Complete(null);
        }
        Instance.isChanging = false;
    }

    #endregion

    private void OnDestroy()
    {
        transitionCts?.Cancel();
        transitionCts?.Dispose();
    }

    /// <summary>
    /// 내부 팝업 상태 및 비동기 완료를 보관하는 레코드
    /// </summary>
    private class PopupItem
    {
        public IPopupHandler Handler { get; }
        public object Param { get; }
        public Action<object> CloseCallback { get; }
        public UniTaskCompletionSource<object> CompletionSource { get; }

        public PopupItem(IPopupHandler handler, object param, Action<object> closeCallback, UniTaskCompletionSource<object> utcs)
        {
            Handler = handler;
            Param = param;
            CloseCallback = closeCallback;
            CompletionSource = utcs;
        }

        public void Complete(object result)
        {
            CloseCallback?.Invoke(result);
            CompletionSource?.TrySetResult(result);
        }
    }
}
