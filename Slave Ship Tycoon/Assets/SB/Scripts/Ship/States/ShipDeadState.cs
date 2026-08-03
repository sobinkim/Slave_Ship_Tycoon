using SB.Core;
using UnityEngine;

namespace SB.Scripts.States
{
    public class ShipDeadState : IState
    {
        private Ship _owner;
        private EntityAnimator _entityAnimator;

        public ShipDeadState(Ship owner)
        {
            _owner = owner;
            _entityAnimator = _owner.GetCompo<EntityAnimator>();
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
