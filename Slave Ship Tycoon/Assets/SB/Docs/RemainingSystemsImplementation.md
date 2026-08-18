# Remaining Systems Implementation Guide

이 문서는 이번 작업에서 추가하거나 연결한 시스템을 다시 읽고 학습하기 위한 코드 지도다.
기준 씬은 `Assets/SB/Animations/Scenes/SampleScene.unity`이며, 아래 설명은 현재 코드와 씬 연결을 기준으로 한다.

## 1. 이번 작업 범위

구현 및 연결한 기능:

- 짝수 챕터 화물별 시세 누적
- 짝수 스테이지 실패 시 시세 상승분 차감
- 짝수 진입 시 화물 스냅샷 고정
- 다음 홀수 챕터 진입 시 판매 정산
- 판매 결과 UI 및 재화 지급
- 운송 장비 한 칸과 판매 보상 보정
- 지휘 게이지 최대치와 초당 회복량 강화
- 지휘 스킬 데이터, 런타임 상태, 수동/자동 사용 UI
- 총공세와 집중 포화 스킬
- 호위선 데이터, 카탈로그, 가챠 테이블
- 같은 등급의 미편성 호위선 3척 머지
- 2 x 3 호위선 편성
- 런타임 편성과 실제 스폰 함대 동기화
- Gold, Diamond, Emerald HUD 연결
- 함대 및 운송 장비 패널 연결

의도적으로 제외한 기능:

- 제한 시간 보스전: 직접 학습 후 구현 예정
- 저장 및 불러오기
- 오프라인 보상
- 최종 아트, 사운드, 현지화 리소스

## 2. 전체 구조 원칙

이번 구현은 다음 경계를 유지한다.

1. 정적 설정은 ScriptableObject가 가진다.
2. 플레이 도중 변하는 값은 Manager의 런타임 데이터가 가진다.
3. UI는 Manager의 내부 컬렉션을 직접 수정하지 않는다.
4. 시스템 사이 알림은 기존 `Bus<TEvent>`를 사용한다.
5. 짝수 루프 도중 변경되면 안 되는 값은 Sell 진입 시 복사본을 만든다.
6. 풀링된 함선에 남으면 안 되는 전투 효과는 풀 경계에서 초기화한다.

핵심 스냅샷은 두 개다.

- `SalesManager._sellCargoData`: 짝수 챕터 진입 당시의 화물
- `TransportEquipmentManager._sellSnapshot`: 짝수 챕터 진입 당시의 운송 장비 효과

따라서 짝수 루프 도중 화물이나 장비를 변경해도 현재 운송 결과에는 반영되지 않는다.

## 3. 전체 루프 이벤트 흐름

```text
StageBattleEndedEvent
  -> GameFlowManager.StageClear() / StageFail()
  -> OddStage...Event 또는 EvenStage...Event
  -> 다음 일반 스테이지면 다음 프레임 StartStage()
  -> 챕터 마지막이면 PlayStageClearEffectEvent
  -> 페이드 완료 후 AffterStageClearEvent
  -> 완료한 루프가 Sell이면 SellChapterCompletedEvent
  -> GameFlowManager가 Chapter 증가
  -> SellChapterStartedEvent 또는 ObtainChapterStartedEvent
  -> 다음 프레임 StartStage()
```

`GameFlowManager`가 다음 스테이지를 즉시 시작하지 않고 `QueueStartStage()`로 다음 프레임에 시작하는 이유:

- `StageBattleEndedEvent`를 여러 객체가 동기적으로 받는다.
- 이벤트 처리 도중 새 조우를 즉시 시작하면 이전 전투의 종료 콜백이 새 연출을 다시 정지시킬 수 있다.
- 한 프레임 경계를 두면 이전 전투 정리가 끝난 뒤 새 스테이지가 시작된다.

관련 파일:

- `Assets/SB/Scripts/GameFlowManager.cs`
- `Assets/SB/Scripts/Events/GameFlowEvents.cs`
- `Assets/SB/Scripts/Events/BattleEvent.cs`

핵심 함수:

- `UpdateCurrentChapterData()`: 승패와 현재 루프에 따라 이벤트를 분기한다.
- `HandleAfterStageClear()`: 챕터를 증가시키고 새 루프 시작 이벤트를 발생시킨다.
- `QueueStartStage()`: 중복 예약을 취소하고 하나의 다음 스테이지만 예약한다.
- `StartStageNextFrame()`: 이전 종료 이벤트 처리가 끝난 다음 프레임에 시작한다.

