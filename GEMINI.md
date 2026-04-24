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

## 3. 진행 상황 (최근)
### ✅ 완료된 작업 (2026-04-23)
- **던전 환경 및 시야 시스템 기초 완성**:
    - **시야 제한 (FoV)**: `LocalPlayer` 프리팹에 `Point Light 2D` (Shadows Enabled) 부착하여 플레이어 중심 시야 구현.
    - **그림자 생성**: 벽 타일맵에 `Shadow Caster 2D`를 적용하여 벽 너머 시야 차단 시스템 구축.
    - **환경광 조정**: `GlobalLight_2D` 인텐시티를 조절하여 어두운 던전 톤 연출.
- **가시성 및 렌더링 교정**:
    - **Sorting Order & Z-Depth**: 모든 타일맵의 소팅 순서 정렬 및 Z축 깊이(0) 고정으로 렌더링 누락 방지.
    - **Sword 프리팹 복구**: 검의 `Sorting Order(50)`, `Layer(Default)`, 머티리얼 누락 문제를 해결하여 시각화 정상화.
- **전투 시스템 물리 레이어 정상화**:
    - **Monster 레이어**: 6번 레이어를 `Monster`로 재설정하고 씬 내 몬스터 일괄 복구.
    - **Hit Detection**: 검(`Sword.cs`)의 `hitLayers` 마스크를 몬스터 레이어에 맞게 재구성하여 타격 판정 복구.

### 🛠️ 다음 세션에서 해야 할 일 (Next Steps)
1. **[Combat] 몬스터 전투 디벨롭 (최우선)**:
    - **공격 예고(Telegraphing)**: 몬스터 공격 시 실제 판정 범위에 맞춘 시각적 궤적(Arc) 또는 이펙트 표시.
    - **AI 다양화**: 원거리 공격 몬스터 추가 및 공격 패턴(돌진, 휘두르기 등) 확장.
    - **전투 무게감**: 타격 시 역경직(HitStop) 수치 미세 조정 및 피격 애니메이션 보강.
2. **[Interaction] 상호작용 시스템**:
    - F키를 이용한 상자(Chest) 열기, 문 개폐 등 기초 상호작용 로직 구현.
3. **[Map] 던전 디테일링**:
    - 무료 에셋(`Cainos`) 타일 팔레트를 활용한 본격적인 던전 맵(복도, 방) 디자인.

---
## 💡 작업 요약
어두운 던전에서의 시야 제한(FoV)과 그림자 시스템을 구축하여 게임의 핵심 비전인 **"보이지 않는 공포"**의 기초를 다졌습니다. 가시성과 레이어 충돌 문제를 해결하여 타일맵, 몬스터, 무기가 정상적으로 보이고 작동하도록 물리 환경을 재정비했습니다. 다음 단계로는 몬스터와의 전투를 보다 깊이 있게 발전시키는 데 집중할 예정입니다.
