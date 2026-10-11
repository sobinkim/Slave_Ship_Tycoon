# 함대·모집·합성 UI Toolkit

이 세 화면의 UI를 수정할 때만 읽습니다. 2026-10-10에 `SampleScene`의 실제 버튼 연결을 UI Toolkit으로 전환했습니다. HUD·강화·적재·지휘·정산 작업에는 [인게임·적재 연결](UI_InGame_Cargo.md)을 추가로 읽습니다.

## 사용자 확정 요구사항

- 함대 배치에서 아군과 적군은 중앙 화살표를 제외한 공간을 **50:50**으로 사용합니다. 두 영역의 높이도 같습니다. 아군 `PlayerFleetLoadout`의 호위선 8칸·후열 중앙 메인 함선 고정 규칙을 유지합니다.
- 개별 `편성 해제`와 `선택 취소` 버튼을 제거했습니다. `전체 편성 해제`는 호위선 전 슬롯을 `TryRemoveEscort`로 해제하고 선택 상태를 초기화합니다. 메인 함선은 고정이며, 같은 카드/슬롯을 다시 눌러 선택을 취소할 수 있습니다. 편성된 호위선이 없으면 전체 해제 버튼은 비활성화합니다.
- 모집 화면은 상단 전투/HUD를 남기고 하단에 모집 카드·아이콘·1회/묶음 모집 버튼·가격을 배치합니다. 왼쪽은 입항 안내, 오른쪽은 현재 모집 종류의 카드입니다. 별도의 모집 종류나 광고·소환 레벨 시스템을 추가한 요구가 아닙니다.
- 뽑기 결과 연출은 기존 전체 화면 모달을 유지합니다. 하단 영역 제한은 모집 구매 화면에만 적용합니다. 사용자가 승인한 모집 구매 화면의 배치는 유지합니다.
- 모집 가격/재화/결과는 기존 `EscortGachaManager`에서 가져옵니다. 현재 1회 5다이아, 묶음 11회 55다이아입니다. 이미지에 있는 다른 게임의 숫자를 게임 규칙으로 사용하지 않습니다.
- 뽑기 카드는 빠르게 공개하고, 추가 확률로 실제 보상이 최대 두 단계 승급합니다. 승급 판정·연출을 수정할 때만 [RecruitmentPromotion.md](../RecruitmentPromotion.md)를 읽습니다. 초기값은 1차 8%, 1차 성공 후 2차 1%입니다.

참고: [기존 함대 화면](References/FleetLayout.png), [모집 화면 구도](References/RecruitmentLayout.png).

## 현행 연결

| 대상 | 씬/자산 | 담당 |
| --- | --- | --- |
| 함대 | `FleetToolkitUI/FleetScreen` | `FleetToolkitScreen` · 편성/선택/공격 우선순위/일시정지 |
| 모집 | `FleetToolkitUI/RecruitmentScreen` | `RecruitmentToolkitScreen` · 1회/11회 모집/결과/반복/건너뛰기 |
| 합성 | `FleetToolkitUI/FleetScreen/MergeScreen` | `FleetMergeToolkitScreen` · 직접 재료 선택/해제/일괄 합성/결과 |
| 공용 설정 | `Assets/SB/UI/Shared/Settings/HarborPanelSettings.asset` | 1080×1920 기준, 너비 기준 스케일, 패널 정렬 150 |
| 한글 | `HarborTextSettings.asset` → `HarborKorean.asset` | 기존 `MalgunGothic.ttf`에서 생성한 TextCore 동적 폰트 |
| 공용 스타일 | `Assets/SB/UI/Shared/Styles/HarborTheme.uss` | 역할별 색상, 등급색, 카드/버튼/텍스트 |
| 기본 컨트롤 | `HarborRuntimeTheme.tss` | 루트 전체 화면, ScrollView/Scroller 기본 스타일 |
| 반복 카드 | `Assets/SB/UI/Shared/ShipCard.uxml` | 보유/합성 재료/획득 결과의 아이콘·이름·등급·수량 |

각 화면의 구조·간격은 `Assets/SB/UI/Fleet/`와 `Recruitment/`의 UXML/USS에서 수정합니다. UXML은 UI Builder로 편집할 수 있습니다. `harbor-document` 클래스는 중첩 UIDocument도 부모 화면 전체를 덮도록 지정하므로 제거하지 않습니다.

하단 Toolkit 메뉴는 `PannelManager` 또는 `RecruitmentToolkitScreen.Open`을 호출합니다. 비활성 상태로 보관한 기존 `FleetPanelButton`의 `PanelView.Panel`은 새 `FleetScreen`을 가리킵니다. `PannelManager.PanelChanged`는 다른 메뉴로 이동할 때 모집을 닫는 연결이며, `CurrentPanel`은 현재 메뉴를 제공합니다.

