using SB.Core;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts
{
    public enum ShipCommonStatType
    {
        Health,
        AttackDamage,
        AttackSpeed,
        HealthRecoveryPerSecond
    }

    public enum CommanderStatType
    {
        MaxGauge,
        GaugeRecoveryPerSecond
    }

    public class ShipStatCompo : EntityStatCompo
    {
        [Header("Common Stat SO")]
        [SerializeField] private StatSO _health_Stat;
        [SerializeField] private StatSO _attackDamage_Stat;
        [SerializeField] private StatSO _attackSpeed_Stat;
        [SerializeField, Tooltip("전투 중과 웨이브 클리어 후 초당 회복하는 HP")]
        private StatSO healthRecoveryPerSecondStat;

        [Header("Main Ship Stat SO")]
        [SerializeField] private StatSO _playerFleet_AttackPowerPercent_Stat;
        [SerializeField] private StatSO _playerFleet_AttackSpeedPercent_Stat;
        [SerializeField] private StatSO _mainShip_CargoCapacity_Stat;
        [SerializeField] private StatSO _mainShip_Luck_Stat;

        [Header("Commander Stat SO")]
        [SerializeField] private StatSO _commander_MaxGauge_Stat;
        [SerializeField] private StatSO _commander_GaugeRecoveryPerSecond_Stat;

        public StatSO GetCommonStatSO(ShipCommonStatType targetStatType)
        {
            switch (targetStatType)
            {
                case ShipCommonStatType.Health:
                    return _health_Stat;

                case ShipCommonStatType.AttackDamage:
                    return _attackDamage_Stat;

                case ShipCommonStatType.AttackSpeed:
                    return _attackSpeed_Stat;
                case ShipCommonStatType.HealthRecoveryPerSecond:
                    return healthRecoveryPerSecondStat;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(targetStatType), targetStatType, null);
            }
        }

        public StatSO GetShipStatSO(MainShipUpgradeType targetStatType)
        {
            switch (targetStatType)
            {
                case MainShipUpgradeType.Health:
                    return _health_Stat;

                case MainShipUpgradeType.AttackPowerPercent:
                    return _playerFleet_AttackPowerPercent_Stat;

                case MainShipUpgradeType.AttackSpeedPercent:
                    return _playerFleet_AttackSpeedPercent_Stat;

                case MainShipUpgradeType.CargoCapacity:
                    return _mainShip_CargoCapacity_Stat;

                case MainShipUpgradeType.Luck:
                    return _mainShip_Luck_Stat;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(targetStatType), targetStatType, null);
            }
        }

        public void SetShipBaseValue(MainShipUpgradeType targetStatType, float value)
        {
            SetBaseValue(GetShipStatSO(targetStatType), value);
        }

        public StatSO GetCommanderStatSO(CommanderStatType targetStatType)
        {
            switch (targetStatType)
            {
                case CommanderStatType.MaxGauge:
                    return _commander_MaxGauge_Stat;

                case CommanderStatType.GaugeRecoveryPerSecond:
                    return _commander_GaugeRecoveryPerSecond_Stat;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(targetStatType), targetStatType, null);
            }
        }

        public void SetCommanderBaseValue(CommanderStatType targetStatType, float value)
        {
            SetBaseValue(GetCommanderStatSO(targetStatType), value);
        }
    }
}
