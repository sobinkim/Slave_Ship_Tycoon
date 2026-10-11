# 회복 지원함

## 전투 규칙

- 같은 팀의 살아 있고 손상된 함선을 전열 → 중열 → 후열 우선순위로 탐색하고, 해당 열에서 무작위로 선택한다.
- 플레이어 전열·중열·후열은 각각 편성 슬롯 `x=2/1/0`, 적 전열·중열·후열은 `x=0/1/2`이다. 우선 열에 대상이 없으면 다음 열을 확인한다.
- 자신, 다른 회복 지원선, 적, 최대 체력인 함선은 회복 대상에서 제외한다. 방어함의 직접 공격 보호를 받는 아군은 회복할 수 있다.
- 회복탄이 목표에 명중했을 때 체력을 회복한다. 최대 체력을 넘지 않고, 사망하거나 풀에서 다른 개체로 재사용된 대상은 회복하지 않는다.
- 회복탄은 아군 지원이므로 적 공격 경고, 회피 확률, 피해 판정을 사용하지 않는다.
- 현재 회복 대상으로 선택된 함선의 체력바 아래에 회복 지원 아이콘이 표시된다. 대상 변경·최대 체력·사망·지원함 비활성화·전투 종료 시 해제된다.
- 지원 아이콘은 원 없는 십자다. 128px 이미지 안에서 96px 높이로 배치해 기존 방어 아이콘과 표시 높이를 맞췄다. 공통 아이콘 크기와 간격은 `FleetEffectStatusIcons` Inspector에서 조절한다.

## 에셋과 연결

- 플레이어: `Assets/SB/Prefabs/PlayerShips/HealingSupportShip.prefab`
- 적: `Assets/SB/Prefabs/EnemyShips/EnemyHealingSupportShip.prefab`
- 탄: `Assets/SB/Prefabs/Projectiles/HealingCapsule.prefab`, `EnemyHealingCapsule.prefab`
- 명중 효과: `Assets/SB/Prefabs/Effects/HealingImpact.prefab`, `EnemyHealingImpact.prefab`
- 명중 효과는 `HealingCrossParticles`의 Particle System으로 십자가만 흩뿌린다. 원형 링과 별도 회복 숫자 스프라이트는 사용하지 않는다. 파티클이 모두 사라지면 풀로 반환한다.
- 모집 데이터: `Assets/SB/Data/PlayerFleet/EscortShips/Escort_HealingSupportShip.asset`
- 지원 효과: `Assets/SB/Data/FleetEffects/HealingSupport.asset`, `EnemyHealingSupport.asset`

기존 방어함의 이동·공격 반동·포구 효과·침몰 애니메이션과 이벤트를 복제하고, 각 상태의 스프라이트를 회복 지원함으로 교체했다. 전열 방어 효과를 주는 설정은 회복 대상 아이콘용 명시적 대상 효과로 교체했다. 공격·상태·투사체의 새 설정 기본값은 기존 공격 함선 동작을 유지한다.

플레이어 모집 목록과 뽑기 표 뒤에 신규 항목을 추가했다. 기존 항목의 순서·ID·가중치는 유지했다. 기존 편성 및 적 스테이지 배치는 유지하며, 적 지원함은 적 배치 창의 슬롯에 프리팹을 지정해서 사용할 수 있다.

## Inspector 조절값

| 위치 | 필드 | 초기값 / 의미 |
| --- | --- | --- |
| 지원함 `HealingProjectileAttackCompo` | `healAmount` | 30, 회복탄 1발의 회복량. 임시 밸런스 값 |
| 같은 컴포넌트 | `attackIntervalMultiplier` | 3, 동일 공격 속도에서 기존 함선 발사 간격의 3배 |
| 같은 컴포넌트 | `baselineAttackAnimationDurationSeconds` | 0.45833334, 공격 시작부터 `AttackEnd` 이벤트까지. 공격 클립 교체 시 함께 변경 |
| 지원함 `ShipStatCompo` | Health Stat Override | 120, 임시 기본 체력 |
| 지원함 `Visual/ShootPos`, `MuzzleVfx` | Local Position | 회복포 포구 위치. 아트 교체 시 직접 조정 |
| 지원함 `Visual` | Local Scale | 플레이어 (1.12, 1.44, 1), 적 (-1.12, 1.44, 1). 기존 대비 가로·세로 20% 축소 |
| `HealingProjectileAttackCompo` | `travelMode` | AcceleratingStraight, 파쇄선과 같은 직선 가속 이동 |
| 같은 컴포넌트 | `initialStraightSpeed`, `straightMaximumSpeed` | 2 → 12 월드 단위/초. 코팅 연출은 없음 |
| 명중 효과 `HealingCrossParticles` / Main | Start Lifetime / Start Size | 0.6~1초 / 0.11~0.18 |
| 같은 Particle System / Emission | Bursts | 0초 4개, 0.1초 3개, 0.2초 3개 |
| 같은 Particle System / Velocity over Lifetime | Y | 0.45~0.85 월드 단위/초로 상승 |
| `EscortGachaTable` | 신규 항목 Weight | 25, 기존 가중치에 추가된 임시 뽑기 가중치 |