## 4. 화물 시세

관련 파일:

- `Assets/SB/Scripts/UI/Cago/MarketPriceManager.cs`
- `Assets/SB/Scripts/Events/CagoEvent.cs`

`TransportMarketPriceEntry`는 두 종류의 값을 함께 가진다.

- 이번 스테이지에서 뽑힌 값: `PriceMultiplier`, `BonusPriceMultiplier`
- 이번 운송에서 누적된 최종 값: `AccumulatedPriceMultiplier`

스테이지 보너스 계산:

```text
일반 당첨: PriceMultiplier
보너스 당첨: PriceMultiplier * BonusPriceMultiplier
누적: 이전 누적 + 이번 스테이지 배수 - 1
```

`- 1`을 하는 이유는 모든 상품이 기본 x1을 이미 가지고 있기 때문이다. 매 스테이지마다 기본 배수까지 반복해서 더하지 않고 실제 상승분만 누적한다.

핵심 함수:

- `OnSellChapterStarted()`: 이전 시세를 지우고 적재 화물 타입별 x1을 만든다.
- `OnEvenStageCleared()`: 각 화물의 새 배수를 뽑아 누적한다.
- `OnEvenStageFailed()`: 같은 스테이지에서 최초 실패했을 때만 상승분을 깎는다.
- `ApplyFailurePenalty()`: 기본 x1은 보존하고, 상승분에는 최소 한 단계 차감이 보이도록 처리한다.
- `RaiseMarketPriceChanged()`: 읽기 전용 딕셔너리를 이벤트로 방송한다.

운 수치는 `0~100` 백분율이며 다음처럼 확률로 바뀐다.

```csharp
Mathf.Clamp01(luckPercent * 0.01f)
```

## 5. 판매 정산

관련 파일:

- `Assets/SB/Scripts/UI/Cago/SalesManager.cs`
- `Assets/SB/Scripts/UI/Cago/SaleSettlementResult.cs`
- `Assets/SB/Scripts/Events/SaleEvents.cs`

판매가 현재 화물을 바로 읽지 않는 이유가 가장 중요하다.

```text
SellChapterStartedEvent
  -> 현재 CargoData를 새 CargoData[]로 깊이 복사
  -> _sellCargoData에 보관
  -> 판매 대기 상태 활성화

Sell 진행 중 CargoManager 변경
  -> 현재 편집 데이터만 변경
  -> _sellCargoData는 변경되지 않음

SellChapterCompletedEvent
  -> _sellCargoData와 마지막 시세로 정산
```

`CopyCargoData()`는 배열만 새로 만드는 것이 아니라 각 `CargoData` 객체도 새로 만든다. 배열만 복사하면 내부 `CargoData.Amount`가 같은 객체를 바라봐서 짝수 도중 변경이 판매량에 섞인다.

판매 공식:

```text
화물 보상 = 진입 시 수량 * 물품 기본 재화량 * 최종 시세 배수
```

한 화물이 여러 `CargoSalesRewardData`를 가질 수 있으므로 Gold만이 아니라 Diamond, Emerald도 동시에 지급할 수 있다.

핵심 함수:

- `OnSellChapterStarted()`: 판매 대상 화물 스냅샷 생성
- `OnSellChapterCompleted()`: 정산 성공 후에만 대기 상태를 해제하고 재화 지급
- `TryCreateSettlementResult()`: 화물별 결과와 재화별 총합 생성
- `CopyCargoData()`: 짝수 도중 변경을 막는 복사
- `HasLoadedCargo()`: 빈 운송을 판매 대기로 만들지 않음

`_hasPendingSale`은 정산 데이터가 준비되지 않았을 때 유지된다. 먼저 false로 바꾸면 이벤트 순서 문제로 한 번 실패했을 때 판매 보상이 영구 소실될 수 있기 때문이다.

정산을 `ObtainChapterStartedEvent`가 아니라 `SellChapterCompletedEvent`에서 실행하는 이유도 중요하다. 현재 마지막 데이터가 Sell 챕터일 경우 다음 Obtain 데이터가 없더라도 도착한 화물의 정산은 반드시 지급되어야 한다. 판매 완료와 다음 챕터 데이터 존재 여부를 분리하면 마지막 챕터에서도 보상이 유실되지 않는다.

