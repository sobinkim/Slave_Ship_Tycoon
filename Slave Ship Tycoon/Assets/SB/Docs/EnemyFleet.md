# 적 함선 4종

## 에셋

| 적 함선 | 프리팹 | 플레이어 구조 원본 | 돛 문양 |
| --- | --- | --- | --- |
| 포격선 | Assets/SB/Prefabs/EnemyShips/EnemyGunship.prefab | PlayerShips/Test/TestEscort_A.prefab | 원본 포격선의 기워진 천 |
| 작살 습격함 | Assets/SB/Prefabs/EnemyShips/EnemyHarpoonRaider.prefab | PlayerShips/HarpoonRaider.prefab | 원본 갈고리 |
| 철갑 방어함 | Assets/SB/Prefabs/EnemyShips/EnemyIroncladDefender.prefab | PlayerShips/IroncladDefender.prefab | 원본 방패 |
| 파쇄선 | Assets/SB/Prefabs/EnemyShips/EnemyCrusherShip.prefab | PlayerShips/CrusherShip.prefab | 원본 깨진 방패 |

승인받은 포격선·방어선 시안을 기준으로 4종의 붉은 돛, 사선 철판, 따뜻한 목재색과 금색 부속을 맞췄습니다. 방어선의 방패는 원화에 굽지 않고 기존 빨간 런타임 이펙트로 표시합니다.

새 원화는 `Assets/SB/Art/Sprites/Ships/Enemies`, 원본에서 가져온 돛 문양은 그 아래 `RoleEmblems`에 있습니다. 원본 문양의 픽셀을 복사해 배경 천과 가장자리 선을 제외했습니다. 포격선 원본에는 별도 포격 아이콘이 없어 기존 돛의 천 패치를 재사용했습니다.

4종 모두 기존 Ship 대신 Enemy 컴포넌트를 사용하며 ShipType은 Enemy입니다. 관련 컴포넌트의 owner/ship 참조도 새 Enemy로 연결했습니다. EnemyRewardDropCompo와 기존 보상 종류를 복사하고 dropPosition을 각 함선의 HitPoint로 연결했습니다. 보상은 Enemy의 기존 사망 흐름에서 처리하며 추가 사망 이벤트 연결은 없습니다.

체력·피해량·공격 대기 시간·탄도 방식은 각 플레이어 구조 원본을 이어받았습니다. 새 밸런스 계산이나 자동 보정은 추가하지 않았습니다. 포격선과 작살선은 Dealer, 방어선은 Tanker, 파쇄선은 Piercer 역할입니다.

## Inspector에서 조절할 위치

- 프리팹 루트 `Transform/Scale`: 적 전체 표시 크기. 4종과 보스 Variant 모두 (1, 1, 1)입니다. 그림·방패·체력바·발사점이 함께 조절됩니다.
- `Visual`: 함선별 그림 비율. 원화는 오른쪽을 향하고, 이 Transform의 X Scale을 음수로 설정해 적 함선을 왼쪽으로 향하게 했습니다. 자식의 로컬 X가 양수이면 이 프리팹에서는 화면의 왼쪽으로 배치됩니다.
- `Visual/SailRoleEmblem`: 돛 문양 SpriteRenderer. 로컬 위치와 크기를 직접 조절할 수 있습니다.
- `Visual/ShootPos`: 발사 지점. `AboutBattle/ArcProjectileAttackCompo`의 Fire Points에 명시적으로 연결되어 있습니다.
- `Visual/HitPoint`, `Visual/WaterImpactPoint`: 함선 적중·빗나감 위치. 기존 ShipProjectileAnchorCompo에서 사용합니다.
- `Visual/LoadedHarpoon`, `Visual/LoadedStake`: 별도 장전 탄두. 각 애니메이션의 해당 경로에 위치와 표시 여부가 설정되어 있습니다. 위치를 바꾸면 해당 애니메이션의 위치 키도 함께 수정합니다.
- `Presentation/ShieldAnchor/ShipShieldVisual`: Source Color는 빨강 (1, 0.12, 0.18, 0.925), Protected Color는 연한 빨강 (1, 0.34, 0.38, 0.45)입니다. 모든 적 함선의 보호받는 방패도 빨간 계열로 설정했습니다. 깨짐 효과는 같은 tint를 전달받습니다.
- 방어선 루트의 `FleetEffectProvider`: 기존 ImmediateRearProtection 데이터와 새 Enemy owner가 연결되어 있습니다.
- `EntityStatCompo/ShipStatCompo`: Stat Overrides에서 함선별 체력과 피해량 등을 조절합니다.
- `RewardCompo/EnemyRewardDropCompo`: Reward Type에서 보상 종류를 조절합니다.

