using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 은퇴용사 '땅만파'의 곡괭이 휘두르기 액션 및 광석 피격 연출을 전담하는 뷰.
/// </summary>
public class DiggerActorView : MonoBehaviour
{
    [Header("[Digger Body & Pickaxe]")]
    [SerializeField] private Transform diggerBody;
    [SerializeField] private Transform pickaxeTransform;
    [SerializeField] private Vector3 idlePickaxeEuler = new Vector3(0, 0, 45f);
    [SerializeField] private Vector3 strikePickaxeEuler = new Vector3(0, 0, -40f);

    [Header("[Target Ore]")]
    [SerializeField] private Transform targetOre;
    [SerializeField] private MiningBlockView targetBlockView;

    [Header("[VFX]")]
    [SerializeField] private GameObject hitVfxPrefab;

    private Vector3 originBodyPos;
    private Vector3 originOreScale;
    private CancellationTokenSource swingCts;

    private void Awake()
    {
        if (diggerBody != null)
            originBodyPos = diggerBody.localPosition;

        if (targetOre != null)
            originOreScale = targetOre.localScale;

        if (pickaxeTransform != null)
            pickaxeTransform.localEulerAngles = idlePickaxeEuler;
    }

    /// <summary>
    /// 곡괭이를 힘차게 내리치며 광석 타격 (단타/연타 모두 지원)
    /// </summary>
    public void SwingPickaxe(int damagePower)
    {
        swingCts?.Cancel();
        swingCts?.Dispose();
        swingCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        AnimateSwingAsync(damagePower, swingCts.Token).Forget();
    }

    private async UniTaskVoid AnimateSwingAsync(int damagePower, CancellationToken ct)
    {
        try
        {
            // 1. 곡괭이 내려치기 (빠르게 아래로 회전 + 몸체 살짝 숙임)
            if (pickaxeTransform != null)
                pickaxeTransform.localEulerAngles = strikePickaxeEuler;

            if (diggerBody != null)
                diggerBody.localPosition = originBodyPos + new Vector3(0.12f, -0.08f, 0f);

            // 2. 광석 타격 및 파티클
            if (targetBlockView != null)
            {
                targetBlockView.Damage(damagePower);
            }

            if (targetOre != null)
            {
                PunchOreAsync(ct).Forget();
            }

            if (hitVfxPrefab != null && targetOre != null)
            {
                GameObject vfx = Instantiate(hitVfxPrefab, targetOre.position, Quaternion.identity);
                Destroy(vfx, 0.5f);
            }

            // 3. 0.07초 후 곡괭이 원위치 복귀
            await UniTask.Delay(70, cancellationToken: ct);

            if (pickaxeTransform != null)
                pickaxeTransform.localEulerAngles = idlePickaxeEuler;

            if (diggerBody != null)
                diggerBody.localPosition = originBodyPos;
        }
        catch (System.OperationCanceledException)
        {
            if (pickaxeTransform != null)
                pickaxeTransform.localEulerAngles = idlePickaxeEuler;
            if (diggerBody != null)
                diggerBody.localPosition = originBodyPos;
        }
    }

    private async UniTaskVoid PunchOreAsync(CancellationToken ct)
    {
        if (targetOre == null) return;
        targetOre.localScale = originOreScale * 1.15f;
        try
        {
            await UniTask.Delay(50, cancellationToken: ct);
            if (targetOre != null)
                targetOre.localScale = originOreScale;
        }
        catch
        {
            if (targetOre != null)
                targetOre.localScale = originOreScale;
        }
    }

    public void SetTargetOre(Transform oreTransform, MiningBlockView blockView)
    {
        targetOre = oreTransform;
        targetBlockView = blockView;
        if (targetOre != null)
            originOreScale = targetOre.localScale;
    }

    private void OnDestroy()
    {
        swingCts?.Cancel();
        swingCts?.Dispose();
    }
}
