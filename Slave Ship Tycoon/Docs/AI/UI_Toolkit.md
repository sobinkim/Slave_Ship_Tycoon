# UI 작업 규칙 — UI Toolkit

UI 화면을 만들거나 수정하거나 기존 화면을 이관할 때만 읽습니다. 프로젝트의 새 UI 표준은 Unity UI Toolkit이며, 구조는 UXML, 표현은 USS, 게임 상태 연결과 상호작용은 필요한 경우 C#으로 둡니다.

기존 uGUI 화면을 교체하는 작업이면 [UI_Migration_Preparation.md](UI_Migration_Preparation.md)의 현황·연결 주의사항·교체 단위를 추가로 읽습니다. 새 독립 화면 제작에는 이관 현황 문서를 로드할 필요가 없습니다.

## 시작 전 확인

1. Unity MCP로 대상 씬의 `UIDocument`, `PanelSettings`, 기존 Canvas/UI 오브젝트와 연결 컴포넌트를 확인합니다.
2. 대상 화면이 현재 uGUI인지 UI Toolkit인지 확인합니다. 이관 작업이면 요청된 화면 범위와 연결된 입력·일시정지·재화·게임 상태 흐름을 추적합니다.
3. 기존 UXML/USS, 공용 스타일, 폰트, 아이콘, 패널 설정을 검색해 프로젝트 패턴을 재사용합니다. 실제 씬과 프리팹 상태를 문서보다 우선 확인합니다.
4. 관련 화면 연결을 알아야 하면 [18_UI화면과_연결방법.md](../UserGuide/18_UI화면과_연결방법.md)만 추가로 읽습니다.

## 파일과 책임

- 화면 단위로 `Assets/SB/UI/<Feature>/` 아래에 UXML과 USS를 둡니다. 런타임 연결 코드가 필요할 때만 같은 기능 폴더에 C#을 둡니다.
- UXML은 한 화면의 계층과 요소 이름을 표현합니다. 스타일은 USS 클래스에 둡니다. UXML `style="..."` 인라인 스타일을 쓰지 않습니다.
- C#은 요청된 상호작용/데이터 연결만 맡습니다. 버튼 클릭 연결은 USS가 아니라 C# 이벤트 또는 명시적 조립 지점에서 처리합니다.
- UI를 생성할 때 사용자가 기능 로직도 요청하지 않았다면 UXML/USS만 만듭니다. `"작동하는 UI"`나 버튼 요청을 임의로 게임 로직 구현 허가로 해석하지 않습니다.
- UXML은 `xmlns:ui="UnityEngine.UIElements"`를 선언하고, 필요한 USS를 `<ui:Style src="..." />`로 연결하며, 루트 컨테이너 하나를 사용합니다.
- 런타임 UI는 씬의 `UIDocument`에 올바른 Source Asset과 `PanelSettings`가 연결되어야 표시됩니다. 기존 PanelSettings를 우선 재사용하고 새 자산은 필요할 때만 만듭니다.

## 스타일과 레이아웃

- 먼저 프로젝트의 공용 스타일과 같은 화면의 패턴을 따릅니다. 이름은 camelCase, USS 클래스는 kebab-case를 기본으로 합니다.
- 고정 크기보다 부모의 Flex 흐름, `flex-grow`, `flex-shrink`, 퍼센트를 우선합니다. 루트 화면이나 명확한 디자인 요구가 있는 요소만 고정 치수를 씁니다.
- USS는 CSS 전체가 아닙니다. `gap`, `z-index`, `box-shadow`, `filter`, `outline`, `border` 단축형, `:first-child`/`:nth-child`, 속성 셀렉터, UXML 인라인 스타일을 쓰지 않습니다. 자식 여백·명시 클래스·DOM 순서·중첩 요소 등 지원되는 방법을 사용합니다.
- USS gradient 함수는 사용하지 않습니다. 그라데이션이나 USS로 표현할 수 없는 커스텀 도형은 먼저 기존 에셋/구조를 확인하고, 필요성이 있을 때만 Painter2D 기반 커스텀 요소를 검토합니다.
- 사용자 지정 색상·치수·간격은 지정된 값을 그대로 사용합니다. 본문 텍스트와 배경은 읽을 수 있는 대비를 유지합니다.

