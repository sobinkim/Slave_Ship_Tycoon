# 파쇄선 구현 / 2026-09-29

기획: https://app.notion.com/p/3a12a871b9c1819ea331fe626d820e39
사용자 제공 PNG를 수정 없이 복사했다. 기존 IroncladDefender 프리팹의 전투, 물결, 체력바, 피격, 회피, 보호 효과 수신 구조를 재사용한 독립 프리팹이다. 방어함의 아군 보호 제공 컴포넌트는 새 프리팹에서 제거했다.

## 에셋과 조절 위치
- Assets/SB/Prefabs/PlayerShips/CrusherShip.prefab
  - Ship: 이름 파쇄선, EscortShip / Piercer, 보호 중인 적 직접 공격은 끔.
  - EntityStatCompo / ShipStatCompo / Stat Container: 체력 120, 공격력 16.
  - AboutBattle/ArcProjectileAttackCompo: Attack Cooldown 1.4초, CrusherStake 말뚝 프리팹과 기존 비행 설정.
  - AboutBattle/TargetSelector: Front/None → Middle/None → Back/None, Selection Mode = HighestCurrentHealth.
  - 전열 → 중열 → 후열 순서로 선택하고 각 열 안에서 현재 체력이 가장 높은 적, 동률은 가까운 적을 선택한다. 보호 효과에 의해 직접 공격 불가능한 적은 기존과 같이 제외한다. 지휘 명령은 기존처럼 우선 적용된다. 획득한 표적 유지/사망 후 재선택은 기존 공격 생명주기를 따른다.
  - Visual: 크기 (1.1,1.4,1). X만 줄여 더 짧고 두꺼운 비율로 변경. ShootPos는 장전 말뚝 중심 (.29,-.025,0), MuzzleVfx는 발사대 입구 (.28,-.035,0).
- Assets/SB/Art/Sprites/Ships/CrusherShip.png: 원본 알파/이미지 유지, PPU 1600, Bilinear, 무압축, 밉맵 없음.
- Assets/SB/Art/Materials/Ships/CrusherHull.mat: Waterline -.265, Sink Distance .9.
- Assets/SB/Animations/Ships/CrusherShip/: Idle/Move/Attack/Dead, 전용 컨트롤러.
  - 기존 클립의 반동, 포구 화염/연기, 수면 아래 침몰과 이벤트 타이밍을 유지했다.
  - 모든 스프라이트 키는 파쇄선 그림으로 교체했다. 공격 .5초 / 발사 .166667초 / 종료 .458333초, 사망 .75초 / Dead 이벤트 .6875초.
- Assets/SB/Data/PlayerFleet/EscortShips/Escort_CrusherShip.asset: 고급(Uncommon), stable ID escort_crusher_ship, 아이콘/프리팹 연결.
- EscortShipCatalog: 마지막 항목에 추가. 기존 합성 결과 순서 유지.
- EscortGachaTable: Weight 25 추가, 총 225이므로 현재 약 11.11%. 기존 항목의 가중치는 유지했으므로 전체 확률은 새 합계 기준으로 계산된다.

체력/공격력/공격 간격/등급/뽑기 가중치는 기획서에 세부 값이 없어 정한 조절 가능한 초깃값이다.

## 코드와 호환성
- TargetSelector.cs: SelectionMode enum과 고체력 선택 추가. 기본값 0은 기존 FormationDistribution이므로 기존 함선의 분산 선택은 유지된다.
- FleetAttackPreview.cs: 선택 모드와 규칙에 맞춰 공격 우선순위 표시.
- CrusherTargetingTests.cs: 방어함 우선, 현재 체력 비교, 거리 동률 처리, 사망/비활성 제외, 지휘 명령 우선 7개 테스트.

