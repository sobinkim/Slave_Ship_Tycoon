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
        [SerializeField] private MainShipUpgradeSlotView commanderGaugeMaxUpgradeButton;
        [SerializeField] private MainShipUpgradeSlotView commanderGaugeRecoveryPerSecondUpgradeButton;

        public MainShipUpgradeSlotView HealthUpgradeButton => _healthUpgradeButton;
        public MainShipUpgradeSlotView AttackPowerPercentUpgradeButton => attackPowerPercentUpgradeButton;
        public MainShipUpgradeSlotView AttackSpeedPercentUpgradeButton => attackSpeedPercentUpgradeButton;
        public MainShipUpgradeSlotView CargoCapacityUpgradeButton => cargoCapacityUpgradeButton;
        public MainShipUpgradeSlotView LuckUpgradeButton => luckUpgradeButton;
        public MainShipUpgradeSlotView CommanderGaugeMaxUpgradeButton => commanderGaugeMaxUpgradeButton;
        public MainShipUpgradeSlotView CommanderGaugeRecoveryPerSecondUpgradeButton => commanderGaugeRecoveryPerSecondUpgradeButton;

        public event Func<MainShipUpgradeType, bool> OnUpgradeButtonClicked;

        private void OnEnable()
        {
            SubscribeSlotButtons();
        }

        private void OnDisable()
        {
            UnsubscribeSlotButtons();
        }

        public void SetRefresh(MainShipUpgradeType type, int level, long cost)
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

                case MainShipUpgradeType.CommanderGaugeMax:
                    if (commanderGaugeMaxUpgradeButton != null)
                        commanderGaugeMaxUpgradeButton.Refresh(level, cost);
                    break;

                case MainShipUpgradeType.CommanderGaugeRecoveryPerSecond:
                    if (commanderGaugeRecoveryPerSecondUpgradeButton != null)
                        commanderGaugeRecoveryPerSecondUpgradeButton.Refresh(level, cost);
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

            if (commanderGaugeMaxUpgradeButton != null)
                commanderGaugeMaxUpgradeButton.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;

            if (commanderGaugeRecoveryPerSecondUpgradeButton != null)
                commanderGaugeRecoveryPerSecondUpgradeButton.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;
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

            if (commanderGaugeMaxUpgradeButton != null)
                commanderGaugeMaxUpgradeButton.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;

            if (commanderGaugeRecoveryPerSecondUpgradeButton != null)
                commanderGaugeRecoveryPerSecondUpgradeButton.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;
        }

        private bool HandleUpgradeButtonClicked(MainShipUpgradeType upgradeType)
        {
            return OnUpgradeButtonClicked?.Invoke(upgradeType) ?? false;
        }
    }
}
