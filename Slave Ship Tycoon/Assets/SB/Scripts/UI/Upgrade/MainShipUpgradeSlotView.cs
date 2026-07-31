using System;
using SB.Scripts.Upgrade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.UI.Upgrade
{
    public class MainShipUpgradeSlotView : MonoBehaviour
    {
        [SerializeField] private MainShipUpgradeType _upgradeType;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _cost;
        [SerializeField] private TMP_Text _level;
        [SerializeField] private Button _button;

        public event Action<MainShipUpgradeType> OnUpgradeButtonClicked;

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleUpgradeButtonClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleUpgradeButtonClicked);
        }

        public void Refresh(int level, float cost)
        {
            _level.text = level.ToString();
            _cost.text = cost.ToString();
        }

        private void HandleUpgradeButtonClicked()
        {
            OnUpgradeButtonClicked?.Invoke(_upgradeType);
        }
    }
}
