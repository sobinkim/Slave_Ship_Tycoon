using SB.Scripts.AttackCompo;
using UnityEngine;

namespace SB.Scripts
{
    // Installed explicitly on raiders; target selection remains owned by TargetSelector.
    [DisallowMultipleComponent]
    public sealed class RaidTargetingIndicator : MonoBehaviour
    {
        [SerializeField] private Ship owner;
        [SerializeField] private Base_ShipAttackCompo attack;
        private Ship markedTarget;
        private RaidTargetMarker marker;

        private void OnEnable()
        {
            if (owner == null || attack == null)
            {
                Debug.LogError("RaidTargetingIndicator requires owner and attack references.", this);
                return;
            }
            attack.TargetChanged += MarkTarget;
            owner.OnDeathEvent.AddListener(ClearMarker);
            MarkTarget(attack.CurrentTarget);
        }

        private void OnDisable()
        {
            if (attack != null) attack.TargetChanged -= MarkTarget;
            if (owner != null) owner.OnDeathEvent.RemoveListener(ClearMarker);
            ClearMarker();
        }

        private void MarkTarget(Ship target)
        {
            ClearMarker();
            if (owner.IsDead || target == null || target.IsDead) return;
            markedTarget = target;
            marker = target.GetComponent<RaidTargetMarker>();
            if (marker == null)
                Debug.LogError("Raid target needs a RaidTargetMarker reference configured on its prefab.", target);
            else
                marker.Register(owner);
            target.OnDeathEvent.AddListener(ClearMarker);
        }

        private void ClearMarker()
        {
            if (marker != null) marker.Unregister(owner);
            if (markedTarget != null) markedTarget.OnDeathEvent.RemoveListener(ClearMarker);
            markedTarget = null;
            marker = null;
        }
    }
}