함선 머티리얼은 `Assets/SB/Art/Materials/Ships/Enemies`에 분리했습니다. 돛 문양도 함선과 함께 침몰하도록 별도 Waterline 머티리얼과 Sink Progress 애니메이션을 연결했습니다. 문양의 Y 위치 또는 크기를 바꾸면 문양 머티리얼의 Waterline/Sink Distance도 조절해야 합니다.

## 애니메이션

`Assets/SB/Animations/Ships/Enemies/<함선명>`에 Idle, Move, Attack, Dead 4개 클립과 AnimatorOverrideController를 저장했습니다. 총 16개 클립입니다.

기존 상태머신·파라미터·전이 구조를 공유하고 동작 클립은 적 전용 클립으로 교체합니다. 방어선·작살선·파쇄선은 각 플레이어 컨트롤러가 기반입니다. 포격선은 새 단일 원화에 맞춰 기존 방어선의 이동·반동·발사 섬광·침몰 동작을 사용합니다. 원본 프레임 애니메이션을 참조하는 키가 적 원화로 돌아가지 않도록 모든 함선 Sprite 키를 적 원화로 교체했습니다.

- Idle/Move: 기존 흔들림과 이동 동작.
- Attack: 반동, 포격선·방어선의 발사 섬광/연기, 작살·파쇄 탄두의 표시와 발사 순간 숨김.
- Dead: 회전·침강과 수면 아래 잘림. 돛 문양도 함께 사라집니다.
- AttackStart: 0.1667초, AttackEnd: 0.4583초, Dead: 0.6875초. 기존 EntityAnimatorTrigger 이벤트를 유지합니다.

공유하는 플레이어 기본 AnimatorController의 상태 구조를 수정하면 적 OverrideController에도 영향을 줍니다. 적의 동작만 수정하려면 적 폴더의 클립을 편집합니다.

## 스테이지에서 사용

현재 ChapterDatabase가 사용하는 1~4장 일반 웨이브와 보스, 총 20개 StageEnemyLayout을 새 함선으로 교체했습니다. 각 배치의 기존 함선 수(1/3/6/8척)는 유지했습니다. 기존 TestEnemy_A/B/C와 TestBoss는 배치와 SampleScene의 PoolingManager 프리로드에서 제외했습니다.

행은 위에서 0~3, 열은 전열(0)·중열(1)·후열(2)입니다. 화면에서 적 전열은 왼쪽입니다.

- 방어선(D): 전열. 같은 행 바로 뒤 중열의 함선 하나를 보호합니다.
- 작살선(H): 중열. 방어선의 보호를 받으며 기존 보호 대상 직접 공격 기능을 사용합니다.
- 파쇄선(C): 중열. 방어선의 보호를 받으며 기존 고체력 우선 타겟 선택을 사용합니다.
- 포격선(G): 후열. 지원 사격 위치입니다. 방어선은 바로 뒤 한 칸만 보호하므로 후열까지 보호하지 않습니다.
- 파쇄 대장선(B): 기존 Chapter1_Boss의 단독 보스를 새 파쇄선 Variant로 교체했습니다. `Assets/SB/Prefabs/EnemyShips/EnemyCrusherBoss.prefab`이며 체력 300과 기존 Boss 보상 종류를 유지합니다. 나머지 전투·타겟·외형·애니메이션은 EnemyCrusherShip에서 상속합니다.

일반 적의 전투 수치는 새 함선 프리팹의 기존 값을 사용합니다. 이전 테스트 적의 체력으로 다시 맞추거나 자동 밸런싱하지 않았습니다. 3장의 보스 연결은 기존 Stage_03_05 에셋을 계속 사용합니다.