## 의미 있는 색상 이름

색상값은 화면 곳곳에 직접 반복하지 말고, 역할을 설명하는 USS 토큰으로 정의하고 참조합니다. 토큰 이름은 프로젝트 기존 규칙에 맞추며, 새 토큰은 필요한 최소 역할만 추가합니다.

```uss
:root {
    --color-surface-primary: #18313A;
    --color-text-primary: #F4F0E6;
    --color-action-primary: #36C6B0;
    --color-status-danger: #D85850;
}

.harbor-panel {
    background-color: var(--color-surface-primary);
}

.harbor-title {
    color: var(--color-text-primary);
}
```

`#RRGGBB`는 실제 RGB 색상값이고, `--color-text-primary` 같은 이름은 UI에서 맡는 역할을 뜻합니다. 원시 hex 값에 이름만 붙이는 데 그치지 않고, 같은 역할의 요소가 같은 토큰을 참조하게 합니다. 프로젝트 고유 팔레트나 기존 색상 에셋이 있으면 그것을 먼저 조사하고 임의의 새 색을 확정하지 않습니다.

위 USS 블록의 hex 값은 변수 문법을 보여주기 위한 예시입니다. 실제 화면에 적용할 때는 현재 프로젝트 팔레트에서 확인한 색상값을 사용합니다.

USS 커스텀 프로퍼티는 `--이름`으로 정의하고 `var(--이름)`으로 참조합니다. USS는 CSS의 일부 기능만 지원하므로 변수 내부 함수 중첩이나 CSS 수학 표현식을 사용하지 않습니다.

## 상호작용과 데이터

- `Button.clicked` 등 목적이 분명한 이벤트로 동작을 연결하고, 구독/해제 생명주기를 맞춥니다. 한 동작을 C#과 별도 UnityEvent 양쪽에서 중복 연결하지 않습니다.
- 반복 데이터 UI는 실제 데이터 공급자와 갱신 시점을 추적합니다. 요청하지 않은 Presenter/Manager나 데이터 바인딩 계층을 새로 만들지 않습니다.
- UI Toolkit은 TextCore 기반입니다. TMP 폰트를 그대로 가져다 쓸 수 있다고 가정하지 말고 프로젝트에 맞는 `FontAsset`, `TextSettings`, 폴백 연결을 확인합니다.
- 아이콘은 프로젝트 기존 UI 에셋을 우선 재사용합니다. `UnityDefaultRuntimeTheme.tss`와 Unity 내장 테마 아이콘을 앱 UI 자산으로 직접 참조하지 않습니다.

## 이관과 검증

- 한 번에 요청된 화면만 이관합니다. 공통 UI를 바꿀 때는 참조 화면을 확인해 영향 범위를 좁힙니다.
- 기존 화면의 표시 문구, 가격, 재화 차감, 버튼 이벤트, 닫기/뒤로가기, 입력 차단, 일시정지와 시간 배율 동작을 유지하는지 확인합니다.
- 씬에 UI를 추가할 때는 `UIDocument`와 PanelSettings 참조를 명시적으로 연결합니다. 기존 Canvas와 전환 기간에 공존한다면 같은 오브젝트에 두 시스템을 섞지 말고 표시 우선순위와 입력 중복을 확인합니다.
- UXML/USS는 파일을 완성한 뒤 Unity Editor가 재임포트하도록 합니다. Console 파싱/USS 경고와 Game 뷰의 레이아웃·텍스트 잘림·클릭 동작을 확인합니다. 다른 화면 비율과 Safe Area/기기 터치는 별도 검증으로 기록합니다.
- UI 작업의 완료 보고에는 UXML/USS/C#과 씬 연결, Inspector에서 조절하는 값, 실제로 확인한 화면 상태를 적습니다.
