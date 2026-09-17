# 함선 효과 아이콘

효과를 받는 함선의 `WorldUI/HullCondition/StatusIcons`에 아이콘을 표시한다. 체력바의 자식이므로 체력바 위치 변경을 따라간다. 기존 체력바와 물결의 위치는 수정하지 않았다.

## 새 효과 설정

1. `FleetEffectDefinition`을 상속한 효과 데이터 에셋에서 `Status Icon`과 `Status Icon Color`를 설정한다. 아이콘을 비워 두면 해당 효과의 아이콘은 생략한다.
2. 기존 `FleetEffectProvider`의 Effects에 효과를 연결한다. 실제 효과 대상은 기존 CollectTargets 규칙을 따른다.
3. 새 함선은 기존 프리팹을 복제하거나 `FleetEffectStatusIcons`의 Receiver와 Icon Template을 명시적으로 연결한다. 현재 플레이어 5개와 적 4개 프리팹은 연결되어 있다.

`FleetEffectReceiver.CollectReceivedEffects`는 실제로 받는 효과만 중복 없이 제공한다. 공급 함선 자신의 쉴드 연출만으로는 아이콘이 생기지 않는다. 같은 효과를 여러 공급원에서 받아도 아이콘은 하나이며, 마지막 공급원이 사라질 때 제거한다. 다른 종류의 효과는 왼쪽부터 별도로 표시한다.

## 모양 조절

- 위치: 함선 프리팹의 `WorldUI/HullCondition/StatusIcons` 로컬 Position.
- 크기/간격: 같은 오브젝트의 `FleetEffectStatusIcons > Icon Size / Icon Spacing`. 기본 0.1 / 0.025 로컬 단위.
- 방어 아이콘: `Assets/SB/Art/Sprites/Effects/StatusIcons/RearProtection.png`. 사용자가 선택한 곡선형 외곽 방패와 비율이 작은 내부 채움 방패로 구성된 1254×1254 투명 PNG를 그대로 사용한다. Bilinear 필터와 무압축으로 표시한다.
- 방어 색상: ImmediateRearProtection 에셋의 Status Icon Color. 현재 흰색 불투명 값이므로 PNG의 흰색 형태가 그대로 표시된다.
- Icon Template의 공통 재질은 `Fleet/Status Icon` 셰이더를 사용한다. 원본 PNG의 알파 형태를 유지하며 RGB를 지정색으로 표시하므로 검은 원본 아이콘도 네이비로 보인다.

효과 변화 이벤트로만 목록을 갱신한다. 아이콘 오브젝트는 필요한 수만큼 생성한 뒤 재사용하며 프레임마다 생성/탐색하지 않는다. 효과 제거, 사망, 비활성화 시 표시를 정리한다.

## 검증

컴파일 통과. Play Mode에서 보호 대상에만 표시, 중복 공급원 하나로 표시, 한 공급원 제거 후 유지, 마지막 공급원 제거 시 숨김, 대상 사망, 다음 전투 재적용, 풀 비활성화를 확인했다. 실제 렌더 미리보기는 `QA/EffectStatusIconPreview.png`에 저장했다.

아이콘 텍스처는 Bilinear 필터, 압축 없음, Mipmap 없음으로 설정했다. 방어 효과 데이터 에셋은 `RearProtection.png`를 참조한다.