배치를 바꾸려면 각 Layout Inspector의 Enemy Slots를 수정합니다. 방어선의 바로 뒤 칸을 비우면 보호 대상도 사라집니다.

| 배치 | 0행 (전/중/후) | 1행 | 2행 | 3행 |
| --- | --- | --- | --- | --- |
| Chapter1_Boss | - / - / - | B / - / - | - / - / - | - / - / - |
| Stage_01_01 | D / H / G | D / C / G | D / H / - | - / - / - |
| Stage_01_02 | - / - / - | D / H / G | - / - / - | - / - / - |
| Stage_01_03 | - / - / - | - / - / - | D / C / G | - / - / - |
| Stage_01_04 | D / C / G | - / - / - | D / H / G | - / - / - |
| Stage_02_01 | - / - / - | D / H / G | D / C / G | - / - / - |
| Stage_02_02 | D / C / G | - / - / - | - / - / - | - / - / - |
| Stage_02_03 | - / - / - | - / - / - | D / H / G | - / - / - |
| Stage_02_04 | D / C / G | - / - / - | - / - / - | D / H / G |
| Stage_02_05 | - / - / - | D / C / G | D / C / G | - / - / - |
| Stage_03_01 | D / H / G | - / - / - | - / - / - | D / H / G |
| Stage_03_02 | - / - / - | D / C / G | - / - / - | - / - / - |
| Stage_03_03 | - / - / - | - / - / - | D / H / G | - / - / - |
| Stage_03_04 | - / - / - | D / C / G | - / - / - | D / H / G |
| Stage_03_05 | - / - / - | D / C / G | D / H / G | - / - / - |
| Stage_04_01 | D / H / G | - / - / - | D / C / G | - / - / - |
| Stage_04_02 | - / - / - | D / C / G | - / - / - | - / - / - |
| Stage_04_03 | - / - / - | - / - / - | D / H / G | - / - / - |
| Stage_04_04 | D / C / G | - / - / - | - / - / - | D / C / G |
| Stage_04_05 | - / - / - | D / C / G | D / H / G | - / - / - |

프리로드는 포격 2, 작살 2, 방어 3, 파쇄 2, 대장선 1척으로 명시적으로 설정했습니다. FloatingText_Gold의 프리로드 12개는 기존 값입니다. 런타임 스폰 구조는 기존 StageSpawnManager를 사용합니다. 적 스폰 위치는 아래 간격 조정 값으로 저장했습니다.

## 확인한 내용

- 4종 Enemy/보상 컴포넌트, 각 4개 클립, 프리팹 필수 참조, 발사점, 탄환 프리팹, 적중 위치 연결 확인.
- 플레이어 owner 참조 및 원화 Sprite 키가 남아 있지 않음.
- 발사·공격 종료·사망 이벤트와 모든 애니메이션 경로 확인.
- Unity 미리보기에서 이동, 장전 준비, 발사, 침몰 프레임을 렌더링해 문양·탄두·빨간 방패·수면 잘림 확인.
- 컴파일 상태 up_to_date, compilationFailed=false. 새 함선 프리팹/클립 참조 검증 Issues=0.
- Console에는 이번 교체 전 발생한 MerchantLedger 아이콘 누락과 MCP 호출 시간 초과 기록이 남아 있습니다. 새 프리팹 검증 중 추가 로그 오류는 발생하지 않았습니다.
- SampleScene의 적 프리로드와 12개 적 스폰 지점, 전투 영역 관련 RectTransform 7개, 바다·하늘·섬 타일 9개의 표시 위치/크기를 수정했습니다. 당시에는 플레이어 스폰 지점과 전투 수치를 유지했습니다. 최신 양쪽 진영의 위치 조정은 아래 항목을 참고합니다. 저장 후 dirty=false.
- 플레이 모드에서 20개 배치를 기존 StageSpawnManager로 실제 스폰해 93개 슬롯의 새 프리팹·적 팀·원화·좌표를 확인했습니다. 기존 적 생성은 0개입니다.
- 방어선과 중열의 31개 보호 조합 모두 직접 타겟 보호와 받는 피해 배율 0.7을 확인했습니다. 검증 불일치 0개.
- 이 확인은 시간을 멈춘 상태에서 스폰·보호 효과를 점검한 것이며 장시간 전투 밸런스 검증은 포함하지 않습니다.
- 실제 스폰 확인 중 기존 MerchantLedger 아이콘 누락 오류가 다시 발생했습니다. 적 스폰/보호 오류는 발생하지 않았습니다.
- 최초 배치 확인 뒤 플레이 모드를 종료했습니다. 최신 일자 격자 확인 뒤의 상태는 아래 조정 항목을 참고합니다.

