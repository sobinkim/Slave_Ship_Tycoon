using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.Scripts.TransportEquipment
{
    public sealed class TransportEquipmentManager : MonoBehaviour
    {
        [SerializeField] private TransportEquipmentItemData _initialEquipment;
        [SerializeField] private TransportEquipmentItemData[] _ownedItems =
            Array.Empty<TransportEquipmentItemData>();

        private TransportEquipmentItemData _equippedItem;
        private TransportEquipmentSnapshot _sellSnapshot = TransportEquipmentSnapshot.Empty;

        public TransportEquipmentItemData EquippedItem => _equippedItem;
        public TransportEquipmentSnapshot SellSnapshot => _sellSnapshot;
        public bool HasEquippedItem => _equippedItem != null;

        public bool IsOwned(TransportEquipmentItemData itemData)
        {
            if (itemData == null || _ownedItems == null)
                return false;

            for (int i = 0; i < _ownedItems.Length; i++)
            {
                if (_ownedItems[i] == itemData)
                    return true;
            }

            return false;
        }

        private void Awake()
        {
            if (_initialEquipment != null)
                TryEquip(_initialEquipment);
        }

        private void OnEnable()
        {
            Bus<SellChapterStartedEvent>.OnEvent += OnSellChapterStarted;
        }

        private void OnDisable()
        {
            Bus<SellChapterStartedEvent>.OnEvent -= OnSellChapterStarted;
        }

        public bool TryEquip(TransportEquipmentItemData itemData)
        {
            if (itemData == null)
            {
                Debug.LogWarning($"{nameof(TransportEquipmentManager)} cannot equip a null item.", this);
                return false;
            }

            if (IsOwned(itemData) == false)
            {
                Debug.LogWarning($"{itemData.DisplayName} is not owned.", this);
                return false;
            }

            if (itemData.TryValidate(out string errorMessage) == false)
            {
                Debug.LogError(errorMessage, itemData);
                return false;
            }

            if (ReferenceEquals(_equippedItem, itemData))
                return false;

            if (_equippedItem != null &&
                string.Equals(_equippedItem.ItemId, itemData.ItemId, StringComparison.Ordinal))
            {
                Debug.LogError(
                    $"Transport equipment id '{itemData.ItemId}' is duplicated by " +
                    $"{_equippedItem.name} and {itemData.name}.",
                    itemData);
                return false;
            }

            TransportEquipmentItemData previousItem = _equippedItem;
            _equippedItem = itemData;

            Bus<TransportEquipmentChangedEvent>.Raise(
                new TransportEquipmentChangedEvent(previousItem, _equippedItem));
            return true;
        }

        public bool TryRemoveEquipment(out TransportEquipmentItemData removedItem)
        {
            removedItem = _equippedItem;

            if (removedItem == null)
                return false;

            _equippedItem = null;

            Bus<TransportEquipmentChangedEvent>.Raise(
                new TransportEquipmentChangedEvent(removedItem, null));
            return true;
        }

        /// <summary>
        /// Applies the Sell-entry snapshot in place. Multipliers run before flat rewards,
        /// fractional results round down, and overflow saturates at long.MaxValue.
        /// </summary>
        public void ApplySnapshotModifiers(Dictionary<CurrencyType, long> settlementRewards)
        {
            if (settlementRewards == null)
                throw new ArgumentNullException(nameof(settlementRewards));

            ValidateSettlementRewards(settlementRewards);

            if (_sellSnapshot.HasEquipment == false)
                return;

            ApplyMultipliers(settlementRewards);
            ApplyFlatRewards(settlementRewards);
        }

        private void OnSellChapterStarted(SellChapterStartedEvent evt)
        {
            TransportEquipmentSnapshot previousSnapshot = _sellSnapshot;
            _sellSnapshot = TransportEquipmentSnapshot.Create(evt.Chapter, _equippedItem);

            Bus<TransportEquipmentSnapshotChangedEvent>.Raise(
                new TransportEquipmentSnapshotChangedEvent(previousSnapshot, _sellSnapshot));
        }

        private void ApplyMultipliers(Dictionary<CurrencyType, long> settlementRewards)
        {
            IReadOnlyList<TransportSettlementModifierValue> modifiers =
                _sellSnapshot.SettlementModifiers;

            for (int i = 0; i < modifiers.Count; i++)
            {
                TransportSettlementModifierValue modifier = modifiers[i];

                if (modifier.ModifierType != TransportSettlementModifierType.RewardMultiplier)
                    continue;

                if (settlementRewards.TryGetValue(modifier.CurrencyType, out long currentAmount) == false)
                    continue;

                settlementRewards[modifier.CurrencyType] = SaturatingMultiply(
                    currentAmount,
                    modifier.Multiplier);
            }
        }

        private void ApplyFlatRewards(Dictionary<CurrencyType, long> settlementRewards)
        {
            IReadOnlyList<TransportSettlementModifierValue> modifiers =
                _sellSnapshot.SettlementModifiers;

            for (int i = 0; i < modifiers.Count; i++)
            {
                TransportSettlementModifierValue modifier = modifiers[i];

                if (modifier.ModifierType != TransportSettlementModifierType.FlatReward ||
                    modifier.FlatAmount == 0)
                {
                    continue;
                }

                settlementRewards.TryGetValue(modifier.CurrencyType, out long currentAmount);
                settlementRewards[modifier.CurrencyType] = SaturatingAdd(
                    currentAmount,
                    modifier.FlatAmount);
            }
        }

        private static void ValidateSettlementRewards(
            Dictionary<CurrencyType, long> settlementRewards)
        {
            foreach (KeyValuePair<CurrencyType, long> reward in settlementRewards)
            {
                if (reward.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(settlementRewards),
                        $"Settlement reward for {reward.Key} cannot be negative.");
                }
            }
        }

        private static long SaturatingMultiply(long value, float multiplier)
        {
            if (value == 0)
                return 0;

            double result = value * (double)multiplier;

            if (double.IsInfinity(result) || result >= long.MaxValue)
                return long.MaxValue;

            return (long)Math.Floor(result);
        }

        private static long SaturatingAdd(long value, long amount)
        {
            if (amount > long.MaxValue - value)
                return long.MaxValue;

            return value + amount;
        }
    }
}
