using SB.Core;
using SB.Scripts.AttackCompo;
using UnityEngine;

namespace SB.Scripts.States
{
    public class ShipIdleState : IState
    {
        private Base_ShipAttackCompo _owner;

        public ShipIdleState(Base_ShipAttackCompo owner)
        {
            _owner = owner;
        }

        public void Enter()
        {
        }

        public void Update()
        {
            if (_owner.Owner.IsDead)
            {
                _owner.ChangeState(AttackStateType.Dead);
                return;
            }

            if (_owner.EnsureTarget() == false)
                return;

            _owner.TickCooldown(Time.deltaTime);

            if (_owner.CanAttack)
                _owner.ChangeState(AttackStateType.Attack);
        }

        public void Exit()
        {
        }
    }
}