미리보기: `output/enemy-fleet/EnemyFleet_Overview.png`.
실제 배치 확인 화면: `output/enemy-fleet/EnemyFormation_InGame.png`.
동작 프레임: `output/enemy-fleet/EnemyFleet_AnimationFrames.png` (위에서 이동 / 발사 준비 / 발사 / 침몰 순서).

## 적 함선 크기와 전투 공간 조정

작게 보이던 적 루트 Scale 0.60을 1.00으로 복구한 설정은 유지합니다. 기존 적 원화 비율과 애니메이션, 전투 수치, 20개 역할 배치도 유지합니다.

### 양쪽 진영 배치 정리 — 2026-10-10

플레이어 호위함과 적의 행 높이를 같은 기준으로 정렬했습니다. 적은 사용자가 제시한 **3열 × 4행 격자**, 플레이어 호위함은 기존 2열 × 3행을 사용합니다. 메인 함선은 호위함 뒤쪽에서 가운데 행에 맞춥니다. 같은 열은 X가 같고, 같은 행은 Y가 같습니다. 비어 있는 슬롯의 위치도 유지됩니다.

함선마다 달랐던 체력바의 로컬 위치도 (0, -0.60, 0)으로 통일했습니다. 원화 자체의 함종별 높이 차이는 유지하되 체력바로 행 기준을 읽을 수 있게 합니다.

### Inspector 조절 위치

- 적 프리팹 루트 Transform Scale: 그림·체력바·방패·발사점의 공통 크기입니다. 4종과 보스 Variant 모두 (1, 1, 1)입니다.
- SampleScene의 StageSpawnManager > Enemies > Spawn Points: 12개 적 Transform의 Local Position입니다. 기준 좌표계는 EnemyFleetRoot이며 열 간격 1.20, 행 간격 1.18입니다.
- EnemyFleetRoot가 도착 지점 X=1.5, Y=1.61에 있을 때 전열/중열/후열 World X는 1.57/2.77/3.97입니다.
- StageSpawnManager > Player Fleet > Escort Ship Spawn Points: 6개 플레이어 호위함 Transform입니다. World X=-0.60/0.40, World Y=2.25/1.07/-0.11입니다. 적의 위쪽 세 행과 같은 높이입니다.
- StageSpawnManager > Player Fleet > Main Ship Spawn Point: World Position=(-1.94, 1.07, 0)입니다. 큰 메인 함선과 첫 번째 호위함 열 사이에 여백을 확보했습니다.
- Main Camera: Orthographic Size=6.65, World Position=(0.86, -0.65, -10)입니다. 양쪽 진영 사이와 화면 양 끝에 여백을 확보합니다. 함선의 Transform Scale은 유지하며, 화면상 크기는 이전 카메라 Size 6.00 대비 약 90%입니다.
- 적 4종 및 TestEscort_A/B/C의 WorldUI/HullCondition: Local Position=(0, -0.60, 0)입니다. 체력바 폭과 상태 아이콘 연결은 유지합니다. EnemyCrusherBoss는 EnemyCrusherShip의 체력바 위치를 상속합니다.
- 방어선의 Presentation/ShieldAnchor: Local X=-0.35입니다. 기존 빨간 방패를 유지합니다.
- Canvas/GameFrame/BattleArea 높이 1210, CommandStrip=(24, -1220), AutoUseButton=(0, 66), 하단 패널 높이 378은 앞선 전투 영역 확장 값입니다. 이번 정리에서 변경하지 않았습니다.
- 바다·하늘·섬 타일도 앞선 확장 값을 유지합니다. Sea World Y=0.375, Scale Y=1.761905, Sky World Y=4.725, Island World Y=4.158입니다.

