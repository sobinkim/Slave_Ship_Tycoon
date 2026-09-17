# 함선 효과와 방어 쉴드

## 현재 규칙
- 철갑 방어함(IroncladDefender)이 전열에 있으면 같은 행의 바로 뒤 슬롯 한 척만 보호한다. 빈 슬롯을 건너뛰지 않는다.
- 플레이어는 (1, 행) → (0, 행), 적은 (0, 행) → (1, 행)이다.
- 보호받는 함선은 일반·파쇄 함선의 공격 및 지휘 집중공격 직접 목표 후보에서 제외된다. `Can Target Protected Ships`가 켜진 습격함만 보호 상태의 후열을 직접 목표로 고를 수 있다.
- 보호 중인 함선은 모든 경로에서 최종 피해를 30% 감소시킨다. 습격함이 후열을 직접 공격할 수 있어도 이 감소는 적용된다. 방어함 사망·전투 종료 시 즉시 해제된다.
- 방어함은 파란 방패을 항상 표시하고, 보호받는 함선은 흰색~회색 방패를 표시한다. 뒤 슬롯이 비어 있으면 후열 방패만 없고 방어함 방패는 유지된다.
- 방어함 사망 시 보호 판정은 즉시 끝나고 양쪽 방패가 조각으로 퍼진다. 전투 종료와 일반 회수는 파괴 연출 없이 정리한다.

## 구조와 연결
- `FleetEffectDefinition`: 공유하는 효과 규칙. 대상 선정, 적용/제거 콜백을 정의한다. 실행 중인 함선별 상태는 이 에셋에 저장하지 않는다.
- `ImmediateRearProtection`: 인접 후방 한 척을 선택하고 직접 타겟 보호를 제공하는 규칙 에셋.
- `FleetEffectProvider`: 공급 함선의 명시적인 Owner / Source Receiver / Effects 참조. 전투 초기화 이벤트에서 효과를 배포하고 사망·비활성화·전투 종료에 회수한다.
- `FleetEffectReceiver`: 함선이 받은 효과를 공급자별로 보관한다. Presentations에서 효과 에셋과 표시 컴포넌트를 연결한다.
- `ShipShieldVisual`: 표시·피격 반응·파괴 요청. 전투 판정을 하지 않는다.
- `ShieldBreakEffect`: 함선과 독립된 풀 오브젝트로 파편 이동·회전·소멸을 재생한다.

기존 플레이어/적 함선 9개 프리팹에 Receiver와 ShieldAnchor가 연결돼 있다. Provider는 IroncladDefender에만 연결했다. 기존 Role 값으로 효과를 자동 부여하지 않는다. 기존 선체 크기와 Animator는 유지했다.

## Inspector 조절
- 각 프리팹의 `ShieldAnchor`: Local Position, Rotation, Scale로 함선 앞 위치·각도·크기를 조절한다. 선체 Visual과 별개라 애니메이션 스케일에 덮이지 않는다.
- `ShipShieldVisual`: Source Color / Protected Color의 알파, Pulse Seconds / Amount, Hit Flash Seconds.
- `Assets/SB/Art/Effects/FleetShield/Shield.mat`: 육각 무늬 밀도와 투명도.
- `Assets/SB/Prefabs/Effects/ShieldBreak.prefab`: Duration Seconds(기본 0.55초), Flash Seconds, 중력, 각 조각의 Local Velocity / Spin Degrees Per Second.
- `Assets/SB/Data/FleetEffects/ImmediateRearProtection.asset`: Require Front Line.
- 습격함 프리팹의 `Ship > Can Target Protected Ships`: 보호 중인 함선을 타겟 후보에 포함할지 결정한다. 습격함에만 켠다.

## 다음 효과 함선 추가
1. 기존 프리팹을 바탕으로 만들고 Owner와 시각 참조를 확인한다.
2. 공급 함선에 Provider를 붙이고 사용할 효과 에셋을 Effects에 연결한다.
3. 새로운 규칙이 필요하면 FleetEffectDefinition을 상속하고 CollectTargets / OnApplied / OnRemoved를 구현한다. 대상에 수치 효과를 추가하면 공급자별 해제도 짝으로 구현한다.
4. 필요한 시각 효과를 FleetEffectVisual로 구현하고 수신 함선의 Presentations에 연결한다.
5. 현재 배치는 전투 중 고정이다. 향후 전투 중 이동 기능을 넣을 때는 배치 변경 완료 시 Provider.BindBattle을 명시적으로 호출해 다시 계산한다.

## 검증
2026-09-17: Unity 컴파일 및 쉴드 셰이더 오류 없음. Play Mode에서 인접 후방 한 척, 다른 행 제외, 빈 슬롯 건너뛰기 금지, 전투 정리, 사망 즉시 양쪽 표시 해제 및 독립 파편 2개 생성, 적 진영의 반대 방향 인접 규칙을 확인했다.

렌더 미리보기: `Assets/QA/FleetShieldPreview.png`, `Assets/QA/FleetShieldBreakPreview.png`. 실제 프리팹을 별도 테스트 위치에 생성한 화면이며 저장된 함대 편성은 변경하지 않았다. 모바일 실기기에서 최종 밝기·크기·성능 감각은 추가 확인이 필요하다.
