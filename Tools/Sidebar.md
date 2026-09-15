# 사이드바 / 두더지 지원품

- `BaseSidebarIcon.isAvailable`: 기능 공개 여부. 파생 아이콘의 CanShow 조건과 함께 표시를 결정한다.
- `StoreIcon`, `FirstChargeIcon`, `ChallengeIcon`, `LodeIcon`, `MoleSupportIcon`을 각각 프리팹으로 만들고 PlayPage의 SidebarCanvas/SafeArea 아래 중첩 배치했다. 빈 SidebarSlot은 제거했다.
- 프리팹 위치: `Assets/Resources/Prefabs/UI/Sidebar/`. 공통 클릭 이벤트는 BaseSidebarIcon에서 연결하며 외부 씬 오브젝트 참조를 저장하지 않는다.
- 표시 갱신은 부모 ExpeditionSidebarLayout이 비활성 자식도 포함하여 1초마다 수행한다. 지원품 아이콘이 꺼져 있어도 날짜가 바뀌면 다시 표시된다. 기존 safe area와 좌우 앵커를 유지한다.
- 지원품은 기기 현지 날짜 자정 기준 하루 5회. `moleSupportDay`, `moleSupportUsed`를 기존 원정 저장에 추가했다. 구버전 저장은 기본 0에서 초기화한다. 과거 날짜로 바꾸면 초기화하지 않는다.
- 광고 SDK와 보상 기획은 아직 없다. 실제 빌드는 준비 중 안내만 표시한다. 에디터의 명시적인 '광고 완료 테스트' 버튼만 횟수를 소비하며 실제 재화/장비를 지급하지 않는다. 닫기로 취소하면 소비하지 않는다.
- 이후 광고 SDK의 성공 콜백과 실제 보상 지급을 같은 완료 경로에 연결해야 한다. 운영용 날짜 검증은 서버 시간 정책 결정이 필요하다.
- 두더지 아트: `Assets/Prototype/Art/Sidebar/MoleSupport.png`. 내장 image_gen으로 생성, 알파 보존. Unity Single Sprite, 최대 256px, mipmap off, 압축 없음으로 사용한다.
- 검증: Unity 컴파일, 5개 중첩 프리팹, 공통 표시 플래그, 실제 팝업 테스트 버튼, 5회 제한/6회 차단/숨김, PlayerPrefs 저장, 비활성 아이콘 날짜 리셋/재등장 통과. 기록: `PrototypeQA/sidebar-support.txt`, 화면: `PrototypeQA/sidebar-support.png`. 실제 광고 및 모바일 기기 검증은 미실행.

## 생성 프롬프트 (내장 image_gen)

Use case: stylized-concept. Asset type: transparent PNG sprite for a small mobile cartoon mining-game sidebar icon, square. Primary request: one cute mole carrying a supply chest in both front paws, facing mostly forward with slight three-quarter angle. Huge rounded dark brown head, tiny eyes, distinctive broad pink mole snout, small pale digging claws wrapping the box. Small squat body barely visible behind chest. Chest is a simple honey brown wooden box with warm golden corner bands, closed lid, one chunky central latch. Style: clean flat 2D mobile game cartoon, very thick crisp near-black outlines, simple solid color areas and one cel shadow tone, bright warm readable colors, no texture grain, no realism, no glossy 3D rendering. Composition: mole and box fill 90% of square with a compact silhouette, head fills upper half and chest lower half; silhouette recognizable at 64 pixels. Transparent background with real alpha; no frame, no backdrop, no floor shadow, no text, no badge, no numbers, no watermark. Single finished icon only.
