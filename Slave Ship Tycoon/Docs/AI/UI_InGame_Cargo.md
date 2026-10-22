# 인게임·적재 UI Toolkit

인게임 HUD, 강화, 지휘, 보스 진입용 적재, 정산, 스테이지 팝업/페이드를 작업할 때만 읽습니다. 2026-10-10 `SampleScene`의 활성 화면을 Toolkit으로 전환했습니다.

## 현행 화면과 책임

### 해역·보스 진입 경고 연출 (2026-10-22)

- 경고 띠 유지 시간을 보스 2초, 해적 1.5초로 늘렸습니다. 입장/퇴장 각 0.28초를 포함한 전체 길이는 약 2.6초/2.1초이며, 이동 속도와 브리딩 주기는 유지합니다. 프리셋 기본값과 SampleScene Inspector 값을 함께 반영했습니다.
- 운송 항로의 최초 `StageStartedEvent`에도 파란 띠의 `DEPARTURE / 운송 항로 진입` 공용 연출을 추가했습니다. `Transport Entry` 프리셋은 유지 1.5초(전체 약 2.1초)이며 색·문구·아이콘을 Inspector에서 조절합니다. 해적/운송 진입 이력을 별도로 기록해 항로 내부 구간 진행·동일 항로 복귀 시 반복하지 않습니다. 기존 운송 진입 팝업은 중복 표시하지 않습니다. 기존 시세 연출 후 전투를 시작하는 GameFlow 순서는 유지합니다. 런타임 핸들러에서 최초 재생/다음 구간 재생 억제와 timeScale=1을 확인했으며 별도 씬 참조 연결은 필요 없습니다.
- `InGameFeedbackToolkit`의 기존 `StageStartedEvent` 수신에서 보스는 도전마다, 일반 해적 해역은 한 플레이 세션의 Chapter별 최초 진입에만 연출합니다. 반복 웨이브·보스 패배 후 같은 해역 복귀에는 WARNING을 반복하지 않습니다. 진입 이력은 저장 데이터가 아닌 런타임 표시 이력입니다.
- `Shared/ScreenEntryTransition.cs/.uss`와 `InGameFeedback.uxml`의 공용 레이어를 사용합니다. 상단 검은 가림막과 경고 띠는 같은 요소에 붙어 함께 움직이며, 하단 검은 가림막이 성장 UI를 가립니다. DANGER는 빨강, WARNING은 노랑이며 흰 글자를 사용합니다. 경고 띠 전체가 unscaled 시간으로 알파 브리딩합니다.
- `InGameToolkitUI/InGameFeedback`에 `ScreenEntryTransition`을 연결했습니다. 5개 UIDocument와 기존 Canvas 4개를 Inspector에서 명시적으로 지정합니다. 화면 컴포넌트나 GameObject를 끄지 않고 루트 표시·입력 상태, Canvas와 GraphicRaycaster 상태를 저장/복원합니다. 같은 문서의 정산·팝업·페이드도 숨기고 투명 전투 창까지 입력을 차단합니다. 사용자 요청에 따라 연출은 Time.timeScale을 변경하지 않으며 전투가 계속 진행됩니다. 정산 및 다른 화면의 기존 일시정지가 끝날 때까지 진입 연출을 대기하는 규칙은 유지합니다.
- Inspector: `InGameFeedbackToolkit > Entry Transition Presets`의 경고 문구·진입 문구·띠 색·Icon(Texture2D)·Hold Seconds, `ScreenEntryTransition > Motion`의 Enter/Exit Seconds(각 0.28), Breathing Seconds(0.65), Minimum Band Alpha(0.42). 보스에는 기존 KrakenBossSoft 아이콘을 연결했고 해적 아이콘은 선택 연결용으로 비워 두었습니다. 아이콘이 없으면 아이콘과 여백을 함께 접습니다. 아이콘 크기는 USS 88 UI 단위로 문구 72 UI 단위의 실제 글자 높이에 맞췄습니다.
- GameFlow와 전투 이벤트의 발생 횟수는 변경하지 않았습니다. 기존 해적 해역 진입 팝업은 공용 연출이 연결된 경우 중복 표시하지 않습니다. 새 전환은 `EntryTransitionPreset`을 만들어 `ScreenEntryTransition.Play`로 재사용합니다.
- 정산 화면/카운트·기존 페이드·다른 일시정지가 진행 중이면 진입 연출을 즉시 시작하지 않고 해당 해역의 요청 하나를 대기시킵니다. 정산의 자동 닫힘 또는 닫기 버튼 처리 후 진입 연출을 재생하며, 최초 진입 이력은 실제 재생이 시작될 때 기록합니다. 기다리는 동안 해역이 바뀌면 이전 요청을 취소합니다. 정산 표시 중 대기, 자동 종료 후 재생, 닫기 버튼 후 재생을 실제 런타임 핸들러로 확인했습니다. 검증용 정산 표시 데이터는 보유 재화 지급 없이 사용하고 해제했습니다.
- 퇴장은 SmoothStep으로 시작/끝 속도를 완만하게 하고, 화면 밖 최종 위치를 한 프레임 렌더링한 뒤 입력 차단 레이어를 숨겨 마지막 이동이 잘리는 현상을 제거했습니다. 기존 UI는 퇴장 시작 시 먼저 복원해 가림막 뒤에서 드러나며, 전환 레이어와 기존 GraphicRaycaster의 입력 차단은 퇴장이 끝날 때 해제합니다. 실제 퇴장 도중 HUD/Canvas 표시 복원과 전투 창 입력 차단을 확인했습니다. 브리딩 종료 알파를 유지해 퇴장 직전 밝기 점프도 제거했습니다. 실제 프레임 샘플에서 마지막 이동이 101.6559→101.995→102%로 이어지고 숨기기 전 화면 밖 위치가 관측되는지 확인했으며 timeScale=1을 유지했습니다.
- 검증: Unity 컴파일 성공, 실제 540×960 Game 뷰에서 빨강/노랑 연출·아이콘 유무·모든 HUD 숨김 확인. 정상 종료/취소 시 표시·활성·Canvas·시간 배율의 정확한 복원, 비기본 상태(비활성 UI/Canvas 및 timeScale 0.35) 보존, 컴포넌트 비활성화 취소, 전투 창 입력 차단, 같은 해역 반복 억제와 보스 재도전 재생을 확인했습니다. 신규 자동 테스트는 추가하지 않았습니다. 캡처는 `Assets/QA/EntryTransition/01-boss.png`, `02-pirate.png`입니다.
- 전체 전투 연결 검증에는 기존 `MerchantLedger needs an icon` 및 `StageSpawnManager needs a PoolingManager to spawn ships` 설정 오류가 있습니다. 후자는 실제 GameFlow 보스 시작에서 확인했으며 이번 UI 변경에서 해당 스폰 참조는 수정하지 않았습니다. 연출은 실제 런타임 프리셋/핸들러로 별도 검증했습니다. 검증용 시간 배율·일시정지·임시 프리셋 변경은 남기지 않았으며 기본값과 명시적 참조를 씬에 저장했습니다.

