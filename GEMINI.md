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

### 🛠️ 다음 세션에서 해야 할 일 (Next Steps)
1. **[Network] 공격 전진(Lunge) 안정화**:
    - 공격 시 발생하는 위치 떨림(Jitter) 및 서버 보정으로 인한 위치 회귀(Snap back) 문제 해결.
    - 서버와 클라이언트의 Lunge 가속도 계산 로직 정밀 동기화 및 보정 임계값 재설계.
2. **[Combat] 공격 중 이동 입력 차단**:
    - 공격 애니메이션 및 전진(Lunge) 중 WASD 입력이 물리 엔진에 간섭하지 않도록 입력 잠금 로직 강화.
3. **[Visual] 시야 시스템 (FOV)**:
    - `Shadow Caster 2D`와 `Light 2D`를 활용한 캐릭터 가시거리 제한 및 공포감 조성.
4. **[Combat] 타격 피드백**:
    - 타격 시 화면 흔들림(Shake), 히트 스톱(Hit Stop), 피격 애니메이션 및 이펙트 추가.
5. **[AI] 몬스터 기초**:
    - `PoolManager`를 활용한 몬스터 스포닝 및 기본 FSM(Idle/Chase) 구축.

---
## 💡 작업 요약
세피리아 스타일의 **2단계 콤보와 무기 고정 시스템**을 성공적으로 안착시켰습니다. 또한, 네트워크 동기화 문제를 야기하던 공격 전진(Lunge) 기능을 완전히 제거하고, 서버와 클라이언트의 가감속 물리 로직을 일치시켜 **공격 중 자유 이동 및 쫀득한 무빙**을 구현했습니다.

---

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