기존 `Canvas/FleetPanel`, `SummonUI`는 비활성 상태로 보관했습니다. 해당 기존 화면 또는 관련 Setup 도구를 다시 활성화/실행하면 두 UI가 함께 뜨거나 연결이 이전 화면으로 돌아갈 수 있으므로 현행 화면은 위 경로에서 편집합니다. 기존 UI 프리팹은 이번에 변경하지 않았습니다.

## Inspector/USS에서 조절할 값

- `FleetToolkitScreen`: Document, Fleet Manager, Panel Manager, Recruitment Screen, Merge Screen, Card Template, Main Ship Icon 참조. `Enemy Preview Rows`는 공격 대상 미리보기의 열당 마커 수이며 실제 적 배치를 바꾸지 않습니다.
- 함대 USS `.formation-side`: 동일한 `flex-grow: 1`, `flex-basis: 0`, `min-width: 0`을 유지합니다. `.formation-comparison`의 높이로 양쪽 영역을 함께 조절합니다.
- `RecruitmentToolkitScreen`: Recruitment Icon, Multi Count, Reveal Seconds, Card Delay Seconds, Promotion Reveal 및 Failed Promotion Reveal의 시간·떨림·실패 표시 비율. 현재 11회, 공개 0.12초/간격 0.025초입니다. 단가는 `EscortGachaManager.Summon Cost`, 실제 보너스 확률은 First/Second Promotion Chance에서 조절합니다. 카드 확대는 사용하지 않습니다.
- 모집 USS `.recruitment-panel`: `top: 51%`, `bottom: 132px`로 인게임 하단 패널의 시작점과 메인 메뉴 위 경계를 공유합니다. 50:50 요구는 아군/적군 비교 영역에 적용되며 화면 상하 비율과는 별개입니다. 하단 132px은 현재 메인 메뉴 공간입니다. 결과창은 공용 `.modal`의 전체 화면 범위를 사용합니다.
- `FleetMergeToolkitScreen`: Fleet Screen 참조, Reveal Seconds, Entry Scale. 합성 재료 수와 지급 규칙은 기존 `PlayerFleetManager`에서 관리합니다.
- 색상은 `HarborTheme.uss`의 `--color-surface-*`, `--color-action-*`, `--color-text-*`, `--color-grade-*` 토큰을 수정합니다. 현재 씬의 배경/슬롯/버튼 팔레트를 기준으로 옮겼습니다.

## 모집 하단 영역·텍스트 정리 (2026-10-11)

- 모집 구매 화면은 인게임 하단 영역 안에 표시합니다. 상단 전투와 하단 메뉴를 유지하며, 내용이 높이를 넘으면 내부 목록에서 세로 스크롤합니다. 결과창에도 같은 범위를 적용했던 변경은 아래 사용자 피드백에 따라 복원했습니다.
- 모집 종류와 보유 재화를 한 줄로 배치했습니다. 안내문·상태 문구는 각 열의 너비에 맞춰 줄바꿈하고, 1회/11회 버튼은 104px 높이와 중앙 정렬로 모집명과 가격을 담습니다. 입항 안내/모집 카드의 아이콘은 120px입니다.
- 결과 카드는 처음 하단 영역에 맞춰 묶음 224px, 단일 320px로 줄였으나, 사용자 피드백에 따라 기존 묶음 280px, 단일 500px로 복원했습니다.
- 변경 파일은 `RecruitmentScreen.uxml`과 `RecruitmentScreen.uss`이며, Inspector 참조·가격·지급 로직은 변경하지 않았습니다. 치수와 줄바꿈은 USS에서 조절합니다.
- 1080×1920 Game 뷰에서 패널 범위 y=979~1788이 기존 하단 패널 경계와 일치하고, 모집 문구/가격이 부모 영역 안에 들어가는 것을 측정했습니다. 1척/11척 결과는 재화를 차감하거나 함선을 지급하지 않는 UI 미리보기로 확인했습니다. UXML/USS 임포트 경고와 스크립트 컴파일 오류는 없었습니다. 기존 `MerchantLedger needs an icon.` 오류는 남아 있습니다. 다른 화면 비율과 실기기 터치는 별도 확인 항목입니다.
- 화면: `Artifacts/UI/Recruitment-Compact.png`, `Recruitment-ResultCompact.png`, `Recruitment-SingleCompact.png`. 수정 전 백업: `AgentScripts/Backups/RecruitmentCompact-20261011/`.

## 뽑기 연출 기존 유지 확정 (2026-10-11)