## 검증
- 기존 타겟 테스트 17개 + 새 테스트 7개, 총 24개 통과.
- 컴파일 실패 없음. 프리팹 누락 스크립트/깨진 참조 없음. 카탈로그 및 뽑기 테이블 검증 통과.
- 2,000회 표본 추첨에서 파쇄선 233회 등장.
- 실제 EscortGachaManager.TrySummon으로 11회째 획득, PlayerFleetManager.TryPlaceEscort로 편성 성공.
- Play Mode 별도 전투에서 기본 체력 120, 피해량 16 확인. 발사 이벤트 10회, 실제 포탄 비행 및 적 처치 확인.
- 실제 Dead 상태와 Sink Progress 1 확인. 재사용 후 IsDead=false, 체력 120, Sink Progress 0, 파쇄선 그림 및 크기 복구.
- QA/CrusherShipAnimationPreview.png: 위 대기/이동/발사/연기, 아래 침몰 3시점/대기 복귀.
- 현재 SampleScene의 StageEncounterDirector에는 Enemy Fleet Root/Destination 참조가 비어 있어 전체 스테이지 진행은 검증하지 못했다. 해당 기존 씬 설정은 변경하지 않았다. 별도 전투 테스트의 초기 본선 없는 이벤트에서 지휘 스탯 경고가 발생했으며 새 함선의 컴파일/참조 오류와는 구분된다. Unity MCP 자체 파일 공유 충돌 로그도 관찰됐다.
- 테스트 재화/보유 함선/편성은 Play Mode 종료로 제거했다. 씬 파일은 변경하지 않았다.

새 파쇄선 자체에 추가 Inspector 연결은 필요 없다.


## 말뚝 발사 수정 / 2026-09-29
- CrusherShip_Unloaded.png는 말뚝이 빠진 전투 선체이다. 원본 CrusherShip.png는 모집 아이콘으로 유지한다.
- CrusherStake.png와 Prefabs/Projectiles/CrusherStake.prefab을 추가했다. 장전 상태와 실제 비행이 같은 말뚝 이미지를 사용한다. 기존 NormalProjectile의 명중/회피/풀 반환 흐름을 재사용하며 새 런타임 스크립트는 없다.
- Visual/LoadedStake는 발사대 뒤에서 렌더링한다. Move에는 장전 상태, Attack의 0~0.1초에 앞으로 장전하고 AttackStart 이벤트와 같은 0.166667초에 숨긴다. Idle/Dead에서는 숨기고 다음 Attack에서 다시 장전한다.
- 일반 포탄 대신 말뚝 자체가 비행 방향으로 회전하며 날아간다. 포구 화염과 작살함 전용 이동 경로 선은 사용하지 않고 기존 발사 연기는 유지한다.
- LoadedStake의 위치/각도, Visual의 X/Y 스케일, CrusherStake 프리팹의 Scale, Attack의 LoadedStake 위치/표시 트랙을 Inspector와 Animation 창에서 조절한다. 현재 Visual (1.1,1.4,1), LoadedStake 로컬 Scale (1,1,1), 말뚝 발사체 Scale (1.1,1.4,1).
- 실제 Play Mode에서 반복 발사, CrusherStake 이미지의 비행, 비행 중 장전 말뚝 숨김을 확인했다. 8시점 샘플에서 발사 전/정확한 발사 시점/발사 후/대기/재장전/침몰/이동 복구 확인. 컴파일 실패 및 Console 오류/경고 없음.
- 미리보기: QA/CrusherStakeAnimationPreview.png. 위: 이동/장전/발사/비행, 아래: 대기/재장전/침몰/이동 복구.
- 내장 imagegen으로 두 투명 이미지를 제작했고 생성 알파를 그대로 보존했다. 별도 패키지 없음.

### 이미지 제작 프롬프트
Unloaded hull:
Use case: precise-object-edit. Edit target: supplied transparent 1536x1024 game ship sprite. Make the EMPTY/UNLOADED version of EXACTLY this ship. Remove ONLY the pointed gray steel stake sticking out to the right of the large silver circular muzzle collar (roughly x1200-1510 y425-635). Keep the entire cylindrical launcher body, gold bands, large silver muzzle collar, wood mount, sail, flag, rear spikes and ship hull unchanged. The large silver muzzle collar should now have a dark empty opening with no pointed stake protruding. Keep the original ship pixel positions, scale, framing, and 1536x1024 canvas; do not recenter or expand the hull. Preserve original thick dark burgundy outlines and colors. Truly transparent alpha background, no black background, no checkerboard, no text. Output only the single unloaded ship sprite.

