using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts
{
    [Serializable]
    public struct TransportMarketPriceEntry
    {
        public ETransportItemType CargoType;
        public bool HasBonusMultiplier;
        public int PriceMultiplier;
        public int BonusPriceMultiplier;
        public int AccumulatedPriceMultiplier;

        public int GetStageMultiplier()
        {
            return HasBonusMultiplier
                ? PriceMultiplier * BonusPriceMultiplier
                : PriceMultiplier;
        }

        public int GetAppliedMultiplier()
        {
            return Mathf.Max(1, AccumulatedPriceMultiplier);
        }
    }

    public class MarketPriceManager : MonoBehaviour
    {
        [SerializeField, Range(0f, 100f)] private float _sellFailurePenaltyPercent = 15f;

        private readonly Dictionary<ETransportItemType, TransportMarketPriceEntry> _cargoTypePriceRates =
            new Dictionary<ETransportItemType, TransportMarketPriceEntry>();
        private readonly HashSet<int> _penalizedStages = new HashSet<int>();

        private CargoData[] _currentCargoData = Array.Empty<CargoData>();
        private MainShipUpgradeData _mainShipUpgradeData;

        public IReadOnlyDictionary<ETransportItemType, TransportMarketPriceEntry> CargoTypePriceRates =>
            _cargoTypePriceRates;

        private void OnEnable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += OnCurrentCargoCapacityChanged;
            Bus<UpgradeEvent>.OnEvent += OnUpgradeUpdated;
            Bus<SellChapterStartedEvent>.OnEvent += OnSellChapterStarted;
            Bus<EvenStageClearedEvent>.OnEvent += OnEvenStageCleared;
            Bus<EvenStageFailedEvent>.OnEvent += OnEvenStageFailed;
        }

        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= OnCurrentCargoCapacityChanged;
            Bus<UpgradeEvent>.OnEvent -= OnUpgradeUpdated;
            Bus<SellChapterStartedEvent>.OnEvent -= OnSellChapterStarted;
            Bus<EvenStageClearedEvent>.OnEvent -= OnEvenStageCleared;
            Bus<EvenStageFailedEvent>.OnEvent -= OnEvenStageFailed;
        }

        public bool TryGetMarketPrice(
            ETransportItemType cargoType,
            out TransportMarketPriceEntry marketPriceEntry)
        {
            return _cargoTypePriceRates.TryGetValue(cargoType, out marketPriceEntry);
        }

        private void OnCurrentCargoCapacityChanged(ChangedCurrentCargoCapacityEvent evt)
        {
            _currentCargoData = evt.CargoData ?? Array.Empty<CargoData>();
        }

        private void OnUpgradeUpdated(UpgradeEvent evt)
        {
            _mainShipUpgradeData = evt._mainShipUpgradeData;
        }

        private void OnSellChapterStarted(SellChapterStartedEvent evt)
        {
            _penalizedStages.Clear();
            InitializeMarketPrices();
            RaiseMarketPriceChanged();
        }

        private void OnEvenStageCleared(EvenStageClearedEvent evt)
        {
            RollAndAccumulateMarketPrices();
            RaiseMarketPriceChanged();
        }

        private void OnEvenStageFailed(EvenStageFailedEvent evt)
        {
            if (_penalizedStages.Add(evt.Stage) == false)
                return;

            ApplyFailurePenalty();
            RaiseMarketPriceChanged();
        }

        private void InitializeMarketPrices()
        {
            _cargoTypePriceRates.Clear();

            for (int i = 0; i < _currentCargoData.Length; i++)
            {
                CargoData cargoData = _currentCargoData[i];

                if (cargoData == null || cargoData.Item == null || cargoData.Amount <= 0)
                    continue;

                _cargoTypePriceRates[cargoData.Item.Type] = new TransportMarketPriceEntry
                {
                    CargoType = cargoData.Item.Type,
                    PriceMultiplier = 1,
                    AccumulatedPriceMultiplier = 1
                };
            }
        }

        private void RollAndAccumulateMarketPrices()
        {
            if (_cargoTypePriceRates.Count == 0)
                return;

            ETransportItemType[] cargoTypes = new ETransportItemType[_cargoTypePriceRates.Count];
            _cargoTypePriceRates.Keys.CopyTo(cargoTypes, 0);
            float bonusChance = GetBonusChance(_mainShipUpgradeData != null
                ? _mainShipUpgradeData.luck
                : 0f);

            for (int i = 0; i < cargoTypes.Length; i++)
            {
                ETransportItemType cargoType = cargoTypes[i];
                TransportMarketPriceEntry currentEntry = _cargoTypePriceRates[cargoType];
                int priceMultiplier = UnityEngine.Random.Range(1, 10);
                bool hasBonusMultiplier = bonusChance > 0f && UnityEngine.Random.value < bonusChance;
                int bonusPriceMultiplier = hasBonusMultiplier
                    ? UnityEngine.Random.Range(1, 10)
                    : 0;

                TransportMarketPriceEntry stageEntry = new TransportMarketPriceEntry
                {
                    CargoType = cargoType,
                    HasBonusMultiplier = hasBonusMultiplier,
                    PriceMultiplier = priceMultiplier,
                    BonusPriceMultiplier = bonusPriceMultiplier,
                    AccumulatedPriceMultiplier = currentEntry.GetAppliedMultiplier()
                };

                stageEntry.AccumulatedPriceMultiplier += stageEntry.GetStageMultiplier() - 1;
                _cargoTypePriceRates[cargoType] = stageEntry;
            }
        }

        private void ApplyFailurePenalty()
        {
            ETransportItemType[] cargoTypes = new ETransportItemType[_cargoTypePriceRates.Count];
            _cargoTypePriceRates.Keys.CopyTo(cargoTypes, 0);
            float remainingRate = 1f - _sellFailurePenaltyPercent * 0.01f;

            for (int i = 0; i < cargoTypes.Length; i++)
            {
                ETransportItemType cargoType = cargoTypes[i];
                TransportMarketPriceEntry entry = _cargoTypePriceRates[cargoType];
                int accumulatedUplift = Mathf.Max(0, entry.GetAppliedMultiplier() - 1);
                int remainingUplift = Mathf.FloorToInt(accumulatedUplift * remainingRate);

                if (_sellFailurePenaltyPercent > 0f && accumulatedUplift > 0)
                    remainingUplift = Mathf.Min(remainingUplift, accumulatedUplift - 1);

                entry.AccumulatedPriceMultiplier = 1 + remainingUplift;
                _cargoTypePriceRates[cargoType] = entry;
            }
        }

        private void RaiseMarketPriceChanged()
        {
            Bus<GetMarketPriceEvent>.Raise(new GetMarketPriceEvent(_cargoTypePriceRates));
        }

        private static float GetBonusChance(float luckPercent)
        {
            return Mathf.Clamp01(luckPercent * 0.01f);
        }

        private void OnValidate()
        {
            _sellFailurePenaltyPercent = Mathf.Clamp(_sellFailurePenaltyPercent, 0f, 100f);
        }
    }
}
