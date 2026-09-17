# 철갑 방어함 사용 및 조절

## 에셋
- 프리팹: `Assets/SB/Prefabs/PlayerShips/IroncladDefender.prefab`
- 함선 데이터: `Assets/SB/Data/PlayerFleet/EscortShips/Escort_IroncladDefender.asset`
- 이미지: `Assets/SB/Art/Sprites/Ships/IroncladDefender.png`
- 컨트롤러/클립: `Assets/SB/Animations/Ships/IroncladDefender/`
- 비교 렌더: `Assets/QA/IroncladDefender-comparison.png` (왼쪽 기존, 오른쪽 철갑선)

기존 TestEscort_A의 컴포넌트와 참조 구조를 복사한 독립 프리팹이다. 기존 함선 데이터는 교체하지 않았다. 새 데이터는 기존 카탈로그 끝에 추가했으며 초기 지급은 변경하지 않았다.

## 게임에서 지급하기
`PlayerFleetManager.TryGrantEscort(철갑선 데이터, 수량)`으로 지급하면 기존 보유 함선 UI 및 배치 기능을 그대로 이용한다. 초기 지급은 PlayerFleetManager의 Initial Owned Escorts에서 설정한다. 현재 일반 등급이며 합성은 기존의 등급별 결과 규칙을 따른다.

`EscortGachaTable`에 등록되어 실제 모집에서 등장한다. 기존 일반 함선 가중치 70을 기존 일반함 35 / 철갑 방어함 35로 나눠 전체 등급 확률을 유지했다. 전체 가중치 100 기준 확률은 철갑 방어함 35%, 기존 일반함 35%, 고급 25%, 희귀 5%다.

## Inspector와 Animation 창
- `EntityStatCompo > ShipStatCompo > Stat Container > Stat Overrides`: 체력 180, 공격력 8. 임시 시작 밸런스이며 함선별 조절 가능. 공격속도는 기존 공통값을 사용한다.
- 루트 Ship: EscortShip / Tanker. 현재 코드의 방어 역할이다.
- `AboutBattle/TargetSelector`: Front → Middle → Back, 각 열에서 모든 역할 허용. 기존 선택 규칙을 사용하므로 **같은 행의 정면 함선을 반드시 고르는 기능과 전열 방어함의 후열 차단 규칙은 아직 구현되지 않았다.** 타입 시스템 전체 개편과 별도 작업이다.
- `Visual`: 프리팹에서 크기 조절. 현재 로컬 스케일 (1.1, 1.3, 1.3). 애니메이션도 해당 스케일을 기록하므로 크기를 바꾸려면 Visual 위에 별도 크기 조절 부모를 두거나 클립의 Scale 곡선을 함께 수정한다. 런타임 배율 보정은 없다.
- `Visual/ShootPos`: 포구 위치 (.337, -.02, 0). `HitPoint`: (0, -.14, 0). `WaterImpactPoint`: (0, -.27, 0). 모두 Visual 기준 로컬 좌표.
- `HullCondition`: 체력바 높이 .46. `ShipHullFeedback`에서 피격색 및 체력바 속도 조절.
- 텍스처: 실제 알파, PPU 1400, Bilinear, 무압축, 밉맵 없음. 승인한 그림의 선명한 곡선을 유지한다.

## 애니메이션
승인된 단일 그림을 사용하는 Transform 애니메이션이다. 새 그림 프레임을 생성하거나 돛만 별도로 변형하지 않는다.
- Idle: 1.2초 반복, 작은 상하 움직임/기울기.
- Move: .8초 반복, 이동 시 조금 강한 움직임. 기존 물결 효과 참조 유지.
- Attack: .5초, 발사 직후 후방 반동. AttackStart 1/6초, AttackEnd 11/24초. 기존 AttackSpeedMultiplier로 강화 속도 적용.
- Dead: .75초, 기울면서 내려가고 축소되어 사라짐. Dead 이벤트 .6875초.
- Dead → Idle: Dead 파라미터가 false이면 전환. 재사용 시 Visual 위치와 크기 복구.

클립은 Animation 창에서 직접 조절한다. 발사 이벤트 이름과 컨트롤러 파라미터는 기존 전투 코드와 연결되므로 유지한다.

## 검증
- Unity 참조 검사: 누락 스크립트/깨진 참조 없음, 함선 데이터 IsConfigured 정상.
- Play Mode 격리 전투: 반복 발사 이벤트 13회 확인, 체력 180 / 최종 공격력 8 확인.
- 피해 30 적용 후 체력 150, 체력바 표시 확인.
- 치명타격 후 Dead 진입, Visual 축소 종료 및 체력바 숨김 확인.
- 풀 반환 후 동일 인스턴스 재사용: IsDead=false, 체력 180, Visual 스케일 (1.1,1.3,1.3), 렌더러 활성 복구.
- 실제 함대 매니저를 통한 임시 지급/배치 확인. 테스트는 Play Mode에서만 수행하며 종료해 테스트 오브젝트와 임시 편성을 제거했다.
- 실제 모집 테이블 참조 확인 및 2,000회 표본 추첨에서 철갑 방어함 679회 등장 확인.
- Unity 컴파일 실패 없음, 검증 중 Console 오류/경고 0. 합성 화면은 열릴 때 최상단으로 이동하고 이전 결과 팝업을 닫도록 보강했다.

## 이미지 출처
이 대화에서 built-in image_gen으로 만든 최종 승인 이미지(철갑판 확대, 방패 깃발 2개)를 그대로 복사했다. 이번 프리팹 작업에서는 이미지 재생성을 하지 않았다.
