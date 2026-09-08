# Project_SS: 2D Vertical Mobile Game Architecture (뚜카펫 스타일 2D World + UI Canvas)

## 1. 프로젝트 비전 및 아키텍처 개요
- **장르 & 해상도**: 세로형 모바일 2D 방치형/RPG (기본 기준 해상도: 720 x 1280 Portrait)
- **단일 카메라 원칙**: `Main Camera` 1개만 사용 (직교 크기 6.4f, `Screen Space - Camera`, planeDistance 10)
- **2D World Space & UI 분리 (뚜카펫 스타일)**:
  - 게임 뷰: Canvas 밖에서 순수 2D World Space (`SpriteRenderer` + `Transform`)로 렌더링.
  - UI 뷰: `Canvas` (`Screen Space - Camera`, `worldCamera = Main Camera`) 기반 오버레이.
- **2-Scene 라이프사이클 체제**:
  - `Splash.unity` (Build Index 0): 앱 초기화 파이프라인 (UniTask 기반 리소스/데이터/싱글톤 로드) ➔ `Play.unity`로 페이드 아웃 전환.
  - `Play.unity` (Build Index 1): 인게임 메인 씬. `MainGameScene.cs` 같은 불필요한 옥상옥 껍데기 없이, `PageManager` 중심의 단일 루트 구조.

