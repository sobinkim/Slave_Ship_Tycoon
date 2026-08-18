using System.Collections.Generic;
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
        private PlayerFleetUpgradeProvider playerFleetUpgradeProvider;
        private readonly Dictionary<object, float> _commanderAttackSpeedPercentByKey =
            new Dictionary<object, float>();

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

                    float commanderAttackSpeedPercent = GetCommanderAttackSpeedPercent();
                    attackSpeed *= 1f + commanderAttackSpeedPercent * PercentMultiplier;
                }

                return Mathf.Max(MinAttackSpeed, attackSpeed);
            }
        }

        public override void Initialize(Entity entity)
        {
            base.Initialize(entity);

            ownerShip = entity as Ship;
            shipStatCompo = GetCompo<ShipStatCompo>();
        }

        public void SetPlayerFleetUpgradeProvider(PlayerFleetUpgradeProvider upgradeProvider)
        {
            playerFleetUpgradeProvider = upgradeProvider;
        }

        public void AddCommanderAttackSpeedPercent(object key, float attackSpeedPercent)
        {
            if (key == null || _commanderAttackSpeedPercentByKey.ContainsKey(key))
                return;

            float previousAttackSpeed = FinalAttackSpeed;
            _commanderAttackSpeedPercentByKey.Add(key, Mathf.Max(0f, attackSpeedPercent));
            ownerShip?.RefreshAttackSpeed(previousAttackSpeed);
        }

        public void RemoveCommanderAttackSpeedPercent(object key)
        {
            if (key == null || _commanderAttackSpeedPercentByKey.ContainsKey(key) == false)
                return;

            float previousAttackSpeed = FinalAttackSpeed;
            _commanderAttackSpeedPercentByKey.Remove(key);
            ownerShip?.RefreshAttackSpeed(previousAttackSpeed);
        }

        public void ClearCommanderModifiers()
        {
            if (_commanderAttackSpeedPercentByKey.Count == 0)
                return;

            float previousAttackSpeed = FinalAttackSpeed;
            _commanderAttackSpeedPercentByKey.Clear();
            ownerShip?.RefreshAttackSpeed(previousAttackSpeed);
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
            return playerFleetUpgradeProvider != null ? playerFleetUpgradeProvider.AttackPowerPercent : 0f;
        }

        private float GetPlayerFleetAttackSpeedPercent()
        {
            return playerFleetUpgradeProvider != null ? playerFleetUpgradeProvider.AttackSpeedPercent : 0f;
        }

        private float GetCommanderAttackSpeedPercent()
        {
            float totalPercent = 0f;

            foreach (float attackSpeedPercent in _commanderAttackSpeedPercentByKey.Values)
                totalPercent += attackSpeedPercent;

            return totalPercent;
        }
    }
}
