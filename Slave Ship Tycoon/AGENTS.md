# Unity 프로젝트 작업 규칙

이 문서는 이 프로젝트에서 AI가 Unity 작업을 수행할 때 따라야 할 작업 방식과 코드 스타일을 정의한다. 목표는 **AI의 편의적 자동화보다 사람이 빠르게 이해하고, Inspector와 코드에서 정확히 커스텀할 수 있는 구조**를 만드는 것이다.

## 1. Unity 작업 방식

### 도구 우선순위

1. Unity MCP를 우선 사용한다.
   - 씬, GameObject, 컴포넌트, 프리팹, 에셋의 실제 상태를 먼저 조회한다.
   - Unity Editor가 제공하는 안전한 작업 경로로 씬/에셋을 변경한다.
2. Unity CLI는 MCP로 수행하기 어렵거나 반복적인 검증이 필요할 때 사용한다.
   - 컴파일 오류 확인, 테스트 실행, 배치 검증, 에셋/프로젝트 상태 점검에 사용한다.
   - CLI 실행 전후에는 변경 대상과 결과를 명확히 확인한다.
3. 코드나 YAML을 직접 수정하기 전에 기존 구조와 참조 관계를 확인한다.
   - 씬, 프리팹, ScriptableObject, Animator, 입력 설정, 레이어/태그 의존성을 확인한다.
   - 사람이 만든 기존 설정과 Inspector 값은 임의로 덮어쓰지 않는다.

### 기본 절차

1. 요청 범위와 관련 Unity 오브젝트/에셋을 조회한다.
2. 기존 컴포넌트와 데이터 구조를 재사용할 수 있는지 판단한다.
3. 작은 단위로 구현한다. 변경은 요청에 필요한 범위로 제한한다.
4. 컴파일 및 가능한 범위의 동작 검증을 수행한다.
5. 변경한 파일, Inspector에서 사용자가 조절할 값, 남아 있는 확인 사항을 간단히 보고한다.

### 안전 규칙

- 사용자의 명시적 요청 없이 씬 전체, 프리팹, 에셋, ProjectSettings를 대량 변경하거나 삭제하지 않는다.
- 실행 중인 Unity Editor가 있을 때는 파일을 직접 수정해 충돌을 만들지 않는다. 가능한 경우 MCP/Editor 경로를 사용한다.
- 새 패키지, 외부 의존성, 전역 프로젝트 설정 변경은 필요성과 영향 범위를 먼저 밝힌다.
- 컴파일 오류가 기존 오류인지 새 변경으로 인한 오류인지 구분한다.

## 2. 설계 원칙: 명시적이고 커스텀 가능한 구현

### 핵심 원칙

- AI는 요청되지 않은 자동화, 추론, 보정 로직을 임의로 추가하지 않는다.
- 기능의 입력, 결과, 상태 전환, 예외 처리는 코드와 Inspector에서 추적 가능해야 한다.
- 사람이 바꿀 가능성이 있는 게임 디자인 값과 동작 선택지는 명시적인 필드, enum, 데이터 에셋, 메서드 인자로 제공한다.
- 기본값은 제공하되, 숨은 상수나 복잡한 내부 계산으로 사용자의 선택권을 제한하지 않는다.
- 코드를 읽는 사람은 "무엇을 입력하면 무엇이 일어나는지"를 한 번에 파악할 수 있어야 한다.
- 간단하고 명시적인 구현을 우선하고, 일반화·자동화·추상화는 실제로 반복되는 요구가 확인된 뒤에만 추가한다.

### 구현 선택 기준

다음 기준으로 구현을 선택한다.

| 상황 | 우선하는 방식 |
| --- | --- |
| 사용자가 결과를 직접 정하고 싶음 | 명시적 필드, enum, Transform 참조, 데이터 에셋 |
| 프리팹마다 다르게 조절해야 함 | `[SerializeField] private` 필드와 Inspector 그룹 |
| 여러 오브젝트가 같은 설정을 공유함 | ScriptableObject 또는 명확한 설정 클래스 |
| 호출 시점마다 값이 달라짐 | 의미가 분명한 메서드 인자 |
| 게임 규칙상 계산이 필수임 | 입력값과 계산 결과를 분리하고, 조절값을 노출 |
| AI가 "더 편하게" 자동 결정하려 함 | 요청된 자동화인지 먼저 확인; 아니라면 명시적 선택지 제공 |

### 피해야 할 패턴

- 요청하지 않은 자동 탐색, 자동 보정, 자동 배치, 자동 밸런싱
- 한 개의 bool로 여러 의미를 숨기는 상태 처리
- 설정값을 코드 깊숙이 숨기거나 매직 넘버로 고정하는 방식
- 지나치게 범용적인 헬퍼/매니저를 먼저 만들어 실제 흐름을 감추는 방식
- 값이 어디에서 바뀌는지 알 수 없는 전역 상태 및 암묵적 의존성
- "알아서 최적화"한다는 명분으로 사용자가 의도한 결과를 바꾸는 처리

### 위치와 방향 (원칙의 한 예)

- 단일 오브젝트의 배치에는 계산된 오프셋보다 명시적 `Vector2`/`Vector3` 위치 또는 `Transform` 기준점을 우선 사용한다.
- 생성 위치가 게임 디자인 값이라면 `SerializeField`로 노출한다.
- 특정 지점에 배치해야 하면 `Transform spawnPoint`, `Transform targetPoint` 또는 `Vector3 spawnPosition`처럼 목적이 드러나는 이름을 사용한다.
- 월드 좌표와 로컬 좌표를 섞지 않는다. 어느 좌표계인지 변수명이나 주석으로 명확히 한다.

```csharp
[SerializeField] private Transform spawnPoint;
[SerializeField] private Vector3 localVisualOffset;

public void Spawn()
{
    Vector3 worldPosition = spawnPoint.position;
    Quaternion worldRotation = spawnPoint.rotation;
    Instantiate(projectilePrefab, worldPosition, worldRotation);
}
```

- 아래처럼 화면 크기, 주변 오브젝트, 임의 비율에서 위치를 추론하는 방식은 요청상 꼭 필요한 동적 배치 기능이 아닐 경우 피한다.

```csharp
// 지양: 결과 위치를 Inspector에서 직관적으로 조절하기 어렵다.
Vector3 position = player.position + direction.normalized * radius * 0.73f;
```

- 동적 계산이 게임 규칙상 반드시 필요하면, 계산식 전체를 숨기지 않는다.
  - 계산에 쓰는 반지름, 거리, 각도, 오프셋, 보정 계수는 의미 있는 이름으로 분리한다.
  - 조절이 필요한 값은 `SerializeField`로 노출한다.
  - 최종 위치를 별도 변수에 보관하고 Gizmo/로그 등으로 확인 가능하게 한다.

```csharp
[SerializeField] private float spawnDistance = 3f;
[SerializeField] private float spawnAngleDegrees = 0f;

private Vector3 GetSpawnWorldPosition(Transform origin)
{
    Vector3 direction = Quaternion.Euler(0f, spawnAngleDegrees, 0f) * origin.forward;
    return origin.position + direction * spawnDistance;
}
```

### 수치와 게임 밸런스 값

- 매직 넘버를 코드 본문에 넣지 않는다. 체력, 피해량, 속도, 거리, 쿨다운, 확률, 색상, 레이어 등의 의미 있는 값은 이름을 붙인다.
- 인스턴스별로 달라지는 값은 Inspector의 `[SerializeField] private` 필드로 둔다.
- 여러 프리팹이 공유하는 밸런스 데이터는 ScriptableObject 등 명시적인 데이터 에셋으로 분리한다.
- 코드 상수는 진짜로 변하지 않는 기술적 값에만 사용한다.

```csharp
[SerializeField, Min(0f)] private float attackRange = 2.5f;
[SerializeField, Min(0f)] private float attackCooldownSeconds = 1f;
[SerializeField] private Vector3 hitboxLocalCenter;
```

## 3. C# 코드 스타일

### 구조와 명명

- 한 컴포넌트는 하나의 명확한 역할을 맡는다. 입력, 이동, 공격, 체력, 시각 효과를 불필요하게 한 클래스에 섞지 않는다.
- 클래스와 공개 멤버는 `PascalCase`, private 필드는 `camelCase`를 사용한다.
- private Inspector 필드는 `[SerializeField] private`로 선언한다. 공개 필드는 외부 API가 필요한 경우에만 사용한다.
- bool에는 `is`, `has`, `can`, `should` 등의 접두사를 사용한다.
- 단위가 있는 변수에는 단위를 포함한다. 예: `cooldownSeconds`, `speedUnitsPerSecond`, `damagePerHit`.
- `data`, `value`, `temp`, `result`처럼 의미가 불명확한 이름을 피하고 도메인 의미를 담는다.

### Inspector 친화성

- Inspector에서 조절하는 필드에는 Tooltip, Header, Range, Min을 필요한 범위에서 사용한다.
- 필드는 게임플레이/시각/디버그처럼 목적별로 묶어 표시한다.
- 참조 누락이 치명적인 컴포넌트는 `Awake` 또는 `OnValidate`에서 빠르게 감지한다.
- 사용자가 직접 연결해야 하는 Transform, 프리팹, 데이터 에셋은 자동 탐색보다 명시적 할당을 우선한다.

```csharp
[Header("Attack")]
[SerializeField, Min(0f), Tooltip("공격 가능한 최대 월드 거리")]
private float attackRange = 2.5f;

[Header("References")]
[SerializeField] private Transform muzzlePoint;
[SerializeField] private Projectile projectilePrefab;
```

### 구현 품질

- `Update`에서 반복하는 비용이 큰 탐색(`Find`, `GetComponent`, 전체 오브젝트 검색)을 피한다. 필요한 참조는 캐시하거나 Inspector에서 할당한다.
- Nullable 상태, 쿨다운, 거리 판정, 생성 조건은 작은 의도가 드러나는 메서드로 분리한다.
- 코루틴, 이벤트 구독, 생성한 오브젝트는 생명주기와 해제 지점을 명확히 한다.
- 예외적인 처리나 디자인상 의도가 불명확한 계산에만 짧은 주석을 작성한다. 코드가 하는 일을 그대로 반복하는 주석은 쓰지 않는다.
- 기존 프로젝트의 네임스페이스, 비동기 방식, 이벤트 패턴이 있으면 그것을 따른다.

## 4. 완료 보고 기준

작업 완료 시 다음을 간결하게 전달한다.

- 변경한 스크립트/프리팹/씬
- Inspector에서 사용자가 조절할 핵심 값과 의미
- 수행한 검증(컴파일, 테스트, Editor 확인 등)
- 아직 사용자가 Unity Editor에서 확인하거나 연결해야 할 항목
