# 현상금 추적 프로토타입

2026-10-22 구현. 목표 함선의 명칭은 **메인 해적선**으로 통일합니다. 사용자는 이동·도주 시간을 고정하고 기존 배치·함선별 자동 전투 규칙을 활용하는 방향을 확정했습니다.

## 진입과 진행

- SampleScene의 기존 `InGameToolkitUI/InGameScreen`에 있는 `bountyButton`(현상금 추격 이미지 버튼) → `BountyPursuitToolkitScreen` → `BountyPursuitController.EnterDefault()`.
- 일반 파밍 중 진입합니다. 보스·운송·챕터 전환 중에는 진입하지 않습니다. 중단해 둔 운송 매니페스트는 변경하지 않습니다.
- 현재 전투의 투사체·명령·크리쳐 조우를 종료하고, 기존 StageSpawnManager와 StageEncounterDirector로 함대를 생성·진입시킵니다. 전투 시작 이벤트 이후 도주 시간을 계산합니다.
- 메인 해적선 격침 시 호위선 생존 여부와 관계없이 성공합니다. 목적지 도착 또는 기존 본함 침몰 처리 시 실패합니다. 철수도 가능합니다.
- 도주 시간은 두 편성 모두 60초입니다. 게임 일시정지와 함께 멈추며 별도 이동 속도 성장 수치는 없습니다.
- 추적 HUD의 조작은 **철수** 하나입니다. 접근 중·교전 중 철수하면 즉시 진입 전 해역으로 복귀합니다. 별도 결과 모달·재도전·편성 변경·다른 편성 버튼은 없습니다.
- 성공·실패는 HUD에 결과를 1.8초 표시한 뒤 자동 복귀합니다. 다시 도전하려면 기존 현상금 진입 버튼을 누릅니다.
- 성공 보상은 기존 다이아를 사용합니다. 실패 보상은 없습니다. 한 전투 결과의 보상은 한 번만 지급합니다.
- 파밍 복귀 시 원래 챕터와 운송 상태를 유지하고 일반 전투를 재개합니다. 진입 전 플레이어 함대의 체력 비율을 슬롯별로 저장해 복귀 후 복원합니다.

## 연결과 데이터

- 씬 루트 `BountyPursuit`: Controller, NightEnvironment, NightBackgroundOrigin(밤 배경 타일 배치 기준), NightBackground.
- UI: `InGameToolkitUI/BountyScreen`의 별도 UIDocument. 기존 PanelSettings를 공유하며 기존 인게임 문서의 이미지 버튼에 연결합니다.
- 정의·배치: `Assets/SB/Content/Bounty/Data/`. `BasicPursuit`와 `SmokePursuit`가 각각 `BasicConvoy`, `SmokeConvoy`를 참조합니다.
- 프리팹: `Assets/SB/Content/Bounty/Prefabs/`. MainPirate, PursuitDefender, PursuitInterceptor, PursuitRaider, PursuitSmoke.
- 새 그림: `Assets/SB/Art/Sprites/Ships/Pursuit/`. 기존 그림을 참고해 내장 이미지 생성으로 제작했습니다. 오른쪽 선수·오른쪽 진행에 맞춘 돛·왼쪽 후방 사격 구조입니다.
- 전용 클립·머티리얼: `Assets/SB/Content/Bounty/Animations/`, `Materials/`.

기본 4척의 적 논리 슬롯은 행 우선 12칸 중 습격함 2, 방어함 3, 메인 해적선 4, 요격함 5입니다. 연막형은 7번에 연막함을 추가합니다. 방어함→메인 해적선→요격함이 같은 행입니다. 기존 적의 전열 x=0은 해적 도주 방향에서는 추격자에게 가까운 후미입니다.

## 기존 기능 재사용

