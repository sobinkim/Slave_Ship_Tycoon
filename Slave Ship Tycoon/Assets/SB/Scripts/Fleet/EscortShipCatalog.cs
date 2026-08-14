using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Fleet
{
    [CreateAssetMenu(fileName = "EscortShipCatalog", menuName = "SB/Fleet/Escort Ship Catalog")]
    public sealed class EscortShipCatalog : ScriptableObject
    {
        [SerializeField] private EscortShipData[] _ships = Array.Empty<EscortShipData>();

        public int Count => _ships?.Length ?? 0;

        public EscortShipData GetAt(int index)
        {
            return index >= 0 && index < Count ? _ships[index] : null;
        }

        public bool Contains(EscortShipData shipData)
        {
            if (shipData == null || _ships == null)
                return false;

            for (int i = 0; i < _ships.Length; i++)
            {
                if (_ships[i] == shipData)
                    return true;
            }

            return false;
        }

        public bool TryGetByStableId(string stableId, out EscortShipData shipData)
        {
            shipData = null;

            if (string.IsNullOrWhiteSpace(stableId) || _ships == null)
                return false;

            for (int i = 0; i < _ships.Length; i++)
            {
                EscortShipData candidate = _ships[i];

                if (candidate != null &&
                    string.Equals(candidate.StableId, stableId, StringComparison.Ordinal))
                {
                    shipData = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetMergeResult(EscortShipData source, out EscortShipData result)
        {
            result = null;

            if (source == null || source.Grade == EscortShipGrade.Legendary || _ships == null)
                return false;

            EscortShipGrade nextGrade = (EscortShipGrade)((int)source.Grade + 1);

            // Merge results are deterministic: the first next-grade entry in catalog order wins.
            for (int i = 0; i < _ships.Length; i++)
            {
                EscortShipData candidate = _ships[i];

                if (candidate != null && candidate.Grade == nextGrade)
                {
                    result = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryValidate(out string error)
        {
            if (_ships == null || _ships.Length == 0)
            {
                error = "Catalog has no escort ships.";
                return false;
            }

            HashSet<string> stableIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < _ships.Length; i++)
            {
                EscortShipData shipData = _ships[i];

                if (shipData == null)
                {
                    error = $"Catalog entry {i} is empty.";
                    return false;
                }

                if (shipData.IsConfigured == false)
                {
                    error = $"{shipData.name} is missing a stable id, display name, or ship prefab.";
                    return false;
                }

                if (stableIds.Add(shipData.StableId) == false)
                {
                    error = $"Stable id '{shipData.StableId}' is duplicated in the catalog.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
