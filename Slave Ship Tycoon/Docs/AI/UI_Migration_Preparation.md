# UI Toolkit 이관 준비

2026-10-10에 Unity MCP와 프로젝트 소스를 조회한 준비 기록입니다. 실제 UI 교체를 시작할 때 읽습니다. 화면 디자인과 기능 구현의 완료 기록이 아닙니다.

## 확인한 환경

| 항목 | 조회 결과 |
| --- | --- |
| Unity | 6000.0.59f2 |
| 대상 씬 | `Assets/SB/Animations/Scenes/SampleScene.unity` |
| 조회 시 Editor 상태 | Play Mode, 컴파일 진행/실패 없음 |
| UI Toolkit | `com.unity.modules.uielements` 및 `UnityEngine.UIElementsModule` 사용 가능 |
| 입력 | Input System 1.14.2, `EventSystem`에 `InputSystemUIInputModule` 연결 |
| uGUI | 2.0.0 |
| 프로젝트 자체 UI Toolkit 자산 | `Assets` 아래 VisualTreeAsset 0개, StyleSheet 0개, PanelSettings 0개; 씬 UIDocument 0개 |
| 주 Canvas | `Canvas`, Screen Space Overlay, Sorting Order 0, Scale With Screen Size, 기준 1080×1920 |
| 모집 Canvas | `SummonUI`, Screen Space Overlay, Sorting Order 100, 기준 1080×1920 |
| 화물 모달 Canvas | `BossCargoLoadoutUI`, Screen Space Overlay, Sorting Order 200 |
| 함대 Canvas | `Canvas/FleetPanel`, 조회된 renderMode는 WorldSpace, Sorting Order 50, 자체 CanvasScaler 없음 |

자산 수는 `Assets` 폴더로 한정했습니다. 패키지의 Editor용 UXML/USS는 게임 UI의 기존 구현으로 계산하지 않습니다. 씬 현황은 실행 중 조회 결과이며, 변환 직전 Edit Mode에서 실제 저장 씬과 동적 생성 여부를 다시 확인해야 합니다. 중첩 Canvas의 renderMode 값만으로 함대 화면을 독립적인 월드 UI라고 확정하지 말고 루트 Canvas/카메라 연결도 확인합니다.

추가 UI 패키지 설치는 현재 준비에 필요하지 않습니다. 기존 uGUI/TMP 의존성 제거는 모든 사용처 이관을 완료한 후 별도 검토합니다.

## 화면별 교체 단위

경로는 조회 시 Hierarchy 기준입니다. C# 파일은 `Assets/SB/Scripts/UI/` 또는 `Assets/SB/Scripts/Visual/`에서 클래스명으로 찾습니다. 표의 로직은 재사용할 게임 기능이며, 현재 Controller/View가 UI Toolkit과 그대로 호환된다는 뜻은 아닙니다.

