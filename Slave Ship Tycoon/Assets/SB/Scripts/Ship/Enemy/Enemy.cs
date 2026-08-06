using SB.Core.EventBus;
using SB.Scripts.Reward;

namespace SB.Scripts
{
    public class Enemy : Ship
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            OnDeathEvent.AddListener(Death);
        }

        private void OnDisable()
        {
            OnDeathEvent.RemoveListener(Death);
        }

        public override void OnSpawnedFromPool()
        {
            base.OnSpawnedFromPool();
        }

        protected override void Update()
        {
            base.Update();
        }

        private void Death()
        {
            EnemyRewardDropCompo rewardDropCompo = GetCompo<EnemyRewardDropCompo>();
            rewardDropCompo?.DropReward();

            Bus<EnemyEvents.EnemyDead>.Raise(new EnemyEvents.EnemyDead(this));
        }

        public override void OnDespawnedToPool()
        {
        }
    }
}