### 큰 재화 수치

관련 파일:

- `Assets/SB/Scripts/Currency/CurrencyManager.cs`
- `Assets/SB/Scripts/Currency/CurrencyTextFormatter.cs`
- `Assets/SB/Scripts/Events/CurrencyEvents.cs`

보유 재화, 재화 이벤트, 판매 결과는 모두 `long`을 사용한다. 판매 배수가 커졌을 때 약 21억에서 멈추던 `int` 상한을 제거했다.

- 덧셈과 곱셈은 `long.MaxValue`에서 포화시킨다.
- HUD와 결과 UI는 K, M, B, T, Qa, Qi 단위로 축약한다.
- 시스템 내부 계산은 축약 문자열이 아니라 정확한 `long` 값을 유지한다.

## 6. 판매 결과 UI

관련 파일:

- `Assets/SB/Scripts/UI/Cago/SaleSettlementController.cs`
- `Assets/SB/Scripts/UI/Cago/SaleSettlementView.cs`
- `Assets/SB/Scripts/UI/Cago/SaleRewardSlotView.cs`

역할:

- `SalesManager`: 정산 완료 후 `SaleSettlementCompletedEvent` 발생
- `SaleSettlementController`: 이벤트를 받아 View에 전달
- `SaleSettlementView`: 챕터, 판매량, 최고 배수, 재화별 총액 표시 및 페이드
- `SaleRewardSlotView`: 자신이 담당하는 `CurrencyType` 보상만 표시

새 정산이 이전 페이드 도중 들어오면 기존 코루틴을 중지하고 새 결과로 처음부터 재생한다.

## 7. StageInfoBar 화물 스냅샷

관련 파일:

- `Assets/SB/Scripts/UI/Cago/StageInfoBarController.cs`
- `Assets/SB/Scripts/UI/Cago/StageInfoBarView.cs`
- `Assets/SB/Scripts/UI/Cago/ItemElementView.cs`

규칙:

- 홀수 진입: 화물 아이콘 제거
- 홀수 도중 적재: StageInfoBar에 표시하지 않음
- 짝수 진입: 그 순간 수량이 1 이상인 타입만 한 번 생성
- 짝수 도중 추가 적재: 아이콘과 수량을 갱신하지 않음
- 짝수 스테이지 클리어: 아이콘의 시세 배수만 갱신

이 Controller가 `ChangedCurrentCargoCapacityEvent`를 구독하지 않는 것은 의도된 구조다.

## 8. 운송 장비

관련 파일:

- `Assets/SB/Scripts/TransportEquipment/TransportEquipmentItemData.cs`
- `Assets/SB/Scripts/TransportEquipment/TransportSettlementModifier.cs`
- `Assets/SB/Scripts/TransportEquipment/TransportEquipmentSnapshot.cs`
- `Assets/SB/Scripts/TransportEquipment/TransportEquipmentManager.cs`
- `Assets/SB/Scripts/Events/TransportEquipmentEvents.cs`
- `Assets/SB/Scripts/UI/TransportEquipment/TransportEquipmentPanelController.cs`
- `Assets/SB/Scripts/UI/TransportEquipment/TransportEquipmentPanelView.cs`

현재 슬롯 수는 한 칸이다. 장비 효과는 두 종류다.

- `RewardMultiplier`: 특정 재화 총액에 배수 적용
- `FlatReward`: 특정 재화 총액에 고정값 추가

적용 순서:

```text
기본 판매 총액 -> 배수 효과 -> 고정 보상
```

`TransportEquipmentSnapshot.Create()`는 SO의 값을 런타임 값 배열로 복사한다. 따라서 짝수 진입 후 장착을 바꿔도 진행 중인 운송 효과는 바뀌지 않는다.

`TryEquip()`은 `_ownedItems`에 있는 장비만 허용한다. 현재 씬에는 기능 테스트를 위해 네 장비가 초기 보유로 등록되어 있다.

현재 테스트 데이터:

- Merchant Ledger: Gold x1.5
- Command Orders: Emerald +300
- Ship Blueprint: Diamond +5
- Mixed Manifest: Gold x1.2, Emerald +150

