using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.TransportEquipment
{
    public sealed class TransportEquipmentSnapshot
    {
        private readonly TransportSettlementModifierValue[] _settlementModifiers;
        private readonly IReadOnlyList<TransportSettlementModifierValue> _readOnlySettlementModifiers;

        public static TransportEquipmentSnapshot Empty { get; } =
            new TransportEquipmentSnapshot(-1, null, Array.Empty<TransportSettlementModifierValue>());

        public int SellChapter { get; }
        public TransportEquipmentItemData ItemData { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public TransportEquipmentGrade Grade { get; }
        public bool HasEquipment => ItemData != null;
        public IReadOnlyList<TransportSettlementModifierValue> SettlementModifiers =>
            _readOnlySettlementModifiers;

        private TransportEquipmentSnapshot(
            int sellChapter,
            TransportEquipmentItemData itemData,
            TransportSettlementModifierValue[] settlementModifiers)
        {
            SellChapter = sellChapter;
            ItemData = itemData;
            ItemId = itemData != null ? itemData.ItemId : string.Empty;
            DisplayName = itemData != null ? itemData.DisplayName : string.Empty;
            Icon = itemData != null ? itemData.Icon : null;
            Grade = itemData != null ? itemData.Grade : default;
            _settlementModifiers = settlementModifiers ?? Array.Empty<TransportSettlementModifierValue>();
            _readOnlySettlementModifiers = Array.AsReadOnly(_settlementModifiers);
        }

        internal static TransportEquipmentSnapshot Create(
            int sellChapter,
            TransportEquipmentItemData itemData)
        {
            TransportSettlementModifierValue[] modifiers = itemData != null
                ? itemData.CreateModifierSnapshot()
                : Array.Empty<TransportSettlementModifierValue>();

            return new TransportEquipmentSnapshot(sellChapter, itemData, modifiers);
        }

    }
}
