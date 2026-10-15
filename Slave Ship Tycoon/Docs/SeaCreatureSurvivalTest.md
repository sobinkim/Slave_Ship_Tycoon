# 해양 크리쳐 항해·조우 테스트 (V4)

## 실행

`Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity`를 열고 Play를 누릅니다. 하단 일시정지/다시 출항 버튼을 사용하며, `R`도 재출항입니다.

## 현재 흐름

1. 함대와 카메라가 실제 월드에서 오른쪽으로 전진합니다. 앞 해역의 크리쳐는 **왼쪽을 바라보고 제자리에 유영**하며, 조우 전에는 포격 대상이 아닙니다.
2. 전열이 무리에 가까워지면 **함대·카메라·항해 거리가 정지**하고 전투를 시작합니다.
3. 기본 **5 → 7 → 9마리, 3웨이브**를 싸웁니다. 현재 웨이브의 **70%를 처치하면 잔여 적이 있는 상태에서 다음 웨이브가 즉시 합류**합니다. 5마리 웨이브는 4처치, 7마리 웨이브는 5처치가 기준입니다.
   증원은 실제 카메라 오른쪽 경계 밖에서 생성되어 헤엄쳐 들어옵니다. 몸 전체가 화면 안으로 진입한 뒤 일반 전투와 포격 대상 판정을 시작합니다.
4. 마지막 웨이브까지 등장한 뒤 이전 웨이브의 잔여 적을 포함해 모두 처치하고 침수가 끝나면 파괴된 호위선을 기존 전투 준비 절차로 복구하고 다시 항해합니다. 생존 함선은 현재 체력과 자연 회복을 유지합니다. 다음 무리에서 같은 흐름을 반복합니다.
5. 호위선 전멸 후 크리쳐가 모선에 도달하면 패배합니다. 다시 출항하면 전투를 초기화합니다.

기존 자동 포격·스킬·처치 보상을 사용합니다. 앞 아군 함선이 포격 경로를 가리면 후열 사격을 보류하고, 앞 함선이 쓰러지거나 경로가 열리면 재개합니다. 크리쳐는 앞 함체를 통과해 뒤 함선을 물지 못하고, 돌진도 첫 함체에 부딪히면 멈춥니다.

## 외형과 수면 연출

- 두 종의 외형을 기존 함선의 굵은 자주색 외곽선과 단순한 명암에 맞춰 교체했습니다. 시작 방향은 왼쪽입니다.
- 대기 호흡, 꼬리 유영, 몸 기울기, 공격 준비·물기·회복, 돌진, 피격 반동을 구분했습니다.
- 고정 수면 높이에서 몸 아래를 잠기게 하고 접수부에 바다색을 섞습니다. 접수 그림자·포말·잔물결·죽을 때 침수 효과를 연결했습니다.
- 실제 피해가 들어오면 착탄 지점에 크림색 타격광과 물방울 파편이 짧게 표시됩니다. 빗나감·무적·0 피해·회복에는 표시하지 않고, 마지막 치명 타격에도 표시합니다.
- 이미지 생성은 built-in image_gen을 사용했습니다. 최종 이미지 경로와 사용 프롬프트는 [이미지 제작 기록](SeaCreatureV3Art.md)에 있습니다.
- V4에서는 현재 견습 포격선과 운송선 그림을 직접 참고하여 머리와 몸체를 더 둥글게, 외곽선과 명암을 더 단순하게 정리했습니다. 현재 사용 이미지는 `ReefGlider_V4.png`, `ReefbackLeviathan_V4.png`이며 [V4 제작 기록·프롬프트](SeaCreatureV4Art.md)에 정리했습니다.
- 피격 순간 어두운 외곽선을 포함한 스프라이트 전체가 따뜻한 아이보리색으로 짧게 발광합니다. 투명 영역과 수면 경계는 유지합니다.

## Inspector

`SeaCreatureSurvival` → `SeaCreatureSurvivalDirector`:

| 항목 | 현재 값 | 의미 |
|---|---:|---|
| Cruise Speed | 1.25 | 항해 단계의 초당 전진 거리 |
| Encounter Start Distance | 2 | 전열과 앞 무리 사이의 정지 거리 |
| Group Spacing | 4.8 | 격파 후 다음 무리를 배치하기까지 이동 거리 |
| Waves Per Encounter | 3 | 한 무리에서 싸울 웨이브 수 |
| Initial Creatures | 5 | 첫 웨이브 개체 수 |
| Group Size | 2 | 다음 웨이브마다 추가되는 개체 수 |
| Maximum Alive | 14 | 웨이브 개체 수 상한 |
| Next Wave Kill Ratio | 0.7 | 현재 웨이브의 다음 증원 처치율 기준 |
| Wave Delay Seconds | 0.6 | 모든 적의 침수 후 재항해 대기 시간 |
| Reinforcement Offscreen Padding | 0.5 | 몸체 전체를 화면 밖에 두는 추가 여유 거리 |
| Reinforcement Entry Depth | 0.8 | 화면 밖 출발 위치의 앞뒤 분산 |
| Reinforcement Entry Speed | 2.2 | 화면 안으로 헤엄쳐 진입하는 속도 |
| Charger Chance | 0.3 | 갑각수 등장 비율 |
| Allied Fire Block Radius | 0.42 | 앞 아군 함체의 포격 차단 반경 |

두 크리쳐 프리팹의 `SeaCreatureEnemy`에서 체력·유영 속도·물기·돌진 피해·예고 시간·피격 경직을 조절합니다. `SeaCreatureSurfacePresentation`의 `Waterline Local Y`, `Sink Distance`, `Bite Lunge Distance`, `Swim Bend Strength`로 수면 높이와 동작을 조절합니다. 현재 수치는 테스트용 초안입니다.

