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
        
        private void OnClickUpgrade(MainShipUpgradeType upgradeType)
        {
            if (model.TryUpgrade(upgradeType))
            {
                UpgradeGrowthData upgradeData = model.GetTargetUpdateData(upgradeType);
                if (upgradeData == null)
                    return;

                view.SetRefresh(upgradeType, upgradeData.level, upgradeData.currentCost);
            }
        }

        private void RefreshAllSlots()
        {
            RefreshSlot(MainShipUpgradeType.Health);
            RefreshSlot(MainShipUpgradeType.AttackPowerPercent);
            RefreshSlot(MainShipUpgradeType.AttackSpeedPercent);
            RefreshSlot(MainShipUpgradeType.CargoCapacity);
            RefreshSlot(MainShipUpgradeType.Luck);
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

            GameObject managerObject = GameObject.Find("ShipUpgradeManager");
            if (managerObject != null && managerObject.TryGetComponent(out ShipUpgradeManager upgradeManager))
                model = upgradeManager;
        }
    }
}