| 씬 경로 | 담당 파일 | 연결 |
| --- | --- | --- |
| `InGameToolkitUI/InGameScreen` | `Assets/SB/UI/InGame/InGameScreen.uxml`, `.uss`, `InGameToolkitScreen.cs` | 재화/획득 로그, 항로/운송 진행·시세, 보스 진입, 하단 메뉴 |
| 같은 GameObject | `UpgradeToolkitPanel.cs` | `ShipUpgradeManager.TryUpgrade`, 함대 정비, 연속 강화 |
| 같은 GameObject | `CommanderToolkitController.cs` | `CommanderSkillManager`의 게이지·스킬 사용/자동 사용·장착/해제 |
| `InGameToolkitUI/CargoScreen` | `Assets/SB/UI/Cargo/CargoScreen.uxml`, `.uss`, `CargoCard.uxml`, `CargoToolkitScreen.cs` | `CargoManager` 적재/하역/종류 비우기, 용량·수익, 화물 선택, 보스 도전 |
| `InGameToolkitUI/InGameFeedback` | `InGameFeedback.uxml`, `.uss`, `InGameFeedbackToolkit.cs` | 판매 정산, 승패/해역 진입 문구, 전환 페이드 |

전투 함선·배경·탄환 및 함선 HP/피해 표시의 월드 렌더링은 기존 구조를 사용합니다. 이전의 사용하지 않는 `CagoPanel `, `TransportEquipmentPanel`과 기존 UI 프리팹은 보관되어 있습니다.

## 실제 연결

- 메인 화면은 기존 `CurrencyManager`, `CargoManager`, `GameFlowManager`, `ChapterDatabase`, `PannelManager`와 새 적재/강화/모집 컴포넌트를 Inspector에서 참조합니다. 런타임 자동 탐색을 하지 않습니다.
- 새 하단 메뉴가 `PannelManager.ClickPanelButton`을 직접 호출합니다. `PanelChanged`가 강화 영역의 표시와 모집 닫기를 연결합니다. 기존 `PannelManager`는 `Canvas/GameFrame/PannelButtons`에 남아 있으며 자식 uGUI 버튼은 비활성입니다.
- `UpgradePanelButton.PanelView._panel`은 비워 두었습니다. Toolkit 강화/스킬 영역은 GameObject 활성 전환 대신 `PanelChanged`로 표시합니다. `PanelType.CommanderSkills`를 enum 끝에 추가해 기존 직렬화 값을 유지했습니다. 이 화면은 같은 UIDocument의 내부 영역이므로 기존 PanelInfo에 별도 GameObject를 등록하지 않습니다. `FleetPanelButton.PanelView._panel`은 여전히 `FleetToolkitUI/FleetScreen`입니다.
- 보스 도전 버튼 → `CargoToolkitScreen.Open()` → 적재 확인 → `GameFlowManager.StartBossBattle()`입니다. 진입 가능 여부와 적재 유효성은 기존 매니저가 판정합니다.
- 적재 버튼은 누를 때 1개 적재하고 길게 누르면 반복합니다. 강화는 행 오른쪽의 강화 버튼에서만 실행하며 짧게 눌렀다 놓으면 1회, 길게 누르면 반복합니다. `Shared/ToolkitHoldRepeatInput.cs`가 두 입력을 처리하며 스크롤 이동/영역 이탈/비활성화 시 반복을 취소합니다.
- `ChangedCurrentCargoCapacityEvent` 후 적재 UI는 LateUpdate에서 갱신합니다. 카드 전체를 다시 만들지 않아 길게 누르는 중에도 같은 버튼이 유지됩니다.
- 적재는 이전 `Time.timeScale`을 저장하여 0으로 만들고 닫힐 때 복원합니다. UI 반복·획득 로그·정산은 unscaled 시간을 사용합니다. 스테이지 팝업/전환 페이드는 기존과 같이 scaled 시간을 사용합니다.
- 정산은 운송 완료 시 `SaleSettlementCompletedEvent` 결과를 자동으로 표시합니다. 하단의 지난 정산 버튼은 제거했습니다.
- 전환 페이드가 끝나면 `AffterStageClearEvent`를 **한 번** 발생시켜 기존 GameFlow를 진행시킵니다. 기존 `ScreenFadePanelControler`를 함께 활성화하면 중복 진행되므로 비활성 상태를 유지합니다.

## Inspector와 USS 조절

| 대상 | 조절할 값 |
| --- | --- |
| `InGameToolkitScreen` | 모든 Manager/화면 참조, Currency Slots의 종류·커스텀 Sprite·틴트·툴팁, 항로 문구, 로그 최대 표시 수·생존·페이드·생성 간격 |
| `UpgradeToolkitPanel` | Currency Manager(비용 충족 여부 표시), Upgrade Rows의 종류/이름/순서, 반복 시작 지연/간격, 드래그 취소 거리(패널 좌표), 피드백 시간 |
| `CommanderToolkitController` | Document, Manager. 목록은 Manager.AvailableSkills를 통해 기존 Skill Database에서 읽음. 실제 장착 슬롯 수는 기존 Manager의 Loadout에서 결정 |
| `CargoToolkitScreen` | Manager/카드 템플릿 및 HUD와 같은 Gold/Diamond/Emerald Sprite 참조, 반복 지연/간격·드래그 취소 거리 |
| 같은 컴포넌트 / Expected Revenue Feedback | Reward Count Duration Seconds(0.5), Reward Delta Duration Seconds(0.85), Reward Delta Rise Pixels(10) |
| `InGameFeedbackToolkit` | 정산 재화 목록, 정산 페이드/카운트/행 간격/유지/진입 크기, 팝업 속도·유지, 전환 페이드 시간 |
| `InGameScreen.uss` | 재화 바(64px), Lower Panel Background(top 51%), Command Area(240px), 스킬 버튼(128px), Bottom Nav(132px), 스킬 슬롯/4열 목록 |
| `CargoScreen.uss` / `CargoCard.uxml` | 세로 목록의 각 행에서 아이콘/이름/단위 중량, −/수량/+, 재화 아이콘 보상을 표시. 목록만 스크롤하고 요약과 출항 버튼은 고정 |
| `HarborTheme.uss` | 기존 역할별 색상과 `--color-surface-overlay/command/log` 토큰 |

가격·강화 증가량·화물 보상·적재 무게는 UI 코드에서 정하지 않습니다. 해당 Manager/TransportItemSO를 편집합니다. 기존 숫자와 설정을 재사용했으며 새 밸런스 규칙은 추가하지 않았습니다.

## 적재 예상 수익 피드백 (2026-10-12)

- 예상 기본 수익 영역은 화면 제목 아래, 적재함 위에 배치합니다.
- 적재함은 슬롯 내용에 맞춰 높이를 사용하고, 아래 남던 여유 공간은 화물 목록에 배분합니다. 빈 + 슬롯은 유지합니다.

- 적재 화면 상단의 준비 안내와 보유 재화 줄은 사용자 요청으로 제거했습니다. 상세창에서 해당 화물이 0개가 되면 상세창을 닫고 반복 입력도 취소합니다.
- 상세창은 비우기 −1 / 적재 +1 두 버튼을 사용합니다. 별도 전체 비우기 버튼은 제거했습니다. 둘 다 짧게 누르면 1개, 길게 누르면 반복 시작 지연(0.35초) 이후 반복 간격(0.1초)마다 1개씩 처리합니다. 예상 수익 카운트가 이어지며 0개에서 상세창이 자동으로 닫힙니다.

