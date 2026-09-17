# 웨이브 클리어 후 함대 정비

전투 중과 클리어 후 정비 단계에서 현재 플레이어 함대가 회복한다. 회복량은 각 함선의 `HealthRecoveryPerSecond` 런타임 스탯과 함대 강화값의 합 × 게임 경과 시간이다. 일시정지 중에는 회복하지 않는다. 웨이브 클리어 뒤에는 회복 완료를 기다리지 않고 다음 전투를 시작하며, 살아남은 함선의 현재 HP와 회복은 다음 전투에도 이어진다. 전투 중 파괴된 함선은 정비 중 되살리지 않고, 새 전투가 시작될 때 최대 체력으로 복구한다.

## 조절

- 자연 회복은 마지막으로 실제 피해를 받은 뒤 2초부터 시작한다. 피격 시 대기시간이 갱신되며, 웨이브 전환은 이 시간을 초기화하지 않는다. `StageSpawnManager` 오브젝트의 `PlayerFleetRecovery > Recovery Delay After Damage Seconds`에서 조절한다. 일시정지 중에는 대기시간도 흐르지 않는다. 직접적인 스킬 회복에는 이 대기를 적용하지 않는다.

- 공통값: `Assets/SB/SO/CommonStat/HealthRecoveryPerSecond.asset`의 Base Value. 기본 5 HP/초이며 100 HP 회복에 20초가 걸린다.
- 함선별 값: 프리팹 `ShipStatCompo > Stat Container > Stat Overrides`의 해당 스탯에서 Use Override를 켜고 Override Base Value를 설정한다.
- 함대 강화값: 강화 UI의 `체력 회복 속도`에서 올린 수치가 모든 플레이어 함선의 기본 회복량에 더해진다.
- 양산 시 `ShipStatCompo`의 Health Recovery Per Second Stat 참조와 Stat Overrides 항목을 모두 연결한다. 현재 플레이어 프리팹 5개는 연결 완료했다.
- 스탯 에셋 최솟값은 0.01 HP/초다.

## 구조

- `PlayerFleetRecovery`: BattleStartEvent에서 현재 함대를 수집해 전투 중 매 프레임 ApplyHeal한다. 클리어 뒤에는 같은 방식으로 정비를 이어가며, 피해 감소와 무관하게 회복한다.
- `StageSpawnManager.CollectPlayerShips`: 현재 출전 함대를 명시적으로 제공한다.
- `GameFlowManager`: 정비 상태와 무관하게 다음 웨이브를 시작한다.
- `Ship.PrepareForRecovery`: 생존 함선의 현재 HP를 유지한다. 파괴된 호위함은 정비 중 회복하지 않는다.
- `Ship.PrepareForNextBattle`: 파괴된 함선만 새 전투 시작 시 최대 체력으로 복구한다. 생존 함선은 현재 HP를 유지한다.
- 다음 전투는 같은 플레이어 함대를 다시 배치해 현재 HP를 유지한다. 실패 후 재도전과 최초 스폰의 체력 초기화는 기존대로다.
- `EntityHealth.ResetHealth(float)`는 복원 시 시작 체력 비율을 지정한다. 기존 인자 없는 ResetHealth는 100% 초기화 동작을 유지한다.

## 확인

Unity 컴파일 통과. Play Mode에서 기본 5 HP/초와 함대 강화 2 HP/초가 합산되어 1초에 7 HP 회복되는 것을 확인했다. 파괴 함선은 정비 중 파괴 상태를 유지하고 새 전투에서 최대 체력으로 복구되며, 생존 함선은 현재 체력을 유지하는 것도 검증했다. 강화 UI 슬롯 참조와 성장 데이터 연결도 확인했다.
