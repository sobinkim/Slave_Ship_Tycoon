# 요격 호위함 구현 기록

구현 완료: 2026-10-14. 사용자 승인 이미지 `codex-clipboard-5777c573-ab4a-449c-96af-01198ab1e64b.png`를 그대로 게임 스프라이트로 사용했습니다.

후속 수정: 포신을 낮추고 폭발을 Particle System으로 교체한 뒤, 밝기와 형태를 다시 다듬었습니다. 최신 이펙트 설정은 [BrightEffect-2026-10-14.md](BrightEffect-2026-10-14.md), 포신 수정 기록은 [VisualUpdate-2026-10-14.md](VisualUpdate-2026-10-14.md)를 참조하세요. 아래 표의 최초 InterceptionBurst Duration / Radius는 교체 전 값입니다.

## 동작

- 같은 가로 배치 행(SpawnSlot.y)의 아군을 향하는 포탄과 본선을 향하는 포탄을 보호합니다.
- 사거리 안에서 요격할 수 있는 탄은 후열 → 전열 → 중열 우선순위로 고릅니다. 각 우선 열에 유효한 탄이 없을 때 다음 열을 확인하고, 같은 열에서는 착탄이 가장 임박한 탄을 우선합니다. 플레이어와 적의 슬롯 x축 방향이 반대인 점을 논리 열로 변환해 적용합니다.
- 기존 Arc / AcceleratingStraight / AcceleratingArc / BallisticArc의 실제 확정 경로를 예측합니다. 요격탄은 예측 지점을 향해 일정 속도로 직진하며 발사 후 유도하지 않습니다.
- 요격탄이 도달하기 전에 착탄하는 포탄, 운으로 빗나가기로 확정된 탄, 이미 다른 요격탄이 예약한 탄은 건너뜁니다.
- NormalProjectile의 `Interceptable`을 켠 포탄과 연막탄을 대상으로 합니다. 같은 행 또는 본선을 향하는 적의 연막탄도 착탄 전에 요격되며, 성공하면 연막 효과가 생기지 않습니다. 회복탄은 지원탄이라 제외합니다.
- 요격 성공은 실제 상대 운동 선분으로 판정합니다. 낮은 FPS에서는 원래 탄도를 잘게 나눠 검사하고 원래 착탄 시각 이후 구간은 검사하지 않습니다.
- 성공한 적 포탄은 `Intercepted`로 한 번만 종료하고 피해·물 착탄을 적용하지 않습니다. 기존 피격 예정 표시에서도 제거됩니다.
- 포탄 예약은 발사체 수명과 함께 해제됩니다. 전투 종료, 최종 일제사격, 풀 반환, 재사용 시 세대 번호가 바뀌는 경우도 정리합니다.
- 요격은 공격 상태 진입과 동시에 발사하고 전용 클립의 반동·섬광을 재생합니다. 공격 종료 이후 쿨다운을 진행하며 공격속도 강화가 기존 체계를 따라 적용됩니다.
- 적 함선에 대한 일반 공격은 하지 않는 지원 함선입니다. 연사와 여러 포탄을 모두 막지는 못합니다.

## 초기 설정과 Inspector

| 대상 | 항목 | 초기 값 |
|---|---|---|
| InterceptorShip / ShipStatCompo | 체력 / 공격력 | 120 / 0 |
| InterceptorAttackCompo | Attack Cooldown | 1.4초, 전용 공격 애니메이션 종료 후 시작 |
| InterceptorAttackCompo | Projectile Speed | 12 월드 단위/초 |
| InterceptorAttackCompo | Interception Range | 발사점 → 요격 지점 5 월드 단위 |
| InterceptorAttackCompo | Minimum Flight Seconds | 0.04초 |
| InterceptorAttackCompo | Impact Margin Seconds | 적 탄 착탄 전 0.06초 여유 |
| InterceptorAttackCompo | Protects Same Row Only / Protects Main Ship | true / true |
| InterceptorProjectile | Hit Radius | 0.09 월드 단위 |
| InterceptorProjectile | Miss Grace Seconds | 0.15초 |
| InterceptionBurst | Duration / Radius | 0.22초 / 0.23 |
| Escort_InterceptorShip | Stable Id / 등급 | escort_interceptor_ship / Uncommon(고급) |
| EscortGachaTable | 새 항목 Weight | 25 |

카탈로그 맨 뒤에 추가해 기존 합성 결과 순서를 유지했습니다. 가챠 전체 가중치 합은 300으로 기본 선택 확률은 25/300 = 8.33%입니다. 기존 1차 승급 8%로 다른 상위 등급 함선으로 승급하는 경우를 제외한 최종 요격선 획득 확률은 약 7.67%입니다. 기존 승급 규칙은 유지했습니다.

발사점은 `Visual/ShootPos`, 섬광 위치는 `Visual/MuzzleVfx`에서 조절합니다. 함체 크기, 수면 위치, 천 영역은 새 그림에 맞게 전용 머티리얼에 설정했습니다. 추가 Inspector 연결은 없습니다.

## 변경 파일