| 행 | 공통 World Y | 적 전열 Local Position | 적 중열 Local Position | 적 후열 Local Position |
| --- | --- | --- | --- | --- |
| 0 | 2.25 | (0.07, 0.64, 0) | (1.27, 0.64, 0) | (2.47, 0.64, 0) |
| 1 | 1.07 | (0.07, -0.54, 0) | (1.27, -0.54, 0) | (2.47, -0.54, 0) |
| 2 | -0.11 | (0.07, -1.72, 0) | (1.27, -1.72, 0) | (2.47, -1.72, 0) |
| 3 | -1.29 | (0.07, -2.90, 0) | (1.27, -2.90, 0) | (2.47, -2.90, 0) |

좌표는 씬 Transform에, 체력바 위치는 프리팹 Transform에 직접 저장되어 있습니다. 새로운 런타임 자동 배치·크기 보정 코드는 없습니다. 기존 보호 판정은 같은 행의 바로 뒤 칸을 계속 사용합니다.

### 확인

- 저장한 씬과 프리팹으로 실제 스폰한 20개 배치의 적 93개 슬롯, 플레이어 120개 인스턴스, 31개 보호 조합을 확인했습니다. 배치·참조·보호 판정·피해 배율 0.7·체력바 위치의 불일치는 0개입니다.
- 정지한 초기 배치에서 원화의 불투명 픽셀을 0.0125 월드 간격으로 점검했습니다. 플레이어끼리, 적끼리, 양 진영 사이의 선체 겹침이 있는 쌍은 0개입니다.
- 세로 Game View에서 양쪽 행 정렬, 메인 함선과 호위함 사이 간격, 양 진영 사이 여백, 네 번째 행의 체력바·상태 아이콘 표시를 확인했습니다.
- 변경 전후 에셋 내용을 비교했습니다. SampleScene에서는 메인 함선·호위함 6개·적 12개 스폰 지점과 카메라 위치/크기만 바뀌었고, 7개 함선 프리팹에서는 HullCondition의 로컬 위치만 바뀌었습니다.
- C# 스크립트 변경은 없습니다. Unity 컴파일 실패 상태는 false입니다. 기존 MerchantLedger 아이콘 누락 기록은 이번 배치 수정 범위에 포함하지 않습니다.
- 초기 배치 확인이며 장시간 전투, 모든 애니메이션 프레임과 화면비 확인은 포함하지 않습니다.
- 확인용 이미지의 임시 에셋은 제거했습니다. 플레이 모드에서 Pause 해제, Time Scale 1로 복구했습니다.

양쪽 진영 정리 화면: `output/enemy-fleet/FleetLayout_BothSides.png`.
네 번째 행: `output/enemy-fleet/FleetLayout_BothSides_LastRow.png`.
직전 적 격자 화면: `output/enemy-fleet/EnemyFleet_Aligned.png`.

## 이번 디자인의 표시 설정

원화 교체 시 기존 에셋 경로와 GUID를 유지했습니다. 아래 값은 프리팹/Importer에 저장된 명시적 값이며 런타임 자동 배치 계산은 없습니다.

| 함선 | 원화 크기 | PPU | 문양 로컬 위치 (X, Y) | 문양 Scale | 장전 탄두 로컬 위치 (X, Y) |
| --- | --- | --- | --- | --- | --- |
| 포격선 | 1254 × 1254 | 1254 | (-0.205, 0.061) | 1.4 | 없음 |
| 작살선 | 1454 × 1082 | 1400 | (-0.212, 0.051) | 0.72 | (0.42, -0.055) |
| 방어선 | 1470 × 1070 | 1700 | (-0.165, 0.041) | 0.48 | 없음 |
| 파쇄선 | 1536 × 1024 | 1600 | (-0.127, 0.037) | 0.7 | (0.355, -0.025) |

