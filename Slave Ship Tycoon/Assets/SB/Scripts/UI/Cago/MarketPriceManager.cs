using System;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.Upgrade;
using UnityEngine;

[Serializable]
public struct TransportMarketPriceEntry
{
    public ETransportItemType CargoType;
    public bool isHasBonusMultiplier;
    public int PriceMultiplier;
    public int BonusPriceMultiplier;
}

public class MarketPriceManager : MonoBehaviour
{
    private CargoData[] _currentCargoData;
    private MainShipUpgradeData _mainShipUpgradeData;
    [SerializeField] private TransportMarketPriceEntry[] _cargoTypePriceRates;

    private void OnEnable()
    {
        Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += OnCurrentCargoCapacityChanged;
        Bus<UpgradeEvent>.OnEvent += OnUpgradeUpdated;
    }

    private void OnDisable()
    {
        Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= OnCurrentCargoCapacityChanged;
        Bus<UpgradeEvent>.OnEvent -= OnUpgradeUpdated;
    }

    private void OnCurrentCargoCapacityChanged(ChangedCurrentCargoCapacityEvent evt)
    {
        _currentCargoData = evt.CargoData;
    }

    private void OnUpgradeUpdated(UpgradeEvent evt)
    {
        _mainShipUpgradeData = evt._mainShipUpgradeData;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            GetCargoMarketRates();
        }
    }

    private TransportMarketPriceEntry[] GetCargoMarketRates()
    {
        if (_currentCargoData == null || _currentCargoData.Length == 0)
        {
            _cargoTypePriceRates = Array.Empty<TransportMarketPriceEntry>();
            return _cargoTypePriceRates;
        }

        _cargoTypePriceRates = new TransportMarketPriceEntry[_currentCargoData.Length];

        float bonusChance = GetBonusChance(_mainShipUpgradeData.luck);

        for (int i = 0; i < _currentCargoData.Length; i++)
        {
            int baseMultiplier = UnityEngine.Random.Range(1, 10);
            bool hasBonus = bonusChance > 0f && UnityEngine.Random.value < bonusChance;
            int bonusMultiplier = hasBonus ? UnityEngine.Random.Range(1, 10) : 0;

            _cargoTypePriceRates[i] = new TransportMarketPriceEntry
            {
                CargoType = _currentCargoData[i].Item.Type,
                PriceMultiplier = baseMultiplier,
                isHasBonusMultiplier = hasBonus,
                BonusPriceMultiplier = bonusMultiplier
            };
        }

        Bus<GetMarketPriceEvent>.Raise(new GetMarketPriceEvent(_cargoTypePriceRates));
        return _cargoTypePriceRates;
    }

    private float GetBonusChance(float luckPercent)
    {
        return Mathf.Clamp01(luckPercent * 0.01f);
    }
}