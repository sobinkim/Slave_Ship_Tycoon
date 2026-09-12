using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Fleet
{
    [Serializable]
    public struct EscortInitialOwnership
    {
        [SerializeField] private EscortShipData _shipData;
        [SerializeField, Min(0)] private int _amount;

        public EscortShipData ShipData => _shipData;
        public int Amount => _amount;
    }

    [DisallowMultipleComponent]
    public sealed class PlayerFleetManager : MonoBehaviour
    {
        public const int MergeMaterialCount = 3;

        [SerializeField] private EscortShipCatalog _catalog;
        [SerializeField] private EscortInitialOwnership[] _initialOwnedEscorts =
            Array.Empty<EscortInitialOwnership>();
        [SerializeField] private EscortShipData[] _initialFormation =
            new EscortShipData[PlayerFleetLoadout.SlotCount];

        private readonly Dictionary<EscortShipData, int> _ownedCounts =
            new Dictionary<EscortShipData, int>();
        private readonly EscortShipData[] _formation =
            new EscortShipData[PlayerFleetLoadout.SlotCount];

        private PlayerFleetLoadout _runtimePlayerFleetLoadout;
        private bool _isInitialized;
        private bool _isConfigurationValid;

        public EscortShipCatalog Catalog => _catalog;
        public int FormationSlotCount => PlayerFleetLoadout.SlotCount;
        public bool IsReady => _isInitialized && _isConfigurationValid;

        private void Awake()
        {
            InitializeRuntimeData();
        }

        private void OnValidate()
        {
            if (_initialFormation == null)
                _initialFormation = new EscortShipData[PlayerFleetLoadout.SlotCount];
            else if (_initialFormation.Length != PlayerFleetLoadout.SlotCount)
                Array.Resize(ref _initialFormation, PlayerFleetLoadout.SlotCount);
        }

        public void SetRuntimeLoadout(PlayerFleetLoadout runtimeLoadout)
        {
            InitializeRuntimeData();
            _runtimePlayerFleetLoadout = runtimeLoadout;

            if (_isConfigurationValid)
                SyncAllFormationSlots();
        }

        public void ReleaseRuntimeLoadout(PlayerFleetLoadout runtimeLoadout)
        {
            if (_runtimePlayerFleetLoadout == runtimeLoadout)
                _runtimePlayerFleetLoadout = null;
        }

        public EscortShipData GetFormationAt(int slotIndex)
        {
            InitializeRuntimeData();
            return IsValidSlot(slotIndex) ? _formation[slotIndex] : null;
        }

        public EscortShipData[] GetFormationSnapshot()
        {
            InitializeRuntimeData();
            EscortShipData[] snapshot = new EscortShipData[_formation.Length];
            Array.Copy(_formation, snapshot, _formation.Length);
            return snapshot;
        }

        public EscortOwnershipSnapshot[] GetOwnershipSnapshot()
        {
            InitializeRuntimeData();

            if (_catalog == null)
                return Array.Empty<EscortOwnershipSnapshot>();

            List<EscortOwnershipSnapshot> snapshot = new List<EscortOwnershipSnapshot>();

            for (int i = 0; i < _catalog.Count; i++)
            {
                EscortShipData shipData = _catalog.GetAt(i);
                int ownedCount = GetOwnedCount(shipData);

                if (ownedCount <= 0)
                    continue;

                snapshot.Add(CreateOwnershipSnapshot(shipData));
            }

            return snapshot.ToArray();
        }

        public int GetOwnedCount(EscortShipData shipData)
        {
            InitializeRuntimeData();

            if (shipData == null)
                return 0;

            return _ownedCounts.TryGetValue(shipData, out int amount) ? amount : 0;
        }

        public int GetEquippedCount(EscortShipData shipData)
        {
            InitializeRuntimeData();

            if (shipData == null)
                return 0;

            int count = 0;

            for (int i = 0; i < _formation.Length; i++)
            {
                if (_formation[i] == shipData)
                    count++;
            }

            return count;
        }

        public int GetUnequippedCount(EscortShipData shipData)
        {
            return Mathf.Max(0, GetOwnedCount(shipData) - GetEquippedCount(shipData));
        }

        public bool CanGrantEscort(EscortShipData shipData, int amount = 1)
        {
            InitializeRuntimeData();

            if (_isConfigurationValid == false ||
                shipData == null ||
                amount <= 0 ||
                shipData.IsConfigured == false ||
                _catalog.Contains(shipData) == false)
            {
                return false;
            }

            return GetOwnedCount(shipData) <= int.MaxValue - amount;
        }

        public bool TryGrantEscort(EscortShipData shipData, int amount = 1)
        {
            if (CanGrantEscort(shipData, amount) == false)
                return false;

            _ownedCounts[shipData] = GetOwnedCount(shipData) + amount;
            RaiseOwnershipChanged(shipData);
            return true;
        }

        public bool TryPlaceEscort(
            int slotIndex,
            EscortShipData shipData,
            out EscortFormationFailureReason failureReason)
        {
            InitializeRuntimeData();

            if (IsValidSlot(slotIndex) == false)
            {
                failureReason = EscortFormationFailureReason.InvalidSlot;
                return false;
            }

            if (_isConfigurationValid == false ||
                shipData == null ||
                shipData.IsConfigured == false ||
                _catalog.Contains(shipData) == false)
            {
                failureReason = EscortFormationFailureReason.InvalidEscort;
                return false;
            }

            EscortShipData previousEscort = _formation[slotIndex];

            if (previousEscort == shipData)
            {
                failureReason = EscortFormationFailureReason.None;
                return true;
            }

            int ownedCount = GetOwnedCount(shipData);

            if (ownedCount <= 0)
            {
                failureReason = EscortFormationFailureReason.EscortNotOwned;
                return false;
            }

            if (GetEquippedCount(shipData) >= ownedCount)
            {
                failureReason = EscortFormationFailureReason.NotEnoughCopies;
                return false;
            }

            _formation[slotIndex] = shipData;
            SyncFormationSlot(slotIndex);

            Bus<PlayerFleetFormationChangedEvent>.Raise(
                new PlayerFleetFormationChangedEvent(slotIndex, previousEscort, shipData));

            RaiseOwnershipChanged(previousEscort);
            RaiseOwnershipChanged(shipData);

            failureReason = EscortFormationFailureReason.None;
            return true;
        }

        public bool TryRemoveEscort(int slotIndex)
        {
            InitializeRuntimeData();

            if (_isConfigurationValid == false || IsValidSlot(slotIndex) == false)
                return false;

            EscortShipData previousEscort = _formation[slotIndex];

            if (previousEscort == null)
                return true;

            _formation[slotIndex] = null;
            SyncFormationSlot(slotIndex);

            Bus<PlayerFleetFormationChangedEvent>.Raise(
                new PlayerFleetFormationChangedEvent(slotIndex, previousEscort, null));
            RaiseOwnershipChanged(previousEscort);
            return true;
        }

        public bool CanMerge(
            EscortShipData source,
            out EscortMergeFailureReason failureReason,
            out EscortShipData result)
        {
            InitializeRuntimeData();
            result = null;

            if (source == null || source.IsConfigured == false)
            {
                failureReason = EscortMergeFailureReason.InvalidSource;
                return false;
            }

            if (_isConfigurationValid == false || _catalog == null)
            {
                failureReason = EscortMergeFailureReason.MissingCatalog;
                return false;
            }

            if (_catalog.Contains(source) == false)
            {
                failureReason = EscortMergeFailureReason.InvalidSource;
                return false;
            }

            if (source.Grade == EscortShipGrade.Legendary)
            {
                failureReason = EscortMergeFailureReason.HighestGrade;
                return false;
            }

            if (_catalog.TryGetMergeResult(source, out result) == false)
            {
                failureReason = EscortMergeFailureReason.NoNextGradeCandidate;
                return false;
            }

            if (result == null ||
                result == source ||
                result.IsConfigured == false ||
                _catalog.Contains(result) == false ||
                GetOwnedCount(result) == int.MaxValue)
            {
                failureReason = EscortMergeFailureReason.InvalidResult;
                return false;
            }

            if (GetOwnedCount(source) <= 0)
            {
                failureReason = EscortMergeFailureReason.SourceNotOwned;
                return false;
            }

            if (GetUnequippedCount(source.Grade) < MergeMaterialCount)
            {
                failureReason = EscortMergeFailureReason.NotEnoughUnequippedCopies;
                return false;
            }

            failureReason = EscortMergeFailureReason.None;
            return true;
        }

        public EscortMergeResult TryMerge(EscortShipData source)
        {
            if (CanMerge(source, out EscortMergeFailureReason failureReason, out EscortShipData result) == false)
            {
                EscortMergeResult failedResult = new EscortMergeResult(
                    false,
                    failureReason,
                    source,
                    result,
                    0,
                    result != null ? GetOwnedCount(result) : 0);
                Bus<EscortMergeResultEvent>.Raise(new EscortMergeResultEvent(failedResult));
                return failedResult;
            }

            List<KeyValuePair<EscortShipData, int>> materials = GetMergeMaterials(source);
            return CommitMerge(source, result, materials);
        }

        public bool TryGetMergePreview(EscortShipData source, out EscortShipData result)
        {
            InitializeRuntimeData();
            result = null;
            return source != null && _catalog != null && _catalog.TryGetMergeResult(source, out result);
        }

        public bool TryMergeSelected(IReadOnlyList<EscortShipData> selectedMaterials, out EscortShipData reward)
        {
            reward = null;
            if (selectedMaterials == null || selectedMaterials.Count != MergeMaterialCount) return false;
            EscortShipData source = selectedMaterials[0];
            if (!CanMerge(source, out _, out EscortShipData result)) return false;
            var counts = new Dictionary<EscortShipData, int>();
            foreach (EscortShipData material in selectedMaterials)
            {
                if (material == null || material.Grade != source.Grade || !_catalog.Contains(material)) return false;
                counts.TryGetValue(material, out int count);
                counts[material] = count + 1;
            }
            var materials = new List<KeyValuePair<EscortShipData, int>>();
            foreach (var entry in counts)
            {
                if (GetUnequippedCount(entry.Key) < entry.Value) return false;
                materials.Add(entry);
            }
            CommitMerge(source, result, materials);
            reward = result;
            return true;
        }

        private EscortMergeResult CommitMerge(EscortShipData source, EscortShipData result,
            List<KeyValuePair<EscortShipData, int>> materials)
        {
            int resultOwnedCount = GetOwnedCount(result) + 1;

            for (int i = 0; i < materials.Count; i++)
                SetOwnedCount(materials[i].Key, GetOwnedCount(materials[i].Key) - materials[i].Value);

            SetOwnedCount(result, resultOwnedCount);

            EscortMergeResult successResult = new EscortMergeResult(
                true,
                EscortMergeFailureReason.None,
                source,
                result,
                MergeMaterialCount,
                resultOwnedCount);

            for (int i = 0; i < materials.Count; i++)
                RaiseOwnershipChanged(materials[i].Key);

            RaiseOwnershipChanged(result);
            Bus<EscortMergeResultEvent>.Raise(new EscortMergeResultEvent(successResult));
            return successResult;
        }

        private void InitializeRuntimeData()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            _ownedCounts.Clear();
            Array.Clear(_formation, 0, _formation.Length);

            if (_catalog == null)
            {
                Debug.LogError($"{nameof(PlayerFleetManager)} needs an {nameof(EscortShipCatalog)}.", this);
                return;
            }

            if (_catalog.TryValidate(out string catalogError) == false)
            {
                Debug.LogError($"Invalid escort catalog: {catalogError}", _catalog);
                return;
            }

            _isConfigurationValid = true;
            InitializeOwnedEscorts();
            InitializeFormation();
        }

        private void InitializeOwnedEscorts()
        {
            if (_initialOwnedEscorts == null)
                return;

            for (int i = 0; i < _initialOwnedEscorts.Length; i++)
            {
                EscortInitialOwnership entry = _initialOwnedEscorts[i];

                if (entry.ShipData == null || entry.Amount <= 0)
                    continue;

                if (_catalog.Contains(entry.ShipData) == false)
                {
                    Debug.LogWarning(
                        $"Initial owned escort {entry.ShipData.name} is not in {_catalog.name}.",
                        this);
                    continue;
                }

                int currentAmount = GetOwnedCount(entry.ShipData);

                if (currentAmount > int.MaxValue - entry.Amount)
                {
                    Debug.LogWarning(
                        $"Initial owned amount for {entry.ShipData.name} exceeds Int32.MaxValue.",
                        this);
                    continue;
                }

                _ownedCounts[entry.ShipData] = currentAmount + entry.Amount;
            }
        }

        private void InitializeFormation()
        {
            if (_initialFormation == null)
                return;

            int count = Mathf.Min(_initialFormation.Length, _formation.Length);

            for (int i = 0; i < count; i++)
            {
                EscortShipData shipData = _initialFormation[i];

                if (shipData == null)
                    continue;

                if (_catalog.Contains(shipData) == false || shipData.IsConfigured == false)
                {
                    Debug.LogWarning($"Formation slot {i} has an invalid escort ship.", this);
                    continue;
                }

                if (GetEquippedCount(shipData) >= GetOwnedCount(shipData))
                {
                    Debug.LogWarning(
                        $"Formation slot {i} exceeds the owned count of {shipData.DisplayName}.",
                        this);
                    continue;
                }

                _formation[i] = shipData;
            }
        }

        private void SyncAllFormationSlots()
        {
            if (_runtimePlayerFleetLoadout == null)
                return;

            for (int i = 0; i < _formation.Length; i++)
                SyncFormationSlot(i);
        }

        private void SyncFormationSlot(int slotIndex)
        {
            if (_runtimePlayerFleetLoadout == null)
                return;

            EscortShipData shipData = _formation[slotIndex];
            _runtimePlayerFleetLoadout.SetEscortAt(
                slotIndex,
                shipData != null ? shipData.ShipPrefab : null);
        }

        private void SetOwnedCount(EscortShipData shipData, int amount)
        {
            if (amount <= 0)
                _ownedCounts.Remove(shipData);
            else
                _ownedCounts[shipData] = amount;
        }

        private int GetUnequippedCount(EscortShipGrade grade)
        {
            int count = 0;

            for (int i = 0; i < _catalog.Count; i++)
            {
                EscortShipData shipData = _catalog.GetAt(i);

                if (shipData == null || shipData.Grade != grade)
                    continue;

                int unequippedCount = GetUnequippedCount(shipData);

                if (unequippedCount >= MergeMaterialCount - count)
                    return MergeMaterialCount;

                count += unequippedCount;
            }

            return count;
        }

        public List<KeyValuePair<EscortShipData, int>> GetMergeMaterials(EscortShipData selectedSource)
        {
            InitializeRuntimeData();
            List<KeyValuePair<EscortShipData, int>> materials =
                new List<KeyValuePair<EscortShipData, int>>();

            if (selectedSource == null || _catalog == null || _isConfigurationValid == false)
                return materials;

            int remaining = MergeMaterialCount;
            remaining -= AddMergeMaterial(selectedSource, remaining, materials);

            for (int i = 0; i < _catalog.Count && remaining > 0; i++)
            {
                EscortShipData candidate = _catalog.GetAt(i);

                if (candidate == null ||
                    candidate == selectedSource ||
                    candidate.Grade != selectedSource.Grade)
                {
                    continue;
                }

                remaining -= AddMergeMaterial(candidate, remaining, materials);
            }

            return materials;
        }

        private int AddMergeMaterial(
            EscortShipData shipData,
            int requestedAmount,
            List<KeyValuePair<EscortShipData, int>> materials)
        {
            int consumeAmount = Mathf.Min(requestedAmount, GetUnequippedCount(shipData));

            if (consumeAmount <= 0)
                return 0;

            materials.Add(new KeyValuePair<EscortShipData, int>(shipData, consumeAmount));

            return consumeAmount;
        }

        private EscortOwnershipSnapshot CreateOwnershipSnapshot(EscortShipData shipData)
        {
            return new EscortOwnershipSnapshot(
                shipData,
                GetOwnedCount(shipData),
                GetEquippedCount(shipData));
        }

        private void RaiseOwnershipChanged(EscortShipData shipData)
        {
            if (shipData == null)
                return;

            Bus<EscortOwnershipChangedEvent>.Raise(
                new EscortOwnershipChangedEvent(CreateOwnershipSnapshot(shipData)));
        }

        private static bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < PlayerFleetLoadout.SlotCount;
        }
    }
}
