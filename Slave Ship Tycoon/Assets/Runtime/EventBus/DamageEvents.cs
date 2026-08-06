using UnityEngine;

namespace SB.Core.EventBus
{
    public readonly struct DamageAppliedEvent : IEvent
    {
        public readonly int Damage;
        public readonly Vector3 HitPosition;
        public readonly bool IsCritical;

        public DamageAppliedEvent(int damage, Vector3 hitPosition, bool isCritical)
        {
            Damage = damage;
            HitPosition = hitPosition;
            IsCritical = isCritical;
        }
    }
}
