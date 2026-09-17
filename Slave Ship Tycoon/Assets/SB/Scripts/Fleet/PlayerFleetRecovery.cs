using System.Collections.Generic;
using SB.Core;
using SB.Core.EventBus;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts.Fleet
{
    [DisallowMultipleComponent]
    public sealed class PlayerFleetRecovery : MonoBehaviour
    {
        [SerializeField] private StageSpawnManager stageSpawnManager;
        [SerializeField, Min(0f), Tooltip("마지막 피해 이후 자연 회복을 시작하기까지의 게임 시간(초). 다시 피해를 받으면 갱신됩니다.")]
        private float recoveryDelayAfterDamageSeconds = 2f;
        private readonly List<Ship> ships = new List<Ship>();
        private bool isBattleActive;
        public bool IsRecovering { get; private set; }

        private void OnEnable()
        {
            Bus<BattleStartEvent>.OnEvent += HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        }
        private void OnDisable()
        {
            Bus<BattleStartEvent>.OnEvent -= HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            isBattleActive = false;
            IsRecovering = false;
            ships.Clear();
        }

        private void HandleBattleStarted(BattleStartEvent evt)
        {
            IsRecovering = false;
            isBattleActive = true;
            stageSpawnManager.CollectPlayerShips(ships);
        }

        private void HandleBattleEnded(StageBattleEndedEvent evt)
        {
            isBattleActive = false;
            IsRecovering = false;
            ships.Clear();
            if (!evt.IsClear) return;
            stageSpawnManager.CollectPlayerShips(ships);
            foreach (Ship ship in ships) ship.PrepareForRecovery();
            IsRecovering = ships.Count > 0;
        }

        private void Update() { TickRecovery(Time.deltaTime); }

        public void TickRecovery(float deltaSeconds)
        {
            if ((!isBattleActive && !IsRecovering) || deltaSeconds <= 0f) return;
            bool needsMoreRecovery = false;
            foreach (Ship ship in ships)
            {
                if (ship == null || !ship.isActiveAndEnabled) continue;
                EntityHealth health = ship.GetCompo<EntityHealth>();
                if (!health.IsAlive) continue;
                // Only heal for the part of this frame after the damage delay expires.
                double recoveryStartTime = health.LastDamageTimeSeconds + recoveryDelayAfterDamageSeconds;
                float eligibleRecoverySeconds = (float)System.Math.Clamp(
                    Time.timeAsDouble - recoveryStartTime, 0d, deltaSeconds);
                if (eligibleRecoverySeconds <= 0f)
                {
                    needsMoreRecovery |= health.CurrentHealth < health.MaxHealth;
                    continue;
                }
                ShipStatCompo stats = ship.ShipStatCompo;
                float recoveryPerSecond = stats.GetValue(
                    stats.GetCommonStatSO(ShipCommonStatType.HealthRecoveryPerSecond));
                PlayerFleetUpgradeProvider upgradeProvider = stageSpawnManager.PlayerFleetUpgradeProvider;
                if (upgradeProvider != null)
                    recoveryPerSecond += upgradeProvider.HealthRecoveryPerSecond;
                health.ApplyHeal(Mathf.Max(0f, recoveryPerSecond) * eligibleRecoverySeconds);
                needsMoreRecovery |= health.CurrentHealth < health.MaxHealth;
            }
            if (IsRecovering)
                IsRecovering = needsMoreRecovery;
        }
    }
}
