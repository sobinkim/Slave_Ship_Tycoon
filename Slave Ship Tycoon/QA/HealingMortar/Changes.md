# 회복 지원함 수직 포신 적용

사용자가 승인한 `codex-clipboard-406d66d9-6669-4026-87ac-c1b665080059.png`를 플레이어·적 그림으로 분리해 기존 PNG 에셋에 적용했다. 적 그림은 기존 Visual의 음수 X 스케일에 맞춰 저장했다. 기존 표시 폭과 스프라이트 GUID, PPU, 피벗 설정을 유지했으며, 적 그림의 깃발이 잘리지 않도록 투명 캔버스 높이를 늘렸다.

## 변경 파일

- `Assets/SB/Art/Sprites/Ships/HealingSupportShip.png`
- `Assets/SB/Art/Sprites/Ships/Enemies/EnemyHealingSupportShip.png`
- `Assets/SB/Prefabs/PlayerShips/HealingSupportShip.prefab`
- `Assets/SB/Prefabs/EnemyShips/EnemyHealingSupportShip.prefab`
- 양쪽 `HealingSupportShip_Attack.anim`: 기존 발사 이벤트를 유지하고 반동을 아래 방향으로 변경.
- `Assets/SB/Scripts/Ship/Battle/TargetSelector.cs`: enum 끝에 `RandomInRule` 추가. 기존 enum 직렬화 값 유지. 같은 팀 주함선은 후열 지원 대상으로 취급.
- `Assets/SB/Scripts/Ship/Battle/AttackCompo/HealingProjectileAttackCompo.cs`: 실제 발사 이벤트에서 무작위 대상을 선택하고 지원 아이콘을 갱신.
- `Assets/SB/Scripts/Ship/Battle/Projectile/HealingProjectile.cs`: 전열 전용 착탄 제한 제거. 같은 팀·생존 조건은 유지.
- `Assets/SB/Editor/HealingSupportShipSetup.cs`: 신규 생성 기본값을 포물선과 무작위 열 선택으로 갱신.

## Inspector 조절 지점

회복선 프리팹의 `Visual/ShootPos`가 수직 포구 기준점이다. `Visual/MuzzleVfx`도 같은 위치에서 위로 향한다.

`HealingProjectileAttackCompo`:

- Travel Mode: `BallisticArc`
- Ballistic Apex Height: `0.9` 월드 단위
- Ballistic Time To Apex: `0.38`초
- Ballistic Descent Time Multiplier: `1`
- Ballistic Target Clearance: `0.25` 월드 단위
- Heal Amount: 기존 `30`
- Attack Interval Multiplier: 기존 `3`

`TargetSelector`:

- Selection Mode: `RandomInRule`
- Target Team: `SameTeam`
- Target Rules: `Front → Middle → Back`, 각 역할 제한 없음
- Requires Damaged Target: 활성화. 앞선 열에 회복이 필요한 아군이 없으면 다음 열로 이동.
- Allows Rule Fallback: 비활성화

현재 플레이어 호위선은 기존 2열 편성이라 전열과 후열 슬롯을 사용하며, 적은 기존 3열 편성의 전열·중열·후열을 모두 사용한다. 플레이어 주함선은 후열 지원 후보에 포함된다. 사망·비활성·자기 자신·최대 체력 함선은 후보에서 제외된다.

## 검증

Unity 6000.0.59f2 Editor에서 코드 컴파일 오류 0개. `Verification.json`의 25개 검증 통과: 양쪽 프리팹 연결, 열 순서, 전열 무작위 선택, 다음 열 이동, 중열·후열 착탄 회복, 상대 팀 회복 차단, 주함선 지원, 포물선 상승·정점·착탄, 모집 카탈로그 연결. 전열의 서로 다른 체력 후보 두 척은 고정 seed의 128회 선택에서 각각 56회와 72회 선택됐다.

양쪽 공격 클립의 기존 이벤트 2개를 유지하고 발사 시 Y 반동 `-0.11`을 확인했다. 기존 십자가 파티클과 지원 아이콘 참조도 유지된다. 전체 전투 장면의 Play Mode 시각 확인은 별도 수행하지 않았다. 추가로 연결해야 할 참조는 없다.

변경 전 파일은 `Before`에 보관했다. `ApplyCode.cs`와 `ApplyAssets.cs`는 이번 변경 기록을 위한 일회성 Editor 실행 코드다.
