using SB.Core;
using UnityEngine;

namespace SB.Scripts.States
{
    public class ShipAttackState : IState
    {
        private Ship _owner;
        private EntityAnimator _entityAnimator;

        public ShipAttackState(Ship owner)
        {
            _owner = owner;
            _entityAnimator = _owner.GetCompo<EntityAnimator>();
        }

        public void Enter()
        {
            if (_owner.EnsureAttackTarget() == false)
            {
                _owner.ChangeState(ShipStateType.Idle);
                return;
            }

            _owner.ApplyAttackAnimationSpeed();
            _entityAnimator?.SetParam("ATTACK", true);
        }

        public void Update()
        {
            if (_owner.IsDead)
            {
                _owner.ChangeState(ShipStateType.Dead);
            }
        }

        public void Exit()
        {
            _entityAnimator?.SetParam("ATTACK", false);
            _owner.ResetAttackAnimationSpeed();
        }
    }
}
