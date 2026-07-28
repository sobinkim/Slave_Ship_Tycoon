using System;
using SB.Core;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class Enemy : Ship
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            OnDeathEvent.AddListener(Death);
        }

        private void OnDisable()
        {
            OnDeathEvent.RemoveListener(Death);
        }

        public override void OnSpawnedFromPool()
        {
            base.OnSpawnedFromPool();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _healthCompo.ApplyDamage(50);
                 
            }
        }

        private void Death()
        {
            print(EntityName+"사망");
            Bus<EnemyEvents.EnemyDead>.Raise(new EnemyEvents.EnemyDead(this));
        }

        public override void OnDespawnedToPool()
        {
        }
    }
}