- 결과창의 하단 영역 전용 클래스·스타일을 제거해 기존 전체 화면 `.modal` 배경, 제목·버튼 스타일과 결과 카드 크기를 복원했습니다. 카드가 순서대로 나타나는 확대·페이드, 타이밍, 건너뛰기, 확인·다시 모집·함대 편성 동작은 기존 코드를 유지합니다.
- 두 번째 참고 이미지의 모집 구매 화면은 승인된 배치입니다. 하단 패널 범위, 안내문·아이콘·모집 버튼·보유 재화 배치를 유지합니다.
- 변경 파일: `RecruitmentScreen.uxml`, `RecruitmentScreen.uss`. Inspector 값·가격·차감·지급 코드는 변경하지 않았습니다. 변경 직전 백업: `AgentScripts/Backups/RecruitmentResultRestore-20261011/`.
- Editor 재임포트 후 결과창 클래스가 `modal hidden`으로 복원된 것을 확인했습니다. 최초 임포트의 파일 수정 시각 불일치 경고 1건은 AssetDatabase 재스캔·재임포트 후 재발하지 않았습니다. 실제 모집·지급이나 자동 테스트는 실행하지 않았습니다.

## 빠른 공개·실제 보너스 승급 (2026-10-11)

이후 요청으로 공개 속도를 높이고 실제 보너스 승급과 두 단계 연출을 추가했습니다. 모집 구매 화면 배치와 결과창의 전체 화면 범위는 유지합니다. 실제 지급은 Manager가 최종 함선 한 척으로 처리하며, UI는 기본 → 1차 → 최종 판정 기록을 재생합니다. 변경 파일·확률·시간 조절은 [RecruitmentPromotion.md](../RecruitmentPromotion.md)에 정리했습니다.

추가 요청으로 전조를 1차 1.2초/2차 1.65초로 늘리고 확대를 떨림으로 교체했습니다. 실제 승급 실패 카드 중 일부도 전조를 보여주며, 예고 60% 지점에서 떨림이 끊기고 빛이 사라지면 실패입니다. 2차 실패 시 1차 승급 보상은 유지합니다. 실패 표시 비율은 실제 승급 확률과 별개입니다.

카드는 하나씩 공개하며, 방금 나온 카드가 전조 대상이면 다음 카드 공개를 멈추고 그 카드의 성공·실패 및 2차 연출까지 끝냅니다. 이후 다음 카드부터 빠르게 공개합니다. 아직 공개하지 않은 카드는 전조 중에도 숨기고, 성공·실패 안내 텍스트는 표시하지 않습니다.

## 생명주기

함대 GameObject 활성화 시 이전 `Time.timeScale`을 저장하고 0으로 설정하며 비활성화 시 복원합니다. 합성은 함대의 자식으로 열고 함대 시각 요소만 숨겨 전투 정지를 유지합니다. 합성 종료는 함대 화면을 다시 표시합니다. 모집은 전투를 정지하지 않으며 다른 메인 메뉴 선택 시 닫힙니다. 이벤트 구독과 결과 코루틴은 각 화면의 OnEnable/OnDisable에서 연결·해제합니다.

편성은 `TryPlaceEscort`/`TryRemoveEscort`, 모집은 `TrySummon`, 합성은 `TryMergeSelected`/`MergeAllUnequipped`를 그대로 사용합니다. UI 내부에서 재화나 소유량을 직접 수정하지 않습니다.

## 확인한 범위와 남은 확인

- Unity C# 컴파일 성공, UXML/USS 임포트 경고 없음.
- 1080×1920 Game 뷰에서 한글·아이콘·카드·전체 화면 및 모집 상단 전투 영역 표시 확인. 아군/적군 영역은 동일 Flex 규칙이며 최초 측정에서 각각 493×650이었습니다.
- 함대→합성→함대 닫기 전환에서 시간 배율 0→0→1 복원, 모집→강화 메뉴 전환 시 모집 닫기 확인. 실제 지급을 발생시키는 모집/합성 버튼은 이번 작업에서 실행하지 않았습니다.
- 기존 `MerchantLedger needs an icon.` 오류는 계속 발생하며 이번 UI 변경 오류와 구분합니다.
- 1회/11회 모집의 차감·지급 및 결과 연출, 직접/일괄 합성의 소모·지급, 터치·키보드 입력, 다른 화면 비율과 Safe Area는 후속 동작 확인 대상입니다. 자동 테스트는 추가하거나 실행하지 않았습니다.

완성 화면 캡처는 프로젝트 루트 `Artifacts/UI/`에 있습니다. 변경 전 씬 파일 백업은 `AgentScripts/Backups/SampleScene.before-fleet-toolkit.unity.backup`입니다. `AgentScripts/InstallFleetToolkit.cs`는 이번 전환에 사용한 일회성 Editor 작업 기록이며 이미 전환된 씬에 재실행하지 않습니다.
