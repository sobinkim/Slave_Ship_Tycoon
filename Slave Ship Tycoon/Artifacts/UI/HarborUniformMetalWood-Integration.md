# 일정한 두꺼운 금속 테두리와 목재결 (2026-10-14 최신)

사용자 요청: 테두리를 더 두껍게, 네 변과 요소마다 일정하게 정리하고 맹맹한 목재 안쪽에 나뭇결을 더한다.

## 변경 파일

- `Assets/SB/UI/Shared/Styles/HarborTheme.uss`: 공통 `--texture-frame-slice: 80`, `--texture-frame-scale: 0.24px` 추가. 모든 프레임의 굵기를 이 두 토큰으로 조절한다.
- `Assets/SB/UI/Shared/Styles/HarborMaterials.uss`: 이전의 요소별 0.1 / 0.14 / 0.18 / 0.2px scale 차이를 제거하고 위 공통 값을 적용했다. 네 방향 슬라이스도 80px로 통일했다. 새 목재결 에셋으로 참조를 바꿨다.
- 새 에셋: `Assets/SB/UI/Shared/Textures/HarborShipIronRimWoodGrainV3.png`, `Assets/SB/UI/Shared/Textures/HarborShipGoldRimWoodGrainV2.png`.

목재 안쪽에 넓고 짧은 각진 홈과 밝은 결을 중앙까지 추가했다. 반복 널빤지 이음새는 사용하지 않는다. 금색 에셋의 고른 프레임 형상을 철제 에셋에도 적용해 하단만 두껍던 모양을 정리했다. 철제 최종 원본의 목재 경계 기준 금속 폭은 위123 / 아래124 / 왼쪽126 / 오른쪽124px로 거의 동일하다.

원본은 각각 2172×724 RGBA. Unity MCP 임포트 설정은 Default Texture2D, 최대1024, Clamp, Bilinear, Uncompressed, mipmap 없음, alphaIsTransparency=true다. 실제 표시용 텍스처는1024×341이다. 기존 에셋은 보존했다.

새 Inspector 연결은 없다. 화면 배치·폰트·게임 이벤트·재화 로직도 이번 수정에서 바꾸지 않았다. 굵기는 `HarborTheme.uss`의 공통 프레임 토큰에서 변경한다.

## 생성 기록

Built-in ImageGen으로 편집했다. 정확한 프롬프트, 원본과 출력 경로, 각 수정 단계는 [목재결·철제 프레임 생성 기록](HarborMetalRimWoodGrain-Generation.md), [금색 프레임 생성 기록](HarborUniformGoldRim-Generation.md)에 있다.

## 확인

- 실행 중 실제 HUD의 패널·버튼·슬롯을 확인했고, 두꺼워진 금속 띠가 일정한 폭으로 읽히며 모서리 찌그러짐·글자 겹침이 보이지 않았다.
- 강화 행과 상단 패널에서 목재결이 전보다 뚜렷하게 보인다. 작은 AUTO OFF는 내부 여유가 타 버튼보다 작지만 글자 가독성은 유지된다.
- 실제 boss/auto/nav/upgrade 버튼과 route/currency/lower 패널에서 네 방향 slice80 / scale0.24 적용을 확인했다.
- 변경한 USS2개 모두 importedWithErrors=false / importedWithWarnings=false. compilationFailed=false. 작업 시작 이후 새 경고·오류는 관측하지 않았다.
- 기존 `MerchantLedger needs an icon.` 오류는 남아 있으며 이번 변경과 별개다.
- 작업 전후 Play Mode를 유지했다. 구매·강화 실행, 자동 테스트, 다른 화면 비율 및 실기기 확인은 수행하지 않았다.
- 최종 실제 화면: `Assets/Artifacts/UI/UniformMetalWood-Final.png`.
