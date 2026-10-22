# 기존 해적 구역에 크리쳐 전투 연결

## 적용 범위

- 기존 씬: `Assets/SB/Animations/Scenes/SampleScene.unity`.
- `ChapterRouteType.Obtain`이며 `ChapterEnemyFaction.Pirate`인 챕터의 일반 전투는 `PirateSeaCreatureCombat`에서 진행합니다. 현재 데이터의 1·3챕터가 해당합니다. 운송(`Sell`)은 항상 기존 함선 전투 경로를 사용합니다.
- 항해 → 전방 무리 조우 → 정지 후 5/7/9마리 3웨이브 → 전멸·침수 종료 → 재항해를 반복합니다. 최신 웨이브 70% 처치 시 다음 증원이 화면 밖에서 합류합니다.
- 크리쳐 외형·수면 동작·스프라이트 전체 피격 발광과 앞 함선의 포격 차단을 함께 사용합니다.
- 기존 UI 구성·연결, 보스전 배치, 해군 운송 전투는 유지합니다. 테스트 HUD와 R 재출항 키는 가져오지 않았습니다.

## 기존 시스템 연결

- `GameFlowManager`가 발행한 챕터 시작 정보를 그대로 사용해 보상·출항·보스 도전·운송 복귀를 이어갑니다. 크리쳐 디렉터는 챕터 이벤트를 중복 발행하지 않습니다.
- 적 무리의 모든 웨이브와 침수가 끝나면 매번 기존 `StageSpawnManager.TryPreparePlayerFleet`를 호출합니다. 파괴된 호위선은 체력·외형을 복구하고, 생존 함선은 현재 체력과 기존 자연 회복을 유지합니다. 모집·편성 변경도 이때 적용합니다. 편성 변경에 따른 함대 교체 방식은 기존 동작과 같습니다.
- 보스전·운송 구역·패배로 종료되면 크리쳐를 반환하고 카메라·함대·배경 위치 및 타깃/포격 차단 설정을 복원합니다.
- 호위선 전멸 후 크리쳐가 본선을 공격하면 기존 크리쳐 접촉 패배 경로를 사용합니다. 크리쳐 전투에서는 함선용 패배 포격을 실행하지 않습니다.
- 사령관 수동/자동 전투 스킬은 교전 가능한 크리쳐가 있을 때 사용합니다. 항해·화면 밖 진입 중에는 대기합니다.

## Inspector

- `Managers/StageSpawnManager`의 `Pirate Sea Creature Director`: `PirateSeaCreatureCombat`.
- `PirateSeaCreatureCombat/SeaCreatureSurvivalDirector`: `Initial Creatures` 5, `Waves Per Encounter` 3, `Group Size` 2, `Next Wave Kill Ratio` 0.7, `Cruise Speed` 1.25, `Group Spacing` 4.8.
- 같은 컴포넌트의 `Start On Play`, `Allow Restart Shortcut`은 꺼져 있습니다. 기존 출항 흐름으로 시작합니다.
- `SeaVoyageMotor`는 원래 함대 스폰 루트·새 카메라 부모·기존 배경·크리쳐 진입점을 명시적으로 참조합니다.

## 확인 및 남은 확인

- Unity C# 컴파일 성공, 최종 Console 오류·경고 0.
- 두 씬을 함께 열어 복사하는 동안 전역 조명 중복 메시지가 기록됐습니다. 테스트 씬을 닫고 기존 씬만 남긴 최종 상태에서는 발생하지 않습니다.
- Editor API로 씬 저장 및 필수 참조·같은 씬 연결을 확인했습니다. 기존 UI 객체의 설정 지문은 설치 전후 동일합니다.
- 최초 연결 시에는 Play 실행을 하지 않았으며, 후속 발사 문제 조사에서 기존 씬의 해적·운송 전투를 실행해 실제 투사체 발사를 관찰했습니다. 모집 후 다음 무리 합류와 보스 도전은 추가 플레이 확인 항목입니다.
- 연결 기록: `Artifacts/SeaCreatures/PirateIntegrationConnections.txt`.
- 적용 전 기존 씬 및 미저장 테스트 씬은 `AgentScripts/Backups/PirateCreatureIntegration-20261015/`에 Editor API로 사본을 보존했습니다. 테스트 씬 원본 파일은 저장하지 않았습니다.

## 변경 파일

- `SampleScene.unity`
- `StageSpawnManager.cs`, `SeaCreatureSurvivalDirector.cs`
- `TargetSelector.cs`, `Base_ShipAttackCompo.cs`: 임시 설정 복원을 위한 조회 속성.
- `MainShipDefeatDirector.cs`, `CommanderSkillManager.cs`: 크리쳐 전투 호환.
- Editor 연결 도구: `AgentScripts/InstallPirateCreatureCombat.cs`, `AgentScripts/InspectPirateIntegration.cs`.

## 후속 발사 수정 (2026-10-15)

- 원인: 살아 있는 현재 표적이 아군에게 가려져도 유지되어, 다른 크리쳐로 사선이 열려 있는 함선이 계속 대기했습니다. 실행 진단에서 두 함선에 각각 사선이 열린 대체 표적 2개가 있는 상태를 확인했습니다.
- `TargetSelector.GetTarget(Predicate<Ship>)`와 `Base_ShipAttackCompo`의 사선 필터를 연결해, 현재 표적이 가려지면 기존 선택 규칙 안에서 공격 가능한 다른 표적을 다시 선택합니다. 모든 사선이 닫히면 기존처럼 발사를 보류합니다.
- 필터는 아군 사선 차단이 켜진 공격에만 적용됩니다. 운송의 기존 선택 규칙, 회복함의 아군 선택, 요격함의 적 포탄 요격은 유지합니다.
- 각 8초 실행 관찰: 해적 1챕터 크리쳐 대상 24발/적 함선 대상 0발, 운송 2챕터 크리쳐 대상 0발/적 함선 대상 20발. 전환 전에 이미 날아가던 투사체는 집계에서 제외했습니다.
- C# 컴파일 오류 없음. 기존 `MerchantLedger needs an icon.` 장비 데이터 오류 1건은 남아 있으며 발사 문제와 별개입니다.
- 기록: `Artifacts/SeaCreatures/PirateBlockedAlternatives.txt`, `PirateTransportFiringObservation.txt`. 진단 도구는 `AgentScripts/InspectPirateFiring.cs`이며 게임 런타임에 포함되지 않습니다.
- 새 Inspector 항목은 없습니다. 기존 `Allied Fire Block Radius`는 0.42를 유지합니다.

## 후속 함선 복구 수정 (2026-10-15)

- 원인: 편성 버전이 그대로면 다음 무리 준비를 건너뛰어 `Ship.PrepareForNextBattle()`의 파괴 함선 복구가 실행되지 않았습니다.
- 마지막 웨이브와 이전 웨이브의 잔여 적·침수를 모두 정리한 뒤, 매번 기존 함대 준비 → 전투 참조 재연결 → 재항해 순서로 진행합니다. 70% 처치 증원 시점에는 복구하지 않습니다.
- 파괴된 호위선의 체력·사망 애니메이션·함체 외형은 기존 복구 경로가 복원합니다. 생존 함선 체력과 항해 거리는 유지하며, 새 Inspector 항목이나 UI 변경은 없습니다.
- 변경 파일: `SeaCreatureSurvivalDirector.cs`, `StageSpawnManager.cs`. 기존 복구 경로와 호출 시점을 코드로 확인하고 Unity C# 컴파일을 확인했습니다. 이번 수정에서 별도 실행 테스트는 수행하지 않았습니다.
