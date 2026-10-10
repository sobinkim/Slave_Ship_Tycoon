# 주 운송선

## 연결

- 프리팹: Assets/SB/Prefabs/PlayerShips/TransportMainShip.prefab
- 기존 TestPlayerFleetLoadout의 Main Ship Prefab을 새 운송선으로 교체했습니다.
- SampleScene의 StageSpawnManager > Cargo Manager에 장면의 기존 CargoManager를 연결했습니다.
- MainShip > Cargo Visualizer는 같은 프리팹의 ShipCargoVisualizer를 명시적으로 참조합니다.
- 이동/대기 애니메이션, 업그레이드, 지휘관 스탯, 피격 표시와 물살은 기존 시스템을 사용합니다. EntityHealth는 기존 함대/지휘관 연결을 위해 유지하지만 MainShipDestruction이 일반 피해를 0으로 처리합니다. 메인선의 패배는 체력이 아닌 마지막 일제사격 연출로 결정됩니다.
- 새 프리팹에는 공격 컴포넌트와 TargetSelector가 없습니다. Ship의 기존 공격 진입 조건이 false를 반환하므로 발사하지 않습니다.

## 화물 표시

금화 상자와 함선 재료 상자를 합친 실제 Amount 1개마다 궤짝 1개를 표시합니다. 무게와 적재 가능 여부는 기존 CargoManager가 결정하며 표시용으로 수량을 제한하지 않습니다. 두 종류는 같은 궤짝 스프라이트를 사용합니다.

StageSpawnManager가 생성한 함선에 장면의 CargoManager를 전달합니다. 적재 변경 이벤트를 구독하고, 생성/재활성화 때도 현재 데이터를 읽습니다. 판매·하역·ClearCargo 시 표시 수량이 감소합니다. 숨긴 궤짝 오브젝트는 다음 적재에 재사용합니다.

기본 5열이 왼쪽부터 차고 다음 단으로 쌓입니다. 15개는 3단이며, 더 많은 수량은 위로 이어집니다. 운송 업그레이드나 화물 무게를 임의로 변경하지 않습니다.

## Inspector에서 조절

- Visual > CargoDeck > Column_01~05: 각 열의 시작 위치. 함선 Visual 기준 로컬 좌표입니다. 열을 추가/삭제할 때 ShipCargoVisualizer > Column Base Points 배열도 직접 연결합니다.
- ShipCargoVisualizer > Stack Local Offset: 단 사이의 로컬 위치 간격. 기본 Y=0.066.
- TransportCargoCrate.prefab > Transform Scale: 궤짝 가로/세로 크기. 기본 (0.095, 0.2, 1); 세로를 기존 대비 약 48% 키웠습니다.
- Drop Seconds / Drop Local Height / Drop Progress Curve: 궤짝 낙하 시간·높이·진행 곡선.
- Load Stagger Seconds: 한 번에 여러 개 증가할 때의 시작 간격.
- Settle Seconds / Landing Scale Multiplier: 안착할 때 잠깐 눌리는 시간·비율.
- Crate Sorting Order=1: 난간 안쪽에 표시하는 순서. CargoFrontRailMask는 0~1 범위에서 앞쪽 난간에 겹치는 궤짝 부분을 가립니다.
- Visual Transform Scale: 전체 그림과 적재 위치를 함께 조절합니다. 기본 (2.15, 3.1, 1). 애니메이션 Y 기준점을 함께 올려 기존 수면 높이를 유지합니다. SampleScene의 MainShipSpawnPoint X=-2.01, 호위함 두 열 X=-0.8/0.15로 기존 간격을 유지하며 화면 안에 배치합니다.

## 에셋

- 기존 생성 이미지: Assets/SB/Art/Sprites/Ships/TransportShip.png
- 새 궤짝 이미지: Assets/SB/Art/Sprites/Cargo/TransportCargoCrate.png
- 궤짝 프리팹: Assets/SB/Prefabs/Cargo/TransportCargoCrate.prefab
- 전용 Animator와 클립: Assets/SB/Animations/Ships/TransportMainShip/
- 난간 마스크 Sprite: Assets/SB/Art/Sprites/Ships/TransportFrontRailMask.asset. 원본 이미지의 하부 영역을 사용하는 Unity Sprite입니다.

궤짝 이미지는 built-in image_gen으로 생성했습니다. 사용 프롬프트: 운송선 이미지를 그림체 참고로 사용한 단일 닫힌 목재 운송 궤짝, 얕은 측면 시점, 굵은 자주빛 외곽선, 주황갈색 목재, 은보라색 보강대와 금색 모서리, 작은 크기에서 읽히는 단순한 음영, 수평 바닥, 투명 배경.

## 확인 결과

