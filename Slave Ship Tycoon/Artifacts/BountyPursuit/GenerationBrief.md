# 추적전 함선 이미지 제작 기록

사용 도구: 내장 image_gen. 기존 프로젝트의 함선 PNG를 각 역할의 디자인·시점·묘사 수준 참고 이미지로 사용했습니다. 새 그림을 기존 프리팹·원본 아트 위에 덮어쓰지 않고 별도 폴더에 저장했습니다.

## 공통 제작 프롬프트

Create one isolated transparent 2D game ship sprite matching the supplied existing ship: slightly elevated side view, squat cartoon proportions, chunky deep-plum outlines, orange wooden planks, gold fittings, muted grey armor, simple flat painterly highlights. The hull's raised bow points RIGHT; a stern-mounted weapon on the LEFT points LEFT and fires backwards at pursuers. The red sail is filled toward the RIGHT with consistent rigging. Fully visible ship, wide 3:2 canvas, no UI, labels, health bar, water, wake, background, shadow or glow.

## 역할별 지시

- MainPirate: broad two-deck main pirate ship, two large crimson sails, cream skull medallion, gold captain cabin details and an aft-facing cannon. Clearly more imposing than escorts while matching the existing cartoon family.
- PursuitDefender: match EnemyIroncladDefender; retain riveted grey armor and heavy hull, move cannon to the left stern, retain right-facing reinforced bow.
- PursuitInterceptor: match InterceptorShip; retain narrow interception gun and circular sight, move gun to left stern, crimson sail and ivory interception emblem. A second edit changed only the ivory sail to crimson and its dark emblem to ivory.
- PursuitRaider: match EnemyHarpoonRaider; retain slim hull and wooden harpoon launcher, move launcher to left stern and face left. A second edit removed the baked loaded harpoon so the existing separate loaded-harpoon attachment can animate.
- PursuitSmoke: match EnemySmokeSupportShip; retain red/gold smoke mortar and smoke-cloud emblem, move launcher to left stern and angle it left/upward.

## 프로젝트에서 사용하는 최종 파일

- Assets/SB/Art/Sprites/Ships/Pursuit/MainPirate.png
- Assets/SB/Art/Sprites/Ships/Pursuit/PursuitDefender.png
- Assets/SB/Art/Sprites/Ships/Pursuit/PursuitInterceptor.png
- Assets/SB/Art/Sprites/Ships/Pursuit/PursuitRaider.png
- Assets/SB/Art/Sprites/Ships/Pursuit/PursuitSmoke.png

이미지 생성 결과의 반투명 후광은 함선 머티리얼의 Sprite Edge Alpha Cutoff 0.45로 제거합니다. PNG 픽셀을 코드로 재편집하지 않았습니다. 바다와 하늘은 기존 셰이더·레이어를 복제한 밤 머티리얼로 구현합니다.