- `CargoScreen.uxml` / `.uss`의 기존 한 줄 예상 수익을 재화별 3개 영역으로 나누고, 숫자를 36px 굵은 글씨로 확대했습니다. 기본 수익 및 운송 장비 효과 포함이라는 의미는 유지합니다.
- `CargoToolkitScreen.cs`에서 수익 변경 시 현재 표시값부터 목표까지 0.5초 동안 감속하며 카운트합니다. 연속 적재 중에는 현재 표시 숫자부터 새 목표로 이어집니다. 화면을 처음 열 때는 기존 총액을 즉시 표시합니다.
- 변경된 재화에만 `+증가분` 또는 감소분을 표시하고 테두리를 강조합니다. 변화량은 0.85초 동안 조금 떠오르며 사라집니다. 모든 시간은 unscaled 기준이며, 수익 계산과 실제 적재 로직은 기존 것을 사용합니다.
- Unity C# 컴파일 및 새 UXML 요소 로드를 확인했습니다. 실제 적재 애니메이션, 다른 화면 비율의 시각 확인은 수행하지 않았습니다. 별도 씬 참조 연결은 필요 없습니다.

PanelSettings는 공용 TextCore 폰트/테마와 1080×1920 너비 기준 스케일을 공유합니다. 정렬은 `InGamePanelSettings` 100 → 기존 함대/모집 150 → `CargoPanelSettings` 300 → `FeedbackPanelSettings` 500입니다.

## 재화바·지휘 스킬·적재 디자인과 UX (2026-10-10 최신)

- 재화바는 64px 높이의 한 줄이며 그림 아이콘과 보유량만 표시합니다. 이름/용도와 정확한 보유량은 툴팁에서 확인합니다. 백만 미만은 쉼표로 구분한 수치, 그 이상은 기존 축약 표기를 사용합니다.
- `Assets/SB/Art/Sprites/UI/`의 Gold, Diamond, Emerald를 SampleScene의 Currency Slots.Icon에 연결했습니다. 기존 함선의 굵은 외곽선과 색감을 참고해 생성한 투명 이미지입니다. USS로 만든 동전/보석 도형은 제거했습니다. 아이콘 교체/틴트는 기존 Inspector 필드에서 조절합니다.
- 지휘 스킬은 하단 전투 경계에 128×128 아이콘을 가로로 배치합니다. 자동 사용 버튼은 아이콘 줄의 오른쪽 위에 있습니다. 장착 슬롯이 많아지면 가로 스크롤할 수 있습니다. 버튼에는 지휘 비용·재사용/사용 중 남은 시간·진행 막대를 표시하고, 아래에는 사용 가능/부족/대기/대상 없음/미장착 상태를 표시합니다. 이름은 툴팁과 스킬 탭에서 확인합니다.
- 하단 메뉴는 강화 / 스킬 / 함대 / 모집입니다. 스킬 탭에서 장착 슬롯 선택 → 목록의 스킬 선택 → 장착 버튼 순서로 편집합니다. 선택 슬롯 해제도 이 탭에서 처리합니다. 목록은 4열로 감싸며 세로 스크롤합니다. 중복 장착 방지와 재사용 대기 유지는 기존 Manager 규칙을 사용합니다.
- 2026-10-11 사용자 요청으로 총공세/일제 사격의 생성 그림 아이콘 연결을 제거했습니다. 아이콘이 없는 스킬은 이름으로 표시하며, 미장착 슬롯은 +로 표시합니다.
- 강화 카드는 이름/Lv, 현재→다음 수치, 비용/강화 버튼을 구분합니다. CurrencyChangedEvent와 UpgradeEvent로 상태를 갱신하며 재화 부족 시 강화 버튼을 비활성화하고 이유를 표시합니다. 카드 본문은 강화 입력을 받지 않습니다.
- 적재 슬롯은 상세 카드와 분리했습니다. 빈 칸과 적재된 칸 모두 4열·동일한 140px 높이를 유지합니다. 적재 칸에는 그림/이름/수량을 표시하고, 누르면 상세 팝업에서 무게·수익 확인, −1, 적재 +1(길게 누르기), 해당 화물 비우기를 실행합니다. 기존 두 열 카탈로그와 빈 칸의 선택 팝업도 유지합니다.
- 하단 배경은 화면 양 끝까지 불투명합니다. UI 변경으로 가격·효과·스킬 수·초기 슬롯 수를 바꾸지 않았습니다.
- C# 컴파일 및 UXML/USS 임포트 경고 없음. 1080×1920에서 재화바 높이 64, 아이콘 줄/AUTO 위치, 강화↔스킬 탭 전환, 스킬 목록 첫 행, 적재 상세를 확인했습니다. 실제 적재된 칸과 빈 칸은 모두 230×140이었습니다. 실기기 터치/다른 화면 비율과 실제 비용 차감은 별도 확인 항목입니다.
- 화면: `Artifacts/UI/HudCompact.png`, `SkillsTab.png`, `CargoCompact.png`, `CargoDetail.png`. 이번 변경 전 백업: `AgentScripts/Backups/HudSkillsCompact-20261010/`.

### 스킬 종류와 슬롯 추가

1. 기존 `CommanderSkillManager._skillDatabase`가 참조하는 `CommanderSkillDatabase.Skills`에 데이터를 추가합니다. 별도의 UI Available Skills 배열은 제거했으므로 UI 목록을 이중으로 등록하지 않습니다.
2. 새 효과 종류는 기존 CommanderSkillType, 데이터 클래스와 Manager.CreateSkillEffect의 효과 연결을 함께 구현해야 실제 사용할 수 있습니다. 데이터 등록만으로 새 전투 효과가 생기지는 않습니다.
3. 이름/설명/아이콘/비용/지속/재사용 시간은 해당 CommanderSkillData에서 편집합니다. 초기 슬롯 수와 초기 장착은 기존 CommanderSkillLoadout.EquippedSkills에서 편집합니다. 현재 슬롯은 2개이며 수량 제한을 UI에 하드코딩하지 않았습니다.
4. 장착 편집은 현재 플레이에 즉시 적용됩니다. 기존 Manager와 같이 재실행 시 초기 Loadout을 사용하며, 이번 UI 변경에 별도 저장 기능은 추가하지 않았습니다.

아이콘 생성 방식/프롬프트 기록: `Artifacts/UI/PirateHudIcons.md`.

## 하단 배경과 발사 오류 수정 기록 (2026-10-10)

- 좌우 24px 바깥 여백을 제거하고 하단 불투명 배경을 추가했습니다. 이후 위 디자인 변경에서 `lowerPanelBackground`를 공통 Flex 컨테이너로 확장했습니다.
- `ArcProjectileAttackCompo needs a projectile prefab.` 발생 당시 저장된 프리팹의 탄환 참조는 정상이었지만 Editor가 로드한 함선 에셋에서는 null이었습니다. 관련 함선 프리팹을 의존성 순서로 재임포트하여 복구했습니다. 전투 스크립트나 프리팹의 저장된 연결은 변경하지 않았습니다.
- 재임포트 후 아군/적 함선 프리팹 18개의 탄환 참조를 확인했습니다. Play Mode에서 포탄/작살 비행과 체력 감소, 지휘 게이지 증가를 확인했고 발사 참조 오류는 재발하지 않았습니다. Play Mode 종료 뒤에도 참조가 유지됩니다.
- 수정 화면: 프로젝트 루트 `Artifacts/UI/InGame-FullWidth.png`.

## 보관 및 확인 범위

기존 `BottomUIBackground`, `currencyBar`, `CurrencyLogRoot`, `UpgradePanel`, `Commander`, `CommandStrip`, `BossCargoLoadoutUI`, `SaleSettlementUI`, `StageInfoBar`, 결과 팝업과 `ScreenFadePanelController`는 비활성화했습니다. 결과 팝업의 기존 이벤트 Controller도 껐습니다. UI Setup 도구로 이전 화면을 재활성화하거나 연결을 덮지 않도록 주의합니다.

