using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Fleet.Effects
{
    [CreateAssetMenu(menuName = "Fleet/Effects/Immediate Rear Protection")]
    public sealed class ImmediateRearProtection : FleetEffectDefinition
    {
        [SerializeField, Tooltip("전열에 배치된 공급 함선만 보호를 제공합니다.")]
        private bool requireFrontLine = true;
        [SerializeField, Range(0.01f, 1f), Tooltip("보호 중인 함선이 받는 피해 배율입니다. 0.7은 피해 30% 감소입니다.")]
        private float protectedDamageMultiplier = .7f;
        public override bool BlocksDirectTargeting => true;
        public override float IncomingDamageMultiplier => protectedDamageMultiplier;

        public override void CollectTargets(Ship source, IReadOnlyList<Ship> allies, List<Ship> targets)
        {
            Vector2Int slot = source.myShipData.SpawnSlot;
            bool isEnemy = source.myShipData.ShipType == ShipType.Enemy;
            int frontColumn = isEnemy ? 0 : PlayerFleetLoadout.ColumnCount - 1;
            if (slot.x < 0 || slot.y < 0 || (requireFrontLine && slot.x != frontColumn)) return;
            Vector2Int rearSlot = slot + new Vector2Int(isEnemy ? 1 : -1, 0);
            foreach (Ship ally in allies)
            {
                if (ally == null || ally == source || !ally.isActiveAndEnabled || ally.IsDead) continue;
                if (ally.myShipData.SpawnSlot == rearSlot)
                {
                    targets.Add(ally);
                    return; // Never skip an empty slot or protect another row.
                }
            }
        }
    }
}