추후 장비 획득 시스템은 `_ownedItems`를 세이브 데이터 기반 컬렉션으로 바꾸고, 획득 시 장비 보유 변경 이벤트를 추가하면 된다.

## 9. 지휘 게이지와 강화

관련 파일:

- `Assets/SB/Scripts/Ship/Upgrade/ShipUpgradeManager.cs`
- `Assets/SB/Scripts/Ship/ShipStatCompo.cs`
- `Assets/SB/Scripts/Ship/MainShip.cs`

추가 강화 타입:

- `CommanderGaugeMax`
- `CommanderGaugeRecoveryPerSecond`

두 강화는 씬에서 Emerald를 비용으로 사용한다. `UpgradeGrowthData`가 `CurrencyType`을 직접 가지므로 강화마다 비용 재화를 다르게 설정할 수 있다.

강화 비용도 재화와 같은 `long` 경로를 사용한다.

- `UpgradeGrowthData.CurrentCost`: 현재 강화 비용
- `CalculateNextCost()`: 성장률을 적용한 뒤 올림하고, `long.MaxValue`에서 포화
- `MainShipUpgradeSlotView.Refresh()`: 비용을 `CurrencyTextFormatter`로 축약 표시

스탯 상승량인 `IncreaseValue`는 연속적인 수치이므로 `float`를 유지하고, 실제로 소비되는 비용만 `long`으로 분리했다. 씬의 기존 7개 비용 데이터도 `_currentCost` 필드로 명시적으로 이전하여 타입 변경 과정에서 0으로 초기화되지 않게 했다.

## 10. 지휘 스킬 데이터 구조

관련 파일:

- `Assets/SB/Scripts/Commander/CommanderSkillData.cs`
- `Assets/SB/Scripts/Commander/CommanderSkillDatabase.cs`
- `Assets/SB/Scripts/Commander/CommanderSkillLoadout.cs`
- `Assets/SB/Scripts/Commander/CommanderSkillRuntimeState.cs`
- `Assets/SB/Scripts/Commander/CommanderSkillEffects.cs`
- `Assets/SB/Scripts/Commander/CommanderSkillManager.cs`

책임 분리:

- `CommanderSkillData`: 이름, 아이콘, 비용, 쿨다운, 지속시간
- 파생 SkillData: 스킬 고유 수치
- `CommanderSkillDatabase`: 사용할 수 있는 모든 스킬
- `CommanderSkillLoadout`: 현재 장착 스킬 슬롯
- `CommanderSkillRuntimeState`: 남은 쿨다운과 지속시간
- `ICommanderSkillEffect`: 실제 전투 효과의 생명주기
- `CommanderSkillManager`: 게이지, 런타임 상태, 수동/자동 실행 조정

효과 인터페이스:

```csharp
bool CanActivate(BattleSpawnData battleSpawnData);
void Activate(BattleSpawnData battleSpawnData);
bool Tick(BattleSpawnData battleSpawnData);
void Deactivate(BattleSpawnData battleSpawnData);
```

새 스킬 추가 순서:

1. `CommanderSkillType` 값 추가
2. `CommanderSkillData` 파생 SO 클래스 추가
3. `ICommanderSkillEffect` 구현 추가
4. `CommanderSkillManager.CreateSkillEffect()`에 생성 분기 추가
5. Database와 Loadout SO에 등록

## 11. 구현된 지휘 스킬

### Total Offensive

- 전체 플레이어 함대의 공격 속도 증가
- `ShipCombatStatCompo`에 효과 객체를 key로 임시 퍼센트를 등록
- 종료 시 같은 key로 정확히 제거
- 현재 테스트 수치: 게이지 40, 쿨다운 10초, 지속 5초, 공격 속도 +50%

key 기반인 이유는 이후 여러 버프가 동시에 존재해도 한 효과가 다른 효과의 값을 지우지 않도록 하기 위해서다.

### Focus Fire

- 살아 있는 적 중 가장 앞 열을 찾음
- 메인 함선과 모든 생존 호위선에 해당 열을 지휘 타겟으로 지정
- 현재 열이 전멸하면 다음 살아 있는 열로 이동
- 해당 열에서 자신의 행을 먼저 찾고, 없으면 같은 열의 아무 적을 선택
- 스킬 종료 시 각 함선의 기존 일반 타겟 규칙으로 복귀
- 현재 테스트 수치: 게이지 60, 쿨다운 12초, 지속 4초

