#if UNITY_EDITOR
using SB.Scripts;
using SB.Scripts.AttackCompo;

namespace SB.Tests
{
    public sealed class ShipAttackProbe : Base_ShipAttackCompo
    {
        public int FireCount { get; private set; }
        public Ship FiredTarget { get; private set; }

        protected override void Fire()
        {
            FireCount++;
            FiredTarget = CurrentTarget;
        }
    }
}
#endif
