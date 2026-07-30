using UnityEngine;

namespace SB.Scripts.Upgrade
{
    public sealed class PlayerFleetUpgradeProvider : MonoBehaviour
    {
        [SerializeField] private ShipUpgradeManager shipUpgradeManager;

        public float AttackPowerPercent => UpgradeData?.attackPowerPercent ?? 0f;
        public float AttackSpeedPercent => UpgradeData?.attackSpeedPercent ?? 0f;
        public float Health => UpgradeData?.health ?? 0f;
        public float CargoCapacity => UpgradeData?.cargoCapacity ?? 0f;
        public float Luck => UpgradeData?.luck ?? 0f;

        private MainShipUpgradeData UpgradeData => shipUpgradeManager != null
            ? shipUpgradeManager.MainShipUpgradeData
            : null;
    }
}
