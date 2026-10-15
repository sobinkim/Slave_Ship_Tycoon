# 데미지·MISS 텍스트 (2026-10-13)

## 목적과 적용

방치형 전투에서 개별 피해량과 성장한 숫자를 읽을 시간을 주면서, 연타가 순간적으로 겹쳐 사라지던 문제를 줄였습니다. 실제 피해량·공격 속도·명중률·운·치명타 판정은 변경하지 않았습니다.

- 일반 피해: Lilita One, 44px, 밝은 아이보리에서 옅은 금색으로 내려오는 명암, 짙은 외곽선과 작은 그림자. 기존 `D` 접미사는 제거하고 쉼표로 자릿수를 구분합니다.
- 첫 0.055초에 0.9 → 1.12배로 짧게 팝하고, 0.085초 동안 원래 크기로 돌아옵니다. 총 0.65초 동안 위로 72px 이동하며 앞 65%는 선명하게, 마지막 약 0.23초는 페이드합니다.
- 각 숫자는 타격 위치에서 독립적으로 올라갑니다. 연타하면 앞의 숫자가 올라가는 동안 다음 숫자가 아래에서 생겨 잠깐 겹칩니다. 새 타격이 이전 숫자의 위치나 수명을 바꾸지 않습니다. 기존 4줄 정렬·38px 밀기·초과 숫자 조기 소멸은 사용자 피드백에 따라 제거했습니다. 개별 피해를 합산하거나 버리지 않습니다.
- 치명타 표시: 새 `FloatingText_Critical.prefab`, 52px, 금색·주황 명암, 1.22배 팝, 0.75초, 위로 82px 이동, `!` 접미사. 기존 Critical 이벤트를 표시할 수 있도록 씬의 View Datas에 연결했습니다. 치명타 발생 로직을 새로 추가한 것은 아닙니다.
- MISS: 같은 서체의 청백색, 월드 글자 크기 1.5, 0.9초. 작은 팝 후 대부분 선명하게 유지하다 끝에서 흐려지고 약간 옆·위로 이동합니다. 함선별 기존 단일 라벨을 사용하며 기존 0.4초 재표시 간격과 MISS 집계 규칙은 유지합니다.
- 전투 종료 시 남은 피해 표시를 정리합니다. 풀 반환·재사용 때 코루틴, 크기, 알파, 시작 위치가 초기화됩니다. 골드 획득 텍스트의 기존 외형·동작은 유지합니다.

## 관련 파일

- `Assets/SB/Scripts/Visual/FloatingTextView.cs`: 팝, 선명한 유지 구간, 독립 상승, 마지막 페이드.
- `Assets/SB/Scripts/Visual/FloatingTextManager.cs`: 타격 위치에 개별 생성, 전투 종료 정리, 쉼표 표기. 줄별 그룹화가 없어져 표시 그룹용 `TargetInstanceId` 전달도 제거했습니다.
- `Assets/SB/Scripts/Ship/Battle/Projectile/ShipMissFeedback.cs`: 기존 MISS 라벨의 연출.
- `Assets/SB/Prefabs/UI/FloatingText_Damage.prefab`, `FloatingText_Critical.prefab`.
- MISS가 있는 플레이어·적 함선 프리팹 21개: 기존 라벨과 연결은 유지하고 폰트·색·크기·표시 시간을 수정했습니다.
- `Assets/SB/Animations/Scenes/SampleScene.unity`: FloatingTextRoot의 View Datas 및 발생 위치 설정.
- `Assets/SB/Fonts/Combat/`: Lilita One 원본 TTF, OFL 라이선스, FONTLOG, 숫자·MISS용 정적 TMP 폰트. 512×512 아틀라스와 20개 글자를 사용합니다.

## Inspector 조절

| 대상 | 항목 | 현재 값 |
| --- | --- | --- |
| Canvas/FloatingTextRoot / FloatingTextManager | Damage Position Offset | (0, 18) 캔버스 픽셀 |
| FloatingText_Damage / FloatingTextView | Duration / Fade Start Normalized | 0.65 / 0.65 |
| 같은 컴포넌트 | Pop Seconds / Settle Seconds / Entry Scale / Peak Scale | 0.055 / 0.085 / 0.9 / 1.12 |
| 같은 컴포넌트 | Move Offset / Entry Rise Pixels | (0, 72) / 0 |
| FloatingText_Critical / FloatingTextView | Duration / Move Offset / Peak Scale | 0.75 / (0, 82) / 1.22 |
| 각 함선 / ShipMissFeedback | Visible Seconds / Minimum Interval Seconds | 0.9 / 0.4 |
| 같은 컴포넌트 | Fade Start Normalized / Rise Local Units / Drift Local Units | 0.65 / 0.07 / 0.04 |

## 참고와 사용자 피드백

- [사용자 참고 영상 — 슬레이어 키우기 1분 꿀팁](https://www.youtube.com/shorts/acbxcTQMiqM): 브라우저에서 실제 재생하여 전투 중 숫자가 타격 부근에 연속 발생하고 상승하면서 짧게 겹치는 모습을 확인했습니다. 1.3~1.5초 동안 줄로 정렬하던 첫 구현은 요청한 누적 방식과 달라 폐기했습니다. 현재 수치는 영상에서 계측한 값이 아니라 우리 게임 크기와 전투 화면에 맞춰 정한 값입니다.

- [Blizzard — Engineering Diablo III's Damage Numbers](https://news.blizzard.com/en-us/article/19996041/engineering-diablo-iii-s-damage-numbers): 큰 숫자의 성장 만족감과 가독성을 다루며 숫자 길이, 밝기, 움직임, 강조 색을 검토한 개발자 설명입니다. 이를 참고해 숫자를 즉시 축약하지 않고 쉼표와 선명한 유지 구간을 사용했습니다.
- [Google Fonts — Lilita One](https://github.com/google/fonts/tree/main/ofl/lilitaone): 두껍고 약간 압축된 짧은 글용 서체로, 둥근 함선 일러스트와 작은 숫자에 맞춰 선택했습니다. 원본 OFL 1.1과 저작권 고지를 프로젝트에 동봉했습니다.

## 플레이 확인

- 수정 후 Unity 6000.0.59f2, SampleScene, 1080×1920 일반 전투에서 피해 숫자 3개가 동시에 보이는 상태를 확인했습니다. 현재 화면: `Artifacts/CombatText/After-Flow.png`. `After-Stack.png`는 폐기한 첫 구현의 기록입니다.
- 독립 상승 확인: 표시 시작 약 0.15초 뒤 위치 (0, 16.99)에서 새 피해 이벤트를 보냈을 때 기존 숫자의 위치가 그대로였고, 이후 (0, 37.13)까지 상승했습니다. 중간 알파 1, 약 0.84초 후 비활성·풀 반환을 확인했습니다. 표시 이벤트만 사용해 전투 수치에는 영향을 주지 않았습니다.
- 표시 이벤트만 사용한 별도 확인으로 `12,345`, `123,456`, `1,234,567`, `12,345,678!`, `123,456,789` 표기와 선명한 유지 구간, 만료·풀 반환을 확인했습니다. 이 확인용 숫자는 실제 전투 피해량이나 밸런스 데이터에 저장하지 않았습니다.
- C# 컴파일 정상, 새 폰트 글리프 누락 없음, 이번 변경의 새 런타임 오류 없음. 기존 `MerchantLedger needs an icon.` 오류는 별개입니다.
- 다른 화면 비율과 실기기 성능은 확인하지 않았습니다. 추가 Inspector 연결은 필요 없습니다.
