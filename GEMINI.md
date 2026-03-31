# Project_SS: 2D Hardcore Top-down Extraction Game

## 1. 프로젝트 비전
- **"보이지 않는 공포와 무게감"**: 하드코어 2D 익스트랙션 장르.
- **핵심 키워드**: 가시거리 제한, 스테미나 기반 전투, 물리적 무게감, 사운드 시각화.

## 2. 현재 아키텍처 (Current Tech Stack)
- **Server-Authoritative**: 모든 물리 및 상태 판정은 C# 독립 서버(`Server/Program.cs`)가 담당.
- **Networking**: `LiteNetLib` (TCP/UDP) + 커스텀 패킷 시스템.
- **Predictive Movement**: 클라이언트 측 즉시 이동(Prediction) + 서버 공인 위치 보간(Interpolation).
- **State Machine (Updated)**: `IState` 기반 `PlayerIdleState`, `PlayerMoveState`, `PlayerSprintState`를 통한 로컬/리모트 캐릭터 상태 통합 관리.
- **Stamina System (New)**: 서버 측 스테미나 소모/회복 로직 구현 및 클라이언트 동기화 완료.
- **Async & Tweening**: `UniTask` + `DOTween` (리모트 플레이어 위치 보간).
- **Hierarchy Structure**: 실무형 레이어 구조 (`[--- MANAGERS ---]`, `[--- WORLD ---]`, `[--- UI ---]`).

## 3. 진행 상황 (2026-03-31)
### ✅ 완료된 작업
- **상태 머신 고도화**: `PlayerController`를 베이스로 `LocalPlayerController`와 `RemotePlayer`를 통합.
- **스테미나 시스템**: 대쉬(Sprint) 시 스테미나 소모 및 자동 회복 로직(서버 공인) 구현.
- **패킷 고도화**: `IsSprinting`, `CurrentStamina`, `MoveInput` 등을 포함한 정교한 상태 패킷 설계.
- **월드 기초 공사**: `WorldManager`를 통한 Tilemap 기반 맵 구조 설계 및 자동 생성 기초 작업.

### 🛠️ 다음 세션에서 해야 할 일 (Next Steps)
1. **[Visual] 시야 시스템 (FOV/Fog of War)**: 캐릭터 부채꼴 시야 및 장애물에 가려지는 그림자 시스템 (`Shadow Caster 2D`).
2. **[UI] 스테미나 바 및 상태 표시**: 클라이언트 하단에 스테미나 게이지 및 현재 상태 UI 구현.
3. **[World] 레벨 디자인**: 실제 타일 에셋을 적용하고 복잡한 던전 구조(Colliders) 배치.
4. **[Combat] 기본 공격 시스템**: 근접 공격 및 스테미나 추가 소모 로직 설계.

---
## 💡 다음 작업 추천 가이드
이제 **"시야 시스템(Fog of War)"** 구현을 강력하게 추천합니다. 
이유: "보이지 않는 공포"라는 게임의 정체성을 가장 잘 보여주는 핵심 시스템이기 때문입니다. 
동시에 **"UI(Stamina Bar)"**를 구현하여 플레이어가 자신의 상태를 직관적으로 알 수 있게 하면 게임 플레이의 완성도가 크게 올라갈 것입니다.

*이 파일은 Gemini CLI의 세션 간 컨텍스트 유지를 위해 작성되었습니다.*
