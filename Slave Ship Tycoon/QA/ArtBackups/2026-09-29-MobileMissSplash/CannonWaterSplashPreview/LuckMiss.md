# 운 기반 포탄 MISS

## 규칙과 조절 위치

- `ProjectileEvasionSettings.asset`: 확률 = 최대 확률 × 운 / (운 + 절반 확률 도달 운). 기본 상한 15%, 절반 도달 운 50. 운 0 → 0%, 50 → 7.5%, 100 → 10%.
- `ShipCombatStatCompo.Base Luck`: 함선 고유 운. 아군은 기존 PlayerFleetUpgradeProvider의 함대 운을 더하고, 적은 고유 운만 사용한다. 적의 기본값은 0이므로 기존 전투에 임의의 적 회피를 추가하지 않는다.
- 9개 플레이어/적 프리팹의 `ShipProjectileEvasion`: 공유 설정과 고유 탄착 기준점 참조. `MissWaterLeft`, `MissWaterRight`의 로컬 위치를 프리팹에서 조절한다. 입사 방향 쪽의 수면을 사용해 선체를 관통하는 연출을 피한다.
- `ShipMissFeedback`: MISS 글자, 표시 시간, 재표시 간격, 위로 이동할 거리. `WorldUI/MissLabel`에서 글자 위치·색·크기를 조절한다.

## 흐름

발사 → 목표 함선의 현재 운으로 난수 한 번 판정 → 명중점 또는 수면점으로 연속 곡선 비행 → 명중 시 기존 피해/나무 파편, MISS 시 물기둥과 MISS 글자.

MISS 탄도 목적지를 향하는 동안 ×n에 포함된다. 위험 밝기는 확정 피해가 아니라 접근 중 탄의 잠재 피해를 보여주며, 숨겨진 MISS 판정을 미리 드러내지 않는다. 탄착 때 해당 탄만 등록 해제한다.

목표 사망·비활성화·풀 재사용 시 원래 대상의 표시에서 빠진다. 이미 정해진 행운 MISS의 수면 경로는 바꾸지 않으며, 사라진 대상이나 새로 생성된 대상에 MISS 문구/피해를 적용하지 않는다. 전투 종료와 투사체 회수 경로는 기존 BaseProjectile 등록 해제 흐름을 사용한다.

표시와 회피 규칙은 각각 ShipMissFeedback / ShipProjectileEvasion에 분리했다. 확률은 여러 프리팹이 같은 설정을 쓰므로 ScriptableObject로 공유하고, 애니메이션용 수면점은 프리팹 Transform으로 명시했다. 기존 시세 보너스 확률은 변경하지 않았다.

TestEnemy_A에는 기존 ShipCombatStatCompo가 누락되어 있어 동일 적 프리팹 구성에 맞게 추가했다. 기존 스탯 에셋은 변경하지 않았다.

## 검증

C# 컴파일 통과. ProjectileEvasionTests의 6개 케이스를 Editor에서 직접 호출하여 모두 통과: 확률 경계/감쇠, 입사 방향, 무피해 MISS와 카운트 해제, 정상 명중과 풀 재사용, 대상 사망/재사용. Unity Test Runner의 실행/정리 오류로 표준 러너 결과 대신 같은 NUnit assertion을 직접 실행했다.

프리팹 참조와 물기둥 연결을 검증했다. 화면 밀도와 최종 회피 확률은 실제 게임 밸런스 조정 대상이다.
