namespace SB.Core.EventBus
{
    public static class EnemyEvents
    {
        public readonly struct EnemyDead : IEvent
        {
            public readonly Entity Entity;

            public EnemyDead(Entity entity)
            {
                Entity = entity;
            }
        }
    }
}