- 공격·자동 타겟·보호·요격·연막 로직을 사용합니다. 요격함은 기존 플레이어 요격 프리팹의 구성을 복제해 Enemy로 변환하고 내부 소유자 참조를 새 Enemy에 연결했습니다.
- 일반 포격, 방어함, 습격함, 연막함은 각각 기존 적 프리팹을 복제했습니다. 일반 적 처치 보상 컴포넌트는 추적 전용 복제본에서 제거하고 완료 보상만 지급합니다.
- 원본 애니메이션의 상태 길이·이벤트·부유·반동·침몰·돛 펄럭임 곡선을 복사했습니다. 새 그림을 상태별 Sprite 곡선에 연결하고 기존 포구·장전 작살 부착물을 후방 사격 위치로 맞췄습니다. 요격함 반동 X와 Z 방향도 후방 사격에 맞췄습니다.
- 메인 해적선은 2개의 큰 붉은 돛·해골 문양·큰 선체·한글 이름표로 구분합니다. 이름표는 기존 MalgunGothic SDF와 전용 머티리얼을 사용합니다.
- 바다는 기존 `Fleet/Cartoon Sea`를 복제한 밤 머티리얼입니다. 하늘·섬도 기존 Atmospheric Background 머티리얼과 패럴랙스 구조를 복제했습니다. 주간 원본 머티리얼은 유지합니다.
- 밤 해역에서는 아군 본선·호위선과 모든 해적 함선의 선체·돛·문양에 푸른 주변광을 적용합니다. `ShipClothWaterline`의 `_EnvironmentTint`(기본 흰색)를 Renderer별 MaterialPropertyBlock으로 설정하므로 공유 머티리얼이나 주간 색상을 변경하지 않습니다. RGB만 곱하며 피격 색상·투명도·돛 펄럭임·침몰 처리는 기존 경로를 유지합니다. 체력바·포구 섬광은 대상에서 제외합니다.
- 결과 화면에서도 밤 조명을 유지합니다. 재도전 전과 파밍 복귀 전 풀 반환에 앞서 원래 주변광으로 복원하고, 환경 컴포넌트를 끌 때도 복원합니다.
- ShipWakeEffect에 일시적인 항해 표시 API를 추가해 추적 중에도 기존 항적이 이어집니다. 종료 시 해제합니다.
- 2026-10-23: 추적 해면은 `CartoonSea`의 `_TravelOffset`으로 물결 무늬 자체를 뒤로 흘립니다. 타일 이동만으로 무늬가 제자리에 남던 문제를 수정했습니다. 일반 해역은 기본값 0으로 기존 표현을 유지합니다. 시간 배율 0에서 이동도 멈춥니다.
- 추적 중에만 아군·적의 항적을 `Fleet/Pursuit Wake`로 바꿔 원본 그림의 강한 청록색을 낮추고, 차분한 회청색으로 표시합니다. 수면 접촉 그림자·잔물결에도 전용 NightWaterContact를 적용합니다. 복귀·비활성화·풀 반환 시 원래 머티리얼과 표현으로 복원합니다.
- 추적 프리팹의 Visual 크기는 메인 1.9, 방어 1.65, 요격 1.55, 습격/연막 1.6입니다. 메인은 크다는 사용자 피드백으로 2.15에서 약 12% 줄였습니다. 편집 상태의 Sprite도 추적용 그림으로 일치시키고 항적·수면 접촉 높이·메인 이름표를 맞췄습니다. 카메라는 아침 해역과 같은 위치·시야를 유지합니다.
- ShipClothWaterline의 Sprite Edge Alpha Cutoff 기본값은 0입니다. 새 그림의 전용 머티리얼만 0.45를 사용해 불필요한 반투명 후광을 제거합니다.
- 기존 본함 패배 일제사격은 Arc 무기가 있는 적만 참여하도록 보완했습니다. 요격함은 일제사격용 포격 무기가 없으므로 제외합니다.

## Inspector 조절

