/// <summary>
/// 단일 씬 내에서 전환되는 UI 페이지 종류 정의
/// </summary>
public enum UIPageType
{
    None = 0,
    MainPage,   // 로비 / 홈 / 상점 / 리더보드 페이지
    PlayPage,   // 인게임 조작 및 상태 표시 페이지
    ExpeditionPrototype,
    // 추후 확장: ResultPage, GuildPage 등
}
