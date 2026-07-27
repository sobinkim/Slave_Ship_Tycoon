using System;
using UnityEngine;

namespace SB.Core
{
    [DisallowMultipleComponent]
    public class EntityAnimatorTrigger : EntityComponent
    {
        public event Action OnAnimationEndTrigger;
        public event Action OnAnimationGenericTrigger;
        public event Action OnAttackStartTrigger;
        public event Action OnAttackVfxTrigger;
        public event Action<bool> OnManualRotationTrigger;
        public event Action OnDamageCastTrigger;
        public event Action<bool> OnDamageToggleTrigger;
        public event Action OnCastSkillTrigger;
        public event Action OnDeadTrigger;

        // Animation Event
        public void Dead()
        {
            OnDeadTrigger?.Invoke();
        }

        // Animation Event
        public void AnimationEnd()
        {
            OnAnimationEndTrigger?.Invoke();
        }

        // Animation Event
        public void AttackStart()
        {
            OnAttackStartTrigger?.Invoke();
        }

        // Animation Event
        public void PlayAttackVFX()
        {
            OnAttackVfxTrigger?.Invoke();
        }

        // Animation Event
        public void GenericTriggerStart()
        {
            OnAnimationGenericTrigger?.Invoke();
        }

        // Animation Event
        public void StartManualRotation()
        {
            OnManualRotationTrigger?.Invoke(true);
        }

        // Animation Event
        public void StopManualRotation()
        {
            OnManualRotationTrigger?.Invoke(false);
        }

        // Animation Event
        public void DamageCast()
        {
            OnDamageCastTrigger?.Invoke();
        }

        // Animation Event
        public void StartDamageCast()
        {
            OnDamageToggleTrigger?.Invoke(true);
        }

        // Animation Event
        public void StopDamageCast()
        {
            OnDamageToggleTrigger?.Invoke(false);
        }

        // Animation Event
        public void CastSkill()
        {
            OnCastSkillTrigger?.Invoke();
        }
    }
}
