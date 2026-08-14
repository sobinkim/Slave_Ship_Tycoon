using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.SO.TransportItemSOs;
using SB.Scripts.Currency;
using SB.Scripts.TransportEquipment;
using UnityEngine;

namespace SB.Scripts
{
    public class SalesManager : MonoBehaviour
    {
        [SerializeField] private TransportEquipmentManager _transportEquipmentManager;

        private CargoData[] _currentCargoData = Array.Empty<CargoData>();
        private CargoData[] _sellCargoData = Array.Empty<CargoData>();
        private IReadOnlyDictionary<ETransportItemType, TransportMarketPriceEntry> _marketPriceEntries;
        private int _sellChapter;
        private bool _hasPendingSale;

        private void OnEnable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += OnCargoChanged;
            Bus<GetMarketPriceEvent>.OnEvent += OnMarketPricesChanged;
            Bus<SellChapterStartedEvent>.OnEvent += OnSellChapterStarted;
            Bus<SellChapterCompletedEvent>.OnEvent += OnSellChapterCompleted;
        }

        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= OnCargoChanged;
            Bus<GetMarketPriceEvent>.OnEvent -= OnMarketPricesChanged;
            Bus<SellChapterStartedEvent>.OnEvent -= OnSellChapterStarted;
            Bus<SellChapterCompletedEvent>.OnEvent -= OnSellChapterCompleted;
        }

        private void OnCargoChanged(ChangedCurrentCargoCapacityEvent evt)
        {
            _currentCargoData = evt.CargoData ?? Array.Empty<CargoData>();
        }

        private void OnMarketPricesChanged(GetMarketPriceEvent evt)
        {
            _marketPriceEntries = evt.MarketPrices;
        }

        private void OnSellChapterStarted(SellChapterStartedEvent evt)
        {
            _sellChapter = evt.Chapter;
            _sellCargoData = CopyCargoData(_currentCargoData);
            _hasPendingSale = HasLoadedCargo(_sellCargoData);
        }

        private void OnSellChapterCompleted(SellChapterCompletedEvent evt)
        {
            if (_hasPendingSale == false)
                return;

            if (TryCreateSettlementResult(out SaleSettlementResult settlementResult) == false)
            {
                Debug.LogWarning("Sale settlement is waiting for cargo market data.", this);
                return;
            }

            _hasPendingSale = false;

            IReadOnlyList<SaleCurrencyReward> totalRewards = settlementResult.TotalRewards;

            for (int i = 0; i < totalRewards.Count; i++)
            {
                SaleCurrencyReward reward = totalRewards[i];
                Bus<CurrencyAddRequestEvent>.Raise(
                    new CurrencyAddRequestEvent(reward.CurrencyType, reward.Amount));
            }

            Bus<SaleSettlementCompletedEvent>.Raise(
                new SaleSettlementCompletedEvent(settlementResult));
        }

        private bool TryCreateSettlementResult(out SaleSettlementResult settlementResult)
        {
            settlementResult = null;

            if (_sellCargoData.Length == 0 || _marketPriceEntries == null || _marketPriceEntries.Count == 0)
                return false;

            List<CargoSaleResult> cargoResults = new List<CargoSaleResult>();
            Dictionary<CurrencyType, long> totalRewards = new Dictionary<CurrencyType, long>();

            for (int i = 0; i < _sellCargoData.Length; i++)
            {
                CargoData cargoData = _sellCargoData[i];

                if (cargoData == null || cargoData.Item == null || cargoData.Amount <= 0 ||
                    _marketPriceEntries.TryGetValue(
                        cargoData.Item.Type,
                        out TransportMarketPriceEntry priceEntry) == false)
                {
                    continue;
                }

                CargoSalesRewardData[] rewardDatas = cargoData.Item.Rewards ?? Array.Empty<CargoSalesRewardData>();
                List<SaleCurrencyReward> cargoRewards = new List<SaleCurrencyReward>(rewardDatas.Length);
                int appliedMultiplier = priceEntry.GetAppliedMultiplier();

                for (int rewardIndex = 0; rewardIndex < rewardDatas.Length; rewardIndex++)
                {
                    CargoSalesRewardData rewardData = rewardDatas[rewardIndex];
                    long rewardAmount = SaturatingMultiply(
                        SaturatingMultiply(cargoData.Amount, rewardData.CurrencyAmount),
                        appliedMultiplier);

                    if (rewardAmount <= 0)
                        continue;

                    cargoRewards.Add(new SaleCurrencyReward(rewardData.CurrencyType, rewardAmount));

                    if (totalRewards.TryGetValue(rewardData.CurrencyType, out long currentAmount))
                        totalRewards[rewardData.CurrencyType] = SaturatingAdd(currentAmount, rewardAmount);
                    else
                        totalRewards.Add(rewardData.CurrencyType, rewardAmount);
                }

                cargoResults.Add(new CargoSaleResult(
                    cargoData.Item.Type,
                    cargoData.Amount,
                    appliedMultiplier,
                    cargoRewards.ToArray()));
            }

            if (cargoResults.Count == 0 || totalRewards.Count == 0)
                return false;

            if (_transportEquipmentManager != null)
                _transportEquipmentManager.ApplySnapshotModifiers(totalRewards);

            SaleCurrencyReward[] totalRewardEntries = new SaleCurrencyReward[totalRewards.Count];
            int totalRewardIndex = 0;

            foreach (KeyValuePair<CurrencyType, long> reward in totalRewards)
            {
                totalRewardEntries[totalRewardIndex++] = new SaleCurrencyReward(
                    reward.Key,
                    reward.Value);
            }

            settlementResult = new SaleSettlementResult(
                _sellChapter,
                cargoResults.ToArray(),
                totalRewardEntries);
            return true;
        }

        private static CargoData[] CopyCargoData(CargoData[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<CargoData>();

            CargoData[] copy = new CargoData[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                CargoData cargoData = source[i];

                if (cargoData == null)
                    continue;

                copy[i] = new CargoData
                {
                    Item = cargoData.Item,
                    Amount = cargoData.Amount
                };
            }

            return copy;
        }

        private static bool HasLoadedCargo(CargoData[] cargoData)
        {
            for (int i = 0; i < cargoData.Length; i++)
            {
                if (cargoData[i] != null && cargoData[i].Item != null && cargoData[i].Amount > 0)
                    return true;
            }

            return false;
        }

        private static long SaturatingMultiply(long left, long right)
        {
            if (left <= 0 || right <= 0)
                return 0;

            return left > long.MaxValue / right
                ? long.MaxValue
                : left * right;
        }

        private static long SaturatingAdd(long left, long right)
        {
            return right > long.MaxValue - left
                ? long.MaxValue
                : left + right;
        }
    }
}
