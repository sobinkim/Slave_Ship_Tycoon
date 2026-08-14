using SB.Scripts.TransportEquipment;

namespace SB.Core.EventBus
{
    public readonly struct TransportEquipmentChangedEvent : IEvent
    {
        public readonly TransportEquipmentItemData PreviousItem;
        public readonly TransportEquipmentItemData EquippedItem;

        public TransportEquipmentChangedEvent(
            TransportEquipmentItemData previousItem,
            TransportEquipmentItemData equippedItem)
        {
            PreviousItem = previousItem;
            EquippedItem = equippedItem;
        }
    }

    public readonly struct TransportEquipmentSnapshotChangedEvent : IEvent
    {
        public readonly TransportEquipmentSnapshot PreviousSnapshot;
        public readonly TransportEquipmentSnapshot CurrentSnapshot;

        public TransportEquipmentSnapshotChangedEvent(
            TransportEquipmentSnapshot previousSnapshot,
            TransportEquipmentSnapshot currentSnapshot)
        {
            PreviousSnapshot = previousSnapshot;
            CurrentSnapshot = currentSnapshot;
        }
    }
}
