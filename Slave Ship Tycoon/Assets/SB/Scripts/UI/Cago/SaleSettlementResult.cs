using System;
using System.Collections.Generic;
using SB.Scripts.Currency;

namespace SB.Scripts
{
    [Serializable]
    public readonly struct SaleCurrencyReward
    {
        public readonly CurrencyType CurrencyType;
        public readonly long Amount;

        public SaleCurrencyReward(CurrencyType currencyType, long amount)
        {
            CurrencyType = currencyType;
            Amount = amount;
        }
    }

    [Serializable]
    public sealed class CargoSaleResult
    {
        private readonly SaleCurrencyReward[] _rewards;

        public ETransportItemType CargoType { get; }
        public int CargoAmount { get; }
        public int MarketMultiplier { get; }
        public IReadOnlyList<SaleCurrencyReward> Rewards => _rewards;

        public CargoSaleResult(
            ETransportItemType cargoType,
            int cargoAmount,
            int marketMultiplier,
            SaleCurrencyReward[] rewards)
        {
            CargoType = cargoType;
            CargoAmount = cargoAmount;
            MarketMultiplier = marketMultiplier;
            _rewards = rewards ?? Array.Empty<SaleCurrencyReward>();
        }
    }

    [Serializable]
    public sealed class SaleSettlementResult
    {
        private readonly CargoSaleResult[] _cargoResults;
        private readonly SaleCurrencyReward[] _totalRewards;

        public int SellChapter { get; }
        public IReadOnlyList<CargoSaleResult> CargoResults => _cargoResults;
        public IReadOnlyList<SaleCurrencyReward> TotalRewards => _totalRewards;

        public SaleSettlementResult(
            int sellChapter,
            CargoSaleResult[] cargoResults,
            SaleCurrencyReward[] totalRewards)
        {
            SellChapter = sellChapter;
            _cargoResults = cargoResults ?? Array.Empty<CargoSaleResult>();
            _totalRewards = totalRewards ?? Array.Empty<SaleCurrencyReward>();
        }
    }
}