| 위치 | 조절 항목 |
| --- | --- |
| BasicPursuit / SmokePursuit | Escape Seconds(현재 60), Main Pirate Slot(4), Diamond Reward(25 / 35), Enemy Layout |
| 각 추적 함선 프리팹의 ShipStatCompo | Stat Container 안의 Health / AttackDamage override. 원본 공유 StatSO는 수정하지 않습니다. |
| 초기 HP | 메인 1200, 방어 350, 요격 180, 습격 180, 연막 140. 프로토타입 수치이며 확정 밸런스가 아닙니다. |
| 초기 공격력 | 메인 4, 방어 3, 습격 8. 요격·연막은 기존 전용 행동을 사용합니다. |
| BountyNightEnvironment | 기존/추적 배경, 카메라/목표 위치, 패럴랙스 레이어의 명시적 참조. Ship Night Tint: 함선 주변광 RGB(현재 0.55 / 0.67 / 0.85, 흰색이면 원래 밝기) |
| BountyNightEnvironment > Pursuit water | Sea Flow Speed 8(초당 월드 거리), Acceleration Seconds 0.45, Wake Motion Multiplier 2.8, Wake Tint 및 전용 항적/수면 머티리얼. 사용자 피드백에 따라 초기 해면 속도 10에서 20% 낮췄습니다. |
| 추적 전용 ParallaxLayer | IslandLayerRoot 1.2, SkyLayerRoot 0.144. SeaLayerRoot는 타일 대신 셰이더 물결을 이동합니다. |
| BountyNightEnvironment > Camera | 별도 참조나 덮어쓰기 없음. 아침 해역과 동일한 위치·시야를 유지합니다. |
| BountyPursuitController > Result Return Seconds | 성공·실패 자동 복귀 지연(1.8초). 철수는 지연 없이 복귀합니다. |
| NightSea / NightSky / NightIslands | 기존 셰이더의 색상·파도·포말·안개 항목 |
| 함선별 Hull 머티리얼 | 천 영역 좌표, 바람 위상, 수면과 침몰, Alpha Cutoff |

## 검증과 범위

- 컴파일 및 UI 생성 확인. 실제 540×960 Game 뷰에서 밤바다·기존 HUD·새 함선·항적·메인 해적선 표시를 확인했습니다.
- 기존 이미지 버튼에 UI Toolkit NavigationSubmit 이벤트를 보내 실제 버튼 구독 경로를 확인했습니다. 네이티브 터치 기기 검증은 수행하지 않았습니다.
- `AgentScripts/VerifyBountyPursuit.cs`의 런타임 점검: 진입, 시간 정지, 인접 보호, 메인 단독 격침 성공/잔존 호위선, 단일 보상 지급, 시간 초과 실패/보상 없음, 5척 연막 편성, 적 팀 요격, 편성 화면과 재도전, 기존 본함 침몰 실패, 파밍/배경 복귀, 기존 재화 HUD 유지 통과.
- `AgentScripts/VerifyBountyShipLighting.cs`의 런타임 점검: 아군 전체/기본 적 4척/연막 편성 5척에 밤 조명 적용, 애니메이션 중 유지, 공유 머티리얼 유지, 결과 화면 유지, 재도전 중복 적용 방지, 복귀/컴포넌트 비활성화 시 복원, 다른 PropertyBlock 값 보존 통과. 실제 화면: `Assets/Artifacts/BountyPursuit/04-Night-Ship-Lighting.png`.
- 보스 등장 방향 회귀 수정: SampleScene의 공용 EnemySpawnPoints 시작 위치를 원래 로컬 `(3.884, 0.91, 0)`으로 복원했습니다. StageEncounterDirector는 시작 위치를 캐시하지 않은 상태에서 취소해도 Transform을 기본값 `(0,0,0)`으로 덮어쓰지 않습니다. `AgentScripts/VerifyEncounterDirection.cs`에서 추적 진입/복귀 후 실제 GameFlow 보스 도전의 X=3.884→1.5 왼쪽 이동과 원래 높이 유지 통과.
- 승리·시간 초과·패배 조건 점검에는 런타임 피해와 시간 조정이 포함됩니다. QA 승리 보상은 되돌렸으며 에셋 밸런스는 테스트 과정에서 변경하지 않았습니다. 자연 전투의 장기 밸런스와 모바일 비율별 검증은 별도입니다.
- 기존 `MerchantLedger needs an icon.` 오류와 기존 보상 아이콘 누락 로그는 추적 구현과 별개입니다. Editor 재생 종료 중 SeaVoyageMotor의 기존 Transform 해제 로그도 관측했습니다.
- 설치/보정 도구는 `AgentScripts/InstallBountyPursuit.cs`입니다. 런타임 자동 탐색이나 자동 밸런싱을 추가하지 않았습니다.
- 2026-10-23 보정은 `AgentScripts/RefineBountyPursuit.cs`입니다. `VerifyBountyPursuitRefinement.cs`에서 기존 진입 버튼, 추적 버튼 1개/결과 모달 없음, 해면 속도 8, 전체 함선의 밤 항적, 확대 Sprite, 일시정지, 접근 중/교전 중 즉시 철수, 성공/시간 초과 자동 복귀, 단일 보상 지급/QA 보상 회수, 주간 항적 복원을 확인했습니다. 카메라 7.4에서 적의 최대 화면 X는 약 0.973으로 화면 안에 들어옵니다. `VerifyEncounterDirection.cs`로 추적 복귀 후 실제 GameFlow 보스 진입의 오른쪽→왼쪽 이동도 다시 확인했습니다. 현재 화면은 `Assets/Artifacts/BountyPursuit/05-Refined-Chase.png`입니다. 이전의 편성/재도전 메뉴 검증은 최초 버전의 이력이며 현재 UX에는 해당하지 않습니다.

