# UX·항해 연출 개선 / 2026-09-14

## 적용

- SampleScene: 전투 영역 1080×980, 하단 조작 영역 재배치. 남색·황동색 중심으로 항해/함대 UI 통일.
- 강화: 현재 → 다음 효과, 필요한 재화 이름, 성공/부족 피드백.
- 화물: 화물 이름, 적재 수량, 개당 무게, 기본 보상 및 싣기/내리기 구분.
- 항로: 해적 해역/운송 진행 정보와 다음 행동 안내. 운송 장비 적용 시점 안내.
- 모집: 단일 결과 카드 확대·중앙 배치, 11회 결과 정렬, 미편성 안내와 함대 편성 버튼.
- 정산: 목적지 도착 장부 형태, 보상 순차 카운트, 지난 정산 재확인.
- 전투: 피격 색 반응과 체력 표시, 적 함선 색 완화, 짧은 포탄 연기 궤적. 기존 항적 효과 유지.
- 합성: 재료 불가 사유, 결과 강조, 소진된 카드 숨김.
- 일괄 합성 버튼: 클릭할 때만 실행. 같은 등급의 미편성 함선 3척을 합성하며 편성 함선·최고 등급·부족한 재료는 보존. 낮은 등급부터 처리하고 이번 클릭에서 만들어진 결과도 조건을 만족하면 연속 합성. 기존 카탈로그 순서의 결과 규칙 유지. 완료 횟수와 최종 순증가 결과 표시.

## 주요 파일

- `Assets/SB/Animations/Scenes/SampleScene.unity`
- `Assets/SB/Scripts/Fleet/PlayerFleetManager.cs`
- `Assets/SB/Scripts/UI/Fleet/FleetMergeScreen.cs`, `ShipRecruitmentView.cs`, `EscortInventoryItemView.cs`
- `Assets/SB/Scripts/UI/Cago/`의 화물·항로·정산 View/Controller
- `Assets/SB/Scripts/UI/Upgrade/MainShipUpgradeSlotView.cs`
- `Assets/SB/Scripts/UI/TransportEquipment/` View/Controller
- `Assets/SB/Scripts/Ship/Effects/ShipHullFeedback.cs`
- 적/아군 테스트 함선 8개, NormalProjectile, CagoButtonView 프리팹
- `Assets/SB/Editor/HarborUxSceneSetup.cs`, `HarborCombatPresentationSetup.cs`: 이번 배치의 명시적 Editor 적용 도구. 자동 실행되지 않음.
- `Assets/SB/Editor/Tests/FleetMergeAllTests.cs`

## Inspector 조절

- 각 함선의 ShipHullFeedback: 피격 색·지속 시간, 체력바 크기·위험 임계치·항상 표시 여부. HullCondition의 로컬 위치.
- ShipRecruitmentView: 단일 카드 배율, 공개 시간, 카드 간격.
- SaleSettlementView: 등장/유지/퇴장 시간, 보상 카운트 시간과 행 간격.
- MainShipUpgradeSlotView: 성공/실패 색과 표시 시간.
- FleetMergeScreen: 결과 강조 시간·최대 배율.
- 화면 배치는 씬 RectTransform에서 직접 조절 가능. Editor 적용 도구를 다시 실행하면 도구의 명시적 배치값이 적용됨.

## 검증

- Unity MCP를 사용하여 실제 Editor 조회·에셋 수정·컴파일·PlayMode·화면 캡처 수행. 셸은 소스/차이 읽기 및 문서 작성에 사용.
- 컴파일 성공, 마지막 Editor Console 오류/경고 0.
- 일괄 합성 EditMode 테스트 4/4 통과: 편성 보존·연속 합성·재클릭 무소모, 동일 등급의 서로 다른 함종, 다음 등급 부재 시 무소모, 큰 수량 처리/수량 보존식.
- 실제 일괄 합성 버튼 호출: 총 보유 15 → 9, 편성 일치, 재클릭 수량 변화 없음.
- 강화 1회 입력: 레벨 1 증가 및 해당 비용만 차감 확인.
- 화물 입력 반영, 모집 후 함대 화면 이동 확인.
- 11회 모집 비용 정확성 및 단일/11회 결과 화면 확인.
- 정산 표시/재표시가 재화를 추가 지급하지 않음 확인.
- 캡처: `Assets/QA/2026-09-14/final-battle.png`, `final-recruit.png`, `final-recruit-multi.png`, `final-merge-all.png`.
- PlayMode 종료, 씬과 에셋 저장 완료.
- `git diff --check`는 Unity 직렬화의 빈 YAML 값 뒤 공백을 보고함. 이 때문에 씬/프리팹 직렬화 파일을 일괄 정리하지 않음.

## 범위 및 후속 확인

- 기존 TargetSelector/ShipTargetingTests/항적 관련 사용자 변경은 유지.
- GameFlowManager와 SalesManager의 게임 규칙·정산 계산은 변경하지 않음.
- 실제 모바일 터치, 다양한 기기 비율, 오디오 연출은 이번 검증 범위에 포함되지 않음. 새 사운드 에셋 추가 없음.
- Inspector 참조와 버튼 연결은 적용 완료. 별도 수동 연결 불필요.
