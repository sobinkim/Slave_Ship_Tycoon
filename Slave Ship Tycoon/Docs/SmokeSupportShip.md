# 연막 교란함

## 동작

- 플레이어: `Assets/SB/Prefabs/PlayerShips/SmokeSupportShip.prefab`
- 적: `Assets/SB/Prefabs/EnemyShips/EnemySmokeSupportShip.prefab`
- 회복 지원함의 선체 조립, 체력/기본 능력치, 수직 발사 궤도와 실제 회복선 애니메이션을 복제했습니다. 그림은 승인된 구름 포구 형태에 아군 흰 돛·녹색 포신 / 적 붉은 돛·붉은 포신을 적용했습니다.
- 매 발사 이벤트에 상대 팀의 살아 있는 전체 함선에서 균등 무작위로 다시 선택합니다. 전열 보호와 집중 공격 지시를 무시합니다. 적 연막함은 호위선이 있어도 본선을 후보에 포함합니다.
- 연막탄 자체의 피해는 0입니다. 일반 탄과 같은 회피 판정을 거치고, 빗나가면 물에 떨어집니다.
- 적중하면 `SmokeCloudEffect`가 피격함을 따라다닙니다. **해당 함선이 이후 발사하는 일반 탄의 최종 명중 확률에 0.5를 곱합니다.** 상대 회피율이 10%라면 명중 확률은 90% → 45%입니다.
- 판정은 기존 구조처럼 발사 시 한 번 확정합니다. 이미 발사된 탄은 이후 연막 생성/종료에 따라 다시 판정하지 않습니다. Guaranteed 모드의 회복 캡슐 등은 영향을 받지 않습니다.
- 다시 적중하면 기존 연막 파티클을 재시작하며 배율은 중첩되지 않습니다. 연막함이 침몰해도 이미 적용한 연막은 남습니다.
- 파티클의 `IsAlive(true)`를 효과의 기준으로 사용합니다. 마지막 파티클 소멸, 피격함 사망/비활성화/풀 반환, 전투 종료 때 효과와 시각물을 함께 정리합니다. 별도 효과 종료 타이머는 없습니다.

## Inspector

| 위치 | 항목 | 현재 값 / 의미 |
| --- | --- | --- |
| 양쪽 함선 / SmokeProjectileAttackCompo | Attack Interval Multiplier | 3. 회복선에서 가져온 임시 발사 간격 배율 |
| 같은 컴포넌트 | Baseline Attack Animation Duration Seconds | 0.45833334. 복제한 AttackEnd 이벤트 시간 |
| 같은 컴포넌트 | Fire Points, Flight Data, Ballistic 설정 | 회복선 수직 발사 설정과 내부 ShootPos 참조 |
| `Assets/SB/Prefabs/Effects/SmokeCloud.prefab` / SmokeCloudEffect | Hit Chance Multiplier | 0.5 |
| 같은 컴포넌트 | World Offset | (0, 0.04, 0). 피격함 중심을 감싸는 위치 |
| 같은 프리팹 / ParticleSystem | Duration / Start Lifetime | 3.5초 방출 + 입자별 1~1.5초. 마지막 입자까지 대략 최대 5초. 임시 수치 |
| 양쪽 함선 / TargetSelector | Target Rules / Selection Mode | Any + None / RandomInRule |
| 같은 컴포넌트 | Ignores Commander Target Command | 활성화 |
| 적 함선 / TargetSelector | Includes Main Ship With Escorts | 활성화 |

지속시간은 파티클 방출 시간과 입자 수명으로 조절합니다. 두 필드 외에 별도의 디버프 지속시간을 맞출 필요가 없습니다.

## 파일과 등록

- 새 스크립트: `SmokeProjectileAttackCompo`, `SmokeBombProjectile`, `SmokeCloudEffect`.
- 기존 변경: `Ship`의 활성 연막 참조, `Base_ShipAttackCompo`의 소유 함선 접근자, `ArcProjectileAttackCompo`/`ProjectileLaunchData`의 발사 시 명중 배율 전달, `BaseProjectile`/`ShipProjectileEvasion`의 최종 확률 계산, `TargetSelector`의 전체 무작위 후보 옵션.
- 새 에셋: 양쪽 함선 PNG·머티리얼·애니메이션 4개와 오버라이드 컨트롤러, `SmokeBomb.prefab`, `SmokeCloud.prefab`, 연막탄/연기 메시와 머티리얼.
- 아군 데이터: `Assets/SB/Data/PlayerFleet/EscortShips/Escort_SmokeSupportShip.asset`, Stable Id `escort_smoke_support_ship`. 회복선과 같은 등급이며 카탈로그 끝에 한 번 추가했습니다.
- 사용자 요청에 따라 `Assets/SB/Data/PlayerFleet/EscortGachaTable.asset`에 모집 항목을 추가했습니다. 회복 지원함과 동일한 Weight 25, 전체 Weight 275 기준 등장 확률은 약 9.09%입니다. 실제 SampleScene의 모집 관리자가 이 테이블을 참조합니다.
- 초기 지급/편성, 적 스테이지 편성은 추가하지 않았습니다. 사용하려는 편성/스폰 데이터에 새 프리팹 또는 아군 데이터를 지정하면 됩니다.

