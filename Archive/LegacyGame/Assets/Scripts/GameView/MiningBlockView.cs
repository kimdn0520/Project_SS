using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 지하 광산에 배치되는 채굴 대상 암반/블록 뷰.
/// 내구도(HP)를 가지며, 곡괭이 피격 시 펀치 연출 및 파괴 처리를 담당합니다.
/// </summary>
public class MiningBlockView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer blockRenderer;
    [SerializeField] private int maxHp = 50;
    private int currentHp;
    private bool isDestroyed = false;
    private Vector3 originPos;

    public bool IsDestroyed => isDestroyed;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;

    private void Awake()
    {
        originPos = transform.localPosition;
        currentHp = maxHp;
    }

    public void Damage(int amount)
    {
        if (isDestroyed) return;

        currentHp -= amount;

        // 피격 셰이크 연출
        ShakeBlockAsync(this.GetCancellationTokenOnDestroy()).Forget();

        if (currentHp <= 0)
        {
            isDestroyed = true;
            gameObject.SetActive(false);
        }
    }

    public void ResetBlock(int newMaxHp)
    {
        maxHp = newMaxHp;
        currentHp = maxHp;
        isDestroyed = false;
        transform.localPosition = originPos;
        gameObject.SetActive(true);
    }

    private async UniTaskVoid ShakeBlockAsync(CancellationToken ct)
    {
        transform.localPosition = originPos + new Vector3(Random.Range(-0.06f, 0.06f), Random.Range(-0.04f, 0.04f), 0f);
        try
        {
            await UniTask.Delay(50, cancellationToken: ct);
            transform.localPosition = originPos;
        }
        catch
        {
            transform.localPosition = originPos;
        }
    }
}
