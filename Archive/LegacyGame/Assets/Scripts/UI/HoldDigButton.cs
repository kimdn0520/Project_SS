using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// 은퇴용사 땅만파의 원형 DIG 버튼 컨트롤러.
/// - ICanvasRaycastFilter를 통한 원형 히트 판정 (투명 사각 모서리 터치 간섭 0%)
/// - 탭 시 즉시 1회 스윙, 홀드 유지 시 설정 주기(0.12초)로 반복 채굴
/// - 포인터 이탈(PointerExit) 및 업(PointerUp) 시 홀드 중단
/// - 모달 팝업, 앱 정지, 비활성화 시 Hard Cancel 처리 및 자동 복구 방지
/// </summary>
public class HoldDigButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICanvasRaycastFilter
{
    [Header("[Dig Settings]")]
    [SerializeField] private float initialHoldDelay = 0.25f; // 첫 홀드 판정 대기 (초)
    [SerializeField] private float repeatInterval = 0.12f;    // 연속 채굴 간격 (초)
    [SerializeField] private SessionPausePolicy pausePolicy;
    [SerializeField] private Selectable selectable;
    [SerializeField] private bool circularHitArea = true;

    [Header("[Visual References]")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Transform pressableFace;       // 눌리는 전면 비주얼 (Base는 고정)
    [SerializeField] private Image progressRing;            // 원형 진행 링
    [SerializeField] private Vector3 pressedScale = new Vector3(0.92f, 0.92f, 1f);
    [SerializeField] private Image capShadow;
    [SerializeField] private Graphic impactRing;
    [SerializeField] private Transform pickaxeIcon;
    [SerializeField] private float impactDepth = 10f, reboundLift = 11f, breakLift = 18f;
    private Quaternion iconRest;
    private Color shadowRest;

    public event Action OnDig;
    public event Action OnDigCanceled;
    public event Action<bool> OnPressedChanged;
    public bool IsPressed => isPressed;

    private Vector3 originalScale = Vector3.one;
    [SerializeField] private Vector3 pressOffset;
    private Vector3 originalPosition;
    private bool isPressed = false;
    private int activePointerId = -1;
    private CancellationTokenSource holdCts;

    private void Awake()
    {
        originalScale = pressableFace != null ? pressableFace.localScale : Vector3.one;
        originalPosition = pressableFace != null ? pressableFace.localPosition : Vector3.zero;
        if(pickaxeIcon!=null)iconRest=pickaxeIcon.localRotation;
        if(capShadow!=null)shadowRest=capShadow.color;

        if (progressRing != null)
        {
            progressRing.fillAmount = 0f;
            progressRing.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        if (pausePolicy == null) pausePolicy = SessionPausePolicy.Instance;
        if (pausePolicy != null)
        {
            pausePolicy.OnPauseStateChanged += HandleGlobalPause;
        }
    }

    private void OnDisable()
    {
        if (pausePolicy != null)
        {
            pausePolicy.OnPauseStateChanged -= HandleGlobalPause;
        }
        HardCancel();
    }

    private void HandleGlobalPause(bool isPaused)
    {
        if (isPaused)
        {
            HardCancel();
        }
    }

    #region ICanvasRaycastFilter (Circular Hit Detection)

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (rectTransform == null || !circularHitArea) return true;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        float radiusX = rect.width * 0.5f;
        float radiusY = rect.height * 0.5f;

        if (radiusX <= 0 || radiusY <= 0) return false;

        // 타원/원형 정규화 판정: (x/rx)^2 + (y/ry)^2 <= 1.0
        float normX = (localPoint.x - rect.center.x) / radiusX;
        float normY = (localPoint.y - rect.center.y) / radiusY;

        return (normX * normX + normY * normY) <= 1.0f;
    }

    #endregion

    #region Pointer Lifecycle & Tap / Hold Contract

    public void OnPointerDown(PointerEventData eventData)
    {
        // 전역 정지 상태이면 입력 무시
        if ((pausePolicy != null && pausePolicy.IsPaused) || (selectable != null && !selectable.IsInteractable()) || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // 첫 번째 손가락만 소유, 멀티터치 방지
        if (isPressed)
        {
            return;
        }

        activePointerId = eventData.pointerId;
        isPressed = true;
        OnPressedChanged?.Invoke(true);

        AnimateScale(pressedScale);

        // 1. 단타 1회 즉시 스윙 발동
        OnDig?.Invoke();

        // 2. 꾹 누르기(홀드) 타이머 및 반복 루프 가동
        holdCts?.Cancel();
        holdCts?.Dispose();
        holdCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

        HoldLoopAsync(holdCts.Token).Forget();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            ReleaseButton();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            ReleaseButton();
        }
    }

    private void ReleaseButton()
    {
        if (!isPressed) return;
        isPressed = false;
        activePointerId = -1;
        OnPressedChanged?.Invoke(false);
        OnDigCanceled?.Invoke();

        AnimateScale(originalScale);

        if (progressRing != null)
        {
            progressRing.fillAmount = 0f;
        }

        holdCts?.Cancel();
        holdCts?.Dispose();
        holdCts = null;
    }

    /// <summary>
    /// 팝업 열림, 포커스 아웃, 씬 이탈 시 강제 취소 (Hard Cancel)
    /// </summary>
    public void HardCancel()
    {
        if (isPressed)
        {
            ReleaseButton();
        }
        else
        {
            activePointerId = -1;
            holdCts?.Cancel();
            holdCts?.Dispose();
            holdCts = null;
        }
        if (pressableFace != null)
        {
            pressableFace.DOKill();
            pressableFace.localPosition = originalPosition;
            pressableFace.localScale = originalScale;
        }
        if(pickaxeIcon!=null){pickaxeIcon.DOKill();pickaxeIcon.localRotation=iconRest;}
        if(capShadow!=null){capShadow.DOKill();capShadow.rectTransform.DOKill();capShadow.color=shadowRest;capShadow.transform.localScale=Vector3.one;}
        if(impactRing!=null){impactRing.DOKill();impactRing.transform.DOKill();var c=impactRing.color;c.a=0;impactRing.color=c;impactRing.transform.localScale=Vector3.one;}
    }

    private async UniTaskVoid HoldLoopAsync(CancellationToken ct)
    {
        try
        {
            // 첫 홀드 인식 대기
            float elapsed = 0f;
            while (elapsed < initialHoldDelay)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.unscaledDeltaTime;

                if (progressRing != null)
                {
                    progressRing.fillAmount = Mathf.Clamp01(elapsed / initialHoldDelay);
                }
            }

            if (progressRing != null) progressRing.fillAmount = 1f;

            // 홀드 유지 중 연속 채굴 발동
            while (isPressed && !ct.IsCancellationRequested)
            {
                OnDig?.Invoke();
                await UniTask.Delay(TimeSpan.FromSeconds(repeatInterval), ignoreTimeScale: true, cancellationToken: ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (progressRing != null) progressRing.fillAmount = 0f;
        }
    }

    private void AnimateScale(Vector3 target)
    {
        if (pressableFace != null)
        {
            pressableFace.DOKill();
            float duration=isPressed?.055f:.18f;
            pressableFace.DOScale(target, duration).SetEase(Ease.OutQuad).SetUpdate(true);
            pressableFace.DOLocalMove(originalPosition + (isPressed ? pressOffset : Vector3.zero), duration).SetEase(isPressed?Ease.OutCubic:Ease.OutBack).SetUpdate(true);
        }
        if(capShadow!=null){capShadow.DOKill();capShadow.rectTransform.DOKill();capShadow.DOFade(isPressed?.48f:shadowRest.a,.08f).SetUpdate(true);capShadow.transform.DOScale(new Vector3(1,isPressed?.72f:1,1),.08f).SetUpdate(true);}
    }

    // Called by the mining result, so visual pulses follow actual strikes, not input polling.
    public void Pulse(float interval,bool broken=false)
    {
        if (!isPressed || pressableFace == null) return;
        pressableFace.DOKill();
        float duration = broken ? .30f : Mathf.Clamp(interval * .90f, .065f, .21f);
        Vector3 heldPosition = originalPosition + pressOffset;
        Vector3 contactScale = Vector3.Scale(pressedScale, new Vector3(1f, .975f, 1f));
        DOTween.Sequence().SetTarget(pressableFace).SetUpdate(true)
            .Append(pressableFace.DOLocalMove(heldPosition + Vector3.down * (impactDepth+(broken?3:0)), duration * .16f).SetEase(Ease.OutCubic))
            .Join(pressableFace.DOScale(contactScale, duration * .16f))
            .AppendInterval(duration*.08f)
            .Append(pressableFace.DOLocalMove(heldPosition+Vector3.up*(broken?breakLift:reboundLift),duration*.32f).SetEase(Ease.OutQuad))
            .Join(pressableFace.DOScale(pressedScale,duration*.32f))
            .Append(pressableFace.DOLocalMove(heldPosition,duration*.44f).SetEase(Ease.InQuad));
        if(capShadow!=null){capShadow.DOKill();capShadow.rectTransform.DOKill();capShadow.color=new Color(0,0,0,.6f);capShadow.transform.localScale=new Vector3(1,.65f,1);capShadow.DOFade(.4f,duration).SetUpdate(true);capShadow.transform.DOScaleY(.82f,duration).SetUpdate(true);}
        if(impactRing!=null)
        {
            impactRing.DOKill();impactRing.transform.DOKill();impactRing.transform.localScale=Vector3.one;
            float rippleDuration=broken?.32f:Mathf.Clamp(interval*.85f,.06f,.18f);
            var color=impactRing.color;color.a=broken?.9f:.55f;impactRing.color=color;
            impactRing.DOFade(0,rippleDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            impactRing.transform.DOScale(broken?new Vector3(1.14f,1.2f,1):new Vector3(1.07f,1.1f,1),rippleDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    #endregion

    private void OnDestroy()
    {
        HardCancel();
        if (pressableFace != null) pressableFace.DOKill();
    }

    private void OnApplicationPause(bool paused) { if (paused) HardCancel(); }
    private void OnApplicationFocus(bool focused) { if (!focused) HardCancel(); }

#if UNITY_EDITOR
    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        pressableFace = transform;
        selectable = GetComponent<Selectable>();
    }
#endif
}
