using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.Upgrade;
using UnityEngine;

[Serializable]
public struct TransportMarketPriceEntry
{
    public ETransportItemType CargoType;

    public bool IsHasBonusMultiplier;

    public int PriceMultiplier;
    public int BonusPriceMultiplier;

    public int GetAppliedMultiplier()
    {
        int multiplier = PriceMultiplier;

        if (IsHasBonusMultiplier)
        {
            multiplier *= BonusPriceMultiplier;
        }

        return multiplier;
    }
}

public class MarketPriceManager : MonoBehaviour
{
    private CargoData[] _currentCargoData;
    private MainShipUpgradeData _mainShipUpgradeData;
    private Dictionary<ETransportItemType, TransportMarketPriceEntry> _cargoTypePriceRates;

    private void Awake()
    {
        _cargoTypePriceRates = new Dictionary<ETransportItemType, TransportMarketPriceEntry>();
    }

    private void OnEnable()
    {
        Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += OnCurrentCargoCapacityChanged;
        Bus<UpgradeEvent>.OnEvent += OnUpgradeUpdated;
        Bus<EvenStageClearedEvent>.OnEvent += OnEvenStageCleared;
    }

    private void OnDisable()
    {
        Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= OnCurrentCargoCapacityChanged;
        Bus<UpgradeEvent>.OnEvent -= OnUpgradeUpdated;
        Bus<EvenStageClearedEvent>.OnEvent -= OnEvenStageCleared;
    }

    private void OnCurrentCargoCapacityChanged(ChangedCurrentCargoCapacityEvent evt)
    {
        _currentCargoData = evt.CargoData;
    }

    private void OnUpgradeUpdated(UpgradeEvent evt)
    {
        _mainShipUpgradeData = evt._mainShipUpgradeData;
    }

    private void OnEvenStageCleared(EvenStageClearedEvent evt)
    {
        GetCargoMarketRates();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            GetCargoMarketRates();
        }
    }

    private Dictionary<ETransportItemType, TransportMarketPriceEntry> GetCargoMarketRates()
    {
        _cargoTypePriceRates.Clear();

        if (_currentCargoData == null || _currentCargoData.Length == 0)
        {
            Bus<GetMarketPriceEvent>.Raise(new GetMarketPriceEvent(_cargoTypePriceRates));
            return _cargoTypePriceRates;
        }

        float bonusChance = GetBonusChance(_mainShipUpgradeData.luck);

        for (int i = 0; i < _currentCargoData.Length; i++)
        {
            if (_currentCargoData[i] == null || _currentCargoData[i].Item == null)
                continue;

            int baseMultiplier = UnityEngine.Random.Range(1, 10);
            bool hasBonus = bonusChance > 0f && UnityEngine.Random.value < bonusChance;
            int bonusMultiplier = hasBonus ? UnityEngine.Random.Range(1, 10) : 0;


            TransportMarketPriceEntry transportMarketPriceEntry = new TransportMarketPriceEntry
            {
                CargoType = _currentCargoData[i].Item.Type,
                PriceMultiplier = baseMultiplier,
                IsHasBonusMultiplier = hasBonus,
                BonusPriceMultiplier = bonusMultiplier
            };

            _cargoTypePriceRates[_currentCargoData[i].Item.Type] = transportMarketPriceEntry;
        }

        Bus<GetMarketPriceEvent>.Raise(new GetMarketPriceEvent(_cargoTypePriceRates));
        return _cargoTypePriceRates;
    }

    private float GetBonusChance(float luckPercent)
    {
        return Mathf.Clamp01(luckPercent * 0.01f);
    }
}
