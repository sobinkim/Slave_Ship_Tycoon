using SB.Core;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public class ShipCombatStatCompo : EntityComponent
    {
        private const float PercentMultiplier = 0.01f;
        private const float MinAttackSpeed = 0.01f;

        private Ship ownerShip;
        private ShipStatCompo shipStatCompo;
        private ShipUpgradeManager shipUpgradeManager;

        private bool UsesPlayerFleetUpgrade =>
            ownerShip != null &&
            (ownerShip.myShipData.ShipType == ShipType.MainShip ||
             ownerShip.myShipData.ShipType == ShipType.EscortShip);

        public float FinalAttackDamage
        {
            get
            {
                float damage = GetCommonStatValue(ShipCommonStatType.AttackDamage, 0f);

                if (UsesPlayerFleetUpgrade)
                {
                    float attackPowerPercent = GetPlayerFleetAttackPowerPercent();
                    damage *= 1f + attackPowerPercent * PercentMultiplier;
                }

                return Mathf.Max(0f, damage);
            }
        }

        public float FinalAttackSpeed
        {
            get
            {
                float attackSpeed = GetCommonStatValue(ShipCommonStatType.AttackSpeed, 1f);

                if (UsesPlayerFleetUpgrade)
                {
                    float attackSpeedPercent = GetPlayerFleetAttackSpeedPercent();
                    attackSpeed *= 1f + attackSpeedPercent * PercentMultiplier;
                }

                return Mathf.Max(MinAttackSpeed, attackSpeed);
            }
        }

        public override void Initialize(Entity entity)
        {
            base.Initialize(entity);

            ownerShip = entity as Ship;
            shipStatCompo = GetCompo<ShipStatCompo>();
            shipUpgradeManager = FindAnyObjectByType<ShipUpgradeManager>();
        }

        private float GetCommonStatValue(ShipCommonStatType statType, float defaultValue)
        {
            if (shipStatCompo == null)
                return defaultValue;

            StatSO stat = shipStatCompo.GetCommonStatSO(statType);
            return shipStatCompo.GetValue(stat, defaultValue);
        }

        private float GetPlayerFleetAttackPowerPercent()
        {
            return shipUpgradeManager != null ? shipUpgradeManager.MainShipUpgradeData.attackPowerPercent : 0f;
        }

        private float GetPlayerFleetAttackSpeedPercent()
        {
            return shipUpgradeManager != null ? shipUpgradeManager.MainShipUpgradeData.attackSpeedPercent : 0f;
        }
    }
}