| 화면/기능 | 기존 연결 | 이관 때 보존할 데이터와 동작 |
| --- | --- | --- |
| 재화 HUD + 획득 로그 | `Canvas/GameFrame/currencyBar`, CurrencyHudPresenter / CurrencyHudSlotView / CurrencyGainLogController | CurrencyManager, CurrencyChangedEvent/ CurrencyAddedEvent, 아이콘 공급, 표시 수·생존시간·unscaled 연출 |
| 항로 정보 | `Canvas/GameFrame/BattleArea/StageInfoBar`, StageInfoBarController / View / StageProgressGraphic | StageStartedEvent, ChapterDatabase, CargoManager, 시세 이벤트와 단계 수 |
| 강화 | `Canvas/GameFrame/UpgradePanel`, MainShipUpgradePresenter / View / SlotView | ShipUpgradeManager.TryUpgrade, 레벨·비용·성공/실패, 길게 누르기 |
| 화물 패널 | `Canvas/GameFrame/CagoPanel /CargoController`, CagoController / CagoButtonView / CurrentCagoSliderView / ItemBarManager | CargoManager.LoadCargo/UnloadCargo, ChangedCurrentCargoCapacityEvent, 수량·적재량·아이콘 |
| 운송 장비 | `Canvas/GameFrame/TransportEquipmentPanel`, TransportEquipmentPanelController / View | TransportEquipmentManager.TryEquip, TransportEquipmentChangedEvent, 실제 적용 시점 문구 |
| 지휘 게이지·명령·설정 | `Canvas/GameFrame/Commander`, `CommandStrip/CommanderSkillBar`, CommanderGaugeSliderController / CommanderSkillBarController / CommanderSettingsPanel | CommanderSkillManager의 실제 게이지·충전·활성·자동 사용 상태와 슬롯 수 |
| 함대 편성·보유함 | `Canvas/FleetPanel`, PlayerFleetPanelController / View / FleetFormationSlotView / EscortInventoryItemView | PlayerFleetManager 보유/편성 snapshot과 TryPlaceEscort/TryRemoveEscort, 표적 규칙 설명, 실제 슬롯 수 |
| 모집 | `SummonUI`, ShipRecruitmentView, `Assets/SB/Prefabs/UI/Summon/SummonUI.prefab` | EscortGachaManager의 가격·확률·실제 보상, 1회/다중 모집, 건너뛰기·닫기·재모집·중복 실행 방지 |
| 합성 | `Canvas/FleetPanel/FleetMergeScreen`, FleetMergeScreen / FleetMergeResultView | PlayerFleetManager의 MergeMaterialCount, TryGetMergePreview, TryMergeSelected, MergeAllUnequipped |
| 보스 화물 적재 모달 | `Canvas/GameFrame/BossCargoLoadoutUI`, BossCargoLoadoutPanel / CargoLoadoutCardView | CargoManager, GameFlowManager, TransportEquipmentManager, 선택/빈 슬롯·용량·길게 누르기·확정/취소·일시정지 복구 |
| 판매 정산 | `Canvas/GameFrame/SaleSettlementUI`, SaleSettlementController / View / SaleRewardSlotView | SaleSettlementCompletedEvent의 실제 결과, 보상은 기존 게임 로직에서 지급, 표시 연출만 UI에서 수행 |
| 전투 결과 팝업 | `Canvas/GameFrame/BattleArea/popup_UI`, StageResultPopupController / View | 승패/챕터 이벤트, 표시 순서와 유지시간 |
| 화면 전환 페이드 | `Canvas/ScreenFadePanelController`, ScreenFadePanelControler / View | PlayStageClearEffectEvent 수신 → 페이드 종료 → AffterStageClearEvent 발생 |
| 전투 위치에 뜨는 숫자 | `Canvas/FloatingTextRoot`, FloatingTextManager / FloatingTextView | FloatingTextRequestedEvent의 월드 위치, 월드→패널 좌표 변환, 카메라/패널 해상도, 풀링·해제 |

함선의 SpriteRenderer 효과·보호막 등 게임 월드 시각 요소는 화면 UI와 구분합니다. 함선 체력바 등 추가 uGUI 사용처는 전체 이관 완료를 판정하기 전에 프리팹까지 별도로 조사해야 합니다. 위 표는 현재 씬과 화면 연결의 준비 목록이며, 프로젝트 전체 UI 의존성 제거 완료 목록이 아닙니다.

## 먼저 처리할 연결 문제

1. **재화 아이콘:** CurrencyGainLogController가 CurrencyHudPresenter.TryGetCurrencyIcon을 통해 기존 uGUI Image의 Sprite를 얻습니다. HUD를 교체하면서 Presenter를 없애면 로그 아이콘도 끊깁니다. HUD와 로그를 같은 단위로 옮기거나, 공용 아이콘 참조를 명시적으로 연결합니다.
2. **패널 표시와 일시정지:** PannelManager는 PanelView.PanelActive에서 GameObject.SetActive를 호출합니다. FleetScreenPause는 OnEnable/OnDisable에서 이전 Time.timeScale을 저장/복원합니다. UI Toolkit의 `display: none`은 이 생명주기 콜백을 호출하지 않으므로 화면 열기/닫기에서 명시적으로 일시정지를 시작/복구하는 연결이 필요합니다. BossCargoLoadoutPanel의 일시정지도 함께 확인합니다.
3. **스테이지 진행:** ScreenFadePanelControler의 페이드 완료 콜백은 실제 다음 스테이지 진행 이벤트로 이어집니다. 시각 페이드만 옮기고 이 이벤트를 누락하거나 중복 호출하면 안 됩니다. 기존 철자 `AffterStageClearEvent`는 현재 검색/연결 기준입니다.
4. **게임 규칙과 View 결합:** ShipRecruitmentView, FleetMergeScreen, BossCargoLoadoutPanel에는 상호작용 흐름도 들어 있습니다. 이름에 View가 있다고 표시 전용이라고 가정하지 않습니다. 함수별 책임을 확인한 뒤 표시 부분과 기존 Manager 호출을 연결합니다.
5. **입력 공존:** 기존 EventSystem과 InputSystemUIInputModule을 출발점으로 사용합니다. 별도 EventSystem/InputModule을 자동 생성하지 않습니다. UI Toolkit 모달에서 뒤 Canvas 클릭이 통과하지 않는지, 빈 HUD 영역이 전투 입력을 막지 않는지 확인합니다. 투명 색상만으로 입력이 통과한다고 가정하지 않습니다.
6. **연출 시간:** 함대/모달에서 Time.timeScale이 0이어도 화면이 동작해야 합니다. 기존 unscaled 연출과 취소/해제 동작을 보존합니다.

