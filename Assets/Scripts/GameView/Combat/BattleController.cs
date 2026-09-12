using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public enum BattleState
{
    None,
    Travel,
    Encounter,
    Combat,
    StageClear,
    Defeat,
    RetryReady
}

/// <summary>
/// 상단 자동 전투 시스템의 코어 컨트롤러.
/// Travel -> Encounter -> Combat -> StageClear / Defeat 상태 머신을 구동하며,
/// 용사와 몬스터의 라이프사이클 및 스테이지(1-1 -> 1-2 -> 1-3) 진행을 담당합니다.
/// 하단 채굴(Mining)과 완전히 독립적으로 동작합니다.
/// </summary>
public class BattleController : MonoBehaviour
{
    [Header("[Actor References]")]
    [SerializeField] private ActiveHeroActor activeHero;
    [SerializeField] private EnemyMonsterActor enemyMonster;
    [SerializeField] private ParallaxScroller parallaxScroller;

    [Header("[Transforms & Anchors]")]
    [SerializeField] private Transform battleVisualRoot;
    [SerializeField] private Transform projectileRoot;
    [SerializeField] private GameObject hitVfxPrefab;

    [Header("[Battle Balance]")]
    [SerializeField] private int chapter = 1;
    [SerializeField] private int stage = 1;

    public BattleState CurrentState { get; private set; } = BattleState.None;
    public int Chapter => chapter;
    public int Stage => stage;
    public string StageName => $"{chapter}-{stage}";

    public event Action<string> OnStageChanged;           // ("1-1")
    public event Action<int, int> OnMonsterHpChanged;     // (cur, max)
    public event Action<int, int> OnHeroHpChanged;        // (cur, max)
    public event Action<string> OnBattleLog;              // 로그 메시지
    public event Action<BattleState> OnBattleStateChanged;

    private bool isRunning = false;
    private CancellationTokenSource battleCts;

    private void Start()
    {
        if (SessionPausePolicy.Instance != null)
        {
            SessionPausePolicy.Instance.OnPauseStateChanged += HandlePauseStateChanged;
        }
    }

    public void StartBattle()
    {
        isRunning = true;
        battleCts?.Cancel();
        battleCts?.Dispose();
        battleCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        NotifyStage();
        BattleLoopAsync(battleCts.Token).Forget();
    }

    public void StopBattle()
    {
        isRunning = false;
        battleCts?.Cancel();
        battleCts?.Dispose();
        battleCts = null;

        if (parallaxScroller != null)
        {
            parallaxScroller.SetTargetSpeed(0f, immediate: true);
        }
    }

    private void HandlePauseStateChanged(bool isPaused)
    {
        if (parallaxScroller != null)
        {
            parallaxScroller.SetPaused(isPaused);
        }
    }