## 12. 공격 시스템과 지휘 효과 연결

관련 파일:

- `Assets/SB/Scripts/Ship/Battle/AttackCompo/ShipCombatStatCompo.cs`
- `Assets/SB/Scripts/Ship/Battle/AttackCompo/Base_ShipAttackCompo.cs`
- `Assets/SB/Scripts/Ship/Battle/TargetSelector.cs`
- `Assets/SB/Scripts/Ship/Ship.cs`

공격 속도가 바뀔 때 처리하는 값은 두 개다.

1. 남은 공격 쿨다운
2. Attack 애니메이션의 `AttackSpeedMultiplier`

`RefreshAttackSpeed(previousAttackSpeed)`는 남은 쿨다운 비율을 새 속도에 맞게 환산한다. 버프를 켰을 때 애니메이션만 빨라지고 실제 발사 주기는 그대로인 문제를 막는다.

풀링 경계 처리:

- `Ship.OnDespawnedToPool()`에서 지휘 타겟 제거
- 임시 공격 속도 Modifier 제거
- 공격 상태 초기화

이 초기화가 없으면 풀에서 다시 나온 함선이 이전 전투 버프를 유지할 수 있다.

## 13. 지휘 스킬 UI

관련 파일:

- `Assets/SB/Scripts/UI/Commander/CommanderSkillBarController.cs`
- `Assets/SB/Scripts/UI/Commander/CommanderSkillBarView.cs`
- `Assets/SB/Scripts/UI/Commander/CommanderSkillSlotView.cs`

UI는 전투 영역 우측 상단에 배치했다.

- 스킬 버튼: 수동 사용
- 숫자와 Fill: 쿨다운
- Active 표시: 지속 효과 활성 상태
- AUTO 버튼: 주기적으로 사용 가능한 스킬을 검사

자동 사용은 Support 카테고리를 먼저 보고, 사용 가능한 Support가 없으면 Attack을 시도한다.

## 14. 호위선 데이터와 가챠

관련 파일:

- `Assets/SB/Scripts/Fleet/EscortShipData.cs`
- `Assets/SB/Scripts/Fleet/EscortShipCatalog.cs`
- `Assets/SB/Scripts/Fleet/EscortGachaTable.cs`
- `Assets/SB/Scripts/Fleet/EscortGachaManager.cs`
- `Assets/SB/Scripts/Events/PlayerFleetEvents.cs`

`EscortShipData`가 가지는 값:

- 저장에 사용할 `StableId`
- 표시 이름과 아이콘
- 등급
- 실제 스폰할 `Ship` 프리팹

`EscortGachaTable`은 확률 대신 정수 weight를 가진다. 전체 weight 합 안에서 랜덤 값을 뽑아 순서대로 차감한다.

`EscortGachaManager.TrySummon()` 순서:

1. Manager, Table, 보상 데이터, 재화 검사
2. 가챠 결과 뽑기
3. 보상 지급 가능 여부 재검사
4. Diamond 차감
5. 함선 지급
6. 지급 실패 시 차감 재화 환불
7. `EscortGachaResultEvent` 발생

현재 테스트 테이블:

- Escort A: 70
- Escort B: 25
- Escort C: 5
- 1회 비용: Diamond 5

## 15. 머지

관련 핵심 클래스:

- `PlayerFleetManager`
- `EscortShipCatalog`

현재 규칙:

- 선택한 함선의 등급을 머지 등급으로 사용
- 그 등급의 미편성 함선 3척 소비
- 선택 타입의 미편성 복사본부터 소비
- 부족하면 카탈로그 순서대로 같은 등급의 다른 타입 소비
- 편성 중인 복사본은 소비하지 않음
- 한 단계 높은 등급 1척 지급
- Legendary는 머지 불가

결과 함선은 현재 카탈로그에서 처음 발견한 다음 등급 함선으로 결정된다. 이후 여러 계통이 생기면 `EscortShipData`에 merge result를 직접 두거나 별도 recipe SO를 두는 확장 지점이다.

핵심 함수:

- `CanMerge()`: 결과와 재료를 모두 검사
- `TryMerge()`: 검사 후 소비와 결과 지급을 하나의 작업으로 실행
- `GetUnequippedCount(grade)`: 등급 전체에서 최대 필요한 3개까지만 안전하게 계산
- `ConsumeMergeMaterials()`: 선택 타입 우선 소비

## 16. 2 x 3 편성과 스폰 동기화

관련 파일:

- `Assets/SB/Scripts/Fleet/PlayerFleetManager.cs`
- `Assets/SB/Scripts/Stage/PlayerFleetLoadout.cs`
- `Assets/SB/Scripts/Stage/StageSpawnManager.cs`
- `Assets/SB/Scripts/UI/Fleet/PlayerFleetPanelController.cs`
- `Assets/SB/Scripts/UI/Fleet/PlayerFleetPanelView.cs`

슬롯 상수:

```text
RowCount = 3
ColumnCount = 2
SlotCount = 6
```

`PlayerFleetManager`는 보유 수량과 편성을 관리하고, `PlayerFleetLoadout`은 StageSpawnManager가 읽는 실제 런타임 프리팹 배열이다.

```text
UI 편성 변경
  -> PlayerFleetManager._formation 변경
  -> runtime PlayerFleetLoadout.SetEscortAt()
  -> PlayerFleetLoadout.Version 증가
  -> 다음 전투에서 StageSpawnManager가 버전 차이 감지
  -> 기존 플레이어 함대 풀 반환
  -> 새 편성으로 한 번만 재스폰
```

편성이 바뀌지 않은 전투에서는 함대를 매번 새로 뽑지 않는다. 기존 함선을 체력, 상태, 위치만 초기화해 재사용한다.

## 17. 씬과 생성 데이터

씬 오브젝트:

- `Managers/CommanderSkillManager`
- `Managers/PlayerFleetSystem`
- `Managers/TransportEquipmentManager`
- `Canvas/.../CommanderSkillBar`
- Fleet 패널
- Transport Equipment 패널
- Sale Settlement UI

Commander 데이터:

- `Assets/SB/Data/Commander/CommanderSkillDatabase.asset`
- `Assets/SB/Data/Commander/CommanderSkillLoadout.asset`
- `Assets/SB/Data/Commander/TotalOffensive.asset`
- `Assets/SB/Data/Commander/FocusFire.asset`

Fleet 데이터:

- `Assets/SB/Data/PlayerFleet/EscortShipCatalog.asset`
- `Assets/SB/Data/PlayerFleet/EscortGachaTable.asset`
- `Assets/SB/Data/PlayerFleet/EscortShips/`

Transport Equipment 데이터:

- `Assets/SB/Data/TransportEquipment/`

생성 프리팹:

- `Assets/SB/Prefabs/UI/EscortInventoryItem.prefab`
- `Assets/SB/Prefabs/UI/StageCargoItem.prefab`

Escort D와 E는 현재 시스템 검증을 위해 기존 테스트 함선 프리팹을 재사용한다. 실제 함선 프리팹이 준비되면 각 `EscortShipData.ShipPrefab`만 교체하면 된다.

## 18. 현재 테스트용 초기값

- Gold: 기존 씬 값 사용
- Diamond: 100
- Emerald: 1000
- 호위선: A 7, B 4, C 4
- 초기 6슬롯 편성: A, B, C, Empty, C, C
- 운송 장비 네 종류: 테스트를 위해 모두 보유
- 초기 운송 장비: Merchant Ledger

이 값들은 밸런스가 아니라 기능 검증용이다.

## 19. 실전 검증 결과

Unity Play Mode에서 확인한 흐름:

1. Cargo 패널에서 Gold Box 5개 적재
2. 홀수 일반 전투 시작 및 자동 공격 확인
3. 지휘 게이지 초당 회복 확인
4. Total Offensive 수동 사용: 게이지 40 차감, 활성/쿨다운 표시 확인
5. Focus Fire 수동 사용: 게이지 60 차감, 활성/쿨다운 표시 확인
6. AUTO 사용: 사용 가능한 스킬 자동 발동 확인
7. 홀수 보스 버튼 진입 및 클리어 확인
8. `2-1 Sell` 진입 확인
9. 짝수 진입 시 화물 수량 스냅샷 아이콘 확인
10. 스테이지 진행에 따른 화물별 시세 배수 상승 확인
11. 짝수 챕터 완료 후 `3-x Obtain` 진입 확인
12. 판매 재화 플로팅 로그와 정산 지급 확인
13. 가챠: Diamond 100 -> 95 차감 및 호위선 1척 지급 확인
14. 머지: 미편성 같은 등급 3척 소비 및 다음 등급 1척 지급 확인
15. 편성: 비어 있던 4번 슬롯에 선택 함선 배치, 다음 전투 스폰 반영 확인
16. 운송 장비: Merchant Ledger에서 Command Orders로 교체 및 UI 갱신 확인
17. 3장 랜덤 웨이브에서 보스 레이아웃 제외 확인
18. 4장 Sell 완료 후 5장 데이터가 없어도 `CHAPTER 4 SALE` 정산 UI와 보상 지급 확인
19. `long` 재화 전환 후 HUD 축약 표시와 정산 흐름 확인
20. 체력 강화: Gold 1K -> 900, Lv.0 -> Lv.1, 다음 비용 100 -> 115 및 Console 오류 0 확인

코드 빌드 검증:

- `Assembly-CSharp.csproj`: 경고 0, 오류 0
- `Assembly-CSharp-Editor.csproj`: 경고 0, 오류 0

마지막 빌드는 프로젝트 외부 SDK 검색 경로에 대한 샌드박스 제한이 있으면 같은 명령이 실패할 수 있다. Unity Console의 컴파일 오류 여부와 권한을 허용한 빌드 결과를 함께 기준으로 삼는다.

## 20. 알려진 비기능 항목

- 한국어 챕터 진입 문구는 현재 TMP 폰트에 한글 글리프가 없어 경고가 발생한다.
- 기능 코드 오류는 아니며, 출시 전에 라이선스가 명확한 한글 TMP Font Asset을 추가해야 한다.
- 현재 신규 UI 아이콘과 D/E 함선 외형은 테스트 리소스를 재사용한다.
- 장비 획득 경로와 여러 계통 머지 recipe는 확장 지점만 마련되어 있다.
- 보스 제한 시간은 요청대로 구현하지 않았다.

## 21. 추천 코드 읽기 순서

전체를 한 번에 읽지 말고 다음 순서가 이해하기 쉽다.

1. `GameFlowManager.cs`
2. `MarketPriceManager.cs`
3. `SalesManager.cs`
4. `TransportEquipmentSnapshot.cs`
5. `TransportEquipmentManager.cs`
6. `CommanderSkillData.cs`
7. `CommanderSkillRuntimeState.cs`
8. `CommanderSkillEffects.cs`
9. `CommanderSkillManager.cs`
10. `ShipCombatStatCompo.cs`
11. `TargetSelector.cs`
12. `EscortShipData.cs`
13. `EscortGachaManager.cs`
14. `PlayerFleetManager.cs`
15. `StageSpawnManager.cs`
16. 각 UI Controller와 View

각 시스템을 읽을 때는 다음 세 질문으로 보면 된다.

1. 정적 데이터는 어디에 있는가?
2. 런타임 상태의 소유자는 누구인가?
3. 다른 시스템에는 어떤 이벤트로 결과를 알리는가?

## 22. 최종 자체 평가

점수: **9.0 / 10**

근거:

- 핵심 루프, 스냅샷, 판매, 지휘 스킬, 가챠, 머지, 편성을 실제 플레이로 연결했다.
- 런타임과 에디터 어셈블리가 모두 경고 0, 오류 0으로 빌드된다.
- 터미널 Sell 정산 유실, `int` 재화 상한, 보스의 일반 웨이브 혼입을 독립 리뷰 후 다시 수정했다.
- 강화 비용도 `long`과 포화 계산으로 옮기고 기존 씬 데이터 마이그레이션과 실제 구매를 검증했다.
- ScriptableObject 데이터와 런타임 상태, View와 Manager의 경계를 유지했다.
- 일회성 씬 생성 도구는 결과만 남기고 제거했다.

감점 요소:

- 최종 한글 TMP 폰트, 고유 아이콘, D/E 함선 아트는 아직 테스트 리소스다.
- 장비 획득처와 다계통 머지 결과 recipe는 이후 콘텐츠 데이터가 정해져야 완성할 수 있다.
- 사용자 구현 범위로 남긴 제한 시간 보스전과 저장 시스템은 포함하지 않았다.