모든 Sprite pivot은 (0.5, 0.5)입니다. 발사점, 피격점, 수면 위치, 방패 위치도 새 그림에 맞춰 프리팹에 저장했습니다. 장전 탄두는 공격 0.1667초에 숨겨지고 실제 투사체는 기존 발사 컴포넌트가 생성합니다.

## 원화 제작 프롬프트

Built-in image_gen을 사용했습니다. Transparent Background=true로 생성한 4개 최종 원화를 프로젝트의 기존 적 Sprite 경로로 가져왔습니다. 돛 문양은 플레이어 원본에서 재사용했습니다.

### EnemyGunship

참조: exec-e39fdf3b-24b3-48e1-a999-0912ebfa8961.png

Use case: precise-object-edit. Asset: transparent 2D Unity enemy cannon ship sprite. Image 1 is the APPROVED edit target.
Preserve this exact ship design: warm orange brown curved wooden hull, modest gray cannon, three slanted riveted iron reinforcement strips, gold rim and fittings, deep plum thick outlines, clean simple cartoon cel shading, burgundy ragged sail and red pennant, same detail and proportions.
Make only these production edits: horizontally mirror the entire ship so the bow and cannon point RIGHT and the mast/sail sit toward the LEFT. Remove the tan stitched patch emblem from the main sail, smoothly filling it with the matching burgundy sail folds; leave the broad central sail area empty for a separate original game emblem overlay.
Output one entire isolated ship on genuine transparent background, generous safe transparent margin all around. No water, shadow on ground, aura, VFX, labels, UI, extra equipment, text. Keep the approved drawing style and colours exactly.

### EnemyIroncladDefender

참조: exec-121a582c-9ce1-43c9-8684-623d2fca448e.png

Use case: precise-object-edit. Asset: transparent 2D Unity enemy defender hull sprite. Image 1 is the APPROVED edit target.
Keep this exact warm wooden broad armored ship, gray diagonal overlapping riveted reinforcement plates, modest short cannon and two side gunports, yellow gold fittings, deep plum bold outlines, clean cel shading, red ragged sail and red pennant. Preserve proportions, art style and detail level.
Only production edits: REMOVE the entire floating red translucent magic shield effect ahead of the bow, leaving transparency there, reconstructing any small hull areas it covered. Remove the black shield symbol from the red sail and fill with matching red folds, leaving a large empty central sail area. Mirror the complete ship horizontally so its bow and cannon point RIGHT, mast/sail to LEFT.
One whole isolated ship on genuinely transparent background with ample safe margin. Do not draw any shield effect, glow, aura, water, scene, floor shadow, labels, text, UI. Shield and exact original sail emblem are separate runtime overlays.

### EnemyHarpoonRaider

참조: exec-e39fdf3b-24b3-48e1-a999-0912ebfa8961.png, Assets/SB/Art/Sprites/Ships/HarpoonRaider_Unloaded.png

Use case: stylized-concept. Asset: production transparent 2D idle-game enemy harpoon ship hull.
Image 1 is the approved enemy fleet STYLE and COLOUR reference. Image 2 is the player HARPOON ROLE / weapon reference, not a hull design to copy verbatim.
Create one new enemy harpoon raider belonging to the same red-sail fleet as Image1. Friendly readable chunky cartoon art, thick dark plum outline, simple crisp 2-3 shade planes, warm orange brown wood, gold fasteners, gray lavender metal, ivory rope. Mild enemy attitude, early game comparable strength.
RIGHT-facing small low wooden raider, single mast toward LEFT, one burgundy-red ragged rectangular sail and red pennant. The main sail has a broad BLANK central area: no symbol or patch because a game emblem will be overlaid. Hull has a sharper bow and 3 angular diagonal riveted iron reinforcement strips like Image1. On front deck a wooden-and-metal harpoon launcher/ballista with ivory bowstrings, tilted only slightly upward, barrel aimed RIGHT, gold hub, compact sturdy cradle like Image2. Launcher is UNLOADED with empty muzzle: NO arrow, bolt, harpoon shaft, hook or projectile in/above the weapon. That projectile is separately animated.
Entire ship visible and centered with safe transparent margin. No water, ground shadow, aura, glow, shield, spikes, extra mast, character, skull, lettering, UI. Match image1 colour saturation, line weight and amount of detail. Make the fleet resemblance clear while silhouette and harpoon weapon remain recognizable.

