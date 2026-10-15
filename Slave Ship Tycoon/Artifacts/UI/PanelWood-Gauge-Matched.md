# 큰 패널 목재결과 지휘력 바 통일 (2026-10-14 최신)

## 변경

- 큰 함대 정비·스킬 설정 패널은 버튼용3:1 이미지를 세로로 늘리던 방식을 해제했다. 작고 고른 목재결을 고정512×512px로 반복해 패널 크기가 바뀌어도 결 크기가 유지된다.
- 큰 패널 테두리는 기존 회색 금속 팔레트의 밝은 위쪽·중간 측면·어두운 아래쪽을6px로 그린다. 안쪽 목재와 테두리가 서로 다른 크기 조절을 사용한다.
- 지휘력 바의 검은 받침·황동색 사각 테두리를 기존 AUTO와 같은 철제/목재 프레임으로 바꿨다. 채움은 기존 행동 버튼의 금색·밝은 윗선·어두운 아랫선으로 맞췄다. 아이콘 아래 위치와 수치 표시를 유지했다.
- 스킬 아이콘 뒤 큰 패널은 복원하지 않았다.

## 변경 파일·조절 값

- `Assets/SB/UI/Shared/Styles/HarborMaterials.uss`: 큰 패널만 반복 타일/6px 금속 테두리로 분리. `.command-status`에 기존 프레임을 적용.
- `Assets/SB/UI/Shared/Styles/HarborTheme.uss`: `--texture-panel-wood-size`512px, `--color-texture-fine-wood`, 금속 측면색, 목재 계열 gauge surface/track 색. 별도 황동색 gauge border 토큰은 해제.
- `Assets/SB/UI/InGame/InGameScreen.uss`: 지휘력 받침의 여백·테두리와 채움색/명암, 트랙 높이14px.
- 신규 `Assets/SB/UI/Shared/Textures/HarborFineWoodTile.png`: 원본1254×1254 RGB, 완전 불투명. Unity MCP에서 Default Texture2D / 최대1024 / Repeat / Bilinear / Uncompressed / mipmap없음으로 임포트.

Built-in ImageGen으로 새 타일을 제작했다. 참조·정확한 최종 프롬프트·생성 결과는 [생성 기록](HarborFineWoodTile-Generation.md)에 있다. 이전 에셋은 보존했다. 새 Inspector 연결은 없다. UXML/C#·지휘력 계산·스킬 사용·재화 로직은 변경하지 않았다.

## 확인

- 실제 실행 중 화면 `Assets/Artifacts/UI/PanelWood-Gauge-Matched.png`에서 작은 목재결과 기존 프레임의 지휘력 바를 확인했다.
- 큰 패널의 texture=`HarborFineWoodTile`, 고정512 반복, slice0을 확인했다. 지휘력 받침은 기존 iron rim/wood 에셋을 사용하며 지휘 영역 전체는 투명하다.
- USS3개 임포트 오류·경고 없음. Play Mode 유지. 자동 테스트·구매·강화·다른 화면 비율 검사는 수행하지 않았다.
