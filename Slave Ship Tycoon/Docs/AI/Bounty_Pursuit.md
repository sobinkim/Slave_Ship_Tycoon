# 현상금 추적 프로토타입

2026-10-22 구현. 목표 함선의 명칭은 **메인 해적선**으로 통일합니다. 사용자는 이동·도주 시간을 고정하고 기존 배치·함선별 자동 전투 규칙을 활용하는 방향을 확정했습니다.

## 진입과 진행

- SampleScene의 기존 `InGameToolkitUI/InGameScreen`에 있는 `bountyButton`(현상금 추격 이미지 버튼) → `BountyPursuitToolkitScreen` → `BountyPursuitController.EnterDefault()`.
- 일반 파밍 중 진입합니다. 보스·운송·챕터 전환 중에는 진입하지 않습니다. 중단해 둔 운송 매니페스트는 변경하지 않습니다.
- 현재 전투의 투사체·명령·크리쳐 조우를 종료하고, 기존 StageSpawnManager와 StageEncounterDirector로 함대를 생성·진입시킵니다. 전투 시작 이벤트 이후 도주 시간을 계산합니다.
- 메인 해적선 격침 시 호위선 생존 여부와 관계없이 성공합니다. 목적지 도착 또는 기존 본함 침몰 처리 시 실패합니다. 철수도 가능합니다.
- 도주 시간은 두 편성 모두 60초입니다. 게임 일시정지와 함께 멈추며 별도 이동 속도 성장 수치는 없습니다.
- 결과 화면은 재도전, 편성 변경, 다른 호위 편성, 파밍 복귀를 제공합니다. 편성 변경에는 기존 함대 화면을 사용합니다.
- 성공 보상은 기존 다이아를 사용합니다. 실패 보상은 없습니다. 재도전마다 별도 전투이며, 한 전투 결과의 보상은 한 번만 지급합니다.
- 파밍 복귀 시 원래 챕터와 운송 상태를 유지하고 일반 전투를 재개합니다. 진입 전 플레이어 함대의 체력 비율을 슬롯별로 저장해 복귀 후 복원합니다.

## 연결과 데이터

- 씬 루트 `BountyPursuit`: Controller, NightEnvironment, PursuitCameraPose, NightBackground.
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

## 다음 수정 기준: 플레이테스트 피드백

아래는 현재 구현 완료 항목이 아니라, 다음 수정에서 반영할 사용자 피드백입니다.

- 추적 HUD의 프로그레스바를 기존 게임 UI의 색·테두리·질감과 맞춥니다. 지금 바는 기존 화면과 스타일 차이가 큽니다.
- 추적 중 조작 버튼은 **철수** 하나만 둡니다. 철수하면 별도 결과 선택을 요구하지 않고 추적을 시작했던 해역으로 복귀시킵니다. 결과 화면의 `다시 추적`, `편성 변경`, `다른 호위 편성 추적` 버튼은 제거합니다.
- 현재 전장에 함대가 빠르게 전진하는 연출을 더해 도주 함대를 쫓는 감각을 분명히 합니다. 기존 자동 전투·고정 도주 시간 규칙은 유지합니다.
- 함선 아래 물보라·항적 연출의 청록색이 밤바다와 어긋나므로 색을 다시 맞춥니다. 첨부 참고 이미지의 해면과 항적 대비를 기준으로 확인합니다.
- 새로 만든 적 함선이 기존 함대보다 작아 보입니다. 현재 플레이어/적 함선 프리팹의 화면상 크기를 직접 비교해 새 추적 함선들의 Transform·스프라이트 비율을 재조정하고, 역할별 크기 차이도 일관되게 맞춥니다.
