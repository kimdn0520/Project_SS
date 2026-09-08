using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 각 Page 프리팹의 최상단에 부착되는 공통 생명주기 기본 추상 클래스.
/// GetComponent 런타임 호출을 배제하고 [SerializeField] 인스펙터 사전 할당을 우선합니다.
/// </summary>
public abstract class PageBase : MonoBehaviour
{
    public abstract UIPageType PageType { get; }

    [Header("[Page Base References]")]
    [SerializeField] protected CanvasGroup canvasGroup; // UI Fade 및 입력 차단용 (없을 수도 있음)
    [SerializeField] protected float fadeDuration = 0.2f;

    public CanvasGroup CanvasGroup => canvasGroup;
    public bool IsOpen { get; protected set; }

    protected Coroutine fadeCoroutine;

    /// <summary>
    /// 최초 생성 또는 씬 로드 시 1회 초기화 (데이터 캐싱 등)
    /// </summary>
    public virtual void Init(object param = null) { }

    /// <summary>
    /// 페이지 진입 시 호출 (활성화 및 페이드 인, 게임 루프 시작)
    /// </summary>
    public virtual void OnEnter(object param = null)
    {
        gameObject.SetActive(true);
        IsOpen = true;

        if (canvasGroup != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, fadeDuration, () =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                OnEnterComplete();
            }));
        }
        else
        {
            OnEnterComplete();
        }
    }

    /// <summary>
    /// 페이지 퇴장 시 호출 (입력 차단, 페이드 아웃 후 비활성화, 게임 루프 정지)
    /// </summary>
    public virtual void OnLeave(Action onClosed = null)
    {
        IsOpen = false;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, fadeDuration, () =>
            {
                gameObject.SetActive(false);
                OnLeaveComplete();
                onClosed?.Invoke();
            }));
        }
        else
        {
            gameObject.SetActive(false);
            OnLeaveComplete();
            onClosed?.Invoke();
        }
    }

    /// <summary>
    /// 페이드 인 완료 시점 가상 함수
    /// </summary>
    protected virtual void OnEnterComplete() { }

    /// <summary>
    /// 페이드 아웃 및 비활성화 완료 시점 가상 함수
    /// </summary>
    protected virtual void OnLeaveComplete() { }

    protected IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        if (duration <= 0f || canvasGroup == null)
        {
            if (canvasGroup != null) canvasGroup.alpha = to;
            onComplete?.Invoke();
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
        onComplete?.Invoke();
    }
}