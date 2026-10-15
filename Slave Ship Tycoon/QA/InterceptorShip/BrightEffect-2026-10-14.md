# 요격 폭발 밝기·형태 개선 — 2026-10-14

## 적용

- 기존 포구용 메시를 공중 폭발에 맞는 중심 정렬 메시로 교체했습니다.
- Glow / Flash / HotCore / Fire / Smoke / Sparks / ShellFragments의 Mesh Particle System 7개로 구성했습니다. 전체 최초 입자는 24개입니다.
- 흰 중심과 금빛 섬광은 빛을 더하는 전용 재질, 불꽃·연기·금속은 정상 알파 혼합 재질을 사용합니다.
- 불꽃은 흰색 → 금색 → 주황색으로 변하고 완만하게 팽창합니다. 끝에서 검게 변하는 곡선을 제거하고 투명도로 사라지게 했습니다.
- 후속 조정: 최초 크기 기준 전체 파티클 시작 크기를 1.5배로 키워 전투 화면에서 더 잘 보이게 했습니다. 각 층의 모양과 상대 비율은 유지했습니다.
- 연기는 0.045초 뒤 3개만 나오며 최대 알파 0.28로 제한했습니다. 파편은 밝은 금속 메시로 교체했습니다.
- 포신, 함선 수치, 요격 판정 및 모집 데이터는 이번 수정에서 바꾸지 않았습니다.

## 변경 파일

- Assets/SB/Prefabs/Effects/InterceptionBurst.prefab
- Assets/SB/Art/Shaders/InterceptionParticles.shader
- Assets/SB/Editor/InterceptorExplosionAuthoring.cs
- Assets/SB/Editor/InterceptorShipSetup.cs — 명시적 이펙트 조립을 전용 authoring으로 연결.
- Assets/SB/Art/Effects/Interception/ 아래 전용 메시 6개와 재질 2개.

공유 WoodFragments 셰이더나 기존 다른 이펙트 자산을 수정하지 않았습니다. 글로벌 Bloom이나 렌더링 설정 변경, 외부 패키지 추가는 없습니다.

## Inspector

- 전체 빛의 세기: InterceptionGlow.mat의 Brightness(현재 1.35).
- 각 층 크기·수명·색·입자 수: InterceptionBurst.prefab의 해당 Particle System Main / Emission / Color over Lifetime / Size over Lifetime.
- 반투명 재질: InterceptionColor.mat(현재 Brightness 1).
- 전체 파티클이 끝나면 기존 InterceptionBurst의 IsAlive 판정으로 풀에 반환됩니다.

| 파티클 | 수 | 크기 | 수명 |
|---|---:|---|---|
| Glow | 1 | 0.66–0.72 | 0.23–0.26초 |
| Flash | 1 | 0.41–0.48 | 0.22–0.25초 |
| HotCore | 1 | 0.26–0.30 | 0.20–0.23초 |
| Fire | 6 | 0.20–0.29 | 0.22–0.33초 |
| Smoke | 3 | 0.21–0.30 | 0.32–0.42초 |
| Sparks | 7 | 0.12–0.20 | 0.20–0.30초 |
| ShellFragments | 5 | 0.068–0.113 | 0.24–0.37초 |

추가 연결은 필요 없습니다.

## 검증

- 밝은 바다색 배경에서 실제 게임 화면 49프레임 캡처. 흰 중심·금빛 섬광과 후반 불꽃을 시각 확인.
- 실제 재생 15/15 통과: 네 가지 탄도 × 60/15/5 FPS와 다른 행 제외·본선 보호·두 탄 동시 공격.
- 파티클 수명 종료 후 반환, 동일 인스턴스 재사용, 두 번째 재생·반환 확인: Bright-2026-10-14/pool-reuse-confirmed.txt.
- 새 메시 6개의 색 데이터가 Color32(UNorm8)인 것 확인. 최초 Float32 메시 색 저장으로 발생한 파란 색 표시 문제를 수정했습니다. 이 호환성은 [Unity 직원의 설명](https://discussions.unity.com/t/urp-shader-graph-particle-system-vertex-colored-3d-object/888454)을 참고하고 실제 렌더로 검증했습니다.
- 새 셰이더 컴파일 메시지 없음. 신규 제품 오류 없음. 기존 MerchantLedger 아이콘 오류는 별개입니다.
- 중간 재질 비교용 임시 검사에서 재생 중 randomSeed 변경 경고가 발생했습니다. 최종 이펙트 코드에는 해당 조작이 없으며, 최종 재생 검사에서는 재발하지 않았습니다.

미리보기: Bright-2026-10-14/interception-preview.gif. 게임 캡처를 이어 붙인 GIF이며 생성 이미지가 아닙니다.
이전 버전 백업: AgentScripts/Backups/InterceptorPolish-20261014/InterceptionBurst.prefab.backup.
