# 보스 도전 전 화물 적재

## 화면

시안의 5열 화물 카드와 6열·2행 선택 슬롯을 적용했습니다. 함대 배치 UI의 남색 표면, 얇은 테두리, 일반 굵기 한글 폰트, 청록색 버튼을 사용합니다. 화물 종류별 큰 아이콘과 테두리 색상을 표시하고, 빈 선택 슬롯은 점선과 `+`로 표시합니다.

보유 화물 목록과 화물 선택 창은 세로 ScrollRect입니다. 설정된 화물 종류가 늘어나면 카드가 추가되고 ContentSizeFitter가 목록 높이를 확장합니다. 선택 슬롯 영역도 종류가 12개를 넘으면 스크롤됩니다. 제목과 하단 버튼은 고정입니다.

적재창의 Canvas 정렬 순서는 200으로 지정했습니다. 함대 창(50)과 모집 창(100)보다 앞에서 화면을 덮고 입력을 받습니다.

메인 화면과 적재창의 재화 바는 남색 배경, 청록색 테두리·이름, 밝은 재화 수치로 통일했습니다.

## 화물 설정

기존의 용량 안에서 무제한 적재 규칙을 유지합니다. 보유 수량은 `보유 ∞`로 표시합니다. 중량과 보상은 새로 요청한 시안의 종류별 값으로 맞췄습니다. 본선의 실제 초기 적재 용량은 기존 300t입니다.

| 화물 | 개당 중량 | 골드 | 다이아 | 에메랄드 |
| --- | --- | --- | --- | --- |
| 금괴 화물 | 5t | 20,480,200 | 1,120 | 90 |
| 다이아 화물 | 6t | 28,320,500 | 1,840 | 120 |
| 에메랄드 화물 | 4t | 16,204,300 | 960 | 240 |
| 선박 자재 | 5t | 24,930,100 | 1,400 | 160 |
| 기타 화물 | 5t | 18,420,700 | 1,050 | 110 |

`TransportItemSO`에서 `Display Name`, `Accent Color`, `Icon`, `Base Weight`, `Rewards`를 수정할 수 있습니다. 화물 목록과 표시 순서는 `Managers/Cago/CargoManager`의 Cargo Data 배열에서 지정합니다. 새로운 종류를 추가할 때는 enum의 새 값과 해당 SO를 배열에 등록합니다. 지정한 카드 프리팹을 사용해 목록을 채우므로 별도의 씬 카드 배치는 필요하지 않습니다.

기존 저장 수량과의 호환을 위해 `Gold=0`, `ShipElement=1`을 유지했습니다. 새 값은 `Diamond=2`, `Emerald=3`, `Other=4`입니다. `Loadout Persistence`는 PlayerPrefs, `Loadout Save Key`는 `CargoLoadout.v1`이며 수량 변경 시 저장합니다. 저장된 수량을 자동으로 줄이지 않고, 중량 한도를 넘으면 직접 감소·제거해야 도전할 수 있습니다.

## 기능과 연결

- 하단 메뉴는 강화·함대·모집 3개입니다.
- `BossButton.onClick → BossCargoLoadoutPanel.Open`으로 전체 화면 적재 UI를 엽니다. 전투 시간은 화면이 열린 동안 멈추고 닫거나 도전하면 이전 속도로 돌아갑니다.
- `적재`와 선택 카드의 `+`는 누르는 즉시 1개를 추가합니다. 꾹 누르면 0.25초 뒤부터 0.06초 간격으로 1개씩 반복 적재합니다. 실제 시간을 사용하므로 적재창의 전투 일시정지 중에도 작동합니다. 손을 떼거나 버튼 밖으로 이동·스크롤·화면 비활성화·용량 도달 시 멈춥니다. `−`는 1개 감소, `×`는 종류 전체 제거입니다.
- 빈 슬롯의 `+`는 화물 선택 창을 엽니다. 선택 창에서는 버튼을 누르는 동안 적재하고, 클릭을 완료하면 창을 닫습니다. 키보드·게임패드 Submit은 1개를 적재합니다.
- `적재완료/보스 도전 → ConfirmBossChallenge → GameFlowManager.StartBossBattle`로 보스전을 시작합니다.
- 보스 패배·화면 닫기·운송 정산 후에도 선택 설정을 유지합니다. 보스 승리 후 운송 해역 진입 때 실제 본선 화물 목록을 별도로 복사합니다.
- 운송 진입은 `PrepareTransportCargo → TransportCargoChangedEvent → SellChapterStartedEvent` 순서입니다. 시각화와 판매/시세 시스템은 실제 운송 목록을 사용합니다.
- 예상 보상은 기본 시세 x1과 현재 장비 효과를 적용합니다. 실제 판매액은 운송 중 시세에 따라 달라집니다.

