using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

/// <summary>
/// 가로 스와이프 탭 뷰 컨트롤러 (클래시 로얄 스타일).
/// UniTask 기반의 부드러운 감속 스냅(Smooth Snap)과 수직 스크롤 충돌 방지 로직을 제공합니다.
/// </summary>
public class SwipeTabController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("[Content Settings]")]
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private float tabWidth = 720f;
    [SerializeField] private int totalTabs = 3;
    [SerializeField] private int initialTabIndex = 1;

    [Header("[Snap & Motion Settings]")]
    [SerializeField] private float snapDuration = 0.25f;
    [SerializeField] private float swipeVelocityThreshold = 10f;
    [SerializeField] private float verticalLockRatio = 1.2f;

    public int CurrentTabIndex { get; private set; } = 1;
    public event Action<int> OnTabChanged;

    private bool isSwipingHorizontal = false;
    private bool isDragStarted = false;
    private CancellationTokenSource snapCts;

    private void Awake()
    {
        if (contentRect == null)
            contentRect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        SnapToTab(initialTabIndex, instant: true);
    }

    public void SnapToTab(int targetIndex, bool instant = false)
    {
        targetIndex = Mathf.Clamp(targetIndex, 0, totalTabs - 1);
        CurrentTabIndex = targetIndex;

        float targetPosX = -targetIndex * tabWidth;

        snapCts?.Cancel();
        snapCts?.Dispose();
        snapCts = null;

        if (instant)
        {
            if (contentRect != null)
                contentRect.anchoredPosition = new Vector2(targetPosX, contentRect.anchoredPosition.y);
            OnTabChanged?.Invoke(CurrentTabIndex);
        }
        else
        {
            snapCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            SmoothSnapAsync(targetPosX, snapDuration, snapCts.Token).Forget();
        }
    }

    #region Drag Interfaces & Scroll Conflict Prevention

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragStarted = true;
        isSwipingHorizontal = false;

        float deltaX = Mathf.Abs(eventData.delta.x);
        float deltaY = Mathf.Abs(eventData.delta.y);

        if (deltaY > deltaX * verticalLockRatio)
        {
            isSwipingHorizontal = false;
            PassDragToScrollRect(eventData, ExecuteEvents.beginDragHandler);
            return;
        }

        isSwipingHorizontal = true;
        snapCts?.Cancel();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragStarted) return;

        if (!isSwipingHorizontal)
        {
            PassDragToScrollRect(eventData, ExecuteEvents.dragHandler);
            return;
        }

        Vector2 curPos = contentRect.anchoredPosition;
        curPos.x += eventData.delta.x;

        float minX = -(totalTabs - 1) * tabWidth;
        float maxX = 0f;
        if (curPos.x > maxX)
            curPos.x = maxX + (curPos.x - maxX) * 0.3f;
        else if (curPos.x < minX)
            curPos.x = minX + (curPos.x - minX) * 0.3f;

        contentRect.anchoredPosition = curPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragStarted) return;
        isDragStarted = false;

        if (!isSwipingHorizontal)
        {
            PassDragToScrollRect(eventData, ExecuteEvents.endDragHandler);
            return;
        }

        float curX = contentRect.anchoredPosition.x;
        float velocityX = eventData.delta.x;

        int targetIndex = CurrentTabIndex;

        if (Mathf.Abs(velocityX) > swipeVelocityThreshold)
        {
            if (velocityX < 0)
                targetIndex = Mathf.Min(CurrentTabIndex + 1, totalTabs - 1);
            else
                targetIndex = Mathf.Max(CurrentTabIndex - 1, 0);
        }
        else
        {
            targetIndex = Mathf.RoundToInt(-curX / tabWidth);
            targetIndex = Mathf.Clamp(targetIndex, 0, totalTabs - 1);
        }

        SnapToTab(targetIndex);
        cachedParentScrollRect = null;
    }

    private UnityEngine.UI.ScrollRect cachedParentScrollRect;

    private void PassDragToScrollRect<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> function) where T : IEventSystemHandler
    {
        if (cachedParentScrollRect == null)
        {
            GameObject target = eventData.pointerCurrentRaycast.gameObject;
            if (target != null && target != gameObject)
            {
                cachedParentScrollRect = target.GetComponentInParent<UnityEngine.UI.ScrollRect>();
                if (cachedParentScrollRect != null && cachedParentScrollRect.gameObject == gameObject)
                {
                    cachedParentScrollRect = null;
                }
            }
        }

        if (cachedParentScrollRect != null && cachedParentScrollRect is T handler)
        {
            function(handler, eventData);
        }
    }

    #endregion

    private async UniTask SmoothSnapAsync(float targetX, float duration, CancellationToken ct)
    {
        float startX = contentRect.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f); // Cubic Out

            float newX = Mathf.Lerp(startX, targetX, t);
            contentRect.anchoredPosition = new Vector2(newX, contentRect.anchoredPosition.y);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        contentRect.anchoredPosition = new Vector2(targetX, contentRect.anchoredPosition.y);
        OnTabChanged?.Invoke(CurrentTabIndex);
    }

    private void OnDestroy()
    {
        snapCts?.Cancel();
        snapCts?.Dispose();
    }
}
