# 스킬 영역 배경 정리와 하단 지휘력 바 (2026-10-14 최신)

후속 디자인 수정: 바의 검은 받침은 기존 철제·목재 프레임으로 통일했다. 큰 패널의 늘어나는 무늬도 교체했다. 최신 색상·질감은 [후속 적용 기록](PanelWood-Gauge-Matched.md)을 따른다.

- 스킬 뒤의 큰 목재 패널과 금속 테두리를 없앴다. 후속 요청에 따라 지휘력 바와 숫자는 복원하고 스킬 바로 아래에 붙였다.
- 지휘 영역은216px이며 아이콘 아래 바와 오른쪽 AUTO/운송 복귀 버튼이 놓인다. 바에만 짙은 받침, 얇은 황동색 테두리, 주황 fill의 밝은 윗선을 적용해 바다 위 대비를 확보했다.
- 함대 정비/스킬 장착 영역에만 기존 패널 질감을 남겼다.

## 변경 파일

- `Assets/SB/UI/InGame/InGameScreen.uss`: 바깥 하단 영역 투명화, column-reverse로 아이콘 아래 바 배치, 게이지 프레임·채움 스타일, 스킬 스크롤 높이와 최소 너비.
- `Assets/SB/UI/Shared/Styles/HarborTheme.uss`: `--color-gauge-surface`, `--color-gauge-track`, `--color-gauge-border` 색상 토큰. 바 받침과 테두리를 여기서 조절한다.
- `Assets/SB/UI/Shared/Styles/HarborMaterials.uss`: 바깥 하단 배경의 프레임 참조를 함대 정비·스킬 장착 패널로 이동.

UXML/C#·기존 ID·이벤트·Inspector 연결은 그대로 사용한다. 추가 연결할 항목은 없다. 기존 지휘력 계산과 스킬 사용 규칙은 변경하지 않았다.

## 확인

실행 중 실제 HUD에서 스킬 뒤의 바다가 보이고, 아이콘 아래 바와 숫자가 짙은 받침 위에서 읽히며 AUTO와 겹치지 않는 것을 확인했다. 실제 바깥 하단/지휘 영역은 이미지 null·background alpha0을 유지한다. 스킬 영역 하단1123px, 바 상단1129px로6px 간격이다. 바 받침 alpha0.94, 트랙 높이12px. 변경 USS는 임포트 오류·경고 없음. 이번 적용 이후 새 경고·오류는 관측하지 않았다. 기존 Console 오류와 별개다. Play Mode를 유지했다. 자동 테스트·구매·강화·다른 화면 비율은 확인하지 않았다.

최신 화면: `Assets/Artifacts/UI/CommandGauge-UnderSkills.png`.
