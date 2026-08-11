using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class SalesManager : MonoBehaviour
    {
        private CargoData[] _cargoData;
        private CargoData[] _sellCargoData;
        private Dictionary<ETransportItemType, TransportMarketPriceEntry> _marketPriceEntries;

        private void OnEnable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += GetCargoDatas;
            Bus<GetMarketPriceEvent>.OnEvent += GetMarketPrices;
            Bus<SellChapterStartedEvent>.OnEvent += HandleSellChapterStarted;
            Bus<ObtainChapterStartedEvent>.OnEvent += HandleObtainChapterStarted;
        }

        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= GetCargoDatas;
            Bus<GetMarketPriceEvent>.OnEvent -= GetMarketPrices;
            Bus<SellChapterStartedEvent>.OnEvent -= HandleSellChapterStarted;
            Bus<ObtainChapterStartedEvent>.OnEvent -= HandleObtainChapterStarted;
        }

        private void HandleSellChapterStarted(SellChapterStartedEvent evt)
        {
            _sellCargoData = CopyCargoData(_cargoData);
        }

        private void HandleObtainChapterStarted(ObtainChapterStartedEvent evt)
        {
            if (SaleCargo() == false)
                return;
        }

        private void GetCargoDatas(ChangedCurrentCargoCapacityEvent evt)
        {
            _cargoData = evt.CargoData;
        }

        private void GetMarketPrices(GetMarketPriceEvent evt)
        {
            _marketPriceEntries = evt._marketPrices;
        }

        private bool SaleCargo()
        {
            if (_sellCargoData == null || _sellCargoData.Length == 0 ||
                _marketPriceEntries == null || _marketPriceEntries.Count == 0)
            {
                return false;
            }

            bool hasCargo = false;

            foreach (CargoData cargoData in _sellCargoData)
            {
                if (cargoData == null || cargoData.Item == null || cargoData.Amount <= 0)
                    continue;

                if (_marketPriceEntries.TryGetValue(cargoData.Item.Type, out TransportMarketPriceEntry priceEntry) == false)
                    continue;

                hasCargo = true;

                for (int i = 0; i < cargoData.Item.Rewards.Length; i++)
                {
                    int totalAmount = cargoData.Amount * (cargoData.Item.Rewards[i].CurrencyAmount
                                                          * priceEntry.GetAppliedMultiplier());
                    Bus<CurrencyAddRequestEvent>.Raise(
                        new CurrencyAddRequestEvent(cargoData.Item.Rewards[i].CurrencyType, totalAmount));
                }
            }

            return hasCargo;
        }

        private CargoData[] CopyCargoData(CargoData[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<CargoData>();

            CargoData[] copy = new CargoData[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                    continue;

                copy[i] = new CargoData
                {
                    Item = source[i].Item,
                    Amount = source[i].Amount
                };
            }

            return copy;
        }
    }
}
