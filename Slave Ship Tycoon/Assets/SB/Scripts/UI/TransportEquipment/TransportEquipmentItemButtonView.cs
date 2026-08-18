using System;
using System.Text;
using SB.Scripts.TransportEquipment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public sealed class TransportEquipmentItemButtonView : MonoBehaviour
    {
        [SerializeField] private TransportEquipmentItemData _itemData;
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _effectText;
        [SerializeField] private GameObject _selectedEffect;

        public event Action<TransportEquipmentItemData> OnClicked;

        public TransportEquipmentItemData ItemData => _itemData;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClicked);

            Refresh();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectedEffect != null)
                _selectedEffect.SetActive(isSelected);
        }

        private void Refresh()
        {
            bool hasItem = _itemData != null;

            if (_button != null)
                _button.interactable = hasItem;

            if (_icon != null)
            {
                _icon.sprite = hasItem ? _itemData.Icon : null;
                _icon.enabled = hasItem;
            }

            if (_nameText != null)
                _nameText.text = hasItem ? _itemData.DisplayName : "EMPTY";

            if (_effectText != null)
                _effectText.text = hasItem ? BuildEffectText(_itemData) : string.Empty;
        }

        private void HandleClicked()
        {
            if (_itemData != null)
                OnClicked?.Invoke(_itemData);
        }

        public static string BuildEffectText(TransportEquipmentItemData itemData)
        {
            if (itemData == null || itemData.SettlementModifiers.Count == 0)
                return "No settlement effect";

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < itemData.SettlementModifiers.Count; i++)
            {
                TransportSettlementModifier modifier = itemData.SettlementModifiers[i];

                if (modifier == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append("  ");

                if (modifier.ModifierType == TransportSettlementModifierType.RewardMultiplier)
                    builder.Append($"{modifier.CurrencyType} x{modifier.Multiplier:0.##}");
                else
                    builder.Append($"{modifier.CurrencyType} +{modifier.FlatAmount:N0}");
            }

            return builder.ToString();
        }
    }
}