## 폰트·공통 테마 준비

- 현재 씬의 TMP 폰트는 `Assets/SB/Fonts/MalgunGothic SDF.asset`, `SummonKorean SDF.asset`, `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`입니다.
- 원본 한글 폰트 `Assets/SB/Fonts/MalgunGothic.ttf`가 존재합니다. 첫 UI Toolkit 화면을 만들 때 해당 Unity 버전의 TextCore FontAsset 호환성, PanelSettings의 TextSettings, 한글 폴백과 신규 글리프를 확인합니다. TMP 에셋의 타입을 확인하지 않고 그대로 대입하지 않습니다.
- 공통 스타일은 `Assets/SB/UI/Shared/Styles/`, 설정은 `Assets/SB/UI/Shared/Settings/`, 개별 화면은 `Assets/SB/UI/<Feature>/`를 사용합니다. 아직 자산을 생성하거나 씬에 연결하지 않았습니다.
- 색상 토큰은 `--color-surface-primary`, `--color-surface-raised`, `--color-text-primary`, `--color-action-primary`, `--color-status-danger`처럼 역할로 이름 붙입니다. 함선 등급색과 지휘 충전/준비/활성색은 의미가 다르므로 별도 역할 토큰을 사용합니다.
- `Assets/SB/Editor/HarborUiSetup.cs`에는 Navy `(0.035, 0.065, 0.105, 1)`, Surface `(0.075, 0.12, 0.18, 1)`, Teal `(0.08, 0.52, 0.54, 1)` 후보가 있습니다. 이는 초기 제작 코드의 값이며 현재 씬 Inspector 값의 확정 팔레트가 아닙니다. 실제 이관 시 화면 값과 대조한 후 공용 USS에 옮깁니다. Setup 도구를 다시 실행해 값을 덮어쓰지 않습니다.
- 공통 토큰·스타일·설정은 첫 화면에서 필요한 만큼 만듭니다. 색상/폰트/간격을 바꾸는 곳을 한 파일 또는 명시적 자산에서 찾을 수 있게 합니다.

## 권장 진행 순서

1. 첫 단위는 **재화 HUD + 획득 로그**로 잡습니다. PanelSettings/TextSettings, 1080×1920 기준 스케일, 한글, 공통 USS, 이벤트 갱신과 입력 통과를 이 작은 단위에서 확립합니다.
2. 강화·운송 장비·항로 정보로 같은 구조를 확장합니다. 기존 Manager를 재사용하고 화면 표시/클릭 연결을 옮깁니다.
3. 화물과 지휘 UI를 옮기고 하단 메뉴/패널 열기·닫기를 연결합니다.
4. 함대·합성·모집·보스 화물 모달은 관련 데이터, 결과 화면, 일시정지를 묶어 이관합니다.
5. 판매 정산·전투 결과·페이드를 옮긴 후, 월드 위치 숫자와 함선 프리팹의 UI를 별도 단위로 처리합니다.
6. 전체 씬/프리팹/코드 사용처 조사를 다시 수행하고 기존 UI와 TMP 참조를 제거할 수 있는 범위를 확정합니다.

이 순서는 기술적 의존성에 따른 제안입니다. 각 화면의 새 디자인, 정식 팔레트, 모바일 Safe Area 대응 방식은 아직 확정하지 않았습니다. UI Toolkit의 패널 정렬값을 기존 Canvas Sorting Order와 일대일 대응한다고 가정하지 말고 실제 공존 상태를 확인합니다.

각 단위에서 새 화면 연결·실제 데이터/입력/닫기 확인을 마친 뒤 해당 기존 화면을 전환합니다. 처음부터 기존 Canvas 전체를 비활성화하거나 View가 붙은 부모를 삭제하지 않습니다.

## 기존 확인 사항과 검증 범위

- Console에는 준비 시작 전에 발생한 `MerchantLedger needs an icon.` 오류가 있습니다. 운송 장비 이관 시 자산 참조를 별도로 확인해야 하며, UI Toolkit 변경으로 생긴 오류로 분류하지 않습니다.
- 실행 중 `SummonUI` 하나의 GameObject에서 활성화된 ShipRecruitmentView 컴포넌트 3개가 조회됐습니다. 모집 화면 이관 전에 Edit Mode에서 저장된 중복인지 동적 추가인지, 리스너와 지급 호출이 중복되는지 확인해야 합니다. 준비 작업에서는 삭제/수정하지 않았습니다.
- 이번 준비는 Editor 상태·자산 유무·Canvas/입력/폰트·화면 연결과 관련 소스의 읽기 전용 조사입니다. Play Mode를 새로 시작하거나 중지하지 않았습니다. 신규 UI 화면의 렌더링, 클릭, 폰트와 해상도 동작 검증은 아직 수행하지 않았습니다.
