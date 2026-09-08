using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 방치형 RPG 스타일의 2D 인게임 콘텐츠 구현체.
/// 코루틴을 배제하고 UniTask 기반으로 자동 전투 루프 및 스프라이트 펀치 연출을 구동합니다.
/// </summary>
public class IdleRpgGameContent : MonoBehaviour, IGameContent
{
    public string ContentId => "IdleRpg";

    [Header("[World 2D Objects]")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private Transform monsterTransform;
    [SerializeField] private SpriteRenderer monsterRenderer;

    [Header("[Game Stats]")]
    [SerializeField] private int maxHp = 100;
    private int currentHp;
    private long gold = 0;
    private int stage = 1;
    private bool isPlaying = false;
    private CancellationTokenSource battleCts;

    public event Action<int, int> OnHpChanged;
    public event Action<long> OnGoldChanged;
    public event Action<int> OnStageChanged;
    public event Action<string> OnLogMessage;

    public void Initialize(object param)
    {
        currentHp = maxHp;
        gold = 0;
        stage = 1;

        if (playerTransform != null)
            playerTransform.localPosition = new Vector3(-1.2f, 1.2f, 0f);

        if (monsterTransform != null)
            monsterTransform.localPosition = new Vector3(1.2f, 1.2f, 0f);

        NotifyAllStats();
        OnLogMessage?.Invoke("World initialized ready for battle.");
    }

    public void StartContent()
    {
        isPlaying = true;
        battleCts?.Cancel();
        battleCts?.Dispose();
        battleCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        AutoBattleLoopAsync(battleCts.Token).Forget();
        OnLogMessage?.Invoke("Auto battle loop started!");
    }

    public void PauseContent()
    {
        isPlaying = false;
        battleCts?.Cancel();
        battleCts?.Dispose();
        battleCts = null;
    }

    public void ResumeContent()
    {
        if (!isPlaying)
        {
            StartContent();
        }
    }

    public void StopContent()
    {
        PauseContent();
        OnLogMessage?.Invoke("Battle stopped. World paused.");
    }

    public void ExecuteCommand(string commandName, object payload)
    {
        switch (commandName)
        {
            case "Attack":
                PerformManualAttack();
                break;
            case "Skill1":
                CastSkill();
                break;
            case "Upgrade":
                UpgradeStats();
                break;
        }
    }

    private void PerformManualAttack()
    {
        gold += 50;
        OnGoldChanged?.Invoke(gold);
        OnLogMessage?.Invoke("[Attack] Player Attack! Gold +50");

        if (playerTransform != null)
        {
            SimplePunchAsync(playerTransform, Vector3.right * 0.3f, 0.15f, this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    private void CastSkill()
    {
        gold += 200;
        currentHp = Mathf.Min(maxHp, currentHp + 20);
        OnHpChanged?.Invoke(currentHp, maxHp);
        OnGoldChanged?.Invoke(gold);
        OnLogMessage?.Invoke("[Skill] Area Strike! Gold +200, HP Heal!");
    }

    private void UpgradeStats()
    {
        if (gold >= 100)
        {
            gold -= 100;
            maxHp += 20;
            currentHp = maxHp;
            NotifyAllStats();
            OnLogMessage?.Invoke($"[Upgrade] Level Up! Max HP: {maxHp}");
        }
        else
        {
            OnLogMessage?.Invoke("[Upgrade] Need 100 Gold to upgrade!");
        }
    }

    private async UniTaskVoid AutoBattleLoopAsync(CancellationToken ct)
    {
        try
        {
            while (isPlaying && !ct.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: ct);

                gold += 10 + (stage * 5);
                OnGoldChanged?.Invoke(gold);

                if (monsterTransform != null)
                    SimplePunchAsync(monsterTransform, Vector3.up * 0.2f, 0.12f, ct).Forget();

                if (gold % 60 == 0)
                {
                    stage++;
                    OnStageChanged?.Invoke(stage);
                    OnLogMessage?.Invoke($"[Stage] Wave Cleared! Sector {stage} Start!");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async UniTaskVoid SimplePunchAsync(Transform target, Vector3 offset, float duration, CancellationToken ct)
    {
        if (target == null) return;
        Vector3 origin = target.localPosition;
        target.localPosition = origin + offset;

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: ct);
            if (target != null) target.localPosition = origin;
        }
        catch (OperationCanceledException)
        {
            if (target != null) target.localPosition = origin;
        }
    }

    private void NotifyAllStats()
    {
        OnHpChanged?.Invoke(currentHp, maxHp);
        OnGoldChanged?.Invoke(gold);
        OnStageChanged?.Invoke(stage);
    }

    private void OnDestroy()
    {
        battleCts?.Cancel();
        battleCts?.Dispose();
    }
}