## 확인 기록 (2026-10-12)

- Unity 스크립트 컴파일 성공.
- 양쪽 프리팹의 Missing Script 0, 회복 공격/회복 효과 제공 컴포넌트 제거, 내부 발사점과 연막탄·연기 참조 확인.
- 실제 회복선 클립의 발사/공격 종료/사망 이벤트 시간과 새 그림 바인딩 확인. 공유 원본 클립은 수정하지 않았습니다.
- 아군 카탈로그 등록 1개, 고유 ID 및 아이콘/프리팹 연결 확인.
- 격리된 Editor 미리보기로 양쪽 외형과 연기 파티클 렌더링 확인: `Artifacts/Ships/SmokeSupport/Preview.png`.
- 미리보기 정리 중 발생한 RenderTexture 연결 해제 경고는 미리보기 스크립트에서 수정했습니다. 최종 재실행에서는 재발하지 않았습니다.
- 실제 전투 플레이와 통계적 명중률 검증은 수행하지 않았습니다. 기존 `MerchantLedger needs an icon.` 오류는 이 작업과 별개입니다.

## 연막 외형 개선 (2026-10-12)

사용자 요청에 따라 공유 `SmokeCloud.prefab`의 연막을 더 크고 진하게 변경했습니다. 양쪽 함선이 같은 효과 프리팹을 사용하므로 함께 적용됩니다.

- 기존 작은 반투명 메시 구름을 큰 Billboard 파티클로 교체했습니다. Start Size는 월드 단위 0.82~1.08이며 수명에 따라 0.72 → 1 → 1.14 → 1.25배로 부풉니다. 실제 렌더링에서 기존보다 약 두 배 넓은 연막이 돛을 가립니다.
- 새 `Assets/SB/Art/Shaders/SmokeCloud.shader`와 `Assets/SB/Art/Effects/SmokeSupport/DenseSmoke.mat`으로 짙은 숯빛 내부, 둥글게 이어진 실루엣, 밝은 윗면 굴곡을 표현합니다. 단순 원들이 투명하게 겹쳐 보이던 모습을 줄였습니다. 이 머티리얼은 연기에만 연결되며 연막탄 머티리얼과 별도입니다.
- 초기 Burst 8, Rate over Time 7, 작은 상승/좌우 속도와 Noise로 부풀며 흐르는 움직임을 추가했습니다. Max Particles는 기존 40을 유지합니다.
- 머티리얼에서 Dense Smoke / Rolling Ridges / Silhouette 색, Opacity(0.98), Silhouette Width(0.045), Edge Softness(0.022), Roll Speed(0.4)를 조절할 수 있습니다.
- 방출 3.5초, 입자 수명 1~1.5초, 명중 배율 0.5와 파티클 소멸에 연동된 효과 종료는 유지했습니다.
- 같은 난수 시드로 0.18 / 1.5 / 3.65 / 4.8초 시점을 격리된 Editor 미리보기에서 렌더링했습니다. 변경 전 `Artifacts/Ships/SmokeSupport/Smoke-Before.png`, 변경 후 `Artifacts/Ships/SmokeSupport/Smoke-After.png`. 실제 전투 플레이는 수행하지 않았습니다.
- 새 셰이더의 컴파일 메시지 0, 지원 상태 정상, 이번 변경 중 새 Console 오류 없음. 별도 Editor 연결 작업은 없습니다.

### 참고 자료

