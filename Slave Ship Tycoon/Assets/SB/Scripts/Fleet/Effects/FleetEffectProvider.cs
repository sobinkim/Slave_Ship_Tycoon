using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Fleet.Effects
{
    public enum FleetEffectTargetingMode
    {
        Formation,
        ExplicitTarget
    }

    public sealed class FleetEffectProvider : MonoBehaviour
    {
        [SerializeField] private Ship owner;
        [SerializeField] private FleetEffectReceiver sourceReceiver;
        [SerializeField] private FleetEffectDefinition[] effects = Array.Empty<FleetEffectDefinition>();
        [SerializeField, Tooltip("진형 효과는 전투 시작에 적용하고, 명시적 대상 효과는 지원 컴포넌트가 대상을 지정합니다.")]
        private FleetEffectTargetingMode targetingMode;
        private Ship explicitTarget;
        private FleetEffectDefinition explicitEffect;
        private int explicitTargetSpawnGeneration;
        private struct Grant { public FleetEffectReceiver Receiver; public FleetEffectDefinition Effect; }
        private readonly List<Grant> grants = new List<Grant>();
        private readonly List<Ship> allies = new List<Ship>();
        private readonly List<Ship> targets = new List<Ship>();
        private bool isBound;
        public Ship Owner => owner;
        public bool IsSupplying => isBound && isActiveAndEnabled && owner != null && !owner.IsDead;
        private void OnEnable()
        {
            if (owner == null || sourceReceiver == null)
            { Debug.LogError("Effect provider requires owner and source receiver.", this); return; }
            owner.OnDeathEvent.AddListener(HandleDeath);
            Bus<StageBattleInitializedEvent>.OnEvent += HandleBattleInitialized;
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        }
        private void OnDisable()
        {
            if (owner != null) owner.OnDeathEvent.RemoveListener(HandleDeath);
            Bus<StageBattleInitializedEvent>.OnEvent -= HandleBattleInitialized;
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            ReleaseEffects(owner != null && owner.IsDead);
        }
        private void HandleBattleInitialized(StageBattleInitializedEvent evt) { BindBattle(evt.BattleSpawnData); }
        private void HandleBattleEnded(StageBattleEndedEvent evt) { ReleaseEffects(false); }
        private void HandleDeath() { ReleaseEffects(true); }
        public void BindBattle(BattleSpawnData battle)
        {
            ReleaseEffects(false);
            allies.Clear();
            if (owner == null || owner.IsDead || !isActiveAndEnabled) return;
            if (owner.myShipData.ShipType == ShipType.Enemy)
            {
                foreach (Enemy enemy in battle.EnemySpawnData.Enemies) allies.Add(enemy);
            }
            else
            {
                allies.Add(battle.MainShip);
                foreach (Ship ship in battle.EscortShips) allies.Add(ship);
            }
            if (!allies.Contains(owner)) return;
            isBound = true;
            if (targetingMode == FleetEffectTargetingMode.ExplicitTarget) return;
            foreach (FleetEffectDefinition effect in effects)
            {
                if (effect == null) continue;
                targets.Clear();
                effect.CollectTargets(owner, allies, targets);
                foreach (Ship target in targets)
                {
                    FleetEffectReceiver receiver = target.GetCompo<FleetEffectReceiver>();
                    if (receiver == null) continue;
                    receiver.Add(this, effect, false);
                    grants.Add(new Grant { Receiver = receiver, Effect = effect });
                }

                // The provider's own shield communicates that its defensive effect is active,
                // even when the adjacent rear slot is currently empty.
                sourceReceiver.Add(this, effect, true);
                grants.Add(new Grant { Receiver = sourceReceiver, Effect = effect });
            }
        }
        public void SetExplicitTarget(Ship target, FleetEffectDefinition effect)
        {
            if (targetingMode != FleetEffectTargetingMode.ExplicitTarget) return;
            if (!IsSupplying || target == null || target.IsDead || !target.isActiveAndEnabled ||
                effect == null || !allies.Contains(target))
            {
                RemoveGrants(false);
                return;
            }

            if (explicitTarget == target && explicitEffect == effect &&
                explicitTargetSpawnGeneration == target.SpawnGeneration) return;
            RemoveGrants(false);
            FleetEffectReceiver receiver = target.GetCompo<FleetEffectReceiver>();
            if (receiver == null) return;
            explicitTarget = target;
            explicitTargetSpawnGeneration = target.SpawnGeneration;
            explicitEffect = effect;
            receiver.Add(this, effect, false);
            grants.Add(new Grant { Receiver = receiver, Effect = effect });
        }

        public void ReleaseEffects(bool shouldBreak)
        {
            isBound = false; // Gameplay protection ends before any visual animation starts.
            RemoveGrants(shouldBreak);
        }

        private void RemoveGrants(bool shouldBreak)
        {
            foreach (Grant grant in grants)
                if (grant.Receiver != null) grant.Receiver.Remove(this, grant.Effect, shouldBreak);
            grants.Clear();
            explicitTarget = null;
            explicitTargetSpawnGeneration = 0;
            explicitEffect = null;
        }
    }
}


