using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 상단 자동 전투의 주인공 현역 용사 액터.
/// 우측을 바라보며 Travel 상태에서는 질주 모션을 재생하고,
/// 적 조우(Encounter) 및 교전(Combat) 시 근접 공격 및 피격 연출을 수행합니다.
/// </summary>
public class ActiveHeroActor : MonoBehaviour
{
    [Header("[Visual & Animation]")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform weaponTransform;

    [Header("[Combat Offsets]")]
    [SerializeField] private Vector3 defaultWeaponEuler = Vector3.zero;
    [SerializeField] private Vector3 attackWeaponEuler = new Vector3(0, 0, -45f);

    private Vector3 originPos;
    private CancellationTokenSource actionCts;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        originPos = visualRoot.localPosition;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void PlayRun()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Play("Run", 0, 0f);
            animator.speed = 1.0f;
        }
    }

    public void PlayIdle()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            if (animator.HasState(0, Animator.StringToHash("Idle")))
            {
                animator.Play("Idle", 0, 0f);
            }
            else
            {
                animator.speed = 0f;
            }
        }
    }

    /// <summary>
    /// 공격 모션 및 검 휘두르기 연출
    /// </summary>
    public async UniTask PerformAttackAsync(CancellationToken ct)
    {
        actionCts?.Cancel();
        actionCts?.Dispose();
        actionCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());
        CancellationToken localCt = actionCts.Token;

        try
        {
            // 1. 공격 전진 스텝 (Windup & Strike)
            visualRoot.localPosition = originPos + new Vector3(0.2f, 0f, 0f);
            if (weaponTransform != null)
            {
                weaponTransform.localEulerAngles = attackWeaponEuler;
            }

            await UniTask.Delay(100, cancellationToken: localCt);

            // 2. 복귀 (Recovery)
            visualRoot.localPosition = originPos;
            if (weaponTransform != null)
            {
                weaponTransform.localEulerAngles = defaultWeaponEuler;
            }

            await UniTask.Delay(150, cancellationToken: localCt);
        }
        catch (OperationCanceledException)
        {
            visualRoot.localPosition = originPos;
            if (weaponTransform != null)
            {
                weaponTransform.localEulerAngles = defaultWeaponEuler;
            }
        }
    }

    /// <summary>
    /// 피격 넉백/흔들림
    /// </summary>
    public async UniTaskVoid PlayHitReactionAsync(CancellationToken ct)
    {
        try
        {
            visualRoot.localPosition = originPos + new Vector3(-0.15f, 0.05f, 0f);
            await UniTask.Delay(60, cancellationToken: ct);
            visualRoot.localPosition = originPos;
        }
        catch { }
    }

    public void ResetPosition()
    {
        if (visualRoot != null)
        {
            visualRoot.localPosition = originPos;
        }
    }

    private void OnDestroy()
    {
        actionCts?.Cancel();
        actionCts?.Dispose();
    }
}
