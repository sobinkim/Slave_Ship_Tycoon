# 항해 UI / 호위선 모집

참고 기획: https://app.notion.com/p/3a12a871b9c1819ea331fe626d820e39

## 사용

- Play 모드에서 하단 `모집` 또는 `함대`의 소환 버튼으로 연다.
- 1회 / 11회 모집은 기존 EscortGachaManager의 확률, 가격, 재화와 보유 함대 시스템을 사용한다. 화면 샘플의 50/500 가격은 사용하지 않는다.
- 순차 카드 등장, 등급색 플래시, 건너뛰기, 확인, 배경 클릭 닫기를 제공한다.
- 모집 화면을 닫아도 다시 하단 메뉴에서 열 수 있다.
- 광고 및 자동 모집은 연결되지 않은 샘플 컨트롤을 숨겼다.

## 변경

- SampleScene: 네이비/청록 공통 패널 스타일, 하단 모집 버튼, 모집/함대 연결.
- SummonUI.prefab: 실제 모집 결과, 모달 배경, 결과 연출과 버튼 연결.
- ShipRecruitmentView.cs: 모집 UI 상태와 연출. 결과 지급은 기존 매니저 담당.
- PlayerFleetPanelController.cs: 기존 소환 버튼으로 모집 화면 진입.
- MalgunGothic SDF.asset: 누락된 재질/atlas 복구. 씬 및 관련 UI 프리팹의 비활성화된 TMP 컴포넌트와 재질 참조 복구.
- KoreanUiLocalizationSetup.cs: 신규 폰트 생성 시 재질과 atlas도 에셋에 저장하도록 수정.
- HarborUiSetup.cs: 이번 스타일 적용에 사용한 Editor 유틸리티. 초기 적용용이며 이후 Inspector 커스텀을 덮어쓸 수 있으므로 반복 실행하지 않는다.

## Inspector

- SummonUI / ShipRecruitmentView: Reveal Seconds, Card Delay Seconds, Initial Scale, Grade Colors, Single Card Position.
- Multi Count는 준비된 카드 수(11) 이하로 설정한다.
- 가격은 기존 PlayerFleetSystem / EscortGachaManager / Summon Cost에서 조절한다.
- 개별 패널 배치와 색상은 해당 RectTransform/Image에서 조절한다.

## 검증

- 컴파일 성공.
- 씬 저장 후 재로드: 모집 패널/매니저/재화/배경 연결 유지, 비활성 TMP 텍스트 0개.
- Play 모드: 11회 가격 55 차감, 연속 호출 시 중복 차감 방지, 단일 결과 표시, 건너뛰기, 확인/닫기와 배경 종료 확인.
- 재화 부족 시 다중 모집 차감 0 확인.
- 1080x1920 실행 화면의 한글 표시 및 결과 화면 시각 확인. 최종 단일 카드 중앙 배치는 후속 컴파일 및 저장 확인.
- 마지막 실행 검증에서 새 콘솔 오류 없음. 모바일 기기/노치/다른 화면 비율과 빌드는 미검증.
