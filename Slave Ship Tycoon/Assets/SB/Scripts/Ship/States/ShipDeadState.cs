using SB.Core;
using SB.Scripts.AttackCompo;
using UnityEngine;

namespace SB.Scripts.States
{
    public class ShipDeadState : IState
    {
        private Base_ShipAttackCompo _owner;
        private EntityAnimator _entityAnimator;

        public ShipDeadState(Base_ShipAttackCompo owner)
        {
            _owner = owner;
            _entityAnimator = _owner.Owner.GetCompo<EntityAnimator>();
        }

        public void Enter()
        {
            _entityAnimator.SetParam("Dead", true);
        }

        public void Update()
        {
            
        }

        public void Exit()
        {
            _entityAnimator.SetParam("Dead", false);
        }
    }
}