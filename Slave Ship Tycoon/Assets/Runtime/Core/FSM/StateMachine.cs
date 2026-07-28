using System;
using System.Collections.Generic;

namespace SB.Core
{
    public class StateMachine<TStateKey> where TStateKey : Enum
    {
        private readonly Dictionary<TStateKey, IState> states = new();

        public TStateKey CurrentStateKey { get; private set; }
        public IState CurrentState { get; private set; }

        public void AddState(TStateKey stateKey, IState state)
        {
            states[stateKey] = state;
        }

        public void ChangeState(TStateKey stateKey)
        {
            if (!states.TryGetValue(stateKey, out IState nextState))
                return;

            if (EqualityComparer<TStateKey>.Default.Equals(CurrentStateKey, stateKey) && CurrentState == nextState)
                return;

            CurrentState?.Exit();
            CurrentStateKey = stateKey;
            CurrentState = nextState;
            CurrentState.Enter();
        }

        public void Update()
        {
            CurrentState?.Update();
        }
    }
}
