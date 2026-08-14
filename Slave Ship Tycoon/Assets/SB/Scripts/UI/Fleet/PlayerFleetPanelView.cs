using System;
using System.Collections.Generic;
using SB.Scripts.Currency;
using SB.Scripts.Fleet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.UI.Fleet
{
    public sealed class PlayerFleetPanelView : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private Transform _inventoryRoot;
        [SerializeField] private EscortInventoryItemView _inventoryItemPrefab;

        [Header("Formation")]
        [SerializeField] private FleetFormationSlotView[] _formationSlots =
            new FleetFormationSlotView[PlayerFleetLoadout.SlotCount];

        [Header("Commands")]
        [SerializeField] private Button _summonButton;
        [SerializeField] private Button _mergeButton;
        [SerializeField] private TMP_Text _summonCostText;
        [SerializeField] private TMP_Text _selectedEscortText;
        [SerializeField] private TMP_Text _resultText;

        private readonly Dictionary<EscortShipData, EscortInventoryItemView> _inventoryItems =
            new Dictionary<EscortShipData, EscortInventoryItemView>();

        public event Action<EscortShipData> OnEscortSelected;
        public event Action<int> OnFormationSlotClicked;
        public event Action<int> OnFormationRemoveClicked;
        public event Action OnSummonClicked;
        public event Action OnMergeClicked;

        private void Awake()
        {
            if (_summonButton != null)
                _summonButton.onClick.AddListener(HandleSummonClicked);

            if (_mergeButton != null)
                _mergeButton.onClick.AddListener(HandleMergeClicked);

            SubscribeFormationSlots();
        }

        private void OnDestroy()
        {
            if (_summonButton != null)
                _summonButton.onClick.RemoveListener(HandleSummonClicked);

            if (_mergeButton != null)
                _mergeButton.onClick.RemoveListener(HandleMergeClicked);

            UnsubscribeFormationSlots();

            foreach (KeyValuePair<EscortShipData, EscortInventoryItemView> pair in _inventoryItems)
            {
                if (pair.Value != null)
                    pair.Value.OnSelected -= HandleEscortSelected;
            }
        }

        private void OnValidate()
        {
            if (_formationSlots == null)
                _formationSlots = new FleetFormationSlotView[PlayerFleetLoadout.SlotCount];
            else if (_formationSlots.Length != PlayerFleetLoadout.SlotCount)
                Array.Resize(ref _formationSlots, PlayerFleetLoadout.SlotCount);
        }

        public void RefreshInventory(
            IReadOnlyList<EscortOwnershipSnapshot> ownership,
            EscortShipData selectedEscort)
        {
            foreach (KeyValuePair<EscortShipData, EscortInventoryItemView> pair in _inventoryItems)
            {
                if (pair.Value != null)
                    pair.Value.gameObject.SetActive(false);
            }

            if (ownership == null)
                return;

            for (int i = 0; i < ownership.Count; i++)
            {
                EscortOwnershipSnapshot snapshot = ownership[i];

                if (snapshot.ShipData == null)
                    continue;

                EscortInventoryItemView itemView = GetOrCreateInventoryItem(snapshot.ShipData);

                if (itemView == null)
                    return;

                itemView.gameObject.SetActive(true);
                itemView.Refresh(snapshot, snapshot.ShipData == selectedEscort);
            }
        }

        public void RefreshFormation(IReadOnlyList<EscortShipData> formation)
        {
            if (_formationSlots == null)
                return;

            for (int i = 0; i < _formationSlots.Length; i++)
            {
                EscortShipData shipData = formation != null && i < formation.Count
                    ? formation[i]
                    : null;
                _formationSlots[i]?.Refresh(shipData);
            }
        }

        public void SetSelectedEscort(EscortShipData shipData)
        {
            if (_selectedEscortText != null)
                _selectedEscortText.text = shipData != null ? shipData.DisplayName : string.Empty;
        }

        public void SetSummonCost(CurrencyType currencyType, int cost)
        {
            if (_summonCostText != null)
                _summonCostText.text = $"{cost} {currencyType}";
        }

        public void SetSummonInteractable(bool interactable)
        {
            if (_summonButton != null)
                _summonButton.interactable = interactable;
        }

        public void SetMergeInteractable(bool interactable)
        {
            if (_mergeButton != null)
                _mergeButton.interactable = interactable;
        }

        public void SetResult(string message)
        {
            if (_resultText != null)
                _resultText.text = message;
        }

        private EscortInventoryItemView GetOrCreateInventoryItem(EscortShipData shipData)
        {
            if (_inventoryItems.TryGetValue(shipData, out EscortInventoryItemView itemView))
                return itemView;

            if (_inventoryRoot == null || _inventoryItemPrefab == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerFleetPanelView)} needs an inventory root and item prefab.",
                    this);
                return null;
            }

            itemView = Instantiate(_inventoryItemPrefab, _inventoryRoot);
            itemView.OnSelected += HandleEscortSelected;
            _inventoryItems.Add(shipData, itemView);
            return itemView;
        }

        private void SubscribeFormationSlots()
        {
            if (_formationSlots == null)
                return;

            for (int i = 0; i < _formationSlots.Length; i++)
            {
                FleetFormationSlotView slotView = _formationSlots[i];

                if (slotView == null)
                    continue;

                slotView.Initialize(i);
                slotView.OnSlotClicked += HandleFormationSlotClicked;
                slotView.OnRemoveClicked += HandleFormationRemoveClicked;
            }
        }

        private void UnsubscribeFormationSlots()
        {
            if (_formationSlots == null)
                return;

            for (int i = 0; i < _formationSlots.Length; i++)
            {
                FleetFormationSlotView slotView = _formationSlots[i];

                if (slotView == null)
                    continue;

                slotView.OnSlotClicked -= HandleFormationSlotClicked;
                slotView.OnRemoveClicked -= HandleFormationRemoveClicked;
            }
        }

        private void HandleEscortSelected(EscortShipData shipData)
        {
            OnEscortSelected?.Invoke(shipData);
        }

        private void HandleFormationSlotClicked(int slotIndex)
        {
            OnFormationSlotClicked?.Invoke(slotIndex);
        }

        private void HandleFormationRemoveClicked(int slotIndex)
        {
            OnFormationRemoveClicked?.Invoke(slotIndex);
        }

        private void HandleSummonClicked()
        {
            OnSummonClicked?.Invoke();
        }

        private void HandleMergeClicked()
        {
            OnMergeClicked?.Invoke();
        }
    }
}
