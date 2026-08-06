using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Visual
{
    public class DㅇamageFloatingTextPresenter : MonoBehaviour
    {
        private void OnEnable()
        {
            Bus<DamageAppliedEvent>.OnEvent += HandleDamageApplied;
        }

        private void OnDisable()
        {
            Bus<DamageAppliedEvent>.OnEvent -= HandleDamageApplied;
        }

        private void HandleDamageApplied(DamageAppliedEvent evt)
        {
            FloatingTextType textType = evt.IsCritical
                ? FloatingTextType.Critical
                : FloatingTextType.Damage;

            Bus<FloatingTextRequestedEvent>.Raise(
                new FloatingTextRequestedEvent(textType, evt.Damage, evt.HitPosition)
            );
        }
    }
}