### EnemyCrusherShip

참조: exec-e39fdf3b-24b3-48e1-a999-0912ebfa8961.png, Assets/SB/Art/Sprites/Ships/CrusherShip_Unloaded.png

Use case: stylized-concept. Asset: production transparent 2D idle-game enemy crusher / shield-piercing ship hull.
Image 1 is the APPROVED red-sail enemy fleet STYLE/COLOUR reference. Image2 gives the player CRUSHER ROLE and equipment design only.
Create one new early game enemy crusher in the same fleet, RIGHT-facing. Same thick dark plum cartoon outlines, clean simple 2-3 tone cel shading, warm orange brown wood, gray lavender iron, gold caps/rings, ivory ropes and burgundy-red ragged sail. Mild enemy appearance and comparable equipment strength to Image2, not a boss.
Broad low wooden hull with a sharp armored wedge bow at RIGHT and 3 slanted riveted iron reinforcement strips in the Image1 style. One mast at LEFT with burgundy-red rectangular ragged sail, red pennant, gold rings. Keep a large central sail area completely BLANK for an original game emblem overlay, no drawn symbol. Chunky short wide gray cannon on the RIGHT deck aimed slightly upward, gold collar and round gold hub, stout wooden mount. Two small spare gray pointed stakes stand upright at the REAR deck. The forward cannon is UNLOADED: open empty bore, NO projectile or stake sticking out, it will be a separate animation element.
Entire ship centered, safe margins, genuine transparent background. No water, scene, ground shadow, glow or aura, shield VFX, text, UI, characters, skull, elaborate spikes, second mast. Match the approved enemy cannon ship's detail, red sail hue and outline weight closely; retain crusher's distinct wide muzzle and armored wedge bow.

### 파쇄선 배경 정리

Use case: background-extraction / precise-object-edit. Image1 is the exact production ship to preserve. Remove ONLY the soft purple/magenta glow/aura surrounding this ship. Cut out the entire boat cleanly, right up to its crisp dark plum outline, and make ALL pixels outside its solid silhouette fully transparent, including gaps between ropes, cannon and mast. There must be NO fuzzy coloured halo or soft shadow. Preserve all ship geometry, burgundy-red blank sail, colours, line work, shading, cannon, two rear stakes, diagonal armor, right facing orientation exactly. Do not redesign or add anything. Complete centered isolated sprite on genuinely transparent background. This is a game hull sprite with crisp edges, no VFX.

## 전투 / UI 비율을 참조 화면에 맞춤 — 2026-10-10

사용자가 보낸 세로 화면을 기준으로 전투 영역과 하단 UI의 비율을 조정했습니다.

- BattleArea 높이: 980 / 전체 기준 높이 1920 = 약 51.0%.
- BottomUIBackground: Top=980, 높이=940.
- CommandStrip: Top=990, 높이=165.
- UpgradePanel / CagoPanel / TransportEquipmentPanel: Top=1172, 높이=608. 하단 메뉴 위치는 유지합니다.
- 본선 World Y=1.35, 호위함 3행 World Y=2.55 / 1.35 / 0.15.
- 적 4행 World Y=3.18 / 2.10 / 1.02 / -0.06. 같은 행의 세 열은 동일한 Y이며 기존 3열 × 4행 슬롯과 역할 규칙을 유지합니다.
- 함선 World X, 프리팹 Scale, 원화, 카메라 크기·위치·전체 화면 Viewport, 기존 하늘·바다 패럴랙스는 이번 비율 조정에서 변경하지 않았습니다.
- 참조 화면의 비율을 Inspector에 저장했으며 새 스크립트나 자동 화면 보정은 추가하지 않았습니다.

Inspector 조절 위치: Canvas/GameFrame의 BattleArea, CommandStrip 및 각 하단 패널 RectTransform. 함대 높이는 StageSpawnManager에 연결된 스폰 포인트 Transform에서 조절합니다.
