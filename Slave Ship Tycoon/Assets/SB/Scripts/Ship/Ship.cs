using SB.Core;
using UnityEngine;

namespace SB.Scripts
{
    public class Ship : Entity, IPoolable
    {
        protected EntityAnimator _animator;
        protected EntityAnimatorTrigger _animatorTrigger;
        protected EntityStatCompo _statCompo;
        protected EntityHealth _healthCompo; 


        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _animator = GetCompo<EntityAnimator>();
            _animatorTrigger = GetCompo<EntityAnimatorTrigger>();
            _statCompo = GetCompo<EntityStatCompo>();
            _healthCompo = GetCompo<EntityHealth>();
        }

        public virtual void OnSpawnedFromPool()
        {
            IsDead = false;
            _healthCompo?.ResetHealth();
        }

        public virtual void OnDespawnedToPool()
        {
        }
    }
}
