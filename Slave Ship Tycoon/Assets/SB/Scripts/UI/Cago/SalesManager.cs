using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class SalesManager : MonoBehaviour
    {
        private CargoData[] _cargoData;
        private TransportMarketPriceEntry[] _marketPriceEntries;

        private void OnEnable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += GetCargoDatas;
            Bus<GetMarketPriceEvent>.OnEvent += GetMarketPrices;
        }

        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= GetCargoDatas;
            Bus<GetMarketPriceEvent>.OnEvent -= GetMarketPrices;
        }

        private void GetCargoDatas(ChangedCurrentCargoCapacityEvent evt)
        {
            _cargoData = evt.CargoData;
        }

        private void GetMarketPrices(GetMarketPriceEvent evt)
        {
            _marketPriceEntries = evt._marketPrices;
        }

        private void SaleCago()
        {
            foreach (CargoData cargoData in _cargoData)
            {
                for (int i = 0; i < cargoData.Item.Rewards.Length; i++)
                {
                    int totalAmount = cargoData.Amount * (cargoData.Item.Rewards[i].CurrencyAmount * );
                    Bus<CurrencyAddRequestEvent>.Raise(
                        new CurrencyAddRequestEvent(cargoData.Item.Rewards[i].CurrencyType, totalAmount));
                }
            }
        }
    }
}