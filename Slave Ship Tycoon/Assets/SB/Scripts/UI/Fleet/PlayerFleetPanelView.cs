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
        [SerializeField] private ScrollRect _inventoryScroll;

        [Header("Formation")]
        [SerializeField] private FleetFormationSlotView[] _formationSlots =
            new FleetFormationSlotView[PlayerFleetLoadout.SlotCount];

        [Header("Commands")]
        [SerializeField] private Button _summonButton;
        [SerializeField] private Button _mergeButton;
        [SerializeField] private TMP_Text _summonCostText;
        [SerializeField] private TMP_Text _selectedEscortText;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private TMP_Text _mergeStatusText;

        [Header("Merge Preview")]
        [SerializeField] private GameObject _mergePreviewPanel;
        [SerializeField] private TMP_Text _mergePreviewText;
        [SerializeField] private Button _confirmMergeButton;
        [SerializeField] private Button _cancelMergeButton;

        private readonly Dictionary<EscortShipData, EscortInventoryItemView> _inventoryItems =
            new Dictionary<EscortShipData, EscortInventoryItemView>();

        public event Action<EscortShipData> OnEscortSelected;
        public event Action<int> OnFormationSlotClicked;
        public event Action<int> OnFormationRemoveClicked;
        public event Action OnSummonClicked;
        public event Action OnMergeClicked;
        public event Action OnMergeConfirmed;
        public event Action OnMergeCancelled;

        private void Awake()
        {
            if (_summonButton != null)
                _summonButton.onClick.AddListener(HandleSummonClicked);

            if (_mergeButton != null)
                _mergeButton.onClick.AddListener(HandleMergeClicked);

            if (_confirmMergeButton != null)
                _confirmMergeButton.onClick.AddListener(HandleMergeConfirmed);

            if (_cancelMergeButton != null)
                _cancelMergeButton.onClick.AddListener(HandleMergeCancelled);

            HideMergePreview();

            SubscribeFormationSlots();
        }

        private void OnDestroy()
        {
            if (_summonButton != null)
                _summonButton.onClick.RemoveListener(HandleSummonClicked);

            if (_mergeButton != null)
                _mergeButton.onClick.RemoveListener(HandleMergeClicked);

            if (_confirmMergeButton != null)
                _confirmMergeButton.onClick.RemoveListener(HandleMergeConfirmed);

            if (_cancelMergeButton != null)
                _cancelMergeButton.onClick.RemoveListener(HandleMergeCancelled);

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
            if (ownership == null)
                return;

            HashSet<EscortShipData> visibleItems = new HashSet<EscortShipData>();

            for (int i = 0; i < ownership.Count; i++)
            {
                EscortOwnershipSnapshot snapshot = ownership[i];

                if (snapshot.ShipData == null)
                    continue;

                EscortInventoryItemView itemView = GetOrCreateInventoryItem(snapshot.ShipData);

                if (itemView == null)
                    return;

                itemView.gameObject.SetActive(true);
                itemView.transform.SetSiblingIndex(i);
                itemView.Refresh(snapshot, snapshot.ShipData == selectedEscort);
                visibleItems.Add(snapshot.ShipData);
            }

            foreach (KeyValuePair<EscortShipData, EscortInventoryItemView> pair in _inventoryItems)
            {
                if (pair.Value != null && visibleItems.Contains(pair.Key) == false)
                    pair.Value.gameObject.SetActive(false);
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
                _selectedEscortText.text = shipData != null ? shipData.DisplayName : "선택한 호위선 없음";
        }

        public void SetFormationSelection(int selectedSlotIndex, EscortShipData selectedEscort, bool hasAvailableCopy)
        {
            for (int i = 0; i < _formationSlots.Length; i++)
                _formationSlots[i]?.SetSelection(i == selectedSlotIndex, selectedEscort != null && hasAvailableCopy);
        }

        public void FocusEscort(EscortShipData shipData)
        {
            if (_inventoryScroll == null || _inventoryScroll.content == null ||
                _inventoryScroll.viewport == null || _inventoryItems.TryGetValue(shipData, out var item) == false)
                return;

            Canvas.ForceUpdateCanvases();
            RectTransform viewport = _inventoryScroll.viewport;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item.transform);
            float offset = bounds.max.y > viewport.rect.yMax
                ? viewport.rect.yMax - bounds.max.y
                : bounds.min.y < viewport.rect.yMin ? viewport.rect.yMin - bounds.min.y : 0f;
            _inventoryScroll.StopMovement();
            _inventoryScroll.content.anchoredPosition += new Vector2(0f, offset);
        }

        public void SetSummonCost(CurrencyType currencyType, int cost)
        {
            if (_summonCostText != null)
                _summonCostText.text = $"{CurrencyTextFormatter.Format(cost)} {CurrencyTextFormatter.FormatName(currencyType)}";
        }

        public void SetSummonStatus(CurrencyType currencyType, int cost, string message)
        {
            SetSummonCost(currencyType, cost);

            if (_summonCostText != null && string.IsNullOrEmpty(message) == false)
                _summonCostText.text += $"\n{message}";
        }

        public void SetMergeStatus(string message)
        {
            if (_mergeStatusText != null)
                _mergeStatusText.text = message;
        }

        public void ShowMergePreview(string message)
        {
            if (_mergePreviewText != null)
                _mergePreviewText.text = message;

            if (_mergePreviewPanel != null)
            {
                _mergePreviewPanel.transform.SetAsLastSibling();
                _mergePreviewPanel.SetActive(true);
            }
        }

        public void HideMergePreview()
        {
            if (_mergePreviewPanel != null)
                _mergePreviewPanel.SetActive(false);
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

        private void HandleMergeConfirmed()
        {
            OnMergeConfirmed?.Invoke();
        }

        private void HandleMergeCancelled()
        {
            OnMergeCancelled?.Invoke();
        }
    }
}
