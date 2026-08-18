using System;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using UnityEngine;
using UnityEngine.Serialization;

namespace SB.Scripts.Upgrade
{
    public enum MainShipUpgradeType
    {
        Health,
        AttackPowerPercent,
        AttackSpeedPercent,
        CargoCapacity,
        Luck,
        CommanderGaugeMax,
        CommanderGaugeRecoveryPerSecond
    }

    [Serializable]
    public class MainShipUpgradeData
    {
        public float health;
        [FormerlySerializedAs("attackPower")] public float attackPowerPercent;
        public float attackSpeedPercent;
        public float cargoCapacity;
        public float luck;
        public float commanderGaugeMax;
        public float commanderGaugeRecoveryPerSecond;
    }

    [Serializable]
    public class UpgradeGrowthData
    {
        public MainShipUpgradeType upgradeType;
        public CurrencyType currencyType = CurrencyType.Gold;
        public int level = 0;
        [SerializeField, InspectorName("Current Cost")] private long _currentCost;
        public float costGrowthRate;
        public float IncreaseValue;
        public float increaseGrowthRate;

        public long CurrentCost
        {
            get => _currentCost;
            set => _currentCost = value;
        }
    }

    public class ShipUpgradeManager : MonoBehaviour
    {
        [SerializeField] private UpgradeGrowthData[] upgradeGrowthDatas;
        [SerializeField] private MainShipUpgradeData _mainShipUpgradeData;
        [SerializeField] private CurrencyManager currencyManager;

        public MainShipUpgradeData MainShipUpgradeData => _mainShipUpgradeData;

        private void Awake()
        {
            Bus<UpgradeEvent>.Raise(new UpgradeEvent(_mainShipUpgradeData));
        }

        private void OnEnable()
        {
            Bus<RefreshMainShipStatsEvent>.OnEvent += HandleRefreshStatsEvent;
        }

        private void OnDisable()
        {
            Bus<RefreshMainShipStatsEvent>.OnEvent -= HandleRefreshStatsEvent;
        }

        public bool TryUpgrade(MainShipUpgradeType upgradeType)
        {
            return Upgrade(upgradeType);
        }

        private bool Upgrade(MainShipUpgradeType upgradeType)
        {
            ResolveReferences();

            UpgradeGrowthData currentTargetUpdateData = GetTargetUpdateData(upgradeType);

            if (currentTargetUpdateData == null)
                return false;

            if (currencyManager == null)
            {
                Debug.LogError($"{nameof(ShipUpgradeManager)} needs a {nameof(CurrencyManager)}.", this);
                return false;
            }

            long cost = currentTargetUpdateData.CurrentCost;
            if (cost <= 0)
            {
                Debug.LogError($"{upgradeType} upgrade cost must be greater than zero.", this);
                return false;
            }

            if (!currencyManager.TrySpendCurrency(currentTargetUpdateData.currencyType, cost))
                return false;

            currentTargetUpdateData.level++;
            AddUpgradeValue(upgradeType, currentTargetUpdateData.IncreaseValue);

            currentTargetUpdateData.CurrentCost = CalculateNextCost(
                currentTargetUpdateData.CurrentCost,
                currentTargetUpdateData.costGrowthRate);
            currentTargetUpdateData.IncreaseValue *= currentTargetUpdateData.increaseGrowthRate;

            Bus<UpgradeEvent>.Raise(new UpgradeEvent(MainShipUpgradeData));

            return true;
        }

        public UpgradeGrowthData GetTargetUpdateData(MainShipUpgradeType upgradeType)
        {
            foreach (UpgradeGrowthData data in upgradeGrowthDatas)
            {
                if (data.upgradeType == upgradeType)
                    return data;
            }

            return null;
        }

        private void DebugUpgrade(MainShipUpgradeType upgradeType)
        {
            UpgradeGrowthData currentTargetUpdateData = GetTargetUpdateData(upgradeType);

            if (currentTargetUpdateData == null)
            {
                Debug.LogWarning($"Debug upgrade data not found: {upgradeType}", this);
                return;
            }

            currentTargetUpdateData.level++;
            AddUpgradeValue(upgradeType, currentTargetUpdateData.IncreaseValue);

            currentTargetUpdateData.CurrentCost = CalculateNextCost(
                currentTargetUpdateData.CurrentCost,
                currentTargetUpdateData.costGrowthRate);
            currentTargetUpdateData.IncreaseValue *= currentTargetUpdateData.increaseGrowthRate;

            Bus<UpgradeEvent>.Raise(new UpgradeEvent(MainShipUpgradeData));
            Debug.Log(
                $"Debug Upgrade {upgradeType} / AttackPower: {_mainShipUpgradeData.attackPowerPercent}% / AttackSpeed: {_mainShipUpgradeData.attackSpeedPercent}%",
                this);
        }

        private void AddUpgradeValue(MainShipUpgradeType upgradeType, float value)
        {
            switch (upgradeType)
            {
                case MainShipUpgradeType.Health:
                    _mainShipUpgradeData.health += value;
                    break;

                case MainShipUpgradeType.AttackPowerPercent:
                    _mainShipUpgradeData.attackPowerPercent += value;
                    break;

                case MainShipUpgradeType.AttackSpeedPercent:
                    _mainShipUpgradeData.attackSpeedPercent += value;
                    break;

                case MainShipUpgradeType.CargoCapacity:
                    _mainShipUpgradeData.cargoCapacity += value;
                    break;

                case MainShipUpgradeType.Luck:
                    _mainShipUpgradeData.luck += value;
                    break;

                case MainShipUpgradeType.CommanderGaugeMax:
                    _mainShipUpgradeData.commanderGaugeMax += value;
                    break;

                case MainShipUpgradeType.CommanderGaugeRecoveryPerSecond:
                    _mainShipUpgradeData.commanderGaugeRecoveryPerSecond += value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(upgradeType), upgradeType, null);
            }
        }

        private void HandleRefreshStatsEvent(RefreshMainShipStatsEvent evt)
        {
            Bus<UpgradeEvent>.Raise(new UpgradeEvent(MainShipUpgradeData));
        }

        private void ResolveReferences()
        {
            if (currencyManager == null)
                currencyManager = CurrencyManager.Instance;
        }

        private static long CalculateNextCost(long currentCost, float growthRate)
        {
            if (currentCost <= 0)
                return 1;

            if (float.IsNaN(growthRate) || float.IsInfinity(growthRate) || growthRate < 1f)
                return currentCost;

            double nextCost = Math.Ceiling(currentCost * (double)growthRate);

            if (double.IsInfinity(nextCost) || nextCost >= long.MaxValue)
                return long.MaxValue;

            return Math.Max(currentCost, (long)nextCost);
        }
    }
}
