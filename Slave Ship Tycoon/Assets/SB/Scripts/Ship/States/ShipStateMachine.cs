using SB.Core;
using SB.Scripts.States;

namespace SB.Scripts
{
    public class ShipStateMachine
    {
        private readonly StateMachine<ShipStateType> fsm;

        public ShipStateMachine(Ship owner)
        {
            fsm = new StateMachine<ShipStateType>();
            fsm.AddState(ShipStateType.Idle, new ShipIdleState(owner));
            fsm.AddState(ShipStateType.Attack, new ShipAttackState(owner));
            fsm.AddState(ShipStateType.Dead, new ShipDeadState(owner));
            fsm.ChangeState(ShipStateType.Idle);
        }

        public void Update()
        {
            fsm.Update();
        }

        public void ChangeState(ShipStateType state)
        {
            fsm.ChangeState(state);
        }
    }
}