## 2026-10-23 반영한 플레이테스트 피드백

사용자가 지정한 수정 기준을 아래와 같이 반영했습니다.

- 프로그레스바는 공용 gauge-track/gauge-fill과 회색 HUD·기존 글자 크기를 사용합니다. 추적용 금색·남색 팔레트와 큰 그림 마커를 제거했습니다.
- 철수 하나만 남기고 즉시 원래 해역으로 복귀하도록 연결했습니다. 성공·실패도 추가 선택 없이 자동 복귀합니다.
- **빠르게 추격하는 느낌**을 위해 물결 자체의 후방 흐름·섬 패럴랙스·빠른 항적을 연결했습니다. 이후 사용자가 "지금은 너무 빠르니 조금 느리게" 요청하여 초기 속도보다 20% 낮췄습니다. 도주 시간은 여전히 고정 60초이고 이동 속도 성장 수치는 없습니다.
- 청록색 항적을 채도가 낮은 밤 해면 색에 맞추고, 아군과 적에 같은 규칙을 적용했습니다.
- 기존 함선에 비해 작았던 새 적 5종을 확대하고 물보라 높이를 조정했습니다.

## 포신과 실제 발사 위치 보정

2026-10-23 사용자 지적으로 원본 함선을 단순 좌우 반전한 발사 좌표가 새 그림의 포구와 어긋난 것을 수정했습니다. `AgentScripts/AlignBountyMuzzles.cs`가 추적 프리팹 5종의 명시적인 포구 좌표를 보정합니다. 아래 픽셀은 1536×1024 원본 그림의 왼쪽 위 기준이며, Sprite Pivot/PPU로 Visual 로컬 좌표로 변환합니다.

| 함선 | 그림 포구 픽셀 X / Y | Visual 로컬 X / Y |
| --- | --- | --- |
| MainPirate | 87 / 545 | -0.44336 / -0.02148 |
| PursuitDefender | 166 / 608 | -0.39193 / -0.06250 |
| PursuitInterceptor | 124 / 537 | -0.41927 / -0.01628 |
| PursuitRaider | 192 / 605 | -0.37500 / -0.06055 |
| PursuitSmoke | 205 / 451 | -0.36654 / 0.03971 |

- `Visual/ShootPos`(공격 컴포넌트의 firePoint/firePoints)와 기존 `Visual/MuzzleVfx`의 원점을 일치시켰습니다. 풀링된 ShipMuzzleEffect도 같은 ShootPos를 사용합니다. Visual 아래에 유지해 부유·공격 반동을 함께 따릅니다.
- 머즐의 불필요한 좌우 반전 Scale을 해제하고 포신 방향으로 회전시켰습니다. 연막함은 위로 기울어진 발사관에 맞췄습니다. 습격함의 LoadedHarpoon도 왼쪽 발사관에 재배치하고 크기·방향을 맞췄습니다. 기존 장전 표시/발사 애니메이션 이벤트는 유지합니다.
- 메인 축소 당시 항적·수면 접촉 높이는 -0.57, 이름표 높이는 0.84로 조정했습니다. 이후 선체 잠김 보정에서 항적·접촉 Y를 메인 해적선의 개별 수면선에 다시 맞췄습니다. 현재 평상시 수면은 `ShipWaterContact.Surface Local Y`를 기준으로 하며, [함선 수면 접촉](../UserGuide/17_배경_패럴랙스_물살.md)을 참고합니다.
- `AlignBountyMuzzles.VerifyRuntime`에서 5종의 반동 중 좌표 유지, 실제 자연 발사 탄 10발의 포구 시작점, 5종 모두의 풀링 머즐 섬광 위치를 확인했습니다. 포구에 초록 십자를 표시한 확인 그림은 `Artifacts/BountyPursuit/07-Muzzle-Aligned.png`, 실제 전투 화면은 `Assets/Artifacts/BountyPursuit/08-Muzzles-In-Combat.png`입니다.
- 후속 영상에서 습격함의 장전탄이 오른쪽에 나타나는 문제를 확인했습니다. Prefab 위치를 덮어쓰던 `LoadedHarpoon`의 위치 곡선이 원인이었습니다. `PursuitRaider_Idle/Move/Attack/Dead.anim` 4개의 위치 곡선을 새 포신 축과 장전탄 크기로 보정했습니다. 원본 적 함선의 클립은 수정하지 않으며 원본 곡선을 기준으로 다시 계산하므로 보정 도구를 반복해도 오프셋이 누적되지 않습니다.
- 장전 전 당기는 동작은 왼쪽 포신 축을 따라 짧게 유지하고, 기존 발사 시점(약 0.167초)의 표시 해제와 공격 이벤트는 유지했습니다. 후속 런타임 점검에서 장전 표시 11프레임/해제 89프레임 모두 포신 축 이탈이 없음을 확인했습니다. 장전 순간 캡처는 `Assets/Artifacts/BountyPursuit/09-Loaded-Round-Aligned.png`, 확대 확인은 `Artifacts/BountyPursuit/10-Loaded-Round-Detail.png`입니다.

## 물살을 바다 스타일로 변경 (2026-10-23)

사용자는 물살을 함선 그림체보다 **현재 바다 디자인에 맞춰도 된다**고 지정했습니다. 넓은 흰색 포말 Sprite 반복을 얇은 청회색·청록색 곡선과 끊어진 잔물결로 바꿨습니다. 함선 원화·돛·포구·기존 함선 애니메이션은 변경하지 않습니다.

- `PursuitWake.shader`가 선수 양쪽의 좁은 물살과 뒤쪽의 독립 잔물결을 구분해서 그립니다. 바다와 같은 색 계열, 가늘어지는 끝, 부분적으로 끊긴 파봉을 사용합니다.
- 기존 흰색 `ShipWaterContactQuad`를 재사용합니다. 아군·일반 적·추적 적의 물살 프리팹 23개를 Editor로 변경했습니다. 크리쳐는 대상에서 제외했습니다. 기존 물살 크기·Transform은 보존하고 선수 물살의 Sorting Order를 -2로 내려 선체 뒤에 그립니다.
- 낮 `ShipSailingWake.mat`와 밤 `NightWake.mat`를 사용합니다. 기존 밤 Tint, 속도 배수 2.8, 도주 시간 60초, 해면 속도 8은 유지합니다. 낮 복귀 시 원래 낮 재질을 복원합니다.
- 오른쪽으로 도주하는 추적 적 5종의 Foam Travel Local Offset X를 음수로 정정하여 포말이 뒤인 왼쪽으로 흐릅니다. 일반 왼쪽 진행 적의 반전 설정은 유지합니다.
- 색·굵기·불투명도·끊김은 재질 Inspector에서 조절합니다. 연결·조절 항목은 `Docs/UserGuide/17_배경_패럴랙스_물살.md`에 기록했습니다.
- `SeaStyleShipWake.Validate`에서 23개 프리팹의 Quad·재질·방향과 셰이더 컴파일 확인. `VerifySeaStyleWake`에서 낮 이동 표시(캡처 중 교전해도 유지하도록 Sailing Override 사용), 선수/잔물결 구분, 일시정지, 자연 전투 상태에서 Override 해제 시 페이드, 재이동/재정지 확인. `VerifyBountyPursuitRefinement`에서 밤 항적과 철수/시간초과/성공 후 낮 재질 복원을 확인했습니다.
- 실제 화면: `Assets/Artifacts/BountyPursuit/11-Sea-Style-Wake-Night.png`, `12-Sea-Style-Wake-Day.png`. Editor 연결은 완료했습니다. 모바일 실기기 성능은 별도 검증입니다. 기존 MerchantLedger 아이콘 누락·재생 종료 Transform 해제 오류는 이번 변경과 별개입니다.

## 카메라 시야 통일 (2026-10-23)

사용자 요청으로 추적 진입/복귀 때 카메라 위치·회전·Orthographic Size를 바꾸는 처리를 제거했습니다. 현상금 추적의 밤 배경과 기존 바다는 그대로 전환하되, 카메라는 아침 해역과 동일하게 둡니다. 설치/보정 Editor 도구에서도 추적 카메라 참조와 7.4 시야값을 쓰지 않습니다.