기존 파일:
- Assets/SB/Scripts/Ship/Battle/AttackCompo/Base_ShipAttackCompo.cs — 기존 상태 머신에서 발사체 표적 함선도 사용할 수 있도록 표적 확보·초기화 확장 지점 추가.
- Assets/SB/Scripts/Ship/Battle/Projectile/BaseProjectile.cs — 비행 경로 예측, 활성 포탄 목록, 예약·세대·요격 종료 처리.
- Assets/SB/Scripts/Ship/Battle/Projectile/NormalProjectile.cs — 요격 가능 여부를 명시적으로 노출.
- Assets/SB/UI/Fleet/FleetToolkitScreen.cs — 요격선의 보호 범위와 탄 종류 설명.
- Assets/SB/Data/PlayerFleet/EscortShipCatalog.asset
- Assets/SB/Data/PlayerFleet/EscortGachaTable.asset

신규 코드:
- Assets/SB/Scripts/Ship/Battle/AttackCompo/InterceptorAttackCompo.cs
- Assets/SB/Scripts/Ship/Battle/Projectile/InterceptionPrediction.cs
- Assets/SB/Scripts/Ship/Battle/Projectile/InterceptorProjectile.cs
- Assets/SB/Scripts/Ship/Battle/Projectile/InterceptionBurst.cs
- Assets/SB/Editor/InterceptorShipSetup.cs — 명시적으로 실행한 일회성 에셋 조립. 이미 있는 에셋은 덮어쓰지 않습니다.
- Assets/SB/Editor/Tests/InterceptionTests.cs

신규 에셋:
- Assets/SB/Art/Sprites/Ships/InterceptorShip.png
- Assets/SB/Art/Materials/Ships/InterceptorHull.mat
- Assets/SB/Prefabs/PlayerShips/InterceptorShip.prefab
- Assets/SB/Prefabs/Projectiles/InterceptorProjectile.prefab
- Assets/SB/Prefabs/Effects/InterceptionBurst.prefab
- Assets/SB/Data/PlayerFleet/EscortShips/Escort_InterceptorShip.asset
- Assets/SB/Animations/Ships/InterceptorShip/ 아래 전용 상태 클립 4개와 Override Controller

## 검증

- EditMode: InterceptionTests 11/11, ProjectileEvasionTests 6/6, ShipTargetingTests 17/17 — 합계 34/34 통과.
- 실제 Play 재생: 4개 탄도 × 60/15/5 FPS = 12개, 다른 행 제외·본선 보호·두 발 동시 공격 = 3개. 합계 15/15 통과. 요격 성공 시 보호 대상 HP 180 유지, 통과한 탄은 30 피해를 적용.
- 두 척의 요격선이 같은 탄에 발사한 총 요격탄은 한 발. 과부하 검사에서는 한 발만 요격하고 다른 한 발은 통과.
- 쿨다운 이후 두 번째 공격도 발사·요격 성공. 사망 시 애니메이션 Dead 전환 및 MaterialPropertyBlock의 SinkProgress=1 확인. 다음 전투 준비 시 체력·상태 복원, 풀 재사용 시 세대 증가·HP 120 확인.
- 실제 모집 UI의 정상 모집 경로로 요격선을 획득해 새 카드 아이콘·고급 등급 표시·보유량 증가·0번 슬롯 편성과 런타임 Loadout의 새 프리팹 연결 확인.
- 새 프리팹 내부 참조와 Animator 연결 확인. 방어 Provider가 남지 않았고 공격 컴포넌트는 정확히 한 개.
- 새 제품 코드 컴파일 오류 없음. 실제 Play 구간의 기존 오류는 `MerchantLedger needs an icon.`입니다. 테스트 도중 Play/Edit 전환이 겹쳐 Unity Test Runner 자체 오류가 발생한 실행은 제외하고 재실행해 위 통과 결과를 확인했습니다.
- 원래 씬을 직접 편집하거나 저장하지 않았습니다. Unity Test Runner의 씬 복원 이후 최종 Editor는 Edit Mode, SampleScene의 dirty=false였습니다. 초기 지급·초기 편성·기존 스테이지 설정을 구현 코드에서 변경하지 않았습니다.

기록:
- QA/InterceptorShip/play-results.csv
- QA/InterceptorShip/in-flight.png
- QA/InterceptorShip/recruitment-results.txt
- QA/InterceptorShip/reload-and-sink.txt
- QA/InterceptorShip/lifecycle-results.txt — 최초 수명 검사. Sink 값은 Material을 읽은 값이며, 실제 애니메이션 값은 후속 reload-and-sink의 MaterialPropertyBlock 검사 참조.
- Assets/QA/InterceptorShip/recruitment.png
- AgentScripts/InterceptorPlayQA.cs.txt — 격리된 실제 재생 검사 스크립트. Play Mode에서만 실행하며 원본 씬을 저장하지 않습니다.

남은 작업은 실제 여러 웨이브에서 체감 쿨다운·사거리·모집 가중치를 밸런싱하는 것입니다. 기능 연결과 등록은 완료했습니다.
