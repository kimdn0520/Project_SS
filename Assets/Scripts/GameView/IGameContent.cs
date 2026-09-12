using System;
using UnityEngine;

/// <summary>
/// GameRoot 내부에 동적으로 탑재될 수 있는 게임 콘텐츠 인터페이스.
/// 방치형 RPG, 은퇴용사 땅만파, 주사위 보드게임 등 기획 변경 시 이 인터페이스 구현체만 교체하면 됨.
/// </summary>
public interface IGameContent
{
    /// <summary>
    /// 콘텐츠 식별자 (예: "DiggerGame", "IdleRpg")
    /// </summary>
    string ContentId { get; }

    /// <summary>
    /// 생명주기: 콘텐츠 초기화
    /// </summary>
    void Initialize(object param);

    /// <summary>
    /// 생명주기: 콘텐츠 시작 (페이드 인 완료 후 활성화)
    /// </summary>
    void StartContent();

    /// <summary>
    /// 생명주기: 일시정지 (팝업 노출 또는 씬 전환 직전)
    /// </summary>
    void PauseContent();

    /// <summary>
    /// 생명주기: 재개
    /// </summary>
    void ResumeContent();

    /// <summary>
    /// 생명주기: 콘텐츠 정지 및 정리 (씬 퇴장 시)
    /// </summary>
    void StopContent();

    /// <summary>
    /// UI 또는 외부에서 들어온 사용자 명령 처리 (채굴, 스킬, 업그레이드 등)
    /// </summary>
    void ExecuteCommand(string commandName, object payload);

    // --- 데이터 통신 이벤트 (Decoupled Events) ---
    event Action<int, int> OnHpChanged;         // 현재 체력/보스 HP
    event Action<long> OnGoldChanged;           // 보유 골드
    event Action<int> OnStageChanged;           // 현재 스테이지
    event Action<string> OnLogMessage;          // 화면 로그/알림
    event Action<int> OnDepthChanged;           // 광산 심도 (m)
    event Action<int> OnGemsChanged;            // 보유 보석
}
