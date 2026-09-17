# 방어선 공격·침몰 연출

## 적용
- `Assets/SB/Prefabs/PlayerShips/IroncladDefender.prefab`
- `Assets/SB/Animations/Ships/IroncladDefender/`의 Attack/Dead 연출 변경, Idle/Move에는 효과 초기화 커브 추가.
- 원래 방어선 이미지, Visual 크기, ShootPos, 체력바, 물결, 보호 효과 설정 유지.
- 기본선 ApprenticeGunship Attack/Dead의 AnimationEvent를 복사. 공격 0.5초, 발사 0.166667초, 종료 0.458333초. 침몰 0.75초, Dead 이벤트 0.6875초.

## 표현 방식
공격은 Visual/MuzzleVfx 아래의 작은 메시로 포구 화염과 연기를 표시한다. 화염은 발사 순간에 켜지고 0.25초에 꺼지며 연기는 0.25~0.416667초에 사라진다. 별도 게임플레이 코드나 발사 이벤트를 추가하지 않았다.

침몰은 `Fleet/Ship Waterline` 셰이더의 Sink Progress를 Dead 클립에서 0→1로 변경한다. 그림이 내려가면서 고정 수면 아래 부분이 잘리므로 선체, 돛, 깃발 순으로 물에 잠긴다. 함선 전체 알파를 낮추는 연출이 아니다. 대기/이동/공격 클립은 Sink Progress를 0으로 초기화한다. 루트/Visual 스케일 애니메이션은 추가하지 않았다.

## Inspector 조절
- 포구 위치: 프리팹 `Visual/MuzzleVfx` 로컬 위치. 기존 ShootPos 위치와 동일하게 연결한 초깃값이며, 이후 포구 위치 변경 시 둘을 함께 조절한다. ShootPos의 기존 스케일이 효과를 확대하지 않도록 형제 오브젝트로 분리했다.
- 화염/연기 크기: MuzzleFlash / MuzzleSmoke 자식 Transform Scale.
- 수면: `Assets/SB/Art/Materials/Ships/IroncladHull.mat`의 Waterline. Visual 로컬 Y, 현재 -0.27.
- 침몰 거리: 같은 머티리얼 Sink Distance, 현재 0.9. 최종 프레임에서 깃발까지 수면 아래로 내려가는 값.
- 침몰 속도: Dead 클립의 Sprite Renderer → material._SinkProgress 커브.
- 표시 시간: Attack 클립의 MuzzleFlash/MuzzleSmoke Renderer Enabled 커브.

## 검증
- Unity Shader 오류 없음, 스크립트 컴파일 실패 없음.
- 실제 AnimationClip 샘플링: 발사 시간 화염 ON, 이후 OFF, Dead 0.64초 Sink Progress=1, Idle 복귀 시 0.
- 기본선과 이벤트/길이 일치, Visual 스케일 커브 없음.
- `QA/IroncladCombatPreview.png`: 위 공격 4시점, 아래 침몰 4시점의 실제 Unity 렌더.

## 이미지 제작 검토
imagegen 스킬과 내장 imagegen 도구로 기존 방어선 및 기본선 공격 시트를 참조하여 3×2 포구 화염/연기 시트를 시험 제작했다. 요청 프롬프트는 기존 방어선 디자인 유지, 오른쪽 포구 발사, 6프레임, 투명 배경이었다. 배경 제거 재편집까지 시도했으나 투명 배경이 나오지 않아 게임에 반영하지 않았다. 최종 결과는 기존 원본 스프라이트와 Unity 메시/셰이더 연출을 사용한다. 새 이미지/외부 패키지 의존성은 없다.
