using System;
using UnityEngine;

namespace SB.Scripts
{
    [CreateAssetMenu(fileName = "PlayerFleetLoadout", menuName = "SB/Stage/Player Fleet Loadout")]
    public class PlayerFleetLoadout : ScriptableObject
    {
        public const int RowCount = 3;
        public const int ColumnCount = 2;
        public const int SlotCount = RowCount * ColumnCount;

        [SerializeField] private MainShip mainShipPrefab;
        [SerializeField] private Ship[] escortShipPrefabs = new Ship[SlotCount];

        public MainShip MainShipPrefab => mainShipPrefab;
        public int EscortSlotCount => SlotCount;

        public Ship GetEscortAt(int index)
        {
            ValidateSlotIndex(index);
            EnsureSlotCount();

            return escortShipPrefabs[index];
        }

        public void SetEscortAt(int index, Ship escortShipPrefab)
        {
            ValidateSlotIndex(index);
            EnsureSlotCount();
            escortShipPrefabs[index] = escortShipPrefab;
        }

        public void RemoveEscortAt(int index)
        {
            SetEscortAt(index, null);
        }

        public void SwapEscorts(int firstIndex, int secondIndex)
        {
            ValidateSlotIndex(firstIndex);
            ValidateSlotIndex(secondIndex);
            EnsureSlotCount();

            (escortShipPrefabs[firstIndex], escortShipPrefabs[secondIndex]) =
                (escortShipPrefabs[secondIndex], escortShipPrefabs[firstIndex]);
        }

        private void OnValidate()
        {
            EnsureSlotCount();
        }

        private void EnsureSlotCount()
        {
            if (escortShipPrefabs != null && escortShipPrefabs.Length == SlotCount)
                return;

            Ship[] resizedSlots = new Ship[SlotCount];

            if (escortShipPrefabs != null)
            {
                int copyCount = Mathf.Min(escortShipPrefabs.Length, SlotCount);
                Array.Copy(escortShipPrefabs, resizedSlots, copyCount);
            }

            escortShipPrefabs = resizedSlots;
        }

        private static void ValidateSlotIndex(int index)
        {
            if (index < 0 || index >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    $"Escort slot index must be between 0 and {SlotCount - 1}.");
            }
        }
    }
}
