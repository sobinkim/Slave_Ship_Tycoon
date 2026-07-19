namespace SB.Core.EventBus
{
    public struct EnemyEvents
    {
        public struct EnemyDead : IEvent
        {
            public Entity _entity;

            public EnemyDead(Entity entity)
            {
                _entity = entity;
            }
        }
    }
}