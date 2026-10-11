# 함선 깃발·돛 펄럭임

아군 6종(본선 포함), 적군 5종의 기존 그림에 가벼운 천 움직임을 적용합니다. 적 분쇄선 보스도 공유 머티리얼을 통해 적용됩니다.

## 구성과 변경 범위

- 셰이더: `Assets/SB/Art/Shaders/ShipClothWaterline.shader` (`Fleet/Ship Cloth Waterline`). 기존 침몰·수면 클리핑을 유지하고, 함선별로 지정한 깃발·돛 영역의 UV만 움직입니다.
- 머티리얼: `Assets/SB/Art/Materials/Ships/` 및 `Enemies/` 아래 함체 11개, 별도 적 돛 문양 4개. `ApprenticeGunshipHull.mat`, `TransportClothHull.mat`는 새로 추가했습니다.
- 프리팹: 기존 기본 머티리얼을 사용하던 `PlayerShips/Test/TestEscort_A/B/C`, `PlayerShips/TransportMainShip`의 Visual 머티리얼을 연결했습니다. 나머지는 기존 머티리얼 참조를 유지합니다.
- 애니메이션: `Assets/SB/Animations/Ships/` 아래 기존 상태 클립 43개에 `SpriteRenderer.material._FlutterStrength` 곡선을 추가했습니다. 별도 문양이 있는 적 4종은 `SailRoleEmblem`에도 같은 곡선을 적용했습니다.
- 스프라이트: 수습 포격선 시트 4개, 철갑 방어선·작살 약탈선·분쇄선 이미지의 Mesh Type만 Full Rect로 변경했습니다. 변형된 천 끝이 Tight 메시 밖에서 잘리는 것을 방지합니다. 이미지, 프레임 Rect, Pivot, PPU는 유지합니다.

공격 반동, 발사·공격 종료·사망 이벤트, Animator 전환, 게임 수치는 변경하지 않았습니다. 추가 런타임 컴포넌트나 연결 작업은 없습니다.

## Inspector 조절

각 함선 Visual의 머티리얼에서 조절합니다. 별도 적 돛 문양은 함체와 동일한 바람 값을 유지해야 함께 움직입니다.

| 머티리얼 항목 | 기본값 | 용도 |
| --- | --- | --- |
| Flag Travel (Normalized Canvas) | 0.012 | 깃발 끝의 이동량 |
| Sail Travel (Normalized Canvas) | 0.008 | 돛 아래쪽의 이동량 |
| Wind Cycles Per Second | 1.2 | 바람의 속도 |
| Wind Phase (Radians) | 함선별 지정 | 함선 종류 간 움직임의 시작 위상 |
| State Flutter Strength | 클립에서 지정 | 상태별 배율. 재생 중에는 애니메이션 곡선이 우선합니다. |

상태 배율은 Idle 0.8, Move 1.0, Attack 0.9입니다. Dead는 0.65 → 0.3 → 0.05로 줄어듭니다. 깃대 쪽과 돛 위쪽은 덜 움직이도록 고정점을 두었습니다.

## 함선 그림을 교체할 때

- 천 영역은 자동 색상 탐색을 사용하지 않습니다. 원본 이미지의 왼쪽 위를 `(0, 0)`, 오른쪽 아래를 `(1, 1)`로 보고 Flag Bounds, Flag Pole X And Tip X, Sail Top, Sail Bottom, Sail Foot Curve Lift를 명시적으로 맞춥니다.
- 그림 크기·Pivot·PPU·시트 프레임·문양 Transform을 바꾸면 Sprite Mapping 값도 갱신합니다. 현재 설정 기록은 `AgentScripts/ShipClothFlutterProfiles-20261011.json`에 있습니다.
- 현재 셰이더는 원본 텍스처/시트 UV를 기준으로 하며 `CanUseSpriteAtlas=False`입니다. 아틀라스 사용을 추가할 때는 아틀라스 UV와 회전을 처리하도록 셰이더를 먼저 수정해야 합니다.
- 새 상태 클립에는 함체의 `material._FlutterStrength` 곡선을 추가합니다. 별도 문양 렌더러가 있으면 같은 값을 지정합니다.

## 확인 기록 (2026-10-11)

- 클립 43개의 기존 길이와 이벤트 시간·이름 유지, 프리팹 14개의 셰이더 연결 확인.
- 셰이더 컴파일 메시지 없음. 작업 중 새 제품 코드 오류는 확인되지 않았습니다. 기존 `MerchantLedger needs an icon.` 메시지는 별도 항목입니다.
- Unity Editor의 격리된 미리보기에서 함선 11종을 렌더링했습니다. 바람 위상이 다른 두 프레임 비교에서 각 함선의 깃발·돛 변화와 선체 하단 픽셀 유지 확인.
- GIF: `Artifacts/Ships/ClothFlutter/ShipClothFlutter.gif`. Idle 자세에서 바람만 진행한 미리보기이며 실제 전투 재생은 이번 작업에서 수행하지 않았습니다.
- 원본 백업: `AgentScripts/Backups/ShipClothFlutter-20261011/`.

## 침몰한 함선이 다른 위치에 남는 현상 수정 (2026-10-11)

- 사용자가 멈춰 둔 운송 전투에서 문제 함선들은 `IsDead=true`, 체력 0, `material._SinkProgress=1`이었습니다. 함선 루트는 배치 슬롯의 자식이며 로컬 위치가 0으로 유지됐지만 침몰한 그림이 아래쪽에 남아 있었습니다.
- 이 셰이더는 버텍스의 로컬 좌표로 침몰 거리·수면 클리핑·천 영역을 계산합니다. 동적 배칭으로 좌표가 변환되면 수면 클리핑 기준이 달라져 침몰한 선체가 다른 위치에 보일 수 있습니다. `ShipClothWaterline.shader`의 SubShader 태그에 `DisableBatching=True`를 추가했습니다. 이 셰이더를 쓰는 함선의 렌더링 배칭만 제한하며 씬·프리팹·머티리얼 값이나 전투 로직은 바꾸지 않습니다. 함선 렌더링의 드로우콜은 늘어날 수 있습니다.
- 동일한 일시정지 프레임에서 남아 있던 선체가 사라지고 살아 있는 아군과 적 함선, HUD가 표시되는 것을 확인했습니다. 전투를 재개하거나 시간을 진행하지 않았습니다. 셰이더 컴파일 메시지와 이번 변경의 신규 Console 오류는 없으며 기존 `MerchantLedger needs an icon.` 오류는 별개입니다. 자동 테스트는 실행하지 않았습니다.
- Inspector 추가 연결·조절은 필요하지 않습니다. 수정 전 백업은 `AgentScripts/Backups/ShipSinkBatching-20261011/ShipClothWaterline.shader.backup`, 화면 기록은 `Artifacts/Diagnostics/ShipPosition-Before-20261011.png`, `ShipPosition-After-20261011.png`입니다.