- C# 컴파일 성공, UXML/USS 임포트 경고 없음. 1080×1920 HUD/지휘 설정/적재 화면을 렌더링해 한글·카드·아이콘·배치를 살폈습니다.
- 자동 테스트는 추가하거나 실행하지 않았습니다. 강화 차감, 전체 편성 해제, 보스 출항·실제 운송/정산, 스킬 사용, 반복 입력의 실기기 터치와 Safe Area/다른 화면 비율은 별도 동작 확인이 필요합니다.
- UI 변경 전부터 관측된 `MerchantLedger needs an icon.`은 장비 데이터의 아이콘 누락으로 남아 있습니다. 탄환 참조 오류는 위 재임포트로 복구했습니다.
- 화면 캡처: 프로젝트 루트 `Artifacts/UI/`. 씬 백업: `AgentScripts/Backups/SampleScene.before-hud-cargo-toolkit.unity.backup`.
- `AgentScripts/InstallInGameCargoToolkit.cs`는 일회성 연결 작업 기록입니다. 이미 전환한 씬에 재실행하지 않습니다.

## 운송 시세 보너스 HUD (2026-10-12)

- `InGameToolkitScreen.Transport.cs`는 기존 `InGameToolkitScreen`의 partial 파일로 운송 화물 표시와 시세 피드백을 담당합니다. 별도 화면이나 매니저는 추가하지 않았습니다.
- 운송 중 항로 카드 안에 현재 적재 화물을 병렬로 표시합니다. 타일은 88×88px 정사각형이며, 각 화물의 기존 `TransportItemSO.Icon`, 이름, 적재 수량을 사용합니다. 진행 수치는 제목 줄 오른쪽으로 옮겼고, 기존 항로 상세 안내는 운송 중 숨깁니다.
- 배율은 `MarketPriceManager`의 실제 화물별 독립 정수 누적 배율을 표시합니다. 시안의 고정 예시 배율을 게임 수치로 사용하지 않습니다. 예상 획득 재화는 Gold·Diamond·Emerald를 나누어 표시하고 `TransportEquipmentManager.ApplySnapshotModifiers`의 운송 장비 효과를 포함합니다. 시세·화물 보상·장비 수익 계산은 기존 로직을 사용합니다.
- 화물 아이콘에서 보너스 숫자가 튀어나온 뒤 진동과 halo 강조로 결과를 표시합니다. 이후 기존 재화 아이콘이 해당 예상 수익 칸으로 날아가고 숫자가 증가합니다. 별도 완료·상태 문구는 표시하지 않습니다.
- 시세 이벤트의 스냅샷은 FIFO 큐에서 순서대로 연출합니다. Obtain 시작 및 컴포넌트 비활성화 시 코루틴·대기 스냅샷·비행 아이콘을 정리합니다.
- `InGameFeedbackToolkit`의 StageTransition은 명시적으로 연결한 `inGameScreen.IsTransportBonusPlaying`이 끝나기를 기다립니다. 마지막 시세 연출을 다음 전환이 덮지 않도록 기존 전환 흐름에 대기를 추가했습니다.
- `SampleScene`에서 HUD의 `marketPriceManager`, `transportEquipmentManager`와 피드백의 `inGameScreen` 참조를 연결했습니다.

| Inspector 항목 | 기본값 |
| --- | --- |
| Cargo Acquire Seconds | 0.16초 |
| Cargo Suspense Seconds | 0.48초 |
| Cargo Stagger Seconds | 0.08초 |
| Cargo Resolve Seconds | 0.38초 |
| Currency Flight Seconds | 0.28초 |
| Transport Count Seconds | 0.42초 |

모든 운송 피드백 시간은 unscaled 기준입니다. 정사각형 크기와 배치·색상은 `InGameScreen.uss`의 transport 클래스에서 조절합니다.

- C# 컴파일 및 UXML/USS 임포트에서 새 오류·경고가 없음을 확인했습니다. 기존 `MerchantLedger needs an icon.`은 남아 있습니다.
- 실제 운송 진입 화면에서 기존 화물 아이콘·정사각형 배치·재화별 예상 수익을 확인했습니다. 복사한 시세 데이터로 UI 연출을 수동 확인한 뒤 실제 시세 표시와 원래 시간 배율로 복구했습니다. 자동 테스트 및 보스 진입부터 운송·정산까지의 전체 흐름 검증은 수행하지 않았습니다. 전체 운송 결과별 연출과 다른 화면 비율은 별도 확인 항목입니다.

### 운송 HUD 슬림화와 타격 연출 (2026-10-12)

- 정사각형 화물 타일은 88×88px, 기존 아이콘은 68×68px로 줄이고 중앙에 모았습니다. 이름과 수량은 한 줄입니다. 항로 제목/진행/세 재화 수익은 유지합니다.
- 획득 시 squash/stretch → 진동 강화 → 0.09초 정지 → 배율 확대와 섬광/확산 링/파편 → 재화 연속 흡수 순서로 연출합니다. 증가 없는 결과는 중립, 감소는 실패 색상을 사용합니다.
- 일반 상승은 재화당 3개, 실제 추가 보너스 상승은 재화당 5개가 0.045초 간격으로 날아옵니다. 첫 실제 도착에서 화물 배율과 정확한 예상 수익 target을 한 번 갱신하며, 후속 도착은 아이콘/숫자 punch와 확산 링만 재생합니다.
- 시세 큐, 실제 배율, 장비 snapshot 보정과 정산 금액 계산을 유지합니다. 마지막 stream과 count/impact가 끝날 때까지 기존 정산 전환이 기다립니다. FX와 transform은 종료 및 비활성화 시 정리합니다.
- 추가 Inspector: Cargo Freeze Seconds 0.09, Cargo Impact Strength 1, Currency Stream Count 3, Bonus Currency Stream Count 5, Currency Stream Interval Seconds 0.045, Reward Impact Seconds 0.22. SampleScene의 기존 시간값도 함께 저장했습니다.
- 컴파일 성공과 USS 임포트 경고 없음. 실제 운송의 화물 4개/카드 높이 238px, 시세 상승·재화 stream·수익 표시를 관찰했습니다. 전체 운송의 모든 결과/다른 기기 비율과 터치 검증은 별도 항목입니다. Play Mode 종료 중 Unity 내부 transform assertion이 잠시 기록되었으나 재실행 후 운송 연출 중에는 재발하지 않았습니다. 기존 MerchantLedger 아이콘 누락 오류는 유지됩니다.

### 운송 배경 제거·현재 구간·운 보너스 반짝임 (2026-10-12)

- 운송 중 큰 route-card 배경을 투명하게 바꿨습니다. 현재 구간은 상단 우측 `현재 N / M 구간`의 30px 굵은 금색 배지로 표시합니다. 화물 타일과 재화 칸은 유지하며 제목/화물 이름/수량/예상 재화 제목은 외곽선으로 바다 배경 위 가독성을 보강했습니다.
- 기존 실제 보너스 조건(HasBonusMultiplier && target > acquired)에서 해당 화물만 금색으로 강조하고, Painter2D의 흰 중심/금색 네 갈래 별 7개가 순차적으로 반짝입니다. 추가 보너스가 없는 일반 상승 및 실패에는 별을 재생하지 않습니다.
- 별은 화물 anchor 아래 캐시하고 transform/opacity만 변경합니다. 기존 transportFx.Clear와 별개로 다음 시세에서 재사용합니다. 별과 금색 강조는 1.05초 동안 유지되며 마지막 별까지 정산 전환이 기다립니다. Obtain/비활성화 시 기존 parent 정리로 함께 해제됩니다.
- Inspector의 Lucky Upgrade Sparkle에서 Bonus Sparkle Seconds(1.05초), Bonus Sparkle Count(7개)를 조절합니다. 기존 시세/화물 보상/장비 snapshot 계산은 유지합니다.
- C# 컴파일 및 USS 임포트 오류/경고 없음. 실제 운송에서 큰 배경 alpha0, 카드 높이240px, 현재 구간 배지30px/261px폭을 확인했습니다. 기존 MerchantLedger 아이콘 누락 외 새 런타임 오류는 관측하지 않았습니다. 자동 테스트와 다른 기기 비율 검증은 수행하지 않았습니다.