## 2. Page 프리팹 캡슐화 아키텍처 (PageManager ↔ SceneBase)
- 모든 Page는 [`SceneBase`](file:///C:/Users/kimdn/Project_SS/Assets/Scripts/Core/SceneBase.cs)를 상속받은 가상 씬(Virtual Scene)이자 독립 프리팹으로 캡슐화됨:
  - **`MainPage.prefab`**: 메인 로비 UI 전용 프리팹 (상단 재화 바, 가로 스와이프 3탭 [SHOP / HOME / RANK], 하단 탭 바, START GAME 버튼). Space Exploration GUI 테마 적용.
  - **`PlayPage.prefab`**: 
    - ⚠️ **치명적 규칙 준수**: 루트가 Canvas가 아닌 **일반 `GameObject(Transform)`**이어야 함.
    - 루트 바로 아래에 **`UI_Canvas` (RectTransform)**와 **`Game_Root` (일반 Transform - 2D World)**가 **병렬(Sibling) 계층**으로 공존하여 좌표/스케일 왜곡을 원천 방지.
- **`PageManager`**:
  - `PreloadPages()`에서 사전 인스턴스화 및 `SetupRenderCamera(mainCamera)`를 통한 카메라 의존성 주입(DI).
  - `ShowPageAsync(UIPageType)`: `UniTask` 기반 CanvasGroup 알파 페이드 인/아웃 및 OnWillLeave ➔ OnDidLeave ➔ OnWillEnter ➔ OnDidEnter 생명주기 관리.

## 3. 기술 스택 및 라이브러리
- **Engine**: Unity 6 (6000.3.2f1) 2D (C#)
- **Async Logic**: **순수 UniTask (`com.cysharp.unitask`)** - ⚠️ 코루틴 사용 절대 금지!
- **Animation**: DOTween (`DG.Tweening`) & UniTask 결합
- **UI & Theme**: TextMeshPro, Space Exploration GUI Kit (우주/SF 테마 슬라이스드 스프라이트 및 UI)
- **디바이스 대응**: `SafeAreaHelper` (노치 및 Dynamic Island 자동 패딩)

## 4. 엄격한 성능 최적화 및 클린 코드 컨벤션 (Strict Rules)
1. **무거운 탐색 함수 사용 절대 금지 (No Search Functions)**:
   - `GameObject.Find()`, `FindWithTag()`, `FindObjectOfType()`, `FindAnyObjectByType()` 런타임 사용 금지.
   - 씬 내부 객체 참조는 싱글톤, 의존성 주입(DI: `SetupRenderCamera` 등), 또는 이벤트(`Action`/`UnityAction`)로 전달.
2. **GetComponent / AddComponent 런타임 호출 지양 (Inspector Assignment 우선)**:
   - `Awake()`/`Start()`의 잦은 `GetComponent` 대신 `[SerializeField]` 사전 할당.
3. **코루틴 사용 절대 금지**:
   - 지연, 타이머, 페이드, 씬 로드 등 모든 비동기는 `UniTask` (`UniTask.Yield()`, `UniTask.Delay()`, `CancellationToken`) 사용.
4. **메모리 누수 방지**:
   - 이벤트 리스너는 `OnDestroy()` 또는 `OnDisable()`에서 반드시 `RemoveListener()` 명시적 해제.

## 5. 프로젝트 디렉토리 및 핵심 파일 맵
- `Assets/Scenes/`:
  - `Splash.unity` (Build Settings 0번)
  - `Play.unity` (Build Settings 1번)
- `Assets/Resources/Prefabs/`:
  - `MainPage.prefab`
  - `PlayPage.prefab`
- `Assets/Scripts/Core/`:
  - `SceneBase.cs`: 페이지/씬 추상 베이스 (UniTask 지원, Canvas/CanvasGroup 캐싱, `SetupRenderCamera`)
  - `AppManager.cs`: 앱 생명주기 및 Splash ➔ Play 씬 로드 관리 (UniTask)
  - `SplashScene.cs`: 스플래시 화면 연출 및 로딩 바
- `Assets/Scripts/UI/`:
  - `PageManager.cs`: 메인 페이지 매니저 (UniTask 페이드 및 페이지 인스턴스 관리)
  - `MainPage/MainPageView.cs`: 로비 뷰 (SceneBase 구현, 스와이프 및 탭 바 연동)
  - `MainPage/SwipeTabController.cs`: 가로 스와이프 제어 (StackOverflow 방지 세로 스크롤 위임)
  - `MainPage/BottomTabBar.cs`: 하단 SF 탭 바
  - `SafeAreaHelper.cs`: 기기 안전 영역(Notch) 대응
- `Assets/Scripts/UIView/`:
  - `PlayPageView.cs`: 인게임 뷰 (SceneBase 구현, UI_Canvas와 Game_Root 병렬 제어)
  - `TopHUDView.cs`: 인게임 상단 HUD (재화, 일시정지, 점수)
  - `BottomPanelView.cs`: 인게임 하단 컨트롤 패널
- `Assets/Scripts/GameView/`:
  - `GameRootController.cs`: 2D 월드 객체(배경, 캐릭터, 이펙트) 총괄
  - `IdleRpgGameContent.cs`: 방치형 2D 시뮬레이션 샘플
- `Assets/Editor/`:
  - `PagePrefabBuilder.cs`: 페이지 프리팹 빌드 및 Play 씬 구성 표준 에디터 툴 (`[MenuItem("ProjectSS/Build Page Prefabs")]`)
  - `PlaySceneSetup.cs`, `FullProjectSetup.cs`: PagePrefabBuilder 위임 래퍼

## 6. 최근 해결된 이슈 및 진행 상태 (2026-09-08)
- ✅ **SwipeTabController 무한 재귀 버그 완벽 수정**: 세로 드래그 전달 시 자기 자신을 재귀 호출하던 StackOverflowException 제거.
- ✅ **MainPage worldCamera 미할당 버그 해결**: 프리팹 인스턴스화 시 `PageManager`에서 `mainCamera`를 전달하는 DI 파이프라인 구축.
- ✅ **MainGameScene.cs 제거**: 불필요한 중복 껍데기 클래스 완전 삭제, 컴파일 에러 0건 완료.
- ✅ **Play.unity 씬 정돈**: `Main Camera`, `[--- MANAGERS ---]`, `PageManager` (`PagesRoot`, `PopupRoot`)의 클린한 단일 구조 완성.
- ✅ **전체 파이프라인 UniTask 전면 적용 & 런타임 검증 완료**: Splash ➔ Play ➔ MainPage 진입 정상 확인.
- ✅ **PageManager & PopupManager 클린 아키텍처 리팩토링**:
  - `IPageHandler`, `IPageTransition`(전략 패턴) 및 `IPopupHandler`, `IPopupAnimation` 분리.
  - 무분별한 try-catch 및 레거시 에러코드(int) 전면 제거, 비동기 CancellationToken과 단일 try-finally 상태 복원 구조 완성.
  - `BasePopupHandler`: 딤드(커튼) 터치와 안드로이드 백버튼의 닫기 경로를 `OnEscape()` 단일 진입점으로 통일.
- ✅ **하단 탭 바 야물딱진 팝업 인터랙션 & 계층 구조 일치화**:
  - 스크린샷 계층 구조 완성: `MainPage/UI_Canvas/SafeArea` 하위 `BarBG` + `TabButtonParent` (`TabButtonShop`, `TabButtonHome`, `TabButtonLeaderboard`).
  - 선택 시 아이콘이 상단 골드 라인 위로 돌출(Pop-up, Y +28px) 및 확대(1.22배), 라벨 텍스트 페이드 인.
  - 비선택 시 아이콘 중앙 배치 및 라벨 텍스트 비활성화.
- ✅ **엄격한 런타임 탐색/할당 배제(Zero Heavy Lookup) 전수 검사 및 캐싱 완료**:
  - `PoolManager.cs`의 `GameObject.Find` 런타임 호출 제거 및 1회 캐싱(`cachedMonsterGroup`).
  - `SwipeTabController.cs`의 드래그 중 `GetComponentInParent` 탐색을 드래그 시작 시 1회 캐싱으로 최적화.


