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
### ✅ 완료된 작업 (2026-04-05)
- **멀티플레이어 조준 및 공격 동기화**: 
    - 에임 각도(Aim Angle)와 공격 상태(IsAttacking) 패킷 추가 및 서버 브로드캐스트 구현.
    - 원격 플레이어(Remote Player)의 검 생성 및 궤도 실시간 동기화 성공.
    - 패킷 데이터 정렬 오류(`ArgumentOutOfRangeException`) 해결로 네트워크 안정화.
- **전투 로직 공용화**:
    - `PlayerCombat` 공격 방향을 '무기 소켓 방향'으로 통일하여 로컬/원격 모두 호환되도록 수정.
- **세피리아 스타일 무기 고도화**: 
    - 캐릭터 Flip 시 무기 비주얼의 자연스러운 반전 처리 및 궤도 보정.

### 🛠️ 다음 세션에서 해야 할 일 (Next Steps)
1. **[Visual] 시야 시스템 (FOV)**: `Shadow Caster 2D`와 `Light 2D`를 활용한 캐릭터 가시거리 제한 작업.
2. **[AI] 몬스터 기초**: `PoolManager`를 활용한 몬스터 스포닝 및 기본 FSM(Idle/Chase) 구축.
3. **[UI] 아이템 퀵슬롯**: 하단 중앙 퀵슬롯 UI 배치 및 인벤토리 연동 기초.
4. **[Combat] 휘두르기 애니메이션**: 세피리아 스타일의 역동적인 공격 모션(DOTween 활용 검토).

---
## 💡 작업 요약
컴파일 에러를 해결하고 **세피리아 스타일의 무기 조작감**을 성공적으로 구현했습니다. 이제 무기 자루가 마우스를 찰지게 따라다니며, 좌우 반전 시에도 안정적으로 동작합니다. `Weapon_Socket` 계층 구조를 루트로 옮기는 작업을 통해 물리적 오차를 최소화했습니다.
---