발사 간격은 공격 애니메이션 속도를 유지한 채 대기시간을 연장한다. 기본 공격 대기시간과 공격 클립의 `AttackEnd` 시각을 함께 배율에 반영한다. 회복탄의 비행 속도는 기존 투사체 비행 설정으로 별도 조절한다.

회복량·체력·뽑기 가중치는 첫 구현을 위한 값이며 기획 확정 수치가 아니다. 지원 아이콘은 현재 선택된 회복 대상을 뜻하며 지속 회복 버프를 부여하지 않는다.

## 최초 구현 검증

- Unity 스크립트 컴파일 성공.
- 격리된 Preview Scene에서 회복 지원 규칙 및 프리팹 참조 36개 확인.
- 기존 타겟팅 24개와 투사체 회피 6개 테스트 통과.
- 실제 프리팹을 렌더링해 기존 함선과 크기·그림체, 공격 포구 위치, 사망 침몰, 회복 효과 표시를 확인.
- Play Mode에서 실제 Animator 이벤트 → 회복탄 발사·비행 → 명중 후 30 회복을 확인. 전열 A에서 저체력이 된 전열 B로 대상과 지원 아이콘이 변경되는 것도 확인.
- 실제 발사 간격은 일반 함선 1.4588초, 회복 지원함 4.3744초로 약 3배였다. 프레임에 따른 측정 오차를 포함한다.
- 플레이어·적 회복탄의 궤적 컴포넌트 참조를 확인하고, 실제 생성 시 참조 오류가 없음을 확인.
- 최종 실행 중 신규 오류 없음. 기존 `MerchantLedger needs an icon` 오류는 별도 장비 설정 문제로 남아 있다.
- 검증 후 Play Mode를 종료하고 임시 오브젝트를 정리했다. 기존 SampleScene은 저장 변경 없이 유지했다.

생성 코드는 `SB.Editor.HealingSupportShipSetup`에 있으며 기존 에셋이 존재하면 재생성하지 않는다. 이후 밸런스·참조는 해당 프리팹과 데이터 에셋의 Inspector에서 수정한다.

## 십자가 파티클·크기·직선 가속 수정

- `HealingImpactEffect.cs`를 Particle System 재생·풀 반환 방식으로 변경하고 두 명중 효과 프리팹에 참조를 연결했다.
- 플레이어·적 지원함 프리팹의 Visual을 20% 줄이고, 적의 좌우 반전 방향을 유지했다.
- 두 지원함의 직선 가속 모드·시작/최대 속도·비행 설정을 파쇄선에서 가져왔다. 회복탄의 기존 재질은 유지하고 코팅 컴포넌트를 추가하지 않았다.
- 스크립트 컴파일과 격리된 Editor 프리뷰에서 Idle·Attack·Dead 크기, 십자가 파티클 표시, 프리팹 연결을 확인했다. 씬 파일은 변경하지 않았다.

## 2026-10-11 명중 이펙트 참조 복구

- 저장된 HealingCapsule.prefab의 healingImpactEffectPrefab GUID/fileID는 정상이었지만, Editor가 로드한 아군 회복탄과 기존 풀 인스턴스에서는 null이었다. 적 회복탄의 연결은 정상이었다.
- HealingImpact → HealingCapsule → HealingSupportShip 순서로 의존 에셋을 재임포트해 연결을 복구했다. 실행 중 풀의 원본 프리팹 매핑으로 아군 회복탄 2개를 확인하고 해당 인스턴스의 null 참조도 복구했다.
- 저장된 프리팹 내용과 회복량·파티클 설정은 변경하지 않았다. 런타임 자동 탐색이나 자동 참조 보정 코드는 추가하지 않았다.
- 진행 중인 실제 전투에서 명중을 강제하지 않고 HealingImpact(Clone)이 생성되고 활성 파티클 3개가 재생되는 것을 확인했다. 재임포트 중 HealingCapsule/HealingSupportShip에 PrefabImporter inconsistent result 경고가 관측됐으며, 재임포트 후 참조는 HealingImpact로 유지됐다. 스크립트 컴파일 오류는 없었다.