- 실제 운송 4-1 클리어의 보너스 상승에서 다이아/에메랄드/선박 자재의 금색 배지와 흰색·금색 별빛을 화면 캡처로 확인했습니다. 연출 종료 후 다음 구간과 일반 정착 상태로 진행하며 새 런타임 오류는 없었습니다.


### 화물별 순차 공개·상승률·세로 빌드 (2026-10-13)

- 화물을 한 개씩 강조합니다. 선택되지 않은 화물은 opacity 0.35로 낮추고, 현재 화물의 실제 기본 배율 획득 → 진동 → 잠깐 정지 → 결과 공개 → 재화 stream → 예상 수익 반영을 마친 뒤 다음 화물을 표시합니다. 배율이 바뀌지 않은 화물은 연출을 생략합니다.
- 일시적인 `transportReveal`은 화물 이름과 `시세`, 해당 화물의 기존 대비 시세 상승률, 배율 before→after, 재화별 전체 예상 수익 before→after 및 증가분을 표시합니다. 평상시에는 숨깁니다. 변화가 있는 재화 행만 표시하고 내용에 따라 높이가 달라집니다.
- 상승률은 `(target / before - 1) × 100`입니다. 전체 예상 수익의 퍼센트로 표기하지 않습니다. 전/후 금액은 같은 운송 장비 snapshot 보정을 포함해 계산하며, 단일 화물 override 인자를 사용하므로 계산 중 실제 표시 배율을 임시 변경하지 않습니다.
- 기존 큰 항로 배경은 계속 투명합니다. 제목/화물 이름/수량/예상 재화 제목은 작은 어두운 칩 위에 솔리드 글자를 사용합니다. 현재 구간 배지는 유지합니다.
- `GameFlowManager.transportPresentation`을 기존 InGameScreen에 명시적으로 연결했습니다. 중간 구간의 다음 전투도 현재 화물 연출 완료를 기다리고, 기존 마지막 정산 대기는 유지합니다. 기존 운송 클리어 팝업 표시 구독만 제거해 새 공개 연출과 겹치지 않게 했습니다. 진행/정산 이벤트 및 실제 수익 계산은 유지합니다.
- Inspector 저장값: Acquire 0.14초, Suspense 0.48초, Bonus Suspense 0.78초, Freeze 0.16초(긴장 구간 안에 포함), Resolve 0.28초, Flight 0.24초, Count 0.36초, Sequence Gap 0.12초. 기존 3/5개 stream, 7개 별빛과 1.05초 보너스 반짝임을 유지합니다.
- Windows64 실행 빌드는 540×960 Windowed, Native Resolution false, Resizable Window false, Allow Fullscreen Switch false입니다. UI PanelSettings의 1080×1920과 같은 9:16 비율로 실행합니다. 기존 Android 회전 설정은 변경하지 않았습니다.

- C# 컴파일 성공 및 UXML/USS 임포트 확인. 1080×1920 화면에서 복사한 시세 데이터로 4개 화물의 순차 강조, 상승률/배율, 재화별 전체 예상액과 증가분, 보너스 별빛을 수동 확인했습니다. 원본 MarketPriceManager 데이터는 변경하지 않았고, 실제 표시와 Time.timeScale=1로 복구한 뒤 Play Mode를 종료했습니다. 기존 MerchantLedger 아이콘 누락 외 새 UI 런타임 오류는 관측하지 않았습니다. 자동 테스트와 전체 전투→운송→정산 검증은 수행하지 않았습니다.

- Windows Player 시작 시 기존 저장 해상도도 9:16 Windowed로 덮어씁니다. 최대 540×960이며 작은 모니터에서는 9×16 정수 단위로 축소해 같은 비율을 유지합니다. Editor 및 모바일 빌드에는 이 초기화가 적용되지 않습니다.

- Windows64 Player 빌드 성공(32.1초, 오류 0개). Pipeline Runtime 설정 없음 경고 1개는 Editor 연동용 Pipeline이 Player에서 비활성화된다는 안내입니다. 세로 창 초기화가 Player RuntimeInitialize 목록에 포함된 것을 확인했습니다. 전체 실행 폴더 177개 파일을 Downloads/SlaveShipTycoon-20261013-TransportUI로 이동했고 outputs/SlaveShipTycoon-20261013-TransportUI.zip도 보관했습니다. EXE 단독 실행 또는 실제 PC 창의 수동 검증은 수행하지 않았습니다.

### 운송 루프·동시 시세 공개·상시 축소 HUD (2026-10-13)

이 항목은 위의 화물별 순차 공개와 큰 운송 HUD 설명을 대체하는 최신 구현입니다. 목적은 출항 화물의 가치 상승, 마지막 봉쇄선 돌파, 지급된 수익을 통한 성장, 막혔을 때 정비 후 재개를 연결하는 것입니다.

