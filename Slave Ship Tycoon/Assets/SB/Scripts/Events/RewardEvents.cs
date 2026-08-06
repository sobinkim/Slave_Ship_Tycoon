using SB.Scripts.Reward;
using UnityEngine;

namespace SB.Core.EventBus
{
    public readonly struct EnemyRewardDropRequestedEvent : IEvent
    {
        public readonly EnemyRewardType RewardType;
        public readonly Vector3 DropPosition;

        public EnemyRewardDropRequestedEvent(EnemyRewardType rewardType, Vector3 dropPosition)
        {
            RewardType = rewardType;
            DropPosition = dropPosition;
        }
    }
}
