using SB.Core;
using SB.Scripts.AttackCompo;
using UnityEngine;

namespace SB.Scripts.States
{
    public class ShipAttackState : IState
    {
        private Base_ShipAttackCompo _owner;
        private EntityAnimator _entityAnimator;

        public ShipAttackState(Base_ShipAttackCompo owner)
        {
            _owner = owner;
            _entityAnimator = _owner.Owner.GetCompo<EntityAnimator>();
        }

        public void Enter()
        {
            if (_owner.EnsureTarget() == false)
            {
                _owner.ChangeState(AttackStateType.Idle);
                return;
            }

            _entityAnimator?.SetParam("ATTACK", true);
        }

        public void Update()
        {
            if (_owner.Owner.IsDead)
            {
                _owner.ChangeState(AttackStateType.Dead);
            }
        }

        public void Exit()
        {
            _entityAnimator?.SetParam("ATTACK", false);
        }
    }
}
