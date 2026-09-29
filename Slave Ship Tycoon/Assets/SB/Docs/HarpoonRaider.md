# 작살 습격함 / 전투 표시 (2026-09-21)

## 반영 내용

- 원본 HarpoonRaider.png는 모집 아이콘으로 유지한다. 전투 선체는 작살촉을 제거한 HarpoonRaider_Unloaded.png와 별도 LoadedHarpoon 자식으로 구성한다. 새 선체는 투명 여백을 유지하는 Single Sprite이며 PPU는 1200이다.
- 이미지의 작살 디자인을 참고해 별도 투명 작살 스프라이트를 제작했다. `RaiderHarpoon.prefab`은 흰색 틴트로 원색을 유지하고, 곡선의 접선 방향으로 회전한다.
- 일반탄의 TrailRenderer와 ProjectilePathDisplay는 비활성화했다. 작살만 발사점부터 실제 지나온 경로를 표시하며 명중/회수 때 지운다.
- 후방 → 중열 → 전방 순으로 표적을 선택한다. 기존 보호 대상 직접 타겟 허용 옵션을 습격함에 명시적으로 켰다.
- 공격 대상에 빨간 갈고리 하나를 표시한다. 여러 공격자는 같은 표식을 공유한다. 기존 고유 효과 아이콘과 별도의 WorldUI 오브젝트다.
- 바닥 예고 데칼은 매끄러운 원형 링이다. 수면 원근감은 기존 Transform의 X/Y 비율로 유지한다. 피격 시 솟는 ImpactBurst는 모든 현재 함선 프리팹에서 제거했다. MISS 물기둥과 나무 파편은 유지한다.
- IroncladDefender의 공격/사망 이벤트 타이밍을 재사용했다. 공격은 선체 반동, 사망은 기울어짐과 수면 아래 침몰이다. Idle/Move/Attack/Dead 모든 스프라이트 키는 습격함 그림을 참조한다.

## 주요 에셋과 조절 위치

| 에셋 / 컴포넌트 | 조절 값 |
| --- | --- |
| `Prefabs/PlayerShips/HarpoonRaider.prefab` | 함선 역할·스탯, TargetSelector 규칙, ShootPos 발사점, 애니메이션 |
| `Prefabs/Projectiles/RaiderHarpoon.prefab` | Arc 비행 데이터는 함선 Attack 컴포넌트, 작살 크기는 프리팹 Scale, 경로 Width Units/Path Color/Point Spacing |
| 함선 루트 `RaidTargetMarker` | Marker Color(빨강), 선체 대비 Sorting Order Offset |
| `WorldUI/RaidTargetHook` | Local Position (0, 0.58, 0), Scale 0.24 |
| `Art/Effects/IncomingFire/IncomingFireDecal.mat` | Ring Radius, Ring Width(0.035) |
| `Data/PlayerFleet/EscortGachaTable.asset` | 습격함 Weight 100, 기존 합 100 → 테스트용 등장 확률 50% |
| `Data/PlayerFleet/HarpoonRaider.asset` | 이름 작살 습격함, 등급 Uncommon, 아이콘 및 프리팹 참조 |

새 함선에는 RaidTargetMarker의 Ship/Hull/Hook Icon을 명시적으로 연결한다. 후방 공격자로 사용할 프리팹에는 RaidTargetingIndicator의 Owner/Attack도 연결한다. 자동 탐색으로 런타임 설치하지 않는다. 기존 카탈로그 순서는 유지하고 마지막에 새 데이터를 추가했다.

## 검증

