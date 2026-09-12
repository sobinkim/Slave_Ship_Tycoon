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
        [SerializeField] private Image gradeBorder;
        [SerializeField] private Color[] gradeColors = { new Color(.55f,.65f,.7f), new Color(.2f,.8f,.6f), new Color(.25f,.6f,1), new Color(.7f,.35f,1), new Color(1,.7f,.2f) };

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
                _gradeText.text = _shipData != null ? GradeLabel(_shipData.Grade) : string.Empty;
            if (gradeBorder != null && _shipData != null) gradeBorder.color = gradeColors[(int)_shipData.Grade];

            if (_countText != null)
            {
                _countText.text =
                    $"대기 {ownership.UnequippedCount:N0} · 편성 {ownership.EquippedCount:N0}";
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

        public void SetMaterialState(bool canSelect, int selectedCount)
        {
            _selectButton.interactable = canSelect;
            if (_selectedEffect != null) _selectedEffect.SetActive(selectedCount > 0);
            if (selectedCount > 0) _countText.text = $"선택 {selectedCount}척";
            else if (!canSelect) _countText.text = "선택 불가";
        }

        public static string GradeLabel(EscortShipGrade grade)
        {
            switch(grade) { case EscortShipGrade.Common:return "일반"; case EscortShipGrade.Uncommon:return "고급"; case EscortShipGrade.Rare:return "희귀"; case EscortShipGrade.Epic:return "영웅"; default:return "전설"; }
        }
    }
}
