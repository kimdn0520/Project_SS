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
- **State Machine**: `IState` 기반 `Idle`, `Move`, `Sprint` 상태 통합 관리.
- **Hierarchy Management**: `-- ENTITIES --` > `Player_Group` / `Monster_Group` 규칙 준수.

## 3. 진행 상황 (2026-03-31)
### ✅ 완료된 작업
- **스테미나 시스템**: 대쉬 시 소모, 자동 회복, 스테미나 0일 때 이동 속도 50% 감소 로직 완성.
- **클래스 데이터 시스템**: `PlayerStatsSO` 설계 및 Warrior/Mage/Rogue 데이터 구축 완료.
- **UI 시스템**: `UIManager` 싱글톤 구축 및 HUD(Stamina/HP Bar) 연동 완료.
- **씬 구조화 도구**: `SceneBuilder`를 통한 URP 2D 광원/그림자/충돌체 자동 구축 환경 마련.
- **엔티티 관리 규칙**: 네트워크 스포닝 및 오브젝트 풀링 시 부모 그룹 자동 설정 로직 반영.

### 🛠️ 다음 세션에서 해야 할 일 (Next Steps)
1. **[Combat] 방향성 방어 및 패링**: 마우스 방향을 바라보는 로직 및 가드(Guard) 시스템 구현.
2. **[Visual] 시야 시스템 (FOV)**: `Shadow Caster 2D`와 `Light 2D`를 활용한 캐릭터 가시거리 제한 작업.
3. **[AI] 몬스터 기초**: `PoolManager`를 활용한 몬스터 스포닝 및 기본 FSM(Idle/Chase) 구축.
4. **[UI] 아이템 퀵슬롯**: 하단 중앙 퀵슬롯 UI 배치 및 인벤토리 연동 기초.

---
## 💡 작업 요약
조작감 이슈(지터 및 무거운 느낌)를 **실무형 Velocity 제어 방식**으로 전환하여 해결했습니다. 현재 전사(Warrior) 클래스 데이터가 기본 적용되어 있으며, 스테미나 소모에 따른 속도 저하가 시각적으로 HUD에 반영됩니다.

*이 파일은 Gemini CLI의 세션 간 컨텍스트 유지를 위해 작성되었습니다.*
