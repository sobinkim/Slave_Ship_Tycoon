using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Fleet
{
    [Serializable]
    public struct EscortGachaEntry
    {
        [SerializeField] private EscortShipData _shipData;
        [SerializeField, Min(1)] private int _weight;

        public EscortShipData ShipData => _shipData;
        public int Weight => _weight;
    }

    [CreateAssetMenu(fileName = "EscortGachaTable", menuName = "SB/Fleet/Escort Gacha Table")]
    public sealed class EscortGachaTable : ScriptableObject
    {
        [SerializeField] private EscortGachaEntry[] _entries = Array.Empty<EscortGachaEntry>();

        public int EntryCount => _entries?.Length ?? 0;

        public EscortGachaEntry GetEntryAt(int index)
        {
            return index >= 0 && index < EntryCount ? _entries[index] : default;
        }

        public bool TryRoll(out EscortShipData result)
        {
            result = null;

            if (TryGetTotalWeight(out int totalWeight, out _) == false)
                return false;

            int roll = UnityEngine.Random.Range(0, totalWeight);

            for (int i = 0; i < _entries.Length; i++)
            {
                EscortGachaEntry entry = _entries[i];

                if (roll < entry.Weight)
                {
                    result = entry.ShipData;
                    return true;
                }

                roll -= entry.Weight;
            }

            return false;
        }

        public bool TryValidate(out string error)
        {
            return TryGetTotalWeight(out _, out error);
        }

        private bool TryGetTotalWeight(out int totalWeight, out string error)
        {
            totalWeight = 0;

            if (_entries == null || _entries.Length == 0)
            {
                error = "Gacha table has no entries.";
                return false;
            }

            HashSet<EscortShipData> ships = new HashSet<EscortShipData>();
            long total = 0;

            for (int i = 0; i < _entries.Length; i++)
            {
                EscortGachaEntry entry = _entries[i];

                if (entry.ShipData == null)
                {
                    error = $"Gacha entry {i} has no escort ship data.";
                    return false;
                }

                if (entry.Weight <= 0)
                {
                    error = $"Gacha entry {i} must have a positive weight.";
                    return false;
                }

                if (ships.Add(entry.ShipData) == false)
                {
                    error = $"{entry.ShipData.name} is duplicated in the gacha table.";
                    return false;
                }

                total += entry.Weight;

                if (total > int.MaxValue)
                {
                    error = "The total gacha weight exceeds Int32.MaxValue.";
                    return false;
                }
            }

            totalWeight = (int)total;
            error = null;
            return true;
        }
    }
}