- `InGameToolkitScreen.Transport.cs`는 한 구간의 화물 시세를 동시에 공개합니다. 최신 일반 결과 총시간은 1.6초, 특별 결과는 2.2초로 기존 0.9/1.6초를 대체합니다. 화물 수만큼 대기 시간이 늘어나지 않습니다. 특별 연출은 목표 재화 증가액이 기본 화물 가치의 30배 이상일 때 후보가 되며 운송당 최대 1회입니다. 이는 표시 기준이며 실제 시세 확률·보상 계산을 변경하지 않습니다.
- 일반·특별 상승 모두 0.6초 동안 링을 응축하고 아이콘을 흔든 뒤 0.1초 정지하고 공개합니다. 공개 대상 배율은 공개 전 `× ?`로 가리고 중립 문구를 사용하며 재화 결과 행을 숨깁니다. 결과가 먼저 보이지 않게 한 뒤 배율 pop·별빛과 coin stream을 재생합니다. 예상액 카운트와 목표 숫자·아이콘 punch는 첫 실제 coin stream 도착부터 시작합니다. 감소·변동 없음은 화물을 확대하지 않습니다.
- 화물 타일의 상태별 사각 배경과 사각 flash VisualElement를 제거했습니다. 시세 공개 영역도 투명하게 두고 외곽선이 있는 글자와 링·별빛으로 강조합니다. 기존 화물 그림 에셋은 그대로 사용합니다.
- 운송 HUD는 항상 작은 형태를 유지합니다. 1080×1920 기준 평상시 항로 영역 높이는 실측 126px입니다. 화물 타일은 평상시 56×56px이며 시세 공개 중에만 92×92px로 커집니다. 공개 종료 시 0.18초에 걸쳐 축소합니다. 화물 그림은 50×50px에서 82×82px로 함께 바뀝니다.
- 상단 운송 영역에는 화물과 목표 재화의 `도착 예상` 금액을 중심으로 표시합니다. 중복된 세 재화 예상액, 상세 문구, 기본·시세·장비의 상시 설명 줄은 숨깁니다. 기존 수익 계산과 정산 내역은 유지하며 재화 stream은 화면에 보이는 목표 재화 아이콘으로 향합니다. 미지급 예상액을 보유 재화로 표현하지 않습니다.
- 목표 재화는 출항 시 운송 장비 snapshot의 정산 효과가 한 통화이면 그 통화로 고정하고, 미장착·효과 없음·여러 통화 효과이면 Inspector의 `Transport Goal Currency`를 사용합니다. 실제 적재·시세·지급 통화는 바꾸지 않습니다. `TryGetTransportValueBreakdown`은 동일 snapshot으로 기본 화물 가치, 누적 시세 보너스, 운송 장비 기여를 구분합니다.
- 마지막 구간의 항로 제목은 `마지막 봉쇄선`입니다. 마지막 시세 공개가 끝난 뒤 기존 전환·정산 이벤트를 진행합니다. `AffterStageClearEvent`의 기존 한 번 발생 경로를 유지합니다.
- 정산은 `운송 완주`와 하역 완료를 표시하고 목표 재화의 최종 지급액을 크게 강조합니다. 기존 세 재화 카운트, 지급 완료 강조, `함대 강화`·`호위선 모집` 버튼을 제공합니다. 목표와 분해 내역은 `SaleSettlementCompletedEvent`에서 저장하며 이후 운송 UI 초기화·지난 정산 재생에 따라 바뀌지 않습니다. 분해 합계가 실제 지급액과 다르면 해당 내역을 숨깁니다. 카드 높이는 최신 조정에서 74%이며 내용은 스크롤하고 성장 버튼은 하단에 둡니다. 자동 진행·자동 닫기를 유지하며 UI에서 재화를 다시 지급하지 않습니다.
- `GameFlowManager.ReturnToFarming`으로 현재 운송 구간을 정박한 뒤 이전 파밍 해역에 복귀할 수 있습니다. `운송 재개`는 같은 구간으로 돌아가며 화물, 누적 시세, 출항 장비 snapshot을 유지합니다. 정박·재개 시 출항/정산 이벤트를 다시 발생시키지 않습니다. 정박 중 다른 보스 출항 및 챕터 전환 중 복귀·재개를 막습니다. 실패 문구는 기본 가치 유지와 보너스 차감의 구간당 1회 규칙을 안내합니다.
- `Chapter_02/Stage_02_01.asset`의 첫 적 편성은 6척에서 3척으로 줄였습니다. `Stage_02_05.asset`은 일반 포격함 한 척을 기존 회복 지원함으로 바꿔 마지막 봉쇄선의 역할을 구분했습니다. 해당 두 편성 외 장비·경제·시세 확률을 일괄 변경하지 않았습니다. 변경 전 데이터는 `QA/TransportLoop/Before/`에 보관합니다.

| 주요 조절 위치 | 항목과 기본값 |
| --- | --- |
| InGameToolkitScreen / Transport goal and result feedback | Transport Goal Currency: Gold, Transport Result Seconds: 1.6, Transport Special Result Seconds: 2.2, Transport Anticipation Seconds: 0.6, Transport Reveal Pause Seconds: 0.1, Transport Special Gain Base Multiple: 30, Transport Special Results Per Voyage: 1 |
| InGameFeedbackToolkit / Settlement | Settlement Growth Hold Seconds: 6, Settlement Peak Seconds: 0.28, Settlement Peak Scale: 1.06. 기존 Settlement Hold Seconds와 더 긴 유지 시간을 사용합니다. |
| InGameScreen.uss | 평상시/시세 공개 중 화물 크기 56/92px, 그림 50/82px, 크기 전환 0.18초, 중복 예상액·상세 표시 여부 |
| InGameFeedback.uss | 정산 카드 높이 74%, 내부 스크롤 및 하단 성장 버튼 |

이번 연출 후속 변경 파일은 `InGameToolkitScreen.Transport.cs`, `InGameScreen.uss`, `Assets/SB/Animations/Scenes/SampleScene.unity`입니다. 씬 UI 컴포넌트의 두 결과 총시간은 Editor의 SerializedObject로 1.6/2.2초를 저장했고 다른 Inspector 값은 유지했습니다. 원본 시장 데이터·경제·확률은 변경하지 않았습니다.

최신 연출 코드의 컴파일은 성공했습니다. 응축→정지→공개→실제 coin 도착 단계의 런타임 QA는 진행 중이며, 아래 앞선 화면·동작 기록을 최신 연출 검증 완료로 해석하지 않습니다.

앞선 검증 기록: `TransportSuspensionTests` 4개가 통과했습니다. 현재 구간 저장, 동일 구간 재개와 출항 snapshot 초기화 방지, 정박 중 중복 보스 출항 방지, 챕터 전환 중 복귀·재개 차단을 검사합니다. 실제 전체 운송→정산은 이번 작업 중 1회 확인했으며 기존 `MerchantLedger needs an icon.` 외 새 런타임 오류는 관측하지 않았습니다. 축소 HUD 캡처는 `Assets/QA/TransportLoop/04-transport-compact.png`입니다. 이전 확대·축소 연출(05-transport-bonus.png), 74% 정산 카드와 강화·모집 연결(07-settlement-growth.png), 실제 버튼으로 운송 4-2 복귀·재개와 전후 모든 배율 유지(06-transport-resumed-compact.png)를 확인했습니다. 같은 구간 실패를 두 번 발생시켰을 때 첫 실패만 보너스가 감소하고 기본 x1 이상을 보존했습니다. 다른 기기 비율·Safe Area·실기기 터치는 미검증입니다. 세부 구현·검증 상태는 `QA/TransportLoop/Implementation.md`에 기록합니다.

최신 연출 QA: 자연 운송의 공개 전 ×?·기존 예상액 유지·타일 배경 alpha 0을 확인했습니다. 복사한 UI 시세 snapshot으로 29프레임을 기록했으며 실제 시장 값은 변하지 않았습니다. 공개 표본 0.711초, 기존 예상액 유지 0.926초까지, 금액 증가 표본 0.999초, 카운트 완료 1.281초, 확대 해제 1.636초입니다. 미리보기는 Assets/QA/TransportLoop/10-anticipation-preview.gif, 상세 기록은 QA/TransportLoop/anticipation-timing.tsv입니다. 초기 QA ScreenCapture 오류는 GameView 렌더 타깃 캡처로 교체해 해결했고 게임 소스 오류와 구분합니다. QA 종료 후 Play Mode 정지·시간 배율 1 복구 완료. Canvas 미저장 상태는 보존했습니다.

### 운송 도착 예상액 접기·펼치기 (2026-10-21 최신)

