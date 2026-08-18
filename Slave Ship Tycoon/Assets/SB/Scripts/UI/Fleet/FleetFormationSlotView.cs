using System;
using SB.Scripts.Fleet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.UI.Fleet
{
    public sealed class FleetFormationSlotView : MonoBehaviour
    {
        [SerializeField] private Button _slotButton;
        [SerializeField] private Button _removeButton;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _slotText;
        [SerializeField] private GameObject _emptyEffect;

        private int _slotIndex;

        public event Action<int> OnSlotClicked;
        public event Action<int> OnRemoveClicked;

        private void Awake()
        {
            if (_slotButton != null)
                _slotButton.onClick.AddListener(HandleSlotClicked);

            if (_removeButton != null)
                _removeButton.onClick.AddListener(HandleRemoveClicked);
        }

        private void OnDestroy()
        {
            if (_slotButton != null)
                _slotButton.onClick.RemoveListener(HandleSlotClicked);

            if (_removeButton != null)
                _removeButton.onClick.RemoveListener(HandleRemoveClicked);
        }

        public void Initialize(int slotIndex)
        {
            _slotIndex = slotIndex;

            if (_slotText != null)
                _slotText.text = (slotIndex + 1).ToString();
        }

        public void Refresh(EscortShipData shipData)
        {
            bool hasEscort = shipData != null;

            if (_icon != null)
            {
                _icon.sprite = hasEscort ? shipData.Icon : null;
                _icon.enabled = hasEscort && shipData.Icon != null;
            }

            if (_nameText != null)
                _nameText.text = hasEscort ? shipData.DisplayName : string.Empty;

            if (_removeButton != null)
                _removeButton.gameObject.SetActive(hasEscort);

            if (_emptyEffect != null)
                _emptyEffect.SetActive(hasEscort == false);
        }

        private void HandleSlotClicked()
        {
            OnSlotClicked?.Invoke(_slotIndex);
        }

        private void HandleRemoveClicked()
        {
            OnRemoveClicked?.Invoke(_slotIndex);
        }
    }
}
