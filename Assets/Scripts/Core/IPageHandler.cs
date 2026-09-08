/// <summary>
/// 단일 화면(페이지)의 표시/숨김 및 생명주기 훅을 정의하는 인터페이스.
/// SceneBase나 UI 패널 등 어떤 컴포넌트도 페이지 단위로 관리될 수 있도록 다형성을 제공합니다.
/// </summary>
public interface IPageHandler
{
    /// <summary>
    /// 페이지 오브젝트를 활성화(표시)합니다.
    /// </summary>
    void Show();

    /// <summary>
    /// 페이지 오브젝트를 비활성화(숨김)합니다.
    /// </summary>
    void Hide();

    /// <summary>
    /// 페이지가 화면에 나타나기 직전 호출 (데이터 준비, UI 초기화 등)
    /// </summary>
    void OnWillEnter(object param);

    /// <summary>
    /// 페이지가 트랜지션을 마치고 완전히 화면에 노출되었을 때 호출 (입력 활성화, 게임 루프 시작 등)
    /// </summary>
    void OnDidEnter();

    /// <summary>
    /// 페이지가 화면에서 사라지기 직전 호출 (입력 차단, 조작 정지 등)
    /// </summary>
    void OnWillLeave();

    /// <summary>
    /// 페이지가 트랜지션을 마치고 완전히 퇴장했을 때 호출 (리소스 정리, 이벤트 해제 등)
    /// </summary>
    void OnDidLeave();
}
