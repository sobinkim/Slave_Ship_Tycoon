using System;
using SB.Scripts.Fleet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.UI.Fleet
{
    public sealed class EscortInventoryItemView : MonoBehaviour
    {
        [SerializeField] private Button _selectButton;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _gradeText;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private GameObject _selectedEffect;

        private EscortShipData _shipData;

        public event Action<EscortShipData> OnSelected;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(HandleSelected);
        }

        private void OnDestroy()
        {
            if (_selectButton != null)
                _selectButton.onClick.RemoveListener(HandleSelected);
        }

        public void Refresh(EscortOwnershipSnapshot ownership, bool isSelected)
        {
            _shipData = ownership.ShipData;

            if (_icon != null)
            {
                _icon.sprite = _shipData != null ? _shipData.Icon : null;
                _icon.enabled = _shipData != null && _shipData.Icon != null;
            }

            if (_nameText != null)
                _nameText.text = _shipData != null ? _shipData.DisplayName : string.Empty;

            if (_gradeText != null)
                _gradeText.text = _shipData != null ? _shipData.Grade.ToString() : string.Empty;

            if (_countText != null)
            {
                _countText.text =
                    $"Owned {ownership.OwnedCount}  Equipped {ownership.EquippedCount}  Free {ownership.UnequippedCount}";
            }

            if (_selectButton != null)
                _selectButton.interactable = _shipData != null && ownership.OwnedCount > 0;

            if (_selectedEffect != null)
                _selectedEffect.SetActive(isSelected);
        }

        private void HandleSelected()
        {
            if (_shipData != null)
                OnSelected?.Invoke(_shipData);
        }
    }
}
