using System;
using UnityEngine;

namespace SB.Scripts
{
    [CreateAssetMenu(fileName = "PlayerFleetLoadout", menuName = "SB/Stage/Player Fleet Loadout")]
    public class PlayerFleetLoadout : ScriptableObject
    {
        public const int RowCount = 3;
        public const int ColumnCount = 3;
        public const int SlotCount = 8;

        // Slots 0-5 retain their saved array indices and physical positions.
        // The rear column adds slots 6/7 above/below the fixed main ship.
        private static readonly Vector2Int[] EscortSlotCoordinates =
        {
            new Vector2Int(1, 0), new Vector2Int(2, 0),
            new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(1, 2), new Vector2Int(2, 2),
            new Vector2Int(0, 0), new Vector2Int(0, 2)
        };

        public static Vector2Int MainShipSlot => new Vector2Int(0, 1);

        [SerializeField] private MainShip mainShipPrefab;
        [SerializeField, Tooltip("1~6: 기존 중열/전열 3행. 7: 메인 함선 위 후열. 8: 메인 함선 아래 후열.")]
        private Ship[] escortShipPrefabs = new Ship[SlotCount];

        public MainShip MainShipPrefab => mainShipPrefab;
        public int EscortSlotCount => SlotCount;
        public int Version { get; private set; }

        public static Vector2Int GetEscortSlotCoordinates(int index)
        {
            ValidateSlotIndex(index);
            return EscortSlotCoordinates[index];
        }

        public static int GetEscortSlotIndex(int column, int row)
        {
            var coordinates = new Vector2Int(column, row);
            for (int i = 0; i < EscortSlotCoordinates.Length; i++)
                if (EscortSlotCoordinates[i] == coordinates) return i;
            return -1;
        }

        public static string GetColumnLabel(int column) => column switch
        {
            0 => "후열", 1 => "중열", 2 => "전열", _ => string.Empty
        };

        public static string GetFormationPositionLabel(int slotIndex)
        {
            Vector2Int slot = GetEscortSlotCoordinates(slotIndex);
            return $"{slot.y + 1}행 {GetColumnLabel(slot.x)}";
        }

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

            if (escortShipPrefabs[index] == escortShipPrefab)
                return;

            escortShipPrefabs[index] = escortShipPrefab;
            Version++;
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

            if (escortShipPrefabs[firstIndex] == escortShipPrefabs[secondIndex])
                return;

            (escortShipPrefabs[firstIndex], escortShipPrefabs[secondIndex]) =
                (escortShipPrefabs[secondIndex], escortShipPrefabs[firstIndex]);
            Version++;
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