- 기존 타겟 선택 회귀 테스트 17개 통과, C# 컴파일 오류 없음.
- 실제 모집 매니저를 통해 획득하고 2척 편성 후 전투 실행.
- 앞줄이 살아 있는 상태에서 후방 (2, 0)을 선택하고, 같은 목표에 공격자 2척/갈고리 1개 확인.
- 공격자 비활성/재활성, 표적 사망, 새 표적 전환, 공격자 사망, 풀 반환 시 표시 정리 확인.
- 실제 일반탄은 활성 궤적 없음, 작살은 24점 이상 이동 경로 유지, 피격 벽 오브젝트 없음 확인.
- Game View 전체 UI 캡처: `Assets/QA/raider-full-ui.png`.
- 사망 클립에 습격함 그림 유지, 60% 재생 위치에서 셰이더 `_SinkProgress` 약 0.592 확인.

뽑기 확률 50%는 즉시 테스트를 위한 현재 데이터 값이다. 출시용 확률은 위 테이블에서 직접 조정한다. 별도 Inspector 연결 작업은 필요 없다.

## 그림 제작 기록

함선은 사용자 제공 원본 PNG를 복사했다. 발사체만 imagegen으로 원본의 넓은 회색 쇠 작살촉, 짧은 축, 굵은 윤곽선, 금색 고리와 오른쪽 방향을 참고해 투명 배경으로 분리 제작했다. 새 외부 패키지를 추가하지 않았다.

최종 사망 검증: 자연 상태 전환 후 Dead 이벤트 발생, _SinkProgress=1 확인. 호위함은 기존 회복 생명주기에 따라 활성 상태를 유지한다.

## 발사 후 장전 작살 수정

- 변경: HarpoonRaider.prefab, HarpoonRaider의 Idle/Move/Attack/Dead 클립, HarpoonRaider_Unloaded.png. C# 변경 없음.
- Visual/LoadedHarpoon은 실제 발사체와 같은 Sprite와 Scale을 사용한다. 발사대 뒤에서 렌더링하여 축이 발사대에 가려진다.
- Attack 시작 0~0.10초에 작살이 앞으로 장전되고, AttackStart 이벤트와 동일한 0.1666667초에 Renderer를 끈다. Idle/Dead에서도 숨김을 유지한다. 다음 Attack에서 다시 장전한다. 공격 속도에 따라 두 모션이 함께 빨라진다.
- 장전 위치/각도/크기는 Visual/LoadedHarpoon Transform에서, 위치 보간 및 표시 시점은 Attack 클립의 LoadedHarpoon 트랙에서 조절한다. ShootPos는 장전 완료 중심과 동일한 로컬 (0.32,-0.06,0)이다. 별도 연결 작업 없음.
- 검증: 이동, 발사 직전, 정확한 발사 프레임, 발사 이후, 대기, 다음 공격, 사망의 7개 상태 통과. 실제 전투에서 작살 비행 중 두 함선 모두 LoadedHarpoon.enabled=false이며 빈 발사대인 것을 확인했다.

이미지 제작은 내장 imagegen을 사용했다. 저장 위치: Assets/SB/Art/Sprites/Ships/HarpoonRaider_Unloaded.png. 입력은 기존 HarpoonRaider.png이며 최종 프롬프트는 아래와 같다.

> Use case: precise-object-edit. Edit target: the provided 1280x1280 transparent game ship sprite. Create the EMPTY/UNLOADED version of this EXACT same sprite for animation. Remove ONLY the exposed large gray barbed harpoon arrowhead and short gray shaft sticking out of the right end of the ballista (roughly x970-1235,y625-800). Keep the launcher: all wooden rails, curved bow limbs, ropes, metal/gold circular collars, pivot and stand intact. At the rightmost gray/gold collar make the now-empty launcher opening clearly visible, with no arrow, point, shaft, or projectile extending beyond it. Do NOT remove the wooden bow of the ship below it. Absolutely preserve the original ship hull, sail, mast, flag, outlines, texture, colors, pose, position, scale, and original transparent margins. Do not recenter or zoom. Match original pixel positions as closely as possible: same square canvas 1280x1280, ship original framing. Background must be truly transparent alpha, no black fill or checkerboard. Output only the one unloaded ship sprite, no comparison, no text.
