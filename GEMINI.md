# Project_SS: 2D Hardcore Top-down Extraction Game

## 1. 프로젝트 비전
- **"보이지 않는 공포와 무게감"**: 하드코어 2D 익스트랙션 장르.
- **핵심 키워드**: 가시거리 제한, 스테미나 기반 전투, 물리적 무게감, 사운드 시각화.

## 2. 현재 아키텍처 (Current Tech Stack)
- **Server-Authoritative**: 모든 물리 및 상태 판정은 C# 독립 서버(`Server/Program.cs`)가 담당.
- **Networking**: `LiteNetLib` (TCP/UDP) + 커스텀 패킷 시스템.
- **Predictive Movement**: 클라이언트 측 즉시 이동(Prediction) + 서버 공인 위치 보간(Interpolation).
- **Async & Tweening**: `UniTask` + `DOTween` (유닛 이동 및 연출).
- **Hierarchy Structure**: 실무형 레이어 구조 (`[--- MANAGERS ---]`, `[--- WORLD ---]`, `[--- UI ---]`).

## 3. 진행 상황 (2026-03-29)
### ✅ 완료된 작업
- **멀티플레이 동기화**: 2인 이상의 플레이어가 서로의 움직임을 실시간으로 확인 가능.
- **패킷 시스템**: `Welcome`, `Input`, `State`, `Leave` 패킷을 통한 정교한 핸드셰이킹 및 동기화.
- **버그 수정**: `worldAABB` 에러(좌표 튐 현상) 및 `Run In Background` 설정 해결.
- **프리팹 시스템**: `Resources/Prefabs`를 통한 로컬/리모트 캐릭터 자동 생성 및 색상 구분.

### 🛠️ 다음 세션에서 해야 할 일 (Next Steps)
1. **[Architecture] ScriptableObject 기반 데이터 설계**: 플레이어 스탯(Stamina, Speed, HP)을 SO로 관리.
2. **[System] 스테미나 시스템**: 서버 측 스테미나 소모/회복 로직 구현 및 클라이언트 UI 연동.
3. **[World] 프로페셔널 맵 구조**: 유니티 Tilemap + Shadow Caster 2D를 활용한 하드코어한 던전 환경 구축.
4. **[Visual] 시야 시스템 (Fog of War)**: 캐릭터 FOV(부채꼴 시야) 기반 2D 광원 시스템 기초 작업.

---
## 💡 다음 작업 추천 가이드
다음 세션에서는 **"스테미나 시스템"**을 먼저 구현하는 것을 추천합니다. 
이유: 이 게임의 핵심인 '무게감 있는 전투'의 엔진이 바로 스테미나이기 때문입니다. 
동시에 **"월드 구성(Tilemap)"**을 진행하여 실제 던전 같은 느낌을 내기 시작하면 개발 동기부여가 크게 될 것입니다.

*이 파일은 Gemini CLI의 세션 간 컨텍스트 유지를 위해 작성되었습니다.*
