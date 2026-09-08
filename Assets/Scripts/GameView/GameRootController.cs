using System;
using UnityEngine;

/// <summary>
/// 순수 2D World Space(SpriteRenderer + Transform)를 총괄 관리하는 게임 뷰 컨트롤러.
/// Canvas 바깥에 위치하며, 하위의 IGameContent 모듈(RPG 전투, 주사위, 핀볼 등)을 구동합니다.
/// UI 요소에 대한 직접 참조가 전혀 없으며, Action/Event 기반으로 외부와 통신합니다.
/// </summary>
public class GameRootController : MonoBehaviour
{
    [Header("[Game Content Module]")]
    [SerializeField] private MonoBehaviour contentModuleMono; // 인스펙터 직렬화용 IGameContent 컴포넌트
    private IGameContent activeContent;

    [Header("[World 2D Transforms]")]
    [SerializeField] private Transform environmentRoot;       // 배경/필드 스프라이트 전용 루트
    [SerializeField] private Transform entityRoot;            // 캐릭터, 몬스터 등 오브젝트 루트
    [SerializeField] private Transform vfxRoot;               // 2D 스프라이트 이펙트 전용 루트

    public Transform EnvironmentRoot => environmentRoot;
    public Transform EntityRoot => entityRoot;
    public Transform VfxRoot => vfxRoot;

    // 외부(MainGameScene / UIRoot)로 전달할 decoupled 이벤트
    public event Action<int, int> OnHpChanged;
    public event Action<long> OnGoldChanged;
    public event Action<int> OnStageChanged;
    public event Action<string> OnLogMessage;

    private void Awake()
    {
        // 인스펙터에 지정된 컴포넌트에서 IGameContent 인터페이스 추출
        if (contentModuleMono != null && contentModuleMono is IGameContent content)
        {
            activeContent = content;
        }
        else
        {
            activeContent = GetComponentInChildren<IGameContent>();
        }

        BindContentEvents();
    }

    /// <summary>
    /// 새로운 게임 콘텐츠(예: 방치형 RPG -> 주사위 배틀 -> 핀볼)로 동적 교체
    /// </summary>
    public void SetGameContent(IGameContent newContent, object initParam)
    {
        UnbindContentEvents();

        if (activeContent != null)
        {
            activeContent.StopContent();
        }

        activeContent = newContent;
        BindContentEvents();

        activeContent?.Initialize(initParam);
    }

    /// <summary>
    /// 생명주기: 씬 페이드인 전 초기화
    /// </summary>
    public void Initialize(object param)
    {
        activeContent?.Initialize(param);
    }

    /// <summary>
    /// 생명주기: 페이드인 완료 후 게임 플레이 시작
    /// </summary>
    public void StartPlay()
    {
        activeContent?.StartContent();
    }

    /// <summary>
    /// 생명주기: 일시정지
    /// </summary>
    public void PausePlay()
    {
        activeContent?.PauseContent();
    }

    /// <summary>
    /// 생명주기: 페이드아웃 시 리소스 및 플레이 중지
    /// </summary>
    public void StopPlay()
    {
        activeContent?.StopContent();
    }

    /// <summary>
    /// UI에서 전달받은 명령을 활성 콘텐츠로 라우팅
    /// </summary>
    public void DispatchCommand(string commandName, object payload)
    {
        activeContent?.ExecuteCommand(commandName, payload);
    }

    private void BindContentEvents()
    {
        if (activeContent == null) return;

        activeContent.OnHpChanged += HandleHpChanged;
        activeContent.OnGoldChanged += HandleGoldChanged;
        activeContent.OnStageChanged += HandleStageChanged;
        activeContent.OnLogMessage += HandleLogMessage;
    }

    private void UnbindContentEvents()
    {
        if (activeContent == null) return;

        activeContent.OnHpChanged -= HandleHpChanged;
        activeContent.OnGoldChanged -= HandleGoldChanged;
        activeContent.OnStageChanged -= HandleStageChanged;
        activeContent.OnLogMessage -= HandleLogMessage;
    }

    private void HandleHpChanged(int cur, int max) => OnHpChanged?.Invoke(cur, max);
    private void HandleGoldChanged(long gold) => OnGoldChanged?.Invoke(gold);
    private void HandleStageChanged(int stage) => OnStageChanged?.Invoke(stage);
    private void HandleLogMessage(string msg) => OnLogMessage?.Invoke(msg);

    private void OnDestroy()
    {
        UnbindContentEvents();
    }
}
