using SB.Core;
using UnityEngine;

namespace SB.Scripts.States
{
    public class ShipIdleState : IState
    {
        private Ship _owner;

        public ShipIdleState(Ship owner)
        {
            _owner = owner;
        }

        public void Enter()
        {
        }

        public void Update()
        {
            if (_owner.IsDead)
            {
                _owner.ChangeState(ShipStateType.Dead);
                return;
            }

            if (_owner.EnsureAttackTarget() == false)
                return;

            _owner.TickAttackCooldown(Time.deltaTime);

            if (_owner.CanAttack)
                _owner.ChangeState(ShipStateType.Attack);
        }

        public void Exit()
        {
        }
    }
}
