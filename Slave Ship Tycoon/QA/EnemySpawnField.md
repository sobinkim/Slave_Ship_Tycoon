# 적 함대 3열 × 4행 작업 안내

## 배치 수정

- `SB > Stage Enemies > Layout Editor`에서 챕터와 스테이지를 선택하고 Load → 편집 → Save Stage Layout 순서로 사용한다.
- 슬롯은 행 우선이다. 1~3번 / 4~6번 / 7~9번 / 10~12번이 각각 한 행이다. 각 행의 왼쪽부터 전열 / 중열 / 후열이며 빈칸에는 적이 생성되지 않는다.
- SampleScene의 StageSpawnManager에 12개 Spawn Point Transform이 연결되어 있다. 해당 Point(x,y)의 Local Position으로 위치와 시각적 엇갈림을 조절한다. 무작위 배치나 숨은 오프셋 계산은 없다.
- 부모 EnemySpawnPoints는 조우 연출에서 움직이므로 자식 Point의 로컬 좌표를 수정한다.

## 적 함선 크기

- 적과 플레이어는 독립 프리팹이다. 적의 크기는 StageSpawnManager에서 공통 배율로 조절하지 않는다.
- 각 적 프리팹의 Visual 자식 Transform 크기를 직접 조절한다. Visual만 조절하므로 발사점, 피격점, 체력바 같은 게임플레이 기준 Transform은 유지된다.
- 풀링 재사용 시에도 프리팹에 저장된 Visual 크기를 그대로 사용한다.
- 현재 TestEnemy_A/B/C의 Visual은 `(0.96, 1.12, 1.12)`이며, 보스 프리팹은 변경하지 않았다.

## 데이터 호환과 범위

- 기존 9칸은 인덱스와 전열/중열/후열 의미가 유지된다. 기존 에셋 로드 시 끝에 빈칸 3개가 추가된다.
- 첫 스테이지 Stage_01_01에만 8척 배치를 저장했다. 기존 6척에서 8척으로 늘어 해당 스테이지 난이도와 보상 조정은 별도 판단이 필요하다. 다른 스테이지의 구성은 유지했다.
- 행/열 수는 StageEnemyLayout의 ColumnCount=3, RowCount=4로 명시했다. 열 수를 바꿀 때는 슬롯 인덱스와 TargetLine 규칙을 함께 수정해야 하므로 Inspector에서 임의로 바꾸는 값은 아니다.
- 플레이어 대형은 유지했다. 함종별 능력 차이, 방어함의 후열 보호, 순차 증원은 이번 변경에 포함하지 않았다.

## 관련 파일

- Assets/SB/Scripts/Stage/StageEnemyLayout.cs: 12칸 데이터와 9칸 호환.
- Assets/SB/Scripts/Stage/StageSpawnManager.cs: 스폰 참조 검증과 크기 적용.
- Assets/SB/Editor/StageEnemyLayoutEditorWindow.cs: 배치 편집 창.
- Assets/SB/Editor/StageSpawnManagerEditor.cs: Inspector와 Scene 슬롯 라벨.
- Assets/SB/Scripts/Ship/Battle/TargetSelector.cs: 열 수 참조. 기존 타겟팅 규칙 유지.

## 검증 (2026-09-16)

- Unity 재컴파일 성공 확인. 이후 dotnet build Assembly-CSharp-Editor.csproj --no-restore 성공: 오류 0, 어셈블리 버전 충돌 경고 5.
- Unity MCP로 12개 Transform 연결 및 첫 스테이지 EnemyCount=8 확인 후 씬/에셋 저장.
- StageEnemyLayoutTests에 기존 9칸 호환과 네 번째 행 테스트 추가.
- 테스트 실행 요청 이후 Unity Pipeline 메인 스레드 요청이 시간 초과되어 테스트 결과, 실제 화면 겹침, 풀링 반복 전투 검증은 미완료. Unity Editor 응답 회복 후 EditMode 테스트와 플레이 확인 필요.
