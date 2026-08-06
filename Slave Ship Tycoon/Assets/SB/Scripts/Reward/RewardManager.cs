using System;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using SB.Scripts.Visual;
using UnityEngine;

namespace SB.Scripts.Reward
{
    public class RewardManager : MonoBehaviour
    {
        [SerializeField] private ChapterRewardTable[] rewardTables = Array.Empty<ChapterRewardTable>();
        [SerializeField] private CurrencyManager currencyManager;

        private int currentChapter;
        private int currentStage;
        private ChapterRouteType currentRouteType;
        private bool currentBattleIsBoss;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            Bus<StageStartedEvent>.OnEvent += HandleStageStarted;
            Bus<EnemyRewardDropRequestedEvent>.OnEvent += HandleEnemyRewardDropRequested;
        }

        private void OnDisable()
        {
            Bus<StageStartedEvent>.OnEvent -= HandleStageStarted;
            Bus<EnemyRewardDropRequestedEvent>.OnEvent -= HandleEnemyRewardDropRequested;
        }

        private void HandleStageStarted(StageStartedEvent evt)
        {
            currentChapter = evt.Chapter;
            currentStage = evt.Stage;
            currentRouteType = evt.RouteType;
            currentBattleIsBoss = evt.IsBossBattle;
        }

        private void HandleEnemyRewardDropRequested(EnemyRewardDropRequestedEvent evt)
        {
            ResolveReferences();

            if (currencyManager == null)
            {
                Debug.LogError($"{nameof(RewardManager)} needs a {nameof(CurrencyManager)}.", this);
                return;
            }

            if (!TryGetRewardTable(currentChapter, currentRouteType, out ChapterRewardTable rewardTable))
            {
                Debug.LogWarning($"Reward table not found. Chapter: {currentChapter}, Route: {currentRouteType}", this);
                return;
            }

            if (!rewardTable.TryGetRewardData(evt.RewardType, out EnemyRewardData rewardData))
            {
                Debug.LogWarning($"Reward data not found. Chapter: {currentChapter}, Route: {currentRouteType}, RewardType: {evt.RewardType}", this);
                return;
            }

            CurrencyRewardRange[] rewards = rewardData.rewards;
            for (int i = 0; i < rewards.Length; i++)
            {
                int amount = rewards[i].Roll();
                if (amount > 0)
                {
                    currencyManager.AddCurrency(rewards[i].currencyType, amount);
                    RaiseFloatingText(rewards[i].currencyType, amount, evt.DropPosition);
                }
            }
        }

        private void RaiseFloatingText(CurrencyType currencyType, int amount, Vector3 dropPosition)
        {
            if (currencyType != CurrencyType.Gold)
                return;

            Bus<FloatingTextRequestedEvent>.Raise(
                new FloatingTextRequestedEvent(FloatingTextType.Gold, amount, dropPosition)
            );
        }

        private bool TryGetRewardTable(int chapter, ChapterRouteType routeType, out ChapterRewardTable rewardTable)
        {
            if (rewardTables != null)
            {
                for (int i = 0; i < rewardTables.Length; i++)
                {
                    ChapterRewardTable candidate = rewardTables[i];
                    if (candidate != null && candidate.Chapter == chapter && candidate.RouteType == routeType)
                    {
                        rewardTable = candidate;
                        return true;
                    }
                }
            }

            rewardTable = null;
            return false;
        }

        private void ResolveReferences()
        {
            if (currencyManager == null)
                currencyManager = CurrencyManager.Instance;
        }
    }
}
