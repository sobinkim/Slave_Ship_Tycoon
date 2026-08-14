using System;
using SB.Scripts.TransportEquipment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public sealed class TransportEquipmentPanelView : MonoBehaviour
    {
        [SerializeField] private Image _equippedIcon;
        [SerializeField] private TMP_Text _equippedNameText;
        [SerializeField] private TMP_Text _equippedEffectText;
        [SerializeField] private TransportEquipmentItemButtonView[] _itemButtons =
            Array.Empty<TransportEquipmentItemButtonView>();

        public event Action<TransportEquipmentItemData> OnItemSelected;

        private void Awake()
        {
            for (int i = 0; i < _itemButtons.Length; i++)
            {
                if (_itemButtons[i] != null)
                    _itemButtons[i].OnClicked += HandleItemSelected;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _itemButtons.Length; i++)
            {
                if (_itemButtons[i] != null)
                    _itemButtons[i].OnClicked -= HandleItemSelected;
            }
        }

        public void Refresh(TransportEquipmentItemData equippedItem)
        {
            bool hasItem = equippedItem != null;

            if (_equippedIcon != null)
            {
                _equippedIcon.sprite = hasItem ? equippedItem.Icon : null;
                _equippedIcon.enabled = hasItem;
            }

            if (_equippedNameText != null)
                _equippedNameText.text = hasItem ? equippedItem.DisplayName : "NO EQUIPMENT";

            if (_equippedEffectText != null)
            {
                _equippedEffectText.text = hasItem
                    ? TransportEquipmentItemButtonView.BuildEffectText(equippedItem)
                    : string.Empty;
            }

            for (int i = 0; i < _itemButtons.Length; i++)
            {
                TransportEquipmentItemButtonView itemButton = _itemButtons[i];

                if (itemButton != null)
                    itemButton.SetSelected(itemButton.ItemData == equippedItem);
            }
        }

        private void HandleItemSelected(TransportEquipmentItemData itemData)
        {
            OnItemSelected?.Invoke(itemData);
        }
    }
}