    private async UniTaskVoid BattleLoopAsync(CancellationToken ct)
    {
        try
        {
            while (isRunning && !ct.IsCancellationRequested)
            {
                // 1. Travel 상태
                await RunTravelStateAsync(ct);

                // 2. Encounter 상태
                await RunEncounterStateAsync(ct);

                // 3. Combat 상태
                bool victory = await RunCombatStateAsync(ct);

                // 4. 결과 상태 분기
                if (victory)
                {
                    await RunStageClearStateAsync(ct);
                }
                else
                {
                    await RunDefeatStateAsync(ct);
                    break; // 패배 시 루프 정지 (Retry 대기)
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async UniTask RunTravelStateAsync(CancellationToken ct)
    {
        SetState(BattleState.Travel);
        OnBattleLog?.Invoke($"[스테이지 {StageName}] 용사가 전장을 향해 전진합니다!");

        if (activeHero != null) activeHero.PlayRun();
        if (parallaxScroller != null) parallaxScroller.SetTargetSpeed(1.0f);

        // 2.0초 동안 질주 연출
        await UniTask.Delay(TimeSpan.FromSeconds(2.0f), cancellationToken: ct);
    }

    private async UniTask RunEncounterStateAsync(CancellationToken ct)
    {
        SetState(BattleState.Encounter);

        // 배경 서서히 감속
        if (parallaxScroller != null) parallaxScroller.SetTargetSpeed(0.0f);

        // 적 스폰 (화면 우측 x = 3.5f)
        int monsterHp = 40 + (stage * 20);
        int monsterAtk = 8 + (stage * 4);
        string mName = (stage % 3 == 0) ? "보스 골렘" : "스켈레톤 병사";

        if (enemyMonster != null)
        {
            enemyMonster.Setup(mName, monsterHp, monsterAtk, new Vector3(3.5f, -0.2f, 0f));
            enemyMonster.OnHpChanged += HandleMonsterHp;
        }

        OnBattleLog?.Invoke($"<color=red>[적 출현!]</color> 전방에 '{mName}'(이)가 나타났습니다!");

        // 적이 공격 사거리(x = 1.0f)까지 접근
        if (enemyMonster != null)
        {
            await enemyMonster.MoveToTargetXAsync(1.0f, ct);
        }

        if (activeHero != null) activeHero.PlayIdle();
    }

    private async UniTask<bool> RunCombatStateAsync(CancellationToken ct)
    {
        SetState(BattleState.Combat);

        int heroAtk = HeroStatsModel.Instance != null ? HeroStatsModel.Instance.ActiveHeroAtk : 25;
        int heroMaxHp = HeroStatsModel.Instance != null ? HeroStatsModel.Instance.ActiveHeroMaxHp : 100;
        int heroHp = HeroStatsModel.Instance != null ? HeroStatsModel.Instance.ActiveHeroHp : 100;

        while (enemyMonster != null && !enemyMonster.IsDead && heroHp > 0 && !ct.IsCancellationRequested)
        {
            // 전역 일시정지 체크
            while (SessionPausePolicy.Instance != null && SessionPausePolicy.Instance.IsPaused)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            // 1. 용사의 공격
            if (activeHero != null)
            {
                await activeHero.PerformAttackAsync(ct);
            }

            if (enemyMonster != null && !enemyMonster.IsDead)
            {
                enemyMonster.TakeDamage(heroAtk);
                SpawnHitVfx(enemyMonster.transform.position);

                // 시각 흔들림 (VisualRoot)
                ShakeBattleVisualAsync(ct).Forget();
            }

            if (enemyMonster == null || enemyMonster.IsDead)
            {
                return true; // 용사 승리!
            }

            await UniTask.Delay(300, cancellationToken: ct);

            // 2. 적의 반격
            if (enemyMonster != null && !enemyMonster.IsDead)
            {
                await enemyMonster.PerformAttackAsync(ct);

                heroHp = Mathf.Max(0, heroHp - enemyMonster.AtkPower);
                HeroStatsModel.Instance?.SetHeroHp(heroHp, heroMaxHp);
                OnHeroHpChanged?.Invoke(heroHp, heroMaxHp);

                if (activeHero != null)
                {
                    activeHero.PlayHitReactionAsync(ct).Forget();
                }

                if (heroHp <= 0)
                {
                    return false; // 용사 패배
                }
            }

            await UniTask.Delay(400, cancellationToken: ct);
        }

        return enemyMonster == null || enemyMonster.IsDead;
    }

    private async UniTask RunStageClearStateAsync(CancellationToken ct)
    {
        SetState(BattleState.StageClear);

        long rewardGold = 50 + (stage * 30);
        PlayerInventoryModel.Instance?.AddGold(rewardGold);

        OnBattleLog?.Invoke($"<color=green>[스테이지 클리어!]</color> {StageName} 돌파 완료! (+{rewardGold} 골드)");

        // 다음 스테이지 진행 (1-1 -> 1-2 -> 1-3 ...)
        stage++;
        if (stage > 3)
        {
            chapter++;
            stage = 1;
        }

        NotifyStage();

        if (enemyMonster != null)
        {
            enemyMonster.OnHpChanged -= HandleMonsterHp;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(0.8f), cancellationToken: ct);
    }

    private async UniTask RunDefeatStateAsync(CancellationToken ct)
    {
        SetState(BattleState.Defeat);
        OnBattleLog?.Invoke("<color=red>[전선 후퇴]</color> 용사가 쓰러졌습니다! (채굴은 계속 가능합니다)");

        if (enemyMonster != null)
        {
            enemyMonster.OnHpChanged -= HandleMonsterHp;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
        SetState(BattleState.RetryReady);
    }

    public void RetryBattle()
    {
        if (CurrentState != BattleState.RetryReady && CurrentState != BattleState.Defeat) return;

        // 용사 체력 복원
        int maxHp = HeroStatsModel.Instance != null ? HeroStatsModel.Instance.ActiveHeroMaxHp : 100;
        HeroStatsModel.Instance?.SetHeroHp(maxHp, maxHp);
        OnHeroHpChanged?.Invoke(maxHp, maxHp);

        StartBattle();
    }

    private void SpawnHitVfx(Vector3 pos)
    {
        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, pos, Quaternion.identity, projectileRoot != null ? projectileRoot : transform);
            Destroy(vfx, 0.6f);
        }
    }

    private async UniTaskVoid ShakeBattleVisualAsync(CancellationToken ct)
    {
        if (battleVisualRoot == null) return;
        Vector3 orig = battleVisualRoot.localPosition;
        battleVisualRoot.localPosition = orig + new Vector3(0.06f, -0.04f, 0f);
        try
        {
            await UniTask.Delay(40, cancellationToken: ct);
            if (battleVisualRoot != null) battleVisualRoot.localPosition = orig;
        }
        catch
        {
            if (battleVisualRoot != null) battleVisualRoot.localPosition = orig;
        }
    }

    private void HandleMonsterHp(int cur, int max)
    {
        OnMonsterHpChanged?.Invoke(cur, max);
    }

    private void SetState(BattleState state)
    {
        CurrentState = state;
        OnBattleStateChanged?.Invoke(state);
    }

    private void NotifyStage()
    {
        OnStageChanged?.Invoke(StageName);
    }

    private void OnDestroy()
    {
        if (SessionPausePolicy.Instance != null)
        {
            SessionPausePolicy.Instance.OnPauseStateChanged -= HandlePauseStateChanged;
        }
        StopBattle();
    }
}
