# 해양 크리쳐 V4 아트 및 피격 이미션

## 에셋

- `Assets/SB/Art/Sprites/SeaCreatures/ReefGlider_V4.png`
- `Assets/SB/Art/Sprites/SeaCreatures/ReefbackLeviathan_V4.png`

Built-in `image_gen`으로 생성했습니다. 현재 게임의 `ApprenticeGunship_Move_Sheet.png`와 `TransportShip.png`를 스타일 참고로 사용했습니다. 큰 둥근 형태, 굵은 자주색 외곽선, 단순한 명암, 크림색/붉은색/황토색을 공유하도록 디자인했습니다.

Sprite Editor 스킬이 요구하는 `ISpriteFrameEditCapability`가 현재 Unity 6000.0에 없어 프레임 여백 수정은 중단했습니다. 새 PNG를 전체 이미지 Single Sprite로 가져오고 기존 프리팹의 캔버스 월드 너비에 맞췄습니다. 원본 이미지 픽셀과 투명 영역은 그대로 보존했습니다.

## 피격 몸체 발광

`SeaCreatureHitFeedback`이 실제 피해를 받았을 때 `_HitEmissionFlash`를 MaterialPropertyBlock으로 갱신합니다. `SeaCreatureSurfaceBody.shader`에서 어두운 외곽선을 포함한 스프라이트 전체에 HDR 색을 더합니다. 투명 영역과 수면 클리핑은 유지합니다. 기존 타격광/물방울도 같이 재생합니다.

| Inspector 항목 | 값 | 용도 |
|---|---:|---|
| Emission Color | (1, 1, 1, 1) | 흰색 발광색 |
| Emission Intensity | 1.8 | 가산 이미션 강도 |
| Emission Attack Seconds | 0.02 | 최대 밝기까지 걸리는 시간 |
| Emission Release Seconds | 0.14 | 원래 밝기로 돌아오는 시간 |
| Emission Body Parts | 비움 | 별도 꼬리/턱 렌더러를 추가할 때만 명시 할당 |

유영/침수의 MPB 값과 공존하며 머티리얼 인스턴스를 생성하지 않습니다. 일시정지 중 시간 진행이 멈추며, 연속 피격은 재점등하고 풀 반환 시 0으로 초기화합니다.

## 확인 결과

- 새 두 스프라이트가 프리팹과 테스트 HUD 아이콘에 연결되었습니다.
- C# 컴파일 성공, 몸체 셰이더 지원됨/그래픽 컴파일 메시지 없음.
- 0 피해/회복에서 발광 없음. 두 종의 피해에서 MPB 발광값 0.862/0.962 확인.
- 발광 자연 종료, 치명 타격 발광, 풀 초기화 확인.
- 실제 자동 포격에서도 발광 MPB 값 0.858과 화면 확인.
- 최종 Console 신규 오류/경고 없음.

기록과 화면: `Artifacts/SeaCreatures/CreatureEmissionV4Verification.txt`, `CreatureEmissionV4Combat.txt`, `CreatureEmissionV4Combat.png`, `CreatureDesignV4Combat.png`.

변경 파일: 두 V4 PNG와 importer 메타, 두 크리쳐 프리팹, `SeaCreatureHitFeedback.cs`, `SeaCreatureSurfaceBody.shader`, `SeaCreatureTest.uss`의 이미지 참조. 기존 미저장 씬 변경이 있어 씬 저장은 수행하지 않고 프리팹과 에셋만 저장했습니다.

## 암초 뱀장어 프롬프트

Use case: stylized-concept game production sprite.
Create ONE redesigned hostile reef eel for the exact casual 2D pirate game style in the two attached SHIP references. The first reference is a 4-frame ship sheet: use its drawing STYLE ONLY, do NOT output a sheet. Match the very thick deep burgundy outlines, large simple rounded shapes, chunky proportions, two crisp cel-shading tones, warm cream highlights and brick-red accents. The creature must feel drawn by the same artist as the little apprentice cannon ship.
Design a squat compact sea eel with a big grumpy rounded head facing LEFT, one clearly visible half-lidded angry yellow/cream eye, two large simple ivory fang shapes in a broad jaw, SHORT low curved S-shaped tail extending to the right, a simple torn-looking dark brick-red dorsal fin and warm olive small side fin. Main skin muted deep slate teal, broad warm cream underside. Head-to-tail proportion about 45:55, overall width-to-height about 2.2:1. Keep head low so it looks like it swims at the surface, not a rearing dragon. No slender high fantasy neck, no horns or scales or elaborate tiny markings, no realistic anatomy, no glossy rendering. Broad distinctive silhouette readable at 100px wide.
Whole single full-body creature isolated with real alpha transparency, NO environment, water, bubbles, foam, cast shadow, aura, glow, text, swatches or labels. No pirate clothes, hat, boat parts or weapons. Face LEFT, tail RIGHT. Generous transparent padding on every side and nothing cut off. This sprite will get waterline masking, tail animation and hit emission inside Unity; do not paint effects into it.

## 산호 갑각수 프롬프트

Use case: stylized-concept game production sprite.
Create ONE redesigned heavy armored sea creature for the exact casual 2D pirate game style in the two attached SHIP references. The first is a 4-frame ship sheet: use its drawing STYLE ONLY, do NOT output a sheet. Match its bold deep burgundy outline thickness, short chunky proportions, simple large color regions, 2-tone cel shading and warm cream/golden ochre/brick-red/purple-gray palette. This creature must feel drawn by the same artist as the little apprentice cannon ship, not a fantasy card illustration.
Design a stout hostile reef crab-turtle, facing LEFT in the same slight three-quarter side view as the ship. Huge blunt ivory-and-slate-gray snapping beak with just two big teeth, one expressive angry cream eye, broad low shell made of only THREE large warm burnt-ochre carapace plates with golden edges; muted purple-gray skin/flippers matching the cannon iron palette, cream throat, two blunt red-orange coral nubs and just ONE simple cream barnacle on the shell. Two short paddle-like flippers, short broad body and tiny tail to right. Silhouette should resemble a charging rock with a big face, width-to-height around 1.7:1. Natural creature anatomy, not literal boat, no cannons/bolts/metal plates/clothes. No spikes everywhere, no fine scales/dots, no complex shell tiling, no shiny 3D render, no detailed fantasy beast.
Single full body isolated on genuinely transparent background, hard clean alpha cutout. NO water, bubbles, foam, ground, shadow, haze, aura or glow; no text or labels. Face LEFT, tail RIGHT. Generous transparent padding and all flippers fully visible. Unity will supply all water and emission effects.

