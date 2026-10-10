# 포격 예고와 나무 파편

> 2026-09-21: 피격 벽 및 일반탄 궤적 제거. 작살 습격함 전용 궤적과 빨간 갈고리 설정은 HarpoonRaider.md 참고.
## 책임과 데이터 흐름

- `BaseProjectile`은 발사한 실제 탄을 목표 함선의 `ShipIncomingFireDisplay`에 등록한다. 명중 처리 전, 빗나감 전환, 수명 종료, 비활성화, 풀 반환에서 등록을 해제한다. 해제는 중복 호출해도 안전하다.
- 함선마다 탄을 키로 하는 집합을 유지하므로 여러 공격자가 같은 함선을 공격해도 표시와 숫자는 하나다. 발사 전 공격 대상 선택은 집계하지 않는다.
- 침몰, 전투 종료, 비활성화, 재스폰 세대 변경 시 집계를 비운다. 이전 생명의 탄은 새 생명의 숫자에 포함하지 않는다.
- 숫자는 모든 유효한 비행 탄 수다. 위험도는 `dangerWindowSeconds` 안에 도착하는 탄의 예상 피해 합 / 현재 체력이다. `EntityHealth.EstimateIncomingDamage`가 실제 체력 처리와 동일한 피해 보정 계산을 사용하므로 보호 효과도 반영한다. 이는 현재 상태의 예측이며 확정 피해나 사망 보장은 아니다.
- 강조는 `Presentation/IncomingFireDecal`의 매끄러운 빨간 원형 링(수면 원근에 따라 납작하게 표시)으로 수면에 표시한다. 선체·물결 뒤, 바다 앞에 렌더링하며 기존 피격 색상과 Animator는 수정하지 않는다. 프리팹에 저장된 위치와 크기를 그대로 사용하며 위험도에 따라 알파만 바꾼다.
- `NormalProjectile`은 실제 체력 감소를 확인한 경우에만 `ShipWoodImpactFeedback`을 호출한다. 기존 hitEffectPrefab은 호환을 위해 유지한다.
- `ShipWoodImpactFeedback`은 대상 함선별 파편 생성 빈도만 제한한다. `WoodImpactEffect`는 기존 PoolingManager에서 대여하고 파티클 종료 후 반환한다. 전투 피해와 탄 수에는 이 제한이 적용되지 않는다.

전역 매니저나 매 프레임 전체 탄 검색 대신 발사/종료 시 등록을 사용하는 이유는 공속이 증가해도 함선별 표시 수를 일정하게 유지하고 풀 생명주기를 추적하기 위해서다. 숫자 변경에 확대·점멸 애니메이션을 넣지 않아 고공속에서도 표시가 흔들리지 않는다.

## Inspector 조절

- 데칼은 빨간색이며 함선 루트 ShipIncomingFireDisplay의 Warning Color로 변경한다. 피격 시 솟는 벽은 제거했다.
- 함선 루트 `ShipIncomingFireDisplay`: 색상, 일반/위험 알파, 전환 시간, 위험도 시간 범위, 최대 강조 체력 비율, 선체 기준 데칼 정렬 순서 차이.
- `WorldUI/IncomingFireCount`: 로컬 위치, 텍스트 크기, 색상. 월드 공간 TextMeshPro이며 왼쪽 기준 고정 폭 영역과 숫자 고정 폭을 사용한다.
- `Presentation/IncomingFireDecal`: 로컬 Position으로 위치, Scale X/Y로 타원 너비와 높이를 조절한다. 메시와 전용 머티리얼은 공유하며 개별 색상/알파는 MaterialPropertyBlock으로 전달한다.
- 데칼은 일반 알파 블렌딩을 사용한다. IncomingFireDecal.mat의 Ring Radius/Width로 크기와 두께를 조절한다. MISS 물기둥과 나무 파편은 유지한다.
- 실제 명중 시 나무 파편은 `WoodImpactEffect`가 함선 기준 정렬 오더(기본 4051)로 갱신해 선체 앞에 표시한다. 파편 수는 기본 8개이며 `WoodPlankImpact.prefab`의 Fragment Count와 Base Sorting Order로 조절한다.
- 함선 루트 `ShipWoodImpactFeedback`: 파편 프리팹과 최소 생성 간격(기본 0.08초).
- `Art/Effects/IncomingFire/WoodPlankImpact.prefab`: 파편 수(기본 5), ParticleSystem의 크기·속도·회전·수명·중력. 톱니 모양 판자 메시와 갈색 정점 색상을 사용한다.

새 함선은 기존 함선 프리팹을 기반으로 만들거나 위 컴포넌트의 ship/health/hull/waterDecal/countLabel 및 effectPrefab 참조를 명시적으로 연결한다. 런타임 자동 설치나 위치 추론은 하지 않는다. 숫자·파편·데칼의 Sorting Layer는 기존 함선과 같은 `Ship`이다.

## 검증 (2026-09-19)

- Unity 스크립트 컴파일 및 두 셰이더 오류 없음.
- 기존 타겟팅 회귀 테스트 17개 통과.
- 실제 풀의 탄 3발 등록, 중복 등록, 명중 3→2, 회수, 빗나감, 침몰, 재스폰 세대 변경을 실행해서 확인.
- 기존 플레이어 공속의 4배 조건으로 10초 관측: 최대 동시 비행 탄 23개, 단일 대상 19개, 관측 3,450회 집계 불일치 0회. 임시 공속 보정은 제거함. 실제 최대 강화 수치가 확정되면 해당 수치로 추가 육안 검증 필요.
- 실제 Game View에서 표시, 숫자 배치, 렌더링 레이어를 확인. 에디터 캡처는 화면 UI를 모두 포함하지 않는 카메라 출력이므로 전체 HUD와의 최종 시각 확인은 Game View에서 진행한다.