## Inspector에서 UI 조절

| 대상 | 설정 |
| --- | --- |
| `Canvas/GameFrame/BossCargoLoadoutUI` | 카드 프리팹, Content, ScrollRect, 텍스트·그래프·버튼 참조, `Minimum Selected Slots`(현재 12). |
| `Panel/Loadout/SelectedScroll/Viewport/Content` | GridLayoutGroup의 열 수·크기·간격. 현재 6열, 148×264, 간격 12×22. |
| `Panel/AvailableCargo/InventoryScroll/Viewport/Content` | 현재 5열, 178×380, 간격 15×24. 종류가 늘어나면 다음 행으로 이어집니다. |
| `Assets/SB/Prefabs/UI/CargoLoadoutSelectedCard.prefab` | 선택 카드 아이콘·수량·중량·재화·증감/제거 버튼 배치. |
| `Assets/SB/Prefabs/UI/CargoLoadoutAvailableCard.prefab` | 보유 카드 배치와 적재 버튼의 색상 혼합 값. |
| 두 카드 프리팹의 적재 버튼 `HoldRepeatButton` | `Repeat Start Delay`는 반복 시작 대기 시간(0.25초), `Repeat Interval`은 화물 1개당 반복 간격(0.06초). `Button` 참조와 카드의 `Load Button Repeat` 참조가 연결되어 있습니다. |
| `Assets/SB/Prefabs/UI/CargoLoadoutEmptySlot.prefab` | 빈 슬롯 배경, `CargoSlotBorderGraphic`의 점선 길이·간격·두께. |

씬과 프리팹 참조는 모두 연결했습니다. 추가로 연결할 항목은 없습니다.

## 이번 수정 파일

- 씬: `Assets/SB/Animations/Scenes/SampleScene.unity`
- 스크립트: `BossCargoLoadoutPanel.cs`, `CargoLoadoutCardView.cs`, `HoldRepeatButton.cs`, `CargoManager.cs`, `TransportItemSO.cs`, `CurrencyHudSlotView.cs`
- 새 표시 컴포넌트: `CargoSlotBorderGraphic.cs`, `CargoWeightGraphic.cs`
- 새 UI 프리팹: 선택 카드, 보유 카드, 빈 슬롯의 3개 프리팹
- 화물 데이터: 기존 GoldBox·ShipElementBox와 새 DiamondBox·EmeraldBox·OtherCargoBox
- 그림: `CargoInventoryAtlas.png`와 5종 InventoryIcon Sprite 에셋

한 번 실행하는 Editor 작업 코드는 `QA/BossCargoCatalogSetup.cs`, `QA/BossCargoReferenceUISetup.cs`입니다. 예전 2종 화면용 `BossCargoLoadoutSceneSetup.cs`와 검증 코드는 이전 버전 기록이며, 새 화면에 다시 적용하지 않습니다.

`ApplyHoldAndHud`는 두 카드 프리팹의 반복 입력과 두 재화 바의 색상만 수정합니다. 적재 버튼의 기존 `AddCargo` 클릭 이벤트는 제거했습니다. 첫 적재는 PointerDown에서 실행하므로 손을 뗄 때 중복 추가하지 않습니다. 선택 창의 닫기 클릭 이벤트는 유지합니다. Submit 시에는 적재 후 선택 창이 닫히도록 `HoldRepeatButton`을 `Button`보다 위에 배치했습니다. 기존 `CagoButtonView`가 사용하는 미할당 `Button` 참조와 기존 반복 시간은 호환됩니다.

## 확인

- Unity 컴파일 성공.
- Play Mode에서 5종 카드, 수량·중량·예상 보상 표시, 점선 슬롯, 선택 창 연결과 한글 배치를 확인했습니다.
- 임시로 보유 카드를 두 줄로 늘려 내용 높이 784px / 화면 높이 384px로 확장되는 것과 스크롤 이동 225px를 확인한 뒤 임시 카드를 제거했습니다.
- 함대 창이 열려 있을 때 적재창이 앞에 표시되고 입력을 받는 것을 확인했습니다.
- 실제 카드 프리팹을 사용한 별도 세션에서 꾹 누르기 반복, 용량 도달 시 중지, 짧은 클릭 1개, Submit 1개를 확인했습니다. 전투 시간 0에서도 반복 입력을 확인하고 손을 뗀 뒤 추가 입력이 없는 것을 확인했습니다. 사용자 저장 수량은 변경하지 않았습니다.
- 최종 화면: `Assets/QA/BossCargoLoadout-hold.png`.
- 작업 전부터 존재한 `MerchantLedger needs an icon.` 오류는 별도 장비 아이콘 설정 문제로 남아 있습니다.
