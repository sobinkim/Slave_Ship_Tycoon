using System;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts.UI.Upgrade
{
    public class MainShipUpgradeView : MonoBehaviour
    {
        [SerializeField] private MainShipUpgradeSlotView _healthUpgradeButton;
        [SerializeField] private MainShipUpgradeSlotView attackPowerPercentUpgradeButton;
        [SerializeField] private MainShipUpgradeSlotView attackSpeedPercentUpgradeButton;
        [SerializeField] private MainShipUpgradeSlotView cargoCapacityUpgradeButton;
        [SerializeField] private MainShipUpgradeSlotView luckUpgradeButton;

        public MainShipUpgradeSlotView HealthUpgradeButton => _healthUpgradeButton;
        public MainShipUpgradeSlotView AttackPowerPercentUpgradeButton => attackPowerPercentUpgradeButton;
        public MainShipUpgradeSlotView AttackSpeedPercentUpgradeButton => attackSpeedPercentUpgradeButton;
        public MainShipUpgradeSlotView CargoCapacityUpgradeButton => cargoCapacityUpgradeButton;
        public MainShipUpgradeSlotView LuckUpgradeButton => luckUpgradeButton;

        public event Action<MainShipUpgradeType> OnUpgradeButtonClicked;

        private void OnEnable()
        {
            SubscribeSlotButtons();
        }

        private void OnDisable()
        {
            UnsubscribeSlotButtons();
        }

        public void SetRefresh(MainShipUpgradeType type, int level, float cost)
        {
            switch (type)
            {
                case MainShipUpgradeType.Health:
                    _healthUpgradeButton.Refresh(level, cost);
                    break;

                case MainShipUpgradeType.AttackPowerPercent:
                    attackPowerPercentUpgradeButton.Refresh(level, cost);
                    break;

                case MainShipUpgradeType.AttackSpeedPercent:
                    attackSpeedPercentUpgradeButton.Refresh(level, cost);
                    break;

                case MainShipUpgradeType.CargoCapacity:
                    cargoCapacityUpgradeButton.Refresh(level, cost);
                    break;

                case MainShipUpgradeType.Luck:
                    luckUpgradeButton.Refresh(level, cost);
                    break;
            }
        }

        private void SubscribeSlotButtons()
        {
            if (_healthUpgradeButton != null)
                _healthUpgradeButton.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;

            if (attackPowerPercentUpgradeButton != null)
                attackPowerPercentUpgradeButton.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;

            if (attackSpeedPercentUpgradeButton != null)
                attackSpeedPercentUpgradeButton.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;

            if (cargoCapacityUpgradeButton != null)
                cargoCapacityUpgradeButton.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;

            if (luckUpgradeButton != null)
                luckUpgradeButton.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;
        }

        private void UnsubscribeSlotButtons()
        {
            if (_healthUpgradeButton != null)
                _healthUpgradeButton.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;

            if (attackPowerPercentUpgradeButton != null)
                attackPowerPercentUpgradeButton.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;

            if (attackSpeedPercentUpgradeButton != null)
                attackSpeedPercentUpgradeButton.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;

            if (cargoCapacityUpgradeButton != null)
                cargoCapacityUpgradeButton.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;

            if (luckUpgradeButton != null)
                luckUpgradeButton.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;
        }

        private void HandleUpgradeButtonClicked(MainShipUpgradeType upgradeType)
        {
            OnUpgradeButtonClicked?.Invoke(upgradeType);
        }
    }
}