- 골드 한 통화만 보여주던 상시 목표 칸을 숨기고, 화물 아래 `도착 예상` 버튼과 접이식 재화별 예상액을 사용합니다. 기존 공용 회색 패널 스타일을 유지합니다.
- 항로 카드에 마우스를 올리면 펼치고 벗어나면 접습니다. 버튼 클릭·터치로 펼침 고정/해제하며, 고정 해제 후에도 마우스가 위에 있으면 펼침을 유지합니다. 키보드 Button 동작도 유지합니다.
- 시세 결과를 확정 공개할 때 자동으로 펼치고, 연출 종료 후 `Transport Revenue Hold Seconds`(기본 1.5초)가 지나면 접습니다. 마우스나 고정 상태가 있으면 열린 상태를 유지합니다. 펼침·접힘 전환은 USS의 0.18초 max-height/opacity 전환입니다.
- 기존 Currency Slots에 연결된 골드·다이아·에메랄드의 예상액을 같은 비중으로 표시하며, 재화 stream은 해당 재화 아이콘으로 향합니다. 보상 계산·지급·확률은 변경하지 않았습니다.
- 변경 파일: InGameScreen.uxml, InGameScreen.uss, InGameToolkitScreen.cs, InGameToolkitScreen.Transport.cs. 기존 Inspector 연결은 유지하며 새 수동 연결은 필요 없습니다.
- 검증: 컴파일 성공, UXML/USS 경고 없음. UI에만 복사한 화물·시세 표시값으로 540×960 Game 뷰의 접힘/펼침 및 세 재화 표시를 확인했습니다. 이벤트 처리의 마우스 진입, 클릭 고정 후 이탈, 고정 해제, 결과 공개 자동 열림/닫힘, 컴포넌트 비활성화·재활성화 후 초기화를 확인했습니다. 실제 포인터 이동·실기기 터치 및 전체 전투→운송→정산은 이번 검증에 포함하지 않았습니다.
- 캡처: Assets/QA/TransportRevenue/Collapsed.png, Expanded.png. Play Mode를 종료했습니다. 기존 `MerchantLedger needs an icon.` 오류는 유지되며 새 런타임 오류는 관측하지 않았습니다. 작업 중 발생한 PointerType 이름 충돌 컴파일 오류는 타입을 명시해 해결했습니다. QA 도구의 이벤트 생성 실패는 핸들러 호출 방식으로 교체했습니다.

### 운송 HUD 두께·배경 투명도 조정 (2026-10-21)

- InGameScreen.uss에서 운송 패널 배경만 rgba(48,48,48,0.5)로 조정했습니다. 텍스트·아이콘의 opacity는 낮추지 않았습니다.
- 접힘 기준 헤더 32→28px, 화물 타일 56→44px(아이콘 50→42px), 화물 여백 8→4px, 수량 행 22→18px, 펼침 버튼 30→24px로 줄였습니다. 버튼은 옅은 반투명 배경과 상단 구분선만 사용합니다. 시세 공개 중 확대 크기와 상호작용은 유지합니다.
- Editor 스타일 재임포트 후 운송 카드 높이 약 118px, 배경 alpha 약 0.502, 제목 alpha 1을 확인했고 새 USS 경고는 없었습니다. 사용자가 다른 화면으로 전환 중이어서 최종 Game 뷰 시각 검증은 미완료입니다. 기존 SeaVoyageMotor/SeaCreatureSurvivalDirector 종료 시 MissingReferenceException과 MerchantLedger 아이콘 오류는 UI 스타일 수정과 별개입니다.

### 게임 HUD 기반 숫자 릴·반전 보너스 (2026-10-21 최신)

- 승인된 연출을 실제 UI Toolkit에 연결했습니다. 웹 시안의 둥근 카드·입체 버튼·별도 미리보기 조작부를 게임에 추가하지 않고, 기존 화물 아이콘·Galmuri11 UI Outlined 폰트·각진 패널을 재사용합니다. 운송 배경 alpha는 0.7, 도착 예상 토글은 우측의 작은 문구이며 수량은 `N개`로 구분합니다.
- 화물 자리에 이번 기본 배율을 표시합니다. 기본 결과와 재화별 예상액 카운트가 끝나면 궤짝으로 복귀하고, 실제 HasBonusMultiplier가 있는 화물만 정적 후 `기본 배율 × 추가 배율`로 다시 나타납니다. 숫자 트랙은 3개 Label을 재사용합니다.
- 추가 배율은 칸마다 걸림·감속·경계 버티기·되밀림/넘김·안착을 재생합니다. 결과는 실제 PriceMultiplier/BonusPriceMultiplier에 맞춥니다. 경계 움직임의 변주는 별도 System.Random을 사용해 경제 추첨용 Unity Random을 소모하지 않습니다. 여러 보너스 화물은 동시에 재생하며 다음 진행은 기존 IsTransportBonusPlaying 경로가 기다립니다.
- 예상액은 기본 결과와 추가 결과에서 각각 기존 값부터 새 값까지 카운트하며 재화별 증가분을 잠깐 유지합니다. 접힘·마우스 호버·클릭/터치 고정 기능은 유지합니다. 운송 실패로 누적 배율이 줄어들면 기존 감소 표시 경로를 사용합니다.
- 사용자 확정 규칙 반영: 추가 배율 추첨 범위를 0~9로 변경했습니다. `누적 += Max(0, 이번 배율 - 1)`로 0이 나와도 원금·이전 누적을 지킵니다. 추가 배율 1은 기존 기본 배율, 2 이상은 곱셈입니다. 기존 기본값 1을 제외하는 누적 공식과 보너스 발생 확률(운 수치)은 유지합니다. 미지급 예상액만 연출하며 UI에서 재화를 지급하지 않습니다.
- Inspector / Transport reels: Base Spin Seconds 2.6, Reel Stagger Seconds 0.18, Bonus Quiet Seconds 0.95, Bonus Creep Seconds 0.55, Reel Count Seconds 1.1, Reel Result Hold Seconds 0.65, Detent Time Scale 1.0. 기존 Transport Revenue Hold Seconds는 자동 펼침 종료 유지 시간입니다. 새 수동 참조 연결은 없습니다. 구형 일반/특별 결과 시간은 기존 감소 표시 경로에만 남아 있습니다.
- 변경: InGameToolkitScreen.Reels.cs(신규), InGameToolkitScreen.Transport.cs, InGameScreen.uss, MarketPriceManager.cs, TransportBonusMultiplierTests.cs(신규). UXML·씬·프리팹 저장 연결은 이번 변경에서 수정하지 않았습니다.
- 검증: 컴파일 성공, USS 경고 없음. Transport 관련 Editor 테스트 6개(0/1/큰 보너스 누적 보존 2개, 기존 정박/재개 4개) 통과. 복사한 UI 화물/시세 snapshot으로 540×960 Game 뷰에서 숫자 표시 및 보너스 연출·궤짝 복귀를 확인했습니다. Gold 추가 ×0→누적1, Diamond 추가 ×1→누적5, Emerald 추가 ×7→누적35를 확인했으며 실제 시장/보유 재화/운송 snapshot은 변경하지 않았습니다. 캡처는 Assets/QA/TransportReels/02-reels.png, 03-restored.png입니다.
- 전체 실전 운송→정산·다른 해상도·실기기 터치는 이번 검증에 포함하지 않았습니다. 기존 MerchantLedger 아이콘 오류와 Pipeline 요청 연결 오류는 UI 변경과 별개입니다. 최종 Console 컴파일 오류/USS 경고는 0개이며 Play Mode를 종료하고 일시정지를 해제했습니다.

### 보너스 발생 빈도 조정 (2026-10-21)

- 화물별 독립 추첨은 유지하고, 실제 확률을 `운(%) × Bonus Chance Scale`로 낮췄습니다. MarketPriceManager Inspector의 새 계수 기본값은 0.1입니다. 운 50 기준 화물당 50%→5%, 화물 4종 중 하나 이상 발생할 확률은 구간당 93.75%→18.55%입니다. 운 100은 화물당 10%입니다.
- 운 강화 값과 추가 배율 0~9, 0일 때 원금·이전 누적 보존 규칙은 유지합니다. 위 숫자 릴 항목의 보너스 확률 유지 설명보다 이 항목을 우선합니다. 누적 보존 테스트는 추첨 계수를 명시적으로 1로 설정해 모든 보너스 결과를 계속 검사합니다.
- 검증: 컴파일 성공, 실제 씬 컴포넌트의 계수 0.1 및 운 0/50/100의 확률 0/5/10% 확인. Transport Editor 테스트 6개 통과. 씬·프리팹의 별도 연결은 필요 없으며 실제 플레이 빈도 체감은 추가 확인 대상입니다.

