using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class MainShip : Ship
    {
        private void OnEnable()
        {
            OnDeathEvent.AddListener(Death);
        }
        
        private void OnDisable()
        {
            OnDeathEvent.RemoveListener(Death);
        }

        private void Death()
        {
            print("Test");
            Bus<MainShipDeadEvent>.Raise(new MainShipDeadEvent());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
                _healthCompo.ApplyDamage(50);
        }
    }
}