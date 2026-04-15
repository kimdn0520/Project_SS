# Project_SS: 2D Hardcore Top-down Extraction Game

## 1. 프로젝트 비전
- **"보이지 않는 공포와 무게감"**: 하드코어 2D 익스트랙션 장르.
- **핵심 키워드**: 가시거리 제한, 스테미나 기반 전투, 물리적 무게감, 사운드 시각화.

## 2. 현재 아키텍처 (Current Tech Stack)
- **Server-Authoritative**: 모든 물리 및 상태 판정은 C# 독립 서버(`Server/Program.cs`)가 담당.
- **Networking**: `LiteNetLib` (TCP/UDP) + 커스텀 패킷 시스템 (`IsSprinting`, `Stamina` 등 동기화).
- **Movement System**: 
    - **Prediction**: 클라이언트 측 즉시 이동.
    - **Reconciliation**: 서버 공인 위치 보간 및 **Stop Protection**(정지 시 튕김 방지) 적용.
    - **Direct Velocity Control**: `linearVelocity`와 `MoveTowards`를 이용한 빠릿하고 부드러운 조작감.
- **State Machine**: `IState` 기반 `Idle`, `Move`, `Sprint`, `Guard` 상태 관리.
- **Hierarchy Management**: `-- ENTITIES --` > `Player_Group` / `Monster_Group`. 
    - **중요**: `Weapon_Socket`은 `Visuals` 외부(루트)에 위치하여 Flip 영향을 받지 않도록 설정 권장.

## 3. 진행 상황 (2026-04-12)
### ✅ 완료된 작업 (2026-04-12)
- **세피리아 스타일 2단계 콤보 시스템**: 
    - `WeaponDataSO` 기반의 2단계 연계 공격 구현 (부호 및 방향 보정 완료).
    - **무기 고정(Weapon Lock)**: 공격 중 및 콤보 대기 시간 동안 마우스 추적을 중단하여 자연스러운 공격 궤적 유지.
    - **콤보 상태 관리**: 공격 종료 후 콤보 윈도우가 지나면 자동으로 가드 자세(마우스 방향)로 복귀.
    - 캐릭터 Flip(localScale.y) 상태에 따른 휘두르기 방향 자동 보정 로직 적용.

### ✅ 완료된 작업 (2026-04-15)
- **카메라 셰이크(Camera Shake) 연동**:
    - 타격 시(`ImpactShake`) 및 피격 시(`DamageShake`) 개별 연출 로직 구현.
    - 클라이언트 즉시 피드백과 서버 데미지 수신 시점을 분리하여 타격감 개선.
- **Server-Authoritative 몬스터 AI 시스템**:
    - 서버 측 몬스터 FSM(`Idle`, `Chase`, `Attack`) 및 플레이어 추적 로직 구현.
    - 서버 권한 기반의 공격 판정 및 데미지 브로드캐스팅 적용.
    - 클라이언트 몬스터 보간 이동(Lerp) 및 시각적 상태 피드백 강화.
- **이동 동기화 최적화**:
    - 플레이어 및 몬스터의 순간이동 현상을 잡기 위한 부드러운 위치 보정(Smoothing) 로직 적용.

### 🛠️ 다음 세션에서 해야 할 일 (Next Steps)
1. **[Combat] 몬스터 공격 궤적 및 이펙트**:
    - 몬스터가 공격할 때 실제 공격 범위에 맞춘 시각적 궤적(Arc) 표시.
2. **[UI] 몬스터 체력 바(World Space UI)**:
    - `WorldCharacterUI`를 몬스터에게도 적용하여 남은 체력 표시.
3. **[AI] 몬스터 종류 다양화**:
    - 원거리 공격 몬스터 및 패턴이 다른 몬스터를 위한 베이스 클래스 확장.
4. **[System] PoolManager 연동**:
    - 몬스터 생성/삭제 시 `PoolManager`를 사용하여 메모리 최적화.

---
## 💡 작업 요약
물리 기반의 **검 트리거 판정**과 **서버 검증**을 결합하여, 보안성과 타격감을 동시에 잡는 전투 기초를 완성했습니다. 특히 **HitStop(역경직)**과 **CameraShake**의 즉각적인 적용으로 하드코어 액션의 무게감을 한층 더했습니다. 이제 순간이동 현상을 잡는 **이동 보간(Smoothing)**과 더 체계적인 **엔티티 아키텍처**로 확장할 단계입니다.

## 🎯 다음 작업: 스태미너 시스템 및 World UI 구현

### 1. 하드코어 스태미너 로직
- **소모 메커니즘**:
    - **공격 (Attack)**: 무기 데이터(`WeaponDataSO`)에 정의된 소모량만큼 즉시 차감. 스태미너 부족 시 공격 불가.
    - **달리기 (Sprint)**: 초당 일정량 지속 소모. 0이 되면 강제 걷기 상태 전환.
    - **방어 (Guard)**: 방어 유지 시 지속 소모 및 피격 시 추가 소모.
- **회복 메커니즘**:
    - 행동 중지(Idle/Walk) 시 일정 지연 시간 후 자동 회복 시작.
    - 탈진 상태(Stamina <= 0) 시 회복 속도 페널티 적용.

### 2. World-Space UI (Character Overlay)
- **LocalPlayer 프리팹 구조 개선**:
    - 플레이어 발밑이나 머리 위에 `Canvas (World Space)` 추가.
    - **HP 바**: 붉은색 게이지로 현재 체력 표시.
    - **스태미너 바**: 노란색/녹색 게이지로 현재 기력 표시. 공격 시 즉시 줄어드는 시각적 피드백 강화.
- **최적화**: 매 프레임 `Update` 대신 값이 변할 때만 `Slider.value` 갱신.

### 3. 네트워크 동기화
- 서버(`Server/Program.cs`)에서 계산된 `CurrentStamina` 값을 패킷으로 수신하여 클라이언트 UI에 반영.
- 클라이언트 예측(Client-side Prediction)을 통해 공격 버튼 클릭 즉시 UI 게이지를 먼저 깎아 조작감 향상.

---
