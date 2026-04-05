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

## 3. 진행 상황 (2026-04-02)
### ✅ 완료된 작업
- **컴파일 에러 해결**: 누락된 `PlayerCombat.cs` 클래스 복구 및 공격 입력 로직 정상화.
- **세피리아 스타일 무기 시스템**: 
    - **Orbit Movement**: 무기가 캐릭터 주변을 마우스 방향으로 궤도 이동(0.5m).
    - **Hilt-Center Tracking**: 검 자루(Hilt)가 소켓 위치에 고정되어 마우스를 부드럽게 추적.
    - **Dynamic Flip**: 왼쪽 조작 시 무기가 거꾸로 들리지 않도록 `Scale.y` 반전 처리.
- **무기 비주얼 최적화**: 
    - `Sword` 프리팹 `Sorting Order`를 1로 상향하여 캐릭터 위에 렌더링.
    - 무기 생성 시 `-90도` 회전 오프셋 적용으로 마우스 방향과 일치화.
- **기존 작업**: 스테미나 시스템, 클래스 데이터(SO), UI HUD, 씬 구조화 도구 완성.

### 🛠️ 다음 세션에서 해야 할 일 (Next Steps)
1. **[Visual] 시야 시스템 (FOV)**: `Shadow Caster 2D`와 `Light 2D`를 활용한 캐릭터 가시거리 제한 작업.
2. **[AI] 몬스터 기초**: `PoolManager`를 활용한 몬스터 스포닝 및 기본 FSM(Idle/Chase) 구축.
3. **[UI] 아이템 퀵슬롯**: 하단 중앙 퀵슬롯 UI 배치 및 인벤토리 연동 기초.
4. **[Combat] 휘두르기 애니메이션**: 세피리아 스타일의 역동적인 공격 모션(DOTween 활용 검토).

---
## 💡 작업 요약
컴파일 에러를 해결하고 **세피리아 스타일의 무기 조작감**을 성공적으로 구현했습니다. 이제 무기 자루가 마우스를 찰지게 따라다니며, 좌우 반전 시에도 안정적으로 동작합니다. `Weapon_Socket` 계층 구조를 루트로 옮기는 작업을 통해 물리적 오차를 최소화했습니다.
---
