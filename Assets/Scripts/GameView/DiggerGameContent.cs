using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// '은퇴용사 땅만파' 코어 게임플레이 오케스트레이터 모듈.
/// 상단: ActiveHero의 자동 전투 (BattleController)
/// 하단: 은퇴용사 땅만파의 액티브 채굴 (MiningController)
/// 두 서브시스템의 책임 분리와 라이프사이클을 안전하게 중계합니다.
/// </summary>
public class DiggerGameContent : MonoBehaviour, IGameContent
{
    public string ContentId => "DiggerGame";

    [Header("[Subsystem Controllers]")]
    [SerializeField] private BattleController battleController;
    [SerializeField] private MiningController miningController;

    // Decoupled IGameContent Events
    public event Action<int, int> OnHpChanged;      // 용사 체력
    public event Action<long> OnGoldChanged;        // 골드 변화
    public event Action<int> OnStageChanged;       // 스테이지 번호
    public event Action<string> OnLogMessage;      // 인게임 로그
    public event Action<int> OnDepthChanged;       // 광산 심도 (m)
    public event Action<int> OnGemsChanged;        // 다이아몬드 변화

    public BattleController Battle => battleController;
    public MiningController Mining => miningController;

    private void Awake()
    {
        if (battleController == null)
            battleController = GetComponentInChildren<BattleController>(true);

        if (miningController == null)
            miningController = GetComponentInChildren<MiningController>(true);

        BindEvents();
    }

    private void BindEvents()
    {
        if (battleController != null)
        {
            battleController.OnHeroHpChanged += HandleHeroHpChanged;
            battleController.OnBattleLog += HandleLog;
            battleController.OnStageChanged += HandleStageString;
        }

        if (miningController != null)
        {
            miningController.OnDepthChanged += HandleDepth;
            miningController.OnMiningLog += HandleLog;
        }

        if (PlayerInventoryModel.Instance != null)
        {
            PlayerInventoryModel.Instance.OnGoldChanged += HandleGold;
            PlayerInventoryModel.Instance.OnGemsChanged += HandleGems;
        }
    }

    private void UnbindEvents()
    {
        if (battleController != null)
        {
            battleController.OnHeroHpChanged -= HandleHeroHpChanged;
            battleController.OnBattleLog -= HandleLog;
            battleController.OnStageChanged -= HandleStageString;
        }

        if (miningController != null)
        {
            miningController.OnDepthChanged -= HandleDepth;
            miningController.OnMiningLog -= HandleLog;
        }

        if (PlayerInventoryModel.Instance != null)
        {
            PlayerInventoryModel.Instance.OnGoldChanged -= HandleGold;
            PlayerInventoryModel.Instance.OnGemsChanged -= HandleGems;
        }
    }

    public void Initialize(object param)
    {
        // 모델 초기값 전달
        if (PlayerInventoryModel.Instance != null)
        {
            OnGoldChanged?.Invoke(PlayerInventoryModel.Instance.Gold);
            OnGemsChanged?.Invoke(PlayerInventoryModel.Instance.Gems);
        }

        if (HeroStatsModel.Instance != null)
        {
            OnHpChanged?.Invoke(HeroStatsModel.Instance.ActiveHeroHp, HeroStatsModel.Instance.ActiveHeroMaxHp);
        }

        if (miningController != null)
        {
            OnDepthChanged?.Invoke(miningController.DepthMeters);
        }

        OnLogMessage?.Invoke("은퇴용사 땅만파가 곡괭이를 쥐고 채굴을 준비합니다!");
    }

    public void StartContent()
    {
        battleController?.StartBattle();
        OnLogMessage?.Invoke("[전장 개시] 현역 용사가 출격하고 채굴이 시작됩니다!");
    }

    public void PauseContent()
    {
        battleController?.StopBattle();
        miningController?.CancelDig();
        OnLogMessage?.Invoke("게임 일시정지");
    }

    public void ResumeContent()
    {
        StartContent();
    }

    public void StopContent()
    {
        PauseContent();
    }

    public void RegisterBlock(MiningBlockView block)
    {
        // 하위 호환용
    }

    public void ExecuteCommand(string commandName, object payload)
    {
        switch (commandName)
        {
            case "Mine":
            case "ClickMine":
                miningController?.TriggerDig();
                break;
            case "RetryBattle":
                battleController?.RetryBattle();
                break;
        }
    }

    private void HandleHeroHpChanged(int cur, int max) => OnHpChanged?.Invoke(cur, max);
    private void HandleDepth(int depth) => OnDepthChanged?.Invoke(depth);
    private void HandleGold(long gold) => OnGoldChanged?.Invoke(gold);
    private void HandleGems(int gems) => OnGemsChanged?.Invoke(gems);
    private void HandleLog(string msg) => OnLogMessage?.Invoke(msg);

    private void HandleStageString(string stageStr)
    {
        // "1-1" 등 파싱하여 스테이지 전달
        if (battleController != null)
        {
            OnStageChanged?.Invoke(battleController.Stage);
        }
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }
}
