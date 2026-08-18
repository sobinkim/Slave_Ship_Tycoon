using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.TransportEquipment
{
    public enum TransportEquipmentGrade
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(
        fileName = "TransportEquipmentItem",
        menuName = "SB/Transport Equipment/Item")]
    public sealed class TransportEquipmentItemData : ScriptableObject
    {
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private TransportEquipmentGrade _grade;
        [SerializeField] private TransportSettlementModifier[] _settlementModifiers =
            Array.Empty<TransportSettlementModifier>();

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public TransportEquipmentGrade Grade => _grade;
        public IReadOnlyList<TransportSettlementModifier> SettlementModifiers =>
            _settlementModifiers ?? Array.Empty<TransportSettlementModifier>();

        internal bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(_itemId))
            {
                errorMessage = $"{name} needs a stable item id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errorMessage = $"{name} needs a display name.";
                return false;
            }

            if (_icon == null)
            {
                errorMessage = $"{name} needs an icon.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        internal TransportSettlementModifierValue[] CreateModifierSnapshot()
        {
            if (_settlementModifiers == null || _settlementModifiers.Length == 0)
                return Array.Empty<TransportSettlementModifierValue>();

            List<TransportSettlementModifierValue> values =
                new List<TransportSettlementModifierValue>(_settlementModifiers.Length);
            HashSet<TransportSettlementModifierKey> keys = new HashSet<TransportSettlementModifierKey>();

            for (int i = 0; i < _settlementModifiers.Length; i++)
            {
                TransportSettlementModifier modifier = _settlementModifiers[i];

                if (modifier == null)
                {
                    Debug.LogWarning($"{name} has a null settlement modifier at index {i}.", this);
                    continue;
                }

                if (modifier.TryCreateSnapshotValue(out TransportSettlementModifierValue value) == false)
                {
                    Debug.LogWarning($"{name} has an invalid settlement modifier at index {i}.", this);
                    continue;
                }

                TransportSettlementModifierKey key = new TransportSettlementModifierKey(
                    value.CurrencyType,
                    value.ModifierType);

                if (keys.Add(key) == false)
                {
                    Debug.LogWarning(
                        $"{name} has a duplicate {value.CurrencyType} {value.ModifierType} modifier. " +
                        "Only the first entry is used.",
                        this);
                    continue;
                }

                values.Add(value);
            }

            return values.ToArray();
        }

        private void OnValidate()
        {
            _itemId = _itemId?.Trim();
            _displayName = _displayName?.Trim();

            if (_settlementModifiers == null)
            {
                _settlementModifiers = Array.Empty<TransportSettlementModifier>();
                return;
            }

            HashSet<TransportSettlementModifierKey> keys = new HashSet<TransportSettlementModifierKey>();

            for (int i = 0; i < _settlementModifiers.Length; i++)
            {
                TransportSettlementModifier modifier = _settlementModifiers[i];

                if (modifier == null)
                {
                    Debug.LogWarning($"{name} has a null settlement modifier at index {i}.", this);
                    continue;
                }

                modifier.ValidateSerializedValues();
                TransportSettlementModifierKey key = new TransportSettlementModifierKey(
                    modifier.CurrencyType,
                    modifier.ModifierType);

                if (keys.Add(key) == false)
                {
                    Debug.LogWarning(
                        $"{name} has a duplicate {modifier.CurrencyType} {modifier.ModifierType} modifier.",
                        this);
                }
            }
        }
    }
}
