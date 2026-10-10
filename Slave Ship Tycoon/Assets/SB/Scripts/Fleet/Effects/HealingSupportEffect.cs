using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Fleet.Effects
{
    [CreateAssetMenu(menuName = "Fleet/Effects/Healing support")]
    public sealed class HealingSupportEffect : FleetEffectDefinition
    {
        // A capsule heals on impact; this definition only presents the selected ally's status icon.
        public override void CollectTargets(Ship source, IReadOnlyList<Ship> allies, List<Ship> targets)
        {
            targets.Clear();
        }
    }
}

