using System;
using UnityEngine;

namespace SB.Core
{
    [DisallowMultipleComponent]
    public class EntityAnimatorTrigger : EntityComponent
    {
        public event Action OnAnimationEndTrigger;
        public event Action OnAttackStartTrigger;
        public event Action OnAttackEndTrigger;
     
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
        
        public void AttackEnd()
        {
            OnAttackEndTrigger?.Invoke();
        }
    }
}