Unity 컴파일 완료, 새 컴파일 오류 없음. 실제 출항에서 새 프리팹과 이미지 사용을 확인했습니다. 금화 15개 적재 및 16번째 적재 거절, 15→14 하역, 화물 비우기, 금화 10개+재료 4개 혼합 표시, 풀 반환·재생성 시 현재 14개 복원, 임시 적재 용량을 높였을 때 16개/4단 표시, 연속 적재·비우기·재적재를 확인했습니다. 공격 가능/타겟 확보/공격 진입은 모두 false입니다. 검증용 용량·화물·무적 상태는 Play Mode 종료로 제거했습니다.

기존 TransportEquipmentManager의 MerchantLedger 아이콘 누락 오류는 이번 변경과 별개로 계속 발생합니다.

## 궤짝 생성에 사용한 전체 프롬프트

Built-in image_gen, transparent_background=true, reference=TransportShip.png.

Use case: stylized-concept. Asset type: a single reusable 2D cargo crate sprite for a Unity side-view transport ship. The reference image is STYLE AND COLOR REFERENCE ONLY, do not draw any ship. Draw EXACTLY ONE compact rectangular CLOSED WOODEN CARGO CRATE, a shipping box, NOT a rounded treasure chest. Camera: nearly side-on with a shallow visible top and a very narrow RIGHT side face, matching the transport ship's shallow top visibility. Wide rectangular proportions approximately width 1.35 height 1.0. Flat horizontal bottom so identical crates can stack naturally. Thick smooth dark burgundy/plum outlines matching the reference, orange-brown wooden planks, two broad muted silver-lavender metal reinforcing straps with two simple bright rivets, small gold corner accents. Clean bold simplified polygonal highlights and shade shapes, saturated warm palette, identical crisp cartoon mobile game art style and detail density to the ship. Simple strong silhouette readable when tiny. Full crate centered with very little transparent padding. Transparent alpha background, no ground, no cast shadow outside crate, no glow, no text, no symbols, no contents, no extra objects, no sheet, no border. Single isolated finished game asset.


## 메인선 최후 일제사격

- SampleScene > BattleManager > MainShipDefeatDirector가 전투 데이터의 호위함 생존 여부를 확인합니다. 호위함이 하나도 없고 살아 있는 적이 있을 때 한 번 실행합니다.
- 순서: 적 일반 공격 중단 → 기존 비행 포탄 취소 → 준비 대기 → 생존 적마다 1발을 같은 프레임에 발사 → 실제 명중 콜백이 모두 도착하면 함선 표시를 숨기고 즉시 기존 패배 이벤트. 호위함 없이 전투를 시작해도 같은 흐름입니다.
- 최후 포탄은 기존 NormalProjectile을 사용합니다. ProjectileHitMode.Guaranteed로 회피를 적용하지 않고 피해량은 0입니다. 모든 포탄의 비행 시간을 동일하게 설정합니다. 일반 포탄의 회피/피해 처리는 기존 기본값을 사용합니다.
- MainShipDestruction은 패배 시 시각 오브젝트 숨김/풀 재출항 복원을 담당합니다. MainShipDefeatDirector는 전투 종료 타이밍만 담당합니다. MainShip.OnDeathEvent를 통해 기존 BattleManager 패배 흐름과 연결합니다.
- 전투 초기화·종료·컴포넌트 비활성화 시 코루틴을 중단하고, 추적 포탄의 Resolved 구독을 해제한 뒤 풀로 반환합니다. SpawnGeneration으로 이전 출항의 메인선 참조를 사용하지 않도록 합니다.
- MainShip 프리팹의 MainShip > Destruction 참조와 MainShipDestruction의 Owner / Presentation Roots를 연결했습니다. ShipHullFeedback > Show Health Bar=false입니다.

### Inspector 조절값

- MainShipDefeatDirector > Preparation Seconds=0.35: 공격 중단 후 발사까지 대기.
- Salvo Flight Seconds=0.7: 모든 적 포탄의 비행 시간.
- Salvo Arc Height=0.65: 포물선 높이.
- Salvo Projectile Prefab / Salvo Muzzle Effect Prefab / Enemy Attack State Name: 사용할 포탄, 발사 효과, 적 Animator 상태 이름. 새로운 적 프리팹에는 ArcProjectileAttackCompo와 명시적 Fire Point가 필요합니다.

### 추가 확인 결과

Unity 컴파일 성공. 실제 전투에서 적 8척의 발사 프레임과 0.7초 비행 시간이 같고, 8발 명중 완료 후에만 폭발함을 확인했습니다. 일반 피해로 메인선이 죽지 않으며 체력바/공격이 없습니다. 비행 중 전투 초기화는 포탄과 콜백을 취소하고 패배 이벤트를 발생시키지 않습니다. 호위함 없이 새 전투를 시작하면 새 일제사격이 실행되고, 폭발 연출 후 패배 이벤트는 1회만 발생했습니다. 검증 결과: QA/TransportFinalSalvoVerification.json. 화면 확인: Assets/QA/TransportMainShip/LargerShipTallerCrates.png.


## 폭발 연출 제거

