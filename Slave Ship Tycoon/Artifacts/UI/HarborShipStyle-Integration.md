# 함선풍 UI 적용 (2026-10-14)

후속 사용자 지시인 **금속 테두리 + 목재 안쪽** 적용으로 대체되었다. 최신 에셋·스타일·확인은 [최신 적용 기록](HarborMetalRimWood-Integration.md)을 따른다.

## 기준

실제 `TransportShip.png`, `IroncladDefender.png`, `CrusherShip.png` 원본을 확인했다. 짙은 자주색 외곽선, 주황빛 목재, 보랏빛 철판, 금장식의 넓고 각진 명암을 공통 UI에 반영했다.

## 적용 파일

- `Assets/SB/UI/Shared/Styles/HarborTheme.uss`: 함선 팔레트와 텍스처 밝기·강도 토큰.
- `Assets/SB/UI/Shared/Styles/HarborMaterials.uss`: 질감 배치, 버튼 9-slice, 슬롯 예외 및 상태별 틴트.
- `Assets/SB/UI/InGame/InGameScreen.uss`: 강화 버튼·선택 탭 테두리와 HUD 바탕색.

| 최종 에셋 | 용도 |
| --- | --- |
| `Assets/SB/UI/Shared/Textures/HarborShipWoodTile.png` | 패널·카드·정사각 슬롯 |
| `Assets/SB/UI/Shared/Textures/HarborShipWoodButtonV2.png` | 일반 버튼 |
| `Assets/SB/UI/Shared/Textures/HarborShipGoldButtonV2.png` | 주요 버튼·선택 탭 |
| `Assets/SB/UI/Shared/Textures/HarborShipIronButtonV2.png` | 강화 버튼 |

에셋은 built-in ImageGen으로 함선 원본을 참조해 새로 제작했다. 생성·편집의 정확한 프롬프트는 [목재 생성 기록](HarborShipWood-Generation.md), [금속 생성 기록](HarborShipButtons-Generation.md)에 보관했다. 함선 원본과 이전 UI 텍스처는 보존했다.

## 조절 위치

- `HarborTheme.uss`: `--color-texture-wood` alpha 0.18, `--color-texture-raised` 0.26, `--color-texture-inset` 0.14. 버튼의 평상시·호버·누름 틴트도 같은 파일에서 조절한다.
- `HarborMaterials.uss`: 패널 목재 크기 1024px, 작은 슬롯 384px. 버튼 9-slice는 좌우 96px / 상하 56px, scale 0.18px.
- 새 텍스처 Importer: Default Texture2D, 최대 1024, Uncompressed, Bilinear, mipmap 없음. 타일 Repeat, 버튼 Clamp. 실제 임포트 크기는 타일 1024×1024, 버튼 1024×341.
- 이번 작업에서 추가한 Inspector 연결은 없다. 기존 UIDocument·PanelSettings 연결을 사용한다.

## 확인

- 실제 전투 HUD에서 함선과 새 버튼의 표현, 글자 대비, 버튼 테두리의 늘어짐을 확인했다. 첫 화면은 `Assets/Artifacts/UI/ShipStyle-First.png`.
- 패널 무늬의 강도를 낮추고 크기를 넓힌 최종 강화 화면은 `Assets/Artifacts/UI/ShipStyle-Final.png`, 모집 화면은 `Assets/Artifacts/UI/ShipStyle-Recruitment-Final.png`.
- 일반 버튼·금장 버튼·철판 버튼 및 정사각 슬롯의 실제 적용 텍스처를 확인했다. 모집 화면은 메뉴 이동만 확인했다.
- 변경한 USS 3개 모두 importedWithErrors=false / importedWithWarnings=false. C# compilationFailed=false. 최종 확인 구간에 새 런타임 경고·오류는 없었다.
- 기존 `MerchantLedger needs an icon.` 오류는 이번 UI 변경과 별개로 남아 있다.
- 중간 캡처 1회는 Play Mode가 끝난 시점에 호출되어 캡처 도구 오류가 났다. 짧게 재실행하여 최종 캡처를 완료했고 편집 상태로 복귀했다.
- 자동 테스트, 다른 화면 비율, Safe Area, 실기기 터치, 전체 운송·정산 흐름은 이번 스타일 작업에서 검사하지 않았다.
