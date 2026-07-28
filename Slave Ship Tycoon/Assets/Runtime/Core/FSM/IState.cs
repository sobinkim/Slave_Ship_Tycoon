namespace SB.Core
{
    public interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }
}
