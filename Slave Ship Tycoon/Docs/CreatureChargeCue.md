# 갑각수 돌진 예고 연출

## 연출

- 긴 돌진 데칼을 제거했습니다. 갑각수 프리팹의 `ChargeWarning` 오브젝트와 적 코드의 데칼 갱신 경로를 제거했습니다. 수면 연출이 공유하는 스프라이트 에셋은 유지합니다.
- 준비 중 몸을 조금 뒤로 당기고 낮추며, 머리를 숙이고 호흡·꼬리 움직임을 가라앉힙니다. 반복 떨림을 없애고 짧은 정지감으로 힘을 모읍니다.
- 머리 앞의 작은 포말과 기존 물살 3개가 돌진 방향에서 몸 쪽으로 모입니다. 바다색 포말을 재사용하며 별도 경고 띠나 화살표는 표시하지 않습니다.
- 준비 동작의 마지막 30% 구간에서 흰색 하이라이트가 한 번 부드럽게 켜졌다 꺼집니다. 피격 시 전체 스프라이트 발광과 별도의 셰이더 값으로 구분되어 함께 작동합니다.
- 돌진 시 기존의 몸 뻗기·후방 물살·잔물결로 이어집니다. 풀 복귀·비활성화에서는 포즈, 물살 회전과 발광을 초기화합니다.

## Inspector

`ReefbackLeviathan` → `SeaCreatureSurfacePresentation` → `Charge Anticipation`:

| 항목 | 값 | 역할 |
|---|---:|---|
| Windup Compression | (0.085, 0.025) | 몸 길이 압축·높이 보정 |
| Windup Sink Depth | 0.025 | 힘을 모을 때 몸이 낮아지는 거리 |
| Windup Nose Dip Degrees | 2.5 | 머리를 숙이는 각도 |
| Charge Emission Intensity | 0.4 | 밝은 색면의 발광 강도 |
| Charge Flash Start Progress | 0.7 | 한 번의 발광 시작 비율 |
| Charge Gather Distance | 0.45 | 머리 앞 물살의 이동 거리 |
| Charge Gather Spread | 0.22 | 물살이 모이기 전 좌우 폭 |
| Charge Gather Opacity | 0.65 | 모이는 포말의 불투명도 배율 |

색은 `Charge Emission Color`에서 조절합니다. 기존 돌진 준비 시간 0.65초와 전투 수치는 변경하지 않았습니다.

## 흰색 발광·배경 파도 정리 (2026-10-16)

- 두 크리쳐 프리팹의 `Emission Color`와 `Charge Emission Color`를 모두 흰색 `(1,1,1,1)`으로 변경했습니다. 몸체 머티리얼·셰이더·컴포넌트 기본값과 설정 도구에도 반영했습니다.
- `SampleScene`과 `SeaCreatureTestScene`에서 `BackgroundRoot/WaveLayerRoot` 및 각각의 파도 스프라이트 3개를 제거했습니다. 외부 직렬화 참조가 없음을 확인한 뒤 Editor API로 저장했습니다.
- 바다·하늘·섬과 사용자가 추가한 배경은 유지합니다. 함선 항적과 크리쳐의 접수 포말·돌진 물살도 유지합니다.
- 변경 범위: 두 크리쳐 프리팹·몸체 머티리얼, 피격/수면 컴포넌트의 색 기본값, 몸체 셰이더, 두 씬, 설정 도구의 발광 색. 새 Inspector 항목은 없습니다.
- 연결 기록: `Artifacts/SeaCreatures/WhiteEmissionNoWave.txt`. 사전 사본은 `AgentScripts/Backups/WhiteEmissionNoWave-20261016/`에 있습니다.

## 변경 파일과 확인

- `Assets/SB/Scripts/Ship/Enemy/SeaCreatureEnemy.cs`
- `Assets/SB/Scripts/Ship/Enemy/SeaCreatureSurfacePresentation.cs`
- `Assets/SB/Art/Shaders/SeaCreatureSurfaceBody.shader`
- `Assets/SB/Prefabs/SeaCreatures/ReefbackLeviathan.prefab`
- Editor 설정 도구: `AgentScripts/ConfigureCreatureChargeCue.cs`, `PreviewCreatureChargeCue.cs`.

Unity C# 컴파일 완료, 셰이더 지원 및 그래픽 컴파일 메시지 0을 확인했습니다. 준비 전·준비 중·발광·돌진 네 포즈를 Editor의 임시 미리보기에서 렌더링해 확인했습니다. 이번 요청에서는 별도 전투 실행 테스트를 수행하지 않았습니다.

미리보기: `Artifacts/SeaCreatures/ChargeCueAuthoringPreview.png`. 프리팹 사전 사본: `AgentScripts/Backups/CreatureChargeCue-20261016/ReefbackLeviathan_Before.prefab`.

작업 시작 전부터 상선 장비 아이콘 누락(`MerchantLedger`)과 Play 종료 시 `SeaVoyageMotor`의 파괴된 Transform 참조 오류가 기록되어 있었습니다. 이번 돌진 연출 범위에는 포함하지 않았습니다. 편집 중 발생한 변수 이름 컴파일 오류와 미리보기 RenderTexture 해제 순서 오류는 수정했습니다.
