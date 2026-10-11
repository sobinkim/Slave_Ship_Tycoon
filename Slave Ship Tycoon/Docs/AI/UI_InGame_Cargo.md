# 인게임·적재 UI Toolkit

인게임 HUD, 강화, 지휘, 보스 진입용 적재, 정산, 스테이지 팝업/페이드를 작업할 때만 읽습니다. 2026-10-10 `SampleScene`의 활성 화면을 Toolkit으로 전환했습니다.

## 현행 화면과 책임

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
| `CargoToolkitScreen` | Manager/카드 템플릿 참조, Minimum Selected Slots(빈 칸을 포함한 최소 표시 개수), 반복 지연/간격·드래그 취소 거리 |
| `InGameFeedbackToolkit` | 정산 재화 목록, 정산 페이드/카운트/행 간격/유지/진입 크기, 팝업 속도·유지, 전환 페이드 시간 |
| `InGameScreen.uss` | 재화 바(64px), Lower Panel Background(top 51%), Command Area(240px), 스킬 버튼(128px), Bottom Nav(132px), 스킬 슬롯/4열 목록 |
| `CargoScreen.uss` / `CargoCard.uxml` | 적재 슬롯과 빈 칸은 23% 너비·140px 높이로 동일. 카탈로그는 두 열 카드, 적재 상세는 별도 팝업 |
| `HarborTheme.uss` | 기존 역할별 색상과 `--color-surface-overlay/command/log` 토큰 |

가격·강화 증가량·화물 보상·적재 무게는 UI 코드에서 정하지 않습니다. 해당 Manager/TransportItemSO를 편집합니다. 기존 숫자와 설정을 재사용했으며 새 밸런스 규칙은 추가하지 않았습니다.

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
