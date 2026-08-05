using SB.Core;

namespace SB.Scripts.States
{
    public class ShipMoveState : IState
    {
        private readonly Ship _owner;
        private readonly EntityAnimator _entityAnimator;

        public ShipMoveState(Ship owner)
        {
            _owner = owner;
            _entityAnimator = owner.GetCompo<EntityAnimator>();
        }

        public void Enter()
        {
            _entityAnimator?.SetParam("MOVE", true);
        }

        public void Update()
        {
            if (_owner.IsDead)
                _owner.ChangeState(ShipStateType.Dead);
        }

        public void Exit()
        {
            _entityAnimator?.SetParam("MOVE", false);
        }
    }
}