Stake:
Use case: background-extraction. Input reference is a cartoon game ship. Extract ONLY the single large pointed steel STAKE protruding from the launcher muzzle on the far right, without the ship, cannon barrel, silver collar or gold rings. Make a standalone projectile sprite. This is a short thick solid hexagonal gray steel punch with a broad faceted triangular pyramid point, dark burgundy outline and pale gray upper faces matching reference. Include a short straight shaft on its left to slide inside launcher. No barbs, no fins, no wood, no rope, no fire. Orient exactly horizontally pointing RIGHT. Center this one stake on a transparent 1536x1024 canvas; the stake should occupy about 1100x420 pixels with clear transparent margins. Preserve thick smooth dark burgundy contours and the reference color/shading style. True transparent alpha background. No cast shadow, glow, black fill or checkerboard. Output one isolated steel stake.


## 짧은 말뚝과 비행 보호막 / 2026-09-29
- 사용자 최종 참고 이미지(20195967)의 고리+몸체 형태로 변경. 후방 막대 없이 짧고 두꺼운 형태다. 기존 긴 말뚝 대비 외곽 길이 약 30% 감소.
- 새 이미지: Assets/SB/Art/Sprites/Projectiles/CrusherStake_Short.png. 장전 LoadedStake와 실제 CrusherStake 발사체에 동일한 스프라이트를 연결했다. 기존 함선 X/Y 스케일 (1.1,1.4) 유지.
- 비행 프리팹에만 Assets/SB/Art/Materials/Ships/CrusherStakeAura.mat 적용. 장전 상태에는 기본 스프라이트 머티리얼을 유지한다.
- 셰이더: Assets/SB/Art/Shaders/CrusherStakeAura.shader. 붉은 다각형 외곽광, 밝은 안쪽 테두리, 반투명 육각 패턴, 약한 밝기 맥동. 같은 SpriteRenderer에서 그리므로 회전/정렬/명중/미스/풀 반환 때 탄과 함께 처리되며 별도 런타임 스크립트나 이벤트를 추가하지 않았다.
- Inspector: Aura Color, Rim Highlight, Edge Width, Glow Width/Opacity, Shell Opacity, Hexagon Density/Line Width/Opacity, Pulse Amount/Speed. 외곽 위치/크기는 Shield UV Center/Half Size에서 조절한다.
- 이 셰이더는 전체 텍스처 UV와 여백을 사용한다. CrusherStake_Short는 FullRect Sprite이며 SpriteAtlas에 패킹하지 않는다.
- 실제 공격 이벤트로 발사된 탄에만 Aura 머티리얼 적용, 장전된 탄에는 미적용, 비행 시 장전 탄 숨김 확인. 8시점 장전/발사/대기/재장전/침몰 검증. 셰이더 컴파일 오류 없음, Unity Console 오류/경고 없음.
- QA/CrusherStakeAuraPreview.png: 최종 8시점 미리보기. 모든 변경은 새 파쇄선과 발사체 및 전용 시각 에셋에 한정된다.

### 최종 이미지 생성 기록
내장 imagegen 사용. 최종 참고: codex-clipboard-20195967-b40b-47fc-9593-3625d239fe7d.png. 생성 알파를 그대로 유지하여 CrusherStake_Short.png로 저장.
Prompt:
Edit the provided exact projectile sprite. Remove checkerboard background to real transparent alpha. Keep the entire object CONNECTED, matching the reference: silver base collar directly attached to a faceted steel pointed body, dark burgundy thick outline. Nothing protrudes behind the left collar: NO rear rod, NO rear bar, NO shaft extension. SHORTEN total horizontal length by about 30%, preserve vertical thickness. Final silhouette should be SHORT and chunky with length about twice its height. Point RIGHT horizontally. Preserve the reference straight beveled body and simple triangular point. Do not add a separate diamond head. Center one projectile on a transparent 1536x1024 canvas with ample margins; visible object occupies roughly 800x400 pixels. No glow, no shadow, no words. One connected short metal stake only.


## 발사 후 순차 코팅 연출 / 2026-09-29
- 기존 효과는 셰이더만으로 전체가 즉시 보이는 방식이었다. 이제 CrusherStakeCoating.cs가 실제 비행 중 경과 시간을 각 탄의 MaterialPropertyBlock _CoatingProgress에 전달한다.
- CrusherStake.prefab에 Projectile / SpriteRenderer 참조 연결. 발사 후 Delay Seconds .045초, Duration Seconds .26초 동안 앞쪽 촉부터 뒤쪽 고리 방향으로 씌워진다. Inspector에서 각각 수정 가능하다.
- 공용 머티리얼을 런타임 변경하거나 복제하지 않는다. 탄마다 독립 진행도이며 풀 반환/재활성/재사용 시 0으로 초기화한다. 미스 곡선이 새로 시작되어도 코팅 시간은 계속 흐른다.
- 셰이더의 합성 순서를 수정했다. 금속 뒤에 가려지던 면을 금속 위에 반투명으로 합성한다. Shell Opacity .32, Hexagon Opacity .65, Hexagon Line Width .035, Density 2.2. 외곽 두께 .05, 진행 경계 Softness .16. 면/육각 무늬가 작은 탄에서도 보이도록 조정했다.
- 장전 말뚝은 기존 기본 머티리얼을 사용하므로 코팅 효과가 없다.
- QA/CrusherStakeCoatingProgress.png: 왼쪽부터 0%,25%,60%,100% 진행도. 현재 코팅 방식의 기준 미리보기이며 앞선 즉시 발광 미리보기보다 최신이다.
- Play Mode에서 실제 풀 생성→발사→중간 진행도→1 도달→반환→동일 인스턴스 재사용을 검증했다. 시작/반환/재사용 진행도 0, 중간 진행도와 완성 1 모두 확인. 스크립트 및 셰이더 컴파일 정상, Console 오류/경고 없음.

검증 보충: 일반 속도에서 시작/완성/반환/재사용을 검증하고, Editor 프레임 간격 때문에 중간 상태를 놓치지 않도록 0.1배속 재생에서도 0~1 중간 진행도와 완성을 확인했다. 검증 후 Time.timeScale=1 복구 및 Play Mode 종료.

## 전열 우선 표시 / 2026-09-29
- 사용자 요청에 따라 파쇄선 TargetSelector를 Front/None → Middle/None → Back/None으로 변경했다. 해당 열에 유효한 표적이 있을 때 현재 체력이 가장 높은 적을 고르고, 없으면 다음 열을 확인한다.
- FleetAttackPreview의 고체력 모드 문구에 첫 규칙의 열 이름을 반영했다. 파쇄선 선택 시 적 전열만 강조하며 `전열 · 고체력 우선`으로 표시한다.
- 기존 UI 복제본으로 Refresh를 실행해 문구와 세 열의 색상(전열 994A1FFF, 중열/후열 212B36FF)을 확인했다. 검증 오브젝트는 제거했다. 컴파일 실패 없음, 현재 Console 오류/경고 없음.
- Inspector 조절 위치: CrusherShip/AboutBattle/TargetSelector의 Target Rules와 Selection Mode. 씬 연결 변경 없음.

## 코팅 방향 변경 / 2026-09-29
- CrusherStakeAura.shader의 진행 마스크를 반전하여 뾰족한 앞부분(+X)부터 뒤쪽 고리(-X)까지 채운다. 발사 지연/지속 시간은 기존 값을 유지한다.
- 셰이더 재임포트 후 ShaderUtil 검사: 오류 없음, 컴파일 메시지 없음. 기존 QA/CrusherStakeCoatingProgress.png는 변경 전 방향의 기록이다.

## 명중 화면 흔들림 / 2026-09-30

- CrusherStake.prefab의 CrusherStakeImpactShake가 NormalProjectile의 실제 표적 명중 신호를 받아 화면 흔들림을 요청한다. 물에 떨어진 탄은 요청하지 않는다.
- SampleScene의 Main Camera에 CameraImpactShake를 연결했다. 프리팹이 씬 카메라를 직접 참조할 수 없어 기존 EventBus로 두 컴포넌트를 연결했다.
- 말뚝 프리팹 Inspector의 Shake Distance Units(월드 거리, 기본 0.045), Shake Duration Seconds(기본 0.16초)에서 강도와 시간을 조절한다. 두 값 중 하나를 0으로 하면 흔들림이 꺼진다.
- Main Camera의 CameraImpactShake는 흔들리는 중 들어온 요청을 무시하고, 종료 후 Cooldown Seconds(기본 0.12초) 동안 재발동을 막는다. 여러 말뚝의 동시 명중이 강도나 지속 시간을 누적하지 않는다.
