using SB.Core;
using SB.Scripts.States;

namespace SB.Scripts.AttackCompo
{
    public class ShipAttackStateMachine
    {
        private readonly StateMachine<AttackStateType> fsm;

        public ShipAttackStateMachine(Base_ShipAttackCompo owner)
        {
            fsm = new StateMachine<AttackStateType>();
            fsm.AddState(AttackStateType.Idle, new ShipIdleState(owner));
            fsm.AddState(AttackStateType.Attack, new ShipAttackState(owner));
            fsm.AddState(AttackStateType.Dead, new ShipDeadState(owner));
            fsm.ChangeState(AttackStateType.Idle);
        }

        public void Update()
        {
            fsm.Update();
        }

        public void ChangeState(AttackStateType state)
        {
            fsm.ChangeState(state);
        }
    }
}
