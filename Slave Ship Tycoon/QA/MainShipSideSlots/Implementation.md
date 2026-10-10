# 메인 함선 옆 배치 슬롯

## 적용 결과

호위선 6칸을 8칸으로 확장했다. 기존 배열 인덱스 0~5의 함선과 실제 배치 위치는 유지한다. 추가 인덱스 6/7은 메인 함선 위/아래 후열이다.

| 행 | 후열 | 중열 | 전열 |
| --- | --- | --- | --- |
| 위 | 슬롯 7 | 슬롯 1 | 슬롯 2 |
| 중앙 | 메인 함선 고정 | 슬롯 3 | 슬롯 4 |
| 아래 | 슬롯 8 | 슬롯 5 | 슬롯 6 |

진형 좌표 x는 후열 0, 중열 1, 전열 2이다. 메인 함선은 (0, 1), 신규 슬롯은 (0, 0)/(0, 2)이다. 8칸 배열은 중앙의 메인 함선을 제외하므로 `GetEscortSlotCoordinates` / `GetEscortSlotIndex`로 변환한다. 기존 2열 산술식으로 배열을 변환하지 않는다.

## 변경 파일

- `Assets/SB/Scripts/Stage/PlayerFleetLoadout.cs`: 8칸 및 명시적인 배열/진형 좌표 매핑.
- `Assets/SB/Scripts/Stage/StageSpawnManager.cs`: 새 좌표 매핑으로 메인 함선과 호위선 생성.
- `Assets/SB/Scripts/Ship/Battle/TargetSelector.cs`: 아군 중열 판정 추가. 적 진형 판정은 기존 유지.
- `Assets/SB/Scripts/UI/Fleet/FleetAttackPreview.cs`: 실제 진형 위치 표시, 회복선의 현재 무작위 타겟 규칙 설명.
- `Assets/SB/Editor/StageSpawnManagerEditor.cs`: 메인 함선 고정 칸을 포함한 3열 배치 지점 Inspector.
- `Assets/SB/Data/PlayerFleet/TestPlayerFleetLoadout.asset`: 빈 슬롯 2개 추가.
- `Assets/SB/Animations/Scenes/SampleScene.unity`: 새 배치 지점 2개, 초기 편성/배치 UI 참조 8칸, 후열·중열·전열 UI, 고정 메인 함선 카드.

## Inspector 조절

- `StageSpawnManager > Escort Spawn Points`: 7/8번 지점에 새 Transform 연결 완료.
- `RearTopSpawnPoint` / `RearBottomSpawnPoint`: 실제 전투 위치. 현재 월드 좌표 (-1.94, 2.55, 0), (-1.94, 0.15, 0).
- `PlayerFleetManager > Initial Formation`: 기본 편성 8칸, 새 두 칸은 비어 있음.
- `FleetPanel > FormationRoot > GridLayoutGroup`: 칸 크기 192×176, 간격 24×20, 3열.
- `PlayerFleetPanelView > Formation Slots`: 기존 6개 + 신규 2개 명시적 연결 완료. 씬의 시각적 순서와 배열 인덱스는 위 표에 따른다.

## 확인

- Unity Editor 컴파일 완료, compilationFailed=false.
- 기존 초기 편성 6칸의 참조 및 배치 지점 유지 확인.
- 실제 보유 함선으로 신규 두 칸 배치 성공, 런타임 함대 생성 및 좌표 확인.
- Play Mode 배치 화면과 전투 배치 캡처 확인: `FormationUI.png`, `FormationUIEquipped.png`, `BattlePositions.png`.
- 확인용 임시 편성을 제거하고 저장된 기본 편성으로 복원.
- 기존 `MerchantLedger needs an icon.` 오류는 요청 범위 밖이므로 남아 있다.
- 추가 수동 연결 항목 없음.

## 회복선의 회복 대상 제외

`TargetSelector.excludesHealingSupportShips`를 추가했다. 같은 팀 회복 대상으로 탐색할 때 `HealingProjectileAttackCompo`가 붙은 함선을 제외한다. 기존 자기 자신 제외 조건과 손상된 함선 조건도 유지된다. 플레이어/적 회복선 프리팹과 `HealingSupportShipSetup` 양쪽에 설정을 반영했다. 배치 화면의 회복 규칙 안내 및 `Docs/HealingSupportShips.md`도 업데이트했다.
