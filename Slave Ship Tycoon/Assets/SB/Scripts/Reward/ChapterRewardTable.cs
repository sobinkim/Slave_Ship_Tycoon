using System;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.Scripts.Reward
{
    [Serializable]
    public struct CurrencyRewardRange
    {
        public CurrencyType currencyType;
        public int minAmount;
        public int maxAmount;

        public int Roll()
        {
            int min = Mathf.Min(minAmount, maxAmount);
            int max = Mathf.Max(minAmount, maxAmount);
            return UnityEngine.Random.Range(min, max + 1);
        }
    }

    [Serializable]
    public class EnemyRewardData
    {
        public EnemyRewardType rewardType;
        public CurrencyRewardRange[] rewards = Array.Empty<CurrencyRewardRange>();
    }

    [CreateAssetMenu(fileName = "ChapterRewardTable", menuName = "SB/Reward/Chapter Reward Table")]
    public class ChapterRewardTable : ScriptableObject
    {
        [SerializeField] private int chapter = 1;
        [SerializeField] private ChapterRouteType routeType;
        [SerializeField] private EnemyRewardData[] enemyRewards = Array.Empty<EnemyRewardData>();

        public int Chapter => chapter;
        public ChapterRouteType RouteType => routeType;

        public bool TryGetRewardData(EnemyRewardType rewardType, out EnemyRewardData rewardData)
        {
            if (enemyRewards != null)
            {
                for (int i = 0; i < enemyRewards.Length; i++)
                {
                    EnemyRewardData candidate = enemyRewards[i];
                    if (candidate != null && candidate.rewardType == rewardType)
                    {
                        rewardData = candidate;
                        return true;
                    }
                }
            }

            rewardData = null;
            return false;
        }
    }
}
