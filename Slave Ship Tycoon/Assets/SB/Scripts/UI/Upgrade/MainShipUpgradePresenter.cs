using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts.UI.Upgrade
{
    public class MainShipUpgradePresenter : MonoBehaviour
    {
        [SerializeField] private MainShipUpgradeView view;
        [SerializeField] private ShipUpgradeManager model;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (view == null)
                return;

            view.OnUpgradeButtonClicked += OnClickUpgrade;
        }

        private void Start()
        {
            RefreshAllSlots();
        }

        private void OnDisable()
        {
            if (view != null)
                view.OnUpgradeButtonClicked -= OnClickUpgrade;
        }
        
        private bool OnClickUpgrade(MainShipUpgradeType upgradeType)
        {
            if (model == null)
            {
                Debug.LogError($"{nameof(MainShipUpgradePresenter)} needs a {nameof(ShipUpgradeManager)}.", this);
                return false;
            }

            if (model.TryUpgrade(upgradeType))
            {
                UpgradeGrowthData upgradeData = model.GetTargetUpdateData(upgradeType);
                if (upgradeData == null)
                    return false;

                view.SetRefresh(upgradeType, upgradeData.level, upgradeData.currentCost);
                return true;
            }

            return false;
        }

        private void RefreshAllSlots()
        {
            RefreshSlot(MainShipUpgradeType.Health);
            RefreshSlot(MainShipUpgradeType.AttackPowerPercent);
            RefreshSlot(MainShipUpgradeType.AttackSpeedPercent);
            RefreshSlot(MainShipUpgradeType.CargoCapacity);
            RefreshSlot(MainShipUpgradeType.Luck);
            RefreshSlot(MainShipUpgradeType.CommanderGaugeMax);
            RefreshSlot(MainShipUpgradeType.CommanderGaugeRecoveryPerSecond);
        }

        private void RefreshSlot(MainShipUpgradeType upgradeType)
        {
            if (model == null || view == null)
                return;

            UpgradeGrowthData upgradeData = model.GetTargetUpdateData(upgradeType);
            if (upgradeData == null)
                return;

            view.SetRefresh(upgradeType, upgradeData.level, upgradeData.currentCost);
        }

        private void ResolveReferences()
        {
            if (view == null)
                view = GetComponent<MainShipUpgradeView>();
        }
    }
}
