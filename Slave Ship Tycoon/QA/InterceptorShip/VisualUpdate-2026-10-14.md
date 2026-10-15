# 요격선 외형 및 파티클 폭발 수정 — 2026-10-14

## 적용 결과

- 승인한 요격선의 포신을 약 16도 방향으로 낮추고 길이를 줄였습니다. 선체·돛·문양은 유지했습니다.
- 발사점과 포구 섬광 위치를 새 포신에 맞게 Visual 로컬 좌표 (0.378, -0.012, 0), 회전 Z=16도로 조절했습니다.
- 그림의 GUID를 유지해 모든 상태 클립과 모집 카드에서도 같은 수정 이미지가 표시됩니다.
- 실제 요격 판정 구간에서 두 탄이 가장 가까워진 위치의 중점을 폭발 생성 위치로 사용합니다.
- 기존 본선 폭발 프리팹 `Assets/SB/Art/Effects/TransportMainShip/TransportMainShipExplosion.prefab`에서 Flash / Fire / Smoke / WoodDebris 파티클을 복제해 요격용으로 축소했습니다.
- 최종 이펙트는 Mesh Particle System 4개(Flash / Fire / Smoke / ShellFragments)입니다. 기존 메시와 머티리얼을 공유하며, 파편의 색은 금속 계열로 지정했습니다.
- InterceptionBurst가 각 파티클을 재생하고 전체 IsAlive 상태가 끝나면 기존 PoolingManager로 반환합니다. 풀 재사용과 전투 종료 시 StopEmittingAndClear로 남은 입자를 정리합니다.

## 변경 파일

- Assets/SB/Art/Sprites/Ships/InterceptorShip.png
- Assets/SB/Art/Materials/Ships/InterceptorHull.mat — 새 이미지 높이에 따른 Cloth Canvas 갱신.
- Assets/SB/Prefabs/PlayerShips/InterceptorShip.prefab — 기본 Sprite, ShootPos, MuzzleVfx.
- Assets/SB/Prefabs/Effects/InterceptionBurst.prefab — 파티클 4종과 명시적 참조.
- Assets/SB/Scripts/Ship/Battle/Projectile/InterceptionBurst.cs — 기존 파티클 이펙트 방식의 재생·반환 처리.
- Assets/SB/Scripts/Ship/Battle/Projectile/InterceptorProjectile.cs — 충돌 두 점의 중점에서 폭발.
- Assets/SB/Editor/InterceptorShipSetup.cs — 재생성 시에도 같은 낮은 포신 위치와 파티클 구성 사용.

이미지는 built-in ImageGen으로 편집했습니다. 원본·수정본·전체 프롬프트는 `ArtConcepts/Ships/2026-10-14/InterceptorShip-angle-notes.md`에 기록했습니다.

## Inspector 조절

`InterceptionBurst.prefab`을 열어 각 Particle System의 Main / Emission / Size over Lifetime / Color over Lifetime에서 조절합니다.

| 파티클 | Burst 수 | 입자 수명 | 원본 대비 크기 |
|---|---:|---|---:|
| Flash | 3 | 0.099–0.154초 | 0.28 |
| Fire | 8 | 0.165–0.303초 | 0.28 |
| Smoke | 5 | 0.358–0.578초 | 0.28 |
| ShellFragments | 6 | 0.275–0.495초 | 0.45 |

Main Duration은 0.65초, Loop 및 Play On Awake는 꺼져 있습니다. World 공간에서 재생합니다. 정렬은 루트 InterceptionBurst의 Base Sorting Order / Sorting Order Per Unit / Sorting Order Offsets로 조절합니다. 추가 연결은 없습니다.

## 확인

- 수정한 발사점으로 실제 재생 검사 15/15 통과: 4개 탄도 × 60/15/5 FPS, 다른 행 제외, 본선 보호, 두 탄 동시 공격.
- 성공한 요격에서 보호 대상 HP 유지, 실패/과부하 탄은 정상 피해 적용.
- 실제 폭발 위치 (1.94, 0.98), 발사점 (-0.57, 0.18), 거리 2.63을 기록해 포구가 아닌 공중 요격 지점 생성 확인.
- 게임 화면 49프레임으로 섬광·불꽃·연기·파편을 확인했습니다. GIF는 실제 게임 렌더 프레임을 이어 붙인 미리보기입니다.
- 파티클 수명 종료 후 풀 반환과 동일 인스턴스 재생은 `pool-reuse-confirmed.txt` 참조.
- 제품 코드 컴파일 오류나 파티클 관련 신규 Console 오류 없음. 기존 `MerchantLedger needs an icon.`은 별개입니다.
- 초기 지급, 초기 편성, 스테이지, 뽑기 가중치는 수정하지 않았습니다.

기록 폴더: `QA/InterceptorShip/ParticleUpdate-2026-10-14`.
미리보기: `interception-preview.gif`.