사용자 요청에 따라 메인선 폭발 파티클, 폭발 시 카메라 흔들림, 폭발 대기 시간을 제거했습니다. 최후 일제사격 전탄 명중 후 함선 표시를 숨기고 즉시 패배 처리합니다. 앞 절의 폭발 관련 검증 기록은 제거 이전 기록입니다. 기존 피격 효과와 적 동시 발사는 유지합니다.


## 패배 전환 대기

전탄 명중 후 메인선 표시를 숨기고 On Defeat Presentation 이벤트로 약한 화면 흔들림을 실행합니다. MainShipDefeatDirector > Defeat Hold Seconds(기본 0.8초) 동안 장면을 유지한 뒤 사망 이벤트와 패배 처리를 진행합니다. 폭발 효과는 없습니다. 대기 중 전투 초기화/종료 시 기존 ResetSequence가 코루틴을 취소합니다. 메인선 프리팹의 On Defeat Presentation에는 PlayDefeatShake를 연결하고, On Death Event의 기존 흔들림 연결은 옮겼습니다.


## 파도 움직임 강화

TransportMainShip_Idle/Move 클립의 Visual 로컬 Y 위치와 Z 회전 곡선을 양방향 루프로 변경했습니다. 대기: 상하 ±0.012, 회전 ±1.2도. 이동: 상하 ±0.018, 회전 ±1.8도. 기존 수면 기준 Y=0.24091664와 이동/전투 로직은 유지합니다. Visual 하위 궤짝과 난간 마스크도 함께 움직입니다. Animation 창에서 두 클립의 Transform > Position Y / Rotation Z 곡선을 조절할 수 있습니다.


## 정지 시 물살 페이드 수정

공격 컴포넌트가 없는 메인선은 전투 시작 때 Move 상태를 끝내는 기존 공격 컴포넌트 처리를 받지 못했습니다. MainShip이 자신의 BattleStartEvent를 구독하고 Idle로 전환하도록 수정했습니다. ShipMoveState.Exit에서 MOVE=false가 되어 ShipWakeEffect의 기존 페이드가 작동합니다. OnDisable에서 구독을 해제합니다. 물살의 Fade Out Seconds는 현재 0.3초이며 메인선 프리팹 > Presentation > ShipWaterWake에서 조절합니다.


## 화물 스타일과 비율 교체 (현재 설정)

새 이미지 Assets/SB/Art/Sprites/Cargo/TransportCargoCrate_V2.png를 TransportCargoCrate 프리팹에 연결했습니다. 기존 이미지와 아이콘 참조는 보존합니다. 함선과 같은 굵은 자주색 윤곽선, 주황갈색 목재, 단순한 은보라색 보강대로 묘사를 줄였습니다.

궤짝 Transform Scale=(0.11163, 0.07742, 1)은 함선 Visual Scale=(2.15, 3.1, 1) 아래에서 최종 X/Y 배율이 각각 약 0.24로 같아지도록 명시적으로 설정했습니다. 부모 Visual의 크기를 바꾸면 궤짝 비율도 함께 확인하고 해당 프리팹 값을 조절해야 합니다. 런타임 자동 보정은 없습니다. Column_01~05의 Y=-0.17056, Stack Local Offset Y=0.034로 5열 3단 적재를 맞췄습니다. Landing Scale Multiplier=(1,1,1)로 안착 시 눌림을 제거했고 낙하 연출은 유지합니다.

Editor 별도 미리보기 장면에서 15개 적재 모습을 렌더링했습니다: Assets/QA/TransportMainShip/CargoStyleV2.png. 실제 게임 씬을 변경하지 않는 정적 에셋 미리보기입니다. 새 스크립트 추가/변경 없음.

생성 도구: built-in image_gen, transparent_background=true. 원본 함선을 스타일 참고로 사용. 전체 프롬프트:
Use case: stylized-concept. Asset type: ONE reusable transparent 2D cargo crate sprite for the reference ship in a Unity side-view game. Reference image is STYLE AND PALETTE ONLY, do not draw a ship. Draw exactly ONE CLOSED SQUARE WOODEN SHIPPING CRATE, width approximately 1.08 times height, intentionally designed upright at this natural proportion, not a stretched treasure chest. Match the ship's thick smooth dark burgundy outlines, orange-brown wooden hull colors, simple broad angular cel-shaded surfaces and very LOW detail density. Front face has just TWO broad wooden horizontal boards with one dark seam; two chunky muted lavender-gray vertical metal straps, ONE large round silver rivet per strap, tiny restrained gold corner hardware. No thin highlights, wood grain, scratches or repeated gold studs. Thick readable contour and large simple shapes. Almost straight side view with only a shallow top face and a very narrow right face, flat horizontal bottom and top for stacking. No protruding lid, no feet, no handles, no hinges, no rounded lid, no bevel-heavy shine. Natural square shipping box silhouette that remains readable at 50px. Isolated single object centered and filling canvas with small transparent margin. Actual transparent background, no shadow outside silhouette, no glow, no text, no water, no ground.
