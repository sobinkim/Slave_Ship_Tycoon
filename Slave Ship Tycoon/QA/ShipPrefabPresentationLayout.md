# 함선 프리팹 표시 구조

플레이어·적 함선 프리팹은 선체 애니메이션과 보조 표시 요소가 서로의 위치·크기를 덮지 않도록 아래 구조를 사용한다.

```text
Ship Root
├─ Visual                 선체 SpriteRenderer와 Animator
├─ Presentation           월드 이펙트 기준
│  ├─ ShipWaterWake       물결이 있는 함선만 사용
│  └─ ShieldAnchor        함선 효과 표시 기준
└─ WorldUI                월드 공간 UI 기준
   └─ HullCondition       체력바 Track / Fill
```

`Visual`의 기본 Sprite는 Animator의 첫 Idle 프레임과 맞춘다. 따라서 Prefab Mode에서 보이는 선체 위치와 인게임 첫 프레임의 기준이 같다. 물결과 체력바는 `Visual` 밖에 있으므로 선체 크기나 애니메이션이 바뀌어도 Transform이 함께 확대되거나 이동하지 않는다.

## 체력바 위치

모든 값은 각 함선 루트 기준의 `WorldUI/HullCondition` Local Position이다.

| 프리팹 | X | Y |
| --- | ---: | ---: |
| TestEscort_A | -0.04 | -0.62 |
| TestEscort_B | -0.045 | -0.66 |
| TestEscort_C | -0.045 | -0.66 |
| IroncladDefender | 0 | -0.84 |
| TestMainShip | -0.07 | -1.03 |
| TestEnemy_A/B/C | 0.035 | -0.53 |
| TestBoss | 0.045 | -0.66 |

물결 Local Position: Escort A (-0.12, -0.425), B/C (-0.13, -0.46), Ironclad (-0.10, -0.52), Main (-0.20, -0.765). 애니메이션 선체 하단에 맞춰 내렸으며 체력바는 그 아래 여백을 둔다.

위치는 함선 크기에 맞춰 프리팹별로 직접 조절한다. 공통 계산이나 런타임 자동 보정은 사용하지 않는다.

## 조절 방법

- 선체 크기: `Visual`의 Local Scale
- 물결 위치·크기: `Presentation/ShipWaterWake`
- 쉴드 위치·각도·크기: `Presentation/ShieldAnchor`
- 체력바 위치: `WorldUI/HullCondition`
- 체력바 길이·두께·감소 속도: 루트의 `ShipHullFeedback`

2026-09-17: 플레이어 5개, 적 4개 프리팹의 구조와 기본 Idle Sprite를 정리했다. 실제 Animator 프레임을 적용한 미리보기는 `Assets/QA/FleetRootLayoutRuntimeFrame.png`에서 확인했다.