- [Selle, Mohr, Chenney — Cartoon Rendering of Smoke Animations](https://pages.cs.wisc.edu/~schenney/research/npar/smoke/cartoon-smoke.pdf): 큰 색 덩어리와 읽기 쉬운 실루엣을 사용하는 카툰 연기 표현을 참고했습니다. 논문의 유체 시뮬레이션이나 깊이 기반 윤곽선 알고리즘을 구현한 것은 아닙니다.
- [Atif — Stylized Cartoon Explosion, Overwatch Inspired](https://realtimevfx.com/t/stylized-cartoon-explosion-overwatch-inspired/1767): 연기 덩어리가 팽창하고 남는 시각적 흐름을 참고했습니다. 이미지·텍스처는 가져오지 않았습니다.
- [Unity 6 — Color over Lifetime](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysColorOverLifeModule.html): 파티클 수명에 따른 색과 알파 변화 설정에 참고했습니다.

## 반투명 연막으로 재조정 (2026-10-12, 현재 적용)

이전 카툰 연막이 함선을 완전히 가리고 위쪽에 뜨는 문제를 사용자 피드백에 따라 다시 조정했습니다. 위의 굵은 실루엣·덩어리 표현과 관련한 설정은 이전 기록입니다.

- `SmokeCloud.shader`의 굵은 윤곽선과 고정된 구름 모양을 제거했습니다. 부드러운 노이즈 밀도와 미세한 음영으로 흐르는 연기를 표현합니다.
- `DenseSmoke.mat`의 Opacity Per Puff 0.46, Density Variation 1.1, Flow Speed 0.22로 조절합니다. 사용자 추가 요청으로 불투명도만 0.40 → 0.46으로 조금 높였습니다. 겹친 연기 사이로 선체·돛·문양이 보입니다.
- 사용자 요청으로 현재 연기 색은 흰색 계열입니다. Smoke Shadow는 (0.76, 0.78, 0.80), Smoke Light는 (0.98, 0.99, 1.00)이며 농도·형태·위치는 유지했습니다.
- `SmokeCloud.prefab`의 World Offset Y를 0.35 → 0.04로 내려 실제 피격함 중심을 감싸게 했습니다. 방출 영역도 가로로 넓히고 상승 속도를 줄여 머리 위의 덩어리로 뜨지 않게 했습니다.
- 입자 Start Size X 0.95~1.25 / Y 0.72~0.95, 초기 Burst 5 / 초당 방출 4. 지속시간과 명중률 배율은 유지합니다.
- 0.25 / 1.5 / 3.65초 Editor 렌더링: `Artifacts/Ships/SmokeSupport/Smoke-Soft.png`. 셰이더 컴파일 메시지 없음. 실제 전투 플레이는 수행하지 않았습니다.

## 연막탄 본체 디자인 (2026-10-12, 이전 기록)

- 사용자가 기존 연막탄 외형이 눈에 잘 띄지 않고 매력이 없다고 피드백해, 발사체 본체만 교체했습니다. 연막 이펙트는 변경하지 않았습니다.
- `Assets/SB/Art/Sprites/Projectiles/SmokeBomb.png`의 은회색 금속 캡슐, 황동 띠, 나무 마개, 구름 문양을 `SmokeBomb.prefab`의 SpriteRenderer에 연결했습니다. 양쪽 진영이 같은 탄 프리팹을 사용합니다.
- 3000 PPU 기준 실제 그림 크기는 대략 0.40 × 0.18 월드 단위이며, 기존 발사점·탄도·피격 시 연막 생성은 유지됩니다.
- 프리팹과 플레이어 함선을 함께 둔 미리보기: `Artifacts/Ships/SmokeSupport/SmokeBomb-Preview.png`.

## 연막탄 속도 조정 (2026-10-12)

- 사용자 요청으로 양쪽 연막함의 Ballistic Time To Apex를 0.38 → 0.76초로 변경했습니다. 같은 궤도를 비행하는 시간이 두 배가 되어 상승·하강 모두 기존 절반 속도로 움직입니다.
- 이 탄은 BallisticArc 모드이므로 Flight Speed 대신 Ballistic Time To Apex로 비행 속도를 조절합니다. Inspector 저장값을 확인했습니다.

## 기존 탄 스타일에 맞춘 본체 단순화 (2026-10-12, 현재 적용)

- 기존 `HealingCapsule.png`를 직접 참고해 연막탄을 나무 손잡이, 금색 띠 하나, 은색 연결부, 흰색 캡슐 탄두와 먹구름 문양으로 교체했습니다. 굵은 외곽선과 큰 명암 면으로 기존 탄의 묘사 수준에 맞췄습니다. 리벳·흠집·복잡한 장식은 제거했습니다.
- 내장 이미지 생성 도구로 제작했습니다. 최종 지시: 기존 회복탄의 형태·비율·각도·굵은 외곽선·단순한 셀 명암을 유지하고 녹색 탄두를 흰 회색으로, 십자 문양을 단색 먹구름으로 교체. 외곽의 연기·광채·그림자는 제거.
- 저장: `Assets/SB/Art/Sprites/Projectiles/SmokeBomb.png`. 기존 GUID와 양쪽 진영의 `SmokeBomb.prefab` 참조를 유지했습니다. 2500 PPU로 실제 그림의 폭은 약 0.41 월드 단위입니다.
- Unity에서 스프라이트 연결과 임포트 설정을 확인했습니다. 양쪽 함선의 Ballistic Time To Apex는 0.76초를 유지합니다. 실제 전투 플레이는 수행하지 않았으며 별도 Editor 연결은 필요 없습니다.

## 연막탄 속도 소폭 증가 (2026-10-12, 현재 적용)

- 사용자 요청으로 양쪽 연막함의 Ballistic Time To Apex를 0.76 → 0.60초로 줄였습니다. 같은 궤도에서 비행 시간이 약 21% 짧아지고 탄속은 약 27% 빨라집니다. Editor 경로로 프리팹에 저장했습니다.


## 연막탄에서 새는 연기 (2026-10-12)

- 공유 SmokeBomb.prefab에 LeakingSmoke 파티클을 추가했습니다. 탄 뒤쪽 X -0.18에서 작은 흰 연기가 초당 32개 발생하고, 월드 공간에 남아 0.25~0.45초 동안 조금 커지며 위로 흩어집니다. 선 모양의 TrailRenderer 대신 부드러운 노이즈 연기를 사용합니다.
- SmokeBombTrail.mat은 기존 연막 재질의 독립 복사본입니다. 착탄 연막에는 영향을 주지 않습니다.
- SmokeBombSmokeTrail 컴포넌트에 발사체·SpriteRenderer·파티클·ParticleSystemRenderer를 명시적으로 연결했습니다. 비행 중에만 방출하고 탄 바로 뒤에 정렬하며, 비활성화 시 입자를 제거해 풀 재사용 때 남지 않게 합니다.
- 조절: LeakingSmoke의 Rate over Time, Start Lifetime, Start Size, Size over Lifetime, 재질 Opacity Per Puff. 실제 전투 시각 확인은 수행하지 않았습니다.


### 새는 연기 가시성·잔류 시간 증가 (2026-10-12)

- LeakingSmoke의 Start Lifetime을 0.25~0.45 → 0.65~0.95초, Start Size를 0.045~0.075 → 0.065~0.10으로 늘렸습니다. 방출량은 초당 32 → 40개, Max Particles는 80입니다.
- SmokeBombTrail.mat의 Opacity는 0.65 → 0.90으로 높이고, 수명 중간까지 알파가 유지된 뒤 사라지도록 조정했습니다. 착탄 연막과 탄속은 유지합니다. 실제 전투 시각 확인은 수행하지 않았습니다.


## 회복탄의 부드러운 빛 꼬리 (2026-10-12)

- HealingCapsule.prefab과 EnemyHealingCapsule.prefab의 기존 비활성 trail을 켜고, 전체 경로를 그리던 TravelledFlightPath는 비활성화했습니다. 기존 오브젝트와 스크립트는 남겨두었습니다.
- 탄 뒤 X -0.20에서 0.38초 동안 남는 TrailRenderer입니다. 폭은 0.055 기준으로 시작은 가늘고 중간은 조금 넓어지며 끝은 0으로 줄어듭니다. 아군은 옅은 연두색, 적은 옅은 분홍색이고 뒤쪽으로 투명해집니다.
- HealingProjectileTrail.shader / HealingProjectileTrail.mat으로 선 양옆도 부드럽게 흐려지도록 했습니다. BaseProjectile의 기존 풀 재사용 초기화와 탄 기준 정렬을 사용합니다.
- 조절: trail의 Time, Width, Width Curve, Color Gradient. Editor에서 두 프리팹에 저장했으며 실제 전투 시각 확인은 수행하지 않았습니다.


## 회복탄의 + 잔상 (2026-10-12)

- 두 회복탄 프리팹에 HealingCrossTrail 파티클과 HealingProjectileCrossTrail 컴포넌트를 추가했습니다. 기존 HealCross.png / HealingCrossParticles.mat을 재사용합니다.
- 탄 뒤쪽에서 0.125 월드 단위마다 +를 남깁니다. 회전하지 않고 월드 공간에 남아 살짝 위로 떠오르며 0.55~0.80초 동안 작아지고 흐려집니다. 아군은 연두색, 적은 옅은 분홍색입니다.
- 기존 빛 꼬리의 시작 알파는 0.72 → 0.22로 줄여 + 문양이 주로 보이게 했습니다. 발사 중에만 방출하며 비활성화 때 잔여 입자를 제거합니다.
- 조절: HealingCrossTrail의 Rate over Distance, Start Lifetime, Start Size, Velocity over Lifetime. 프리팹 저장과 참조 할당을 완료했습니다. 실제 전투 시각 확인은 수행하지 않았습니다.