### 두 줄 운송 HUD (2026-10-22 최신, 시안 승인 반영)

- 평상시는 헤더 한 줄과 화물 한 줄입니다. `도착 예상` 버튼을 헤더로 옮기고, 각 화물은 기존 궤짝·누적 배율·수량을 가로로 배치합니다. 별도 수량 줄·긴 화물명·예상 정산 설명 줄을 제거했습니다. 화물 전체 이름과 수량은 툴팁으로 유지합니다.
- 540×960 Game 뷰 실측 접힘 높이는 약 44.6px, 예상 보상 펼침은 약 64.9px입니다. 보너스 회전·확정 동안에도 화물 행 높이가 유지됩니다. 보스 버튼과 상단 위치를 맞추며, 배경 alpha 0.7과 기존 Galmuri11 UI Outlined 폰트를 사용합니다.
- 기본 릴은 궤짝 오른쪽에서 회전합니다. 추가 릴은 작은 기본값과 곱셈 기호, 큰 추가 숫자로 구분하고, 확정 시 `×35`처럼 이번 곱셈 결과 하나로 합칩니다. 종료 후에는 실제 누적 배율로 돌아갑니다. 작은 릴 창과 코드의 행 높이는 모두 44 UI 단위입니다. 실제 확률·보상 지급·릴 타이밍은 이번 디자인 변경에서 수정하지 않았습니다.
- 마우스 호버·클릭/터치 고정·자동 펼침을 유지합니다. 고정 중에는 헤더 버튼 색상과 툴팁으로 표시하고 텍스트 길이를 고정합니다. 재화 예상액은 아래 한 줄로만 펼치며, 카운트는 유지하고 추가 높이를 요구하는 증가분 별도 줄은 숨겼습니다.
- 변경: InGameScreen.uxml/uss, InGameToolkitScreen.cs, InGameToolkitScreen.Transport.cs, InGameToolkitScreen.Reels.cs. 새 Inspector 필드나 수동 참조 연결은 없습니다. 기존 Transport reels 및 Transport Revenue Hold Seconds 조절 항목을 유지합니다. 씬·프리팹을 저장하거나 기존 Inspector 값을 덮어쓰지 않았습니다.
- 검증: C# 컴파일 성공, 새 UXML/USS 경고 없음. 실제 Game 뷰에서 접힘·펼침·추가 숫자 회전·곱셈 결과·궤짝/누적 배율 복귀를 확인했습니다. 복사한 UI용 화물/시세로 실제 공개 코루틴을 재생했으며 실제 적재·시장·보유 재화는 변경하지 않았습니다. 접힘/호버 상태 전환과 클릭 고정 후 이탈/고정 해제 상태를 확인했습니다. 호버는 상태 입력을 재현한 검사이며 네이티브 포인터 및 실기기 터치·전체 운송→정산은 미검증입니다. 별도 자동 테스트는 추가하지 않았습니다.
- 캡처: Assets/QA/TransportThin/01-collapsed.png, 02-expanded.png, 03-bonus-spin.png, 04-bonus-result.png, 05-reward-count.png, 06-final-collapsed.png. 기존 MerchantLedger 아이콘 오류 외 새 게임 오류는 관측하지 않았습니다. 검증 도구의 using/이벤트 생성 오류는 소스 컴파일 오류와 구분하며, 조회를 완전한 타입명으로 수정하고 호버 상태 검사로 대체했습니다. UI 미리보기 해제·Time.timeScale=1·일시정지 해제 후 Play Mode를 종료했습니다.

### 강화 목록과 지휘력 간격 (2026-10-22)

- `함대 정비` 화면에서 본선 체력·공격력·사격 속도 3개 행이 온전히 보이도록 `.upgrade-row` 높이를 152 UI px로 조절했습니다. 다음 항목의 일부가 화면 아래에 비치지 않습니다.
- 지휘력 제목/게이지 수치 행은 좌우 16 UI px, 위아래 4~6 UI px 안쪽 여백을 두고, 44 UI px 제목 행과 12 UI px 게이지 사이에 4 UI px 간격을 둡니다. 전투 자원 계산과 강화 동작은 바꾸지 않았습니다.
- 540×960 실제 Game 뷰에서 3개 행, 지휘력 행과 게이지를 확인했습니다. 캡처: `Assets/QA/RecruitmentPolish/Upgrade-540.png`.

### 화물 적재 최종 디자인 (2026-10-22)

이 절은 위의 4열 빈 슬롯·두 열 카탈로그·선택/상세 팝업 배치 기록을 대체합니다.

- 승인받은 시안을 실제 CargoScreen에 적용했습니다. 중복 슬롯과 선택/상세 팝업을 제거하고 5종 화물을 하나의 세로 목록에서 조절합니다. 상단에는 적재 중량과 세 재화 예상 수익, 하단에는 남은 공간과 보스 도전 버튼을 고정합니다.
- 화물 1개당 보상은 각 행 아래 `재화 아이콘 + 금액`으로 모읍니다. 칩은 왼쪽부터 내용 너비만 사용하고 간격은 24 UI px입니다. HUD에 연결된 Gold/Diamond/Emerald Sprite를 CargoToolkitScreen의 명시적 Inspector 필드에도 연결했습니다. 연결이 없는 다른 씬에는 재화명 대체 표시가 있습니다.
- 공용 HarborKorean 폰트, #303030 배경, #505050 사각 버튼과 공용 hover/disabled 스타일을 사용합니다. Cargo의 배치·간격은 CargoScreen.uss 안에서만 바꿨습니다. 100만 이상은 공용 K/M/B 포맷을 사용하며 정확한 금액은 툴팁에 표시합니다.
- 기존 적재/하역 Manager, 장비 효과를 포함한 수익 계산, 수익 카운트 연출, 길게 누르기, 드래그 취소, 화면 열기/닫기의 시간 배율 복원을 유지합니다. 적재 0개를 허용하는 기존 보스 도전 규칙도 유지합니다. 경제·확률·아이템 데이터는 바꾸지 않았습니다.
- 변경 파일: Assets/SB/UI/Cargo/CargoScreen.uxml, CargoScreen.uss, CargoCard.uxml, CargoToolkitScreen.cs, Assets/SB/Animations/Scenes/SampleScene.unity(3개 Sprite 참조), 본 문서.
- Inspector: 기존 Repeat Start Delay 0.25초, Repeat Interval 0.06초, Drag Cancel Distance 12, 수익 Count 0.5초/Delta 0.85초/Rise 10 값을 유지했습니다. Minimum Selected Slots는 새 목록에서 사용하지 않아 제거했습니다.
- 검증: 컴파일 성공, UXML/USS 경고 없음. 실제 540×960 화면과 360×640 캡처에서 겹침/잘림 없이 5종 목록·고정 하단 버튼을 확인했습니다. 저장하지 않는 SessionOnly 런타임 복사 데이터로 버튼 1회 입력, 40t 한도, 0개 하역 방지, 길게 누르기, 재열기 후 중복 입력 방지, 닫기 후 timeScale 1 복원을 확인했습니다. 금괴 8개의 예상 수익 163,841,600/8,960/720과 UI 목표값이 일치했습니다. 기기 터치와 전체 보스 전투는 이번 확인에 포함하지 않았습니다.
- 캡처: Assets/QA/CargoRedesign/Final.png(실제 저장된 적재 상태), Capacity-full-360.png(임시 QA, 카운트 연출 도중). 기존 MerchantLedger needs an icon. 오류는 남아 있으며 이번 Cargo UI의 새 런타임 오류는 관측하지 않았습니다.
