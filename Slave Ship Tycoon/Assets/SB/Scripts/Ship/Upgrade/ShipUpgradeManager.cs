using System;
using SB.Core.EventBus;
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
        Luck
    }

    [Serializable]
    public class MainShipUpgradeData
    {
        public float health;
        [FormerlySerializedAs("attackPower")] public float attackPowerPercent;
        public float attackSpeedPercent;
        public float cargoCapacity;
        public float luck;
    }

    [Serializable]
    public class UpgradeGrowthData
    {
        public MainShipUpgradeType upgradeType;
        public int level = 0;
        public float currentCost;
        public float costGrowthRate;
        public float IncreaseValue;
        public float increaseGrowthRate;
    }

    public class ShipUpgradeManager : MonoBehaviour
    {
        [SerializeField] private UpgradeGrowthData[] upgradeGrowthDatas;
        [SerializeField] private MainShipUpgradeData _mainShipUpgradeData;

        private float _currentGold = 1000000000;

        public MainShipUpgradeData MainShipUpgradeData => _mainShipUpgradeData;


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                DebugUpgrade(MainShipUpgradeType.AttackPowerPercent);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                DebugUpgrade(MainShipUpgradeType.AttackSpeedPercent);
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
            UpgradeGrowthData currentTargetUpdateData = GetTargetUpdateData(upgradeType);

            if (currentTargetUpdateData == null)
                return false;

            if (currentTargetUpdateData.currentCost > _currentGold)
                return false;

            currentTargetUpdateData.level++;
            _currentGold -= currentTargetUpdateData.currentCost;
            AddUpgradeValue(upgradeType, currentTargetUpdateData.IncreaseValue);

            currentTargetUpdateData.currentCost *= currentTargetUpdateData.costGrowthRate;
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

            currentTargetUpdateData.currentCost *= currentTargetUpdateData.costGrowthRate;
            currentTargetUpdateData.IncreaseValue *= currentTargetUpdateData.increaseGrowthRate;

            Bus<UpgradeEvent>.Raise(new UpgradeEvent(MainShipUpgradeData));
            Debug.Log($"Debug Upgrade {upgradeType} / AttackPower: {_mainShipUpgradeData.attackPowerPercent}% / AttackSpeed: {_mainShipUpgradeData.attackSpeedPercent}%", this);
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

                default:
                    throw new ArgumentOutOfRangeException(nameof(upgradeType), upgradeType, null);
            }
        }

        private void HandleRefreshStatsEvent(RefreshMainShipStatsEvent evt)
        {
            Bus<UpgradeEvent>.Raise(new UpgradeEvent(MainShipUpgradeData));
        }
    }
}
