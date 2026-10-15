# 금속 테두리 + 목재 안쪽 (2026-10-14 최신)

후속 요청으로 테두리 굵기 통일·목재결을 보강했다. 최신 에셋과 공통 조절값은 [최신 적용 기록](HarborUniformMetalWood-Integration.md)을 따른다.

사용자 확정 방향: 테두리는 카툰 금속, 안쪽은 목재. 앞선 `HarborShipStyle-Integration.md`의 널빤지 타일 및 목재 테두리 방향을 대체한다.

## 적용

- 반복 널빤지 타일을 공통 UI에서 해제하고, 조용한 목재 중앙과 금속 테두리가 함께 그려진 9-slice 텍스처로 교체했다.
- 일반 버튼·패널·카드·슬롯은 함선 장갑과 같은 회색/보랏빛 철제 테두리를 사용한다.
- 주요 행동과 선택 탭은 금색 금속 테두리로 구분한다. 내부는 같은 짙은 목재 계열이다.
- 목재 중앙의 판자 이음새·반복 줄무늬를 없애고 양끝의 드문 넓은 나뭇결만 남겼다.
- 화면 바탕과 하단 메뉴 받침은 어두운 금속 그림자색으로 구분했다. 버튼의 목재 안쪽 글자는 크림색으로 통일했다.

## 변경 파일과 조절 값

- `Assets/SB/UI/Shared/Styles/HarborTheme.uss`: 금속 계열 바탕·테두리, 목재 내부 글자색, 공통 틴트 토큰.
- `Assets/SB/UI/Shared/Styles/HarborMaterials.uss`: 두 새 에셋의 참조와 9-slice. 좌우 96px / 위 64px / 아래 72px. 버튼·큰 하단 패널 scale 0.2px, 주요 패널 0.18px, 작은 카드·슬롯·강화 행 0.1px, 기본 0.14px.
- `Assets/SB/UI/InGame/InGameScreen.uss`: 목재 면을 가리지 않도록 지휘 영역 바탕을 투명하게 조절하고, HUD 바탕·구분선 색을 공통 토큰으로 맞췄다.
- 연결할 새 Inspector 항목은 없다. 기존 UIDocument, PanelSettings, 메뉴 연결을 사용한다.

## 최종 새 에셋

| 경로 | 용도 |
| --- | --- |
| `Assets/SB/UI/Shared/Textures/HarborShipIronRimWood.png` | 철제 테두리 + 목재 안쪽 |
| `Assets/SB/UI/Shared/Textures/HarborShipGoldRimWood.png` | 금장 테두리 + 목재 안쪽 |

원본 PNG는 각각 2172×724 RGBA. Unity MCP로 Default Texture2D, 최대 1024, Clamp, Bilinear, Uncompressed, mipmap 없음, alphaIsTransparency=true로 임포트했다. 실제 표시용 텍스처는 1024×341이다. 이전 생성본 및 함선 원본은 보존했다.

Built-in ImageGen 편집으로 제작했다. 원본·참조·정확한 최종 프롬프트는 [철제 테두리 생성 기록](HarborMetalRim-Iron-Generation.md), [금장 테두리 생성 기록](HarborMetalRim-Gold-Generation.md)에 있다.

## 확인

- 실행 중인 실제 전투 HUD를 540×960으로 캡처했다: `Assets/Artifacts/UI/MetalRimWood-Final.png`.
- 금속 테두리의 모서리와 직선이 버튼·긴 강화 행·큰 패널에서 자연스럽게 이어지며, 눈에 띄는 9-slice 찌그러짐은 보이지 않았다.
- 기존 널빤지 반복이 사라지고, 강화 이름·가격·하단 탭의 대비가 유지되는 것을 확인했다.
- 실제 boss/auto/upgrade/nav 버튼에서 새 텍스처와 slice scale 0.2, 크림색 글자 적용을 확인했다.
- 변경 USS 3개 모두 importedWithErrors=false / importedWithWarnings=false. compilationFailed=false. 작업 시작 이후 새 경고·오류는 관측하지 않았다.
- 기존 `MerchantLedger needs an icon.` 오류 1종은 남아 있으며 이번 스타일 변경과 별개다.
- 작업 전후 Play Mode를 유지했다. 현재 실행 중이다.
- 자동 테스트, 구매·강화 실행, 전체 운송/정산 흐름, 다른 화면 비율과 실기기는 확인하지 않았다.