`SeaCreatureHitFeedback`의 `Burst Size`, `Burst Seconds`, `Chip Count`, `Chip Size`, `Chip Speed`, 색상으로 타격 효과를 조절합니다. 두 ParticleSystem과 착탄 앵커가 프리팹에 명시적으로 연결되어 있습니다.

같은 컴포넌트의 `Emission Color`, `Emission Intensity`(1.8), `Emission Attack Seconds`(0.02), `Emission Release Seconds`(0.14)로 몸체 발광을 조절합니다. 실제 피해에서만 켜지고 치명 타격도 포함하며, 풀 반환 시 초기화됩니다.

처치율은 최신 웨이브의 예정 개체 수를 분모로 삼습니다. 이전 웨이브 잔여 적의 처치는 새 웨이브 처치율에 섞이지 않습니다. 동시 등장 상한에 걸린 증원은 빈자리가 나는 대로 등장하며 생략되지 않습니다. 모든 예정 개체가 등장한 뒤 다음 처치율 조건을 평가합니다.

`SeaVoyageMotor`에는 편대 스폰 루트·카메라 이동 부모·배경 루트·다음 무리 배치 루트가 명시적으로 연결되어 있습니다. 카메라 흔들림은 자식 Camera에서 처리합니다. 테스트 씬의 ParallaxLayer에는 이동 기준 배경 루트가 연결되어 장거리 타일 순환 경계도 함께 이동합니다.

## 검증 (2026-10-15)

- C# 재컴파일 성공, 최종 실행 신규 Console 오류/경고 없음.
- 항해 샘플: 크리쳐 월드 X 변화 0, 함대와 카메라는 동일하게 0.669 전진. 조우 전 공격 표적 0.
- 전투 중 항해 거리 고정. 5/7/9마리 웨이브를 순서대로 모두 처치한 뒤 재항해, 두 번째 조우에서 다시 정지.
- 총 21처치·실제 보상 지급. 반복 중 BattleStart 추가 0회/BattleEnded 0회, 편대 생성 세대 유지.
- 가려진 사수 최대 3척 사격 보류 확인. 전열이 피해를 받는 동안 후열 체력 유지.
- 일시정지 시 거리 정지, 재개 확인.
- 최종 아트 보정에서 뱀장어의 수면 높이를 조금 낮춰 몸 동작이 더 보이도록 했습니다.
- 처치율 변경 후 2웨이브는 이전 웨이브 1마리, 3웨이브는 이전 웨이브 2마리가 살아 있는 상태에서 합류했습니다. 동시 생존 최대 11마리, 최종 21처치 뒤 재항해를 확인했습니다.
- 피격 효과는 일반 피해와 치명 타격에서 표시되고, 0 피해와 회복에서는 표시되지 않았습니다. 처음 발견된 피격 셰이더 문법 오류는 수정했으며 그래픽 컴파일 메시지 없음/지원됨과 최종 화면 색상을 확인했습니다.
- 화면 밖 진입 변경 후 2웨이브 증원 7마리 모두 몸 전체가 오른쪽 경계 밖에서 시작했습니다. 이전 적 1마리가 남아 있었고, 부분 노출을 거쳐 왼쪽으로 이동한 뒤 교전에 참여했습니다. 진입 중에는 포격 대상에서 제외됨을 확인했습니다.
- V4 아트와 몸체 이미션 연결 후 일반 피해/치명 타격의 발광, 0 피해/회복에서 미발광, 자연 소등과 풀 초기화를 확인했습니다. 실제 자동 포격에서 발광 화면과 유영/침수 MPB 공존을 확인했습니다.

기존 정지 전투 반복 기록: `Artifacts/SeaCreatures/V3_Verification.txt`. 처치율 웨이브 검증: `WaveOverlapVerification.txt`, 피격 조건 확인: `HitEffectVerification.txt`. 최신 화면: `OverlappingWaves.png`, `CreatureHitEffect.png`.

화면 밖 진입 기록: `OffscreenReinforcementVerification.txt`. 화면: `ReinforcementsOutside.png`, `ReinforcementsEntering.png`, `ReinforcementsJoined.png`.

## 변경 파일과 범위

- 전용 흐름/연출: `SeaCreatureSurvivalDirector.cs`, `SeaCreatureEnemy.cs`, `SeaVoyageMotor.cs`, `SeaVoyagePresentation.cs`, `SeaCreatureSurfacePresentation.cs`.
- 처치율 웨이브/피격 추가: `SeaCreatureSurvivalDirector.cs`, `SeaCreatureHitFeedback.cs`, `SeaCreatureHit.shader`, 크리쳐 프리팹 2개와 피격 머티리얼 2개, 테스트 씬의 처치율 값.
- 공통 코드의 선택 옵션: `Base_ShipAttackCompo.cs`, `ArcProjectileAttackCompo.cs`, `TargetSelector.cs`, `ParallaxLayer.cs`. 앞 함선 차단과 미조우 표적 제외는 이 테스트에서 명시적으로 켭니다.
- 테스트 씬, 크리쳐 프리팹 2개, V3 스프라이트 2개, 수면 셰이더 2개/전용 머티리얼, `UI/SeaCreatureTest/`.
- 이전 구현의 공통 변경인 외부 적 생성 옵션 및 이동 표적 발사체 추적을 계속 사용합니다.

씬·프리팹 연결은 Unity Editor API로 저장했습니다. 주요 원본 백업은 `AgentScripts/Backups/SeaCreatureV3-20261015/`에 있습니다.

복제 씬의 기존 상선 장비 아이콘 오류 때문에 테스트 씬의 `TransportEquipmentManager`는 비활성화되어 있습니다. 상선 장비 UI는 이 테스트 범위에 포함되지 않습니다.

