using SB.Core;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.Scripts.Reward
{
    public class EnemyRewardDropCompo : EntityComponent
    {
        [SerializeField] private EnemyRewardType rewardType;
        [SerializeField] private Transform dropPosition;
        
        private bool hasDropped;
        
        public EnemyRewardType RewardType => rewardType;

        private void OnEnable()
        {
            hasDropped = false;
        }

        public void DropReward()
        {
            if (hasDropped)
                return;

            hasDropped = true;
            
            Vector3 position = dropPosition != null ? dropPosition.position : transform.position;
            Bus<EnemyRewardDropRequestedEvent>.Raise(new EnemyRewardDropRequestedEvent(rewardType, position));
        }
    }
}
