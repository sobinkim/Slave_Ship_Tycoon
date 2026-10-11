using System.Collections.Generic;
using System.Text;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.Currency;
using SB.Scripts.TransportEquipment;
using SB.SO.TransportItemSOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.Cargo
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class CargoToolkitScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument document;
        [SerializeField] private CargoManager cargoManager;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private TransportEquipmentManager transportEquipmentManager;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private VisualTreeAsset cardTemplate;
        [Header("Layout")]
        [SerializeField, Min(1)] private int minimumSelectedSlots = 12;
        [Header("Load button hold (unscaled seconds)")]
        [SerializeField, Min(0)] private float repeatStartDelaySeconds = 0.35f;
        [SerializeField, Min(0.01f)] private float repeatIntervalSeconds = 0.1f;
        [SerializeField, Min(0)] private float dragCancelDistance = 12f;

        private sealed class CargoCard
        {
            public TransportItemSO item;
            public VisualElement root;
            public bool isDetail;
            public bool isPicker;
        }
        private sealed class CargoSlot
        {
            public TransportItemSO item;
            public Button button;
            public Label amount;
        }
        private TransportItemSO detailItem;
        private readonly List<CargoSlot> selectedSlots = new List<CargoSlot>();
        private readonly List<CargoCard> cards = new List<CargoCard>();
        private readonly List<Button> emptySlots = new List<Button>();
        private readonly List<ToolkitHoldRepeatInput> holds = new List<ToolkitHoldRepeatInput>();
        private VisualElement root;
        private float previousTimeScale;
        private bool hasPaused;
        private bool isRefreshPending;

        public bool IsOpen => gameObject.activeSelf;

        private void OnEnable()
        {
            if (document == null || cargoManager == null || gameFlowManager == null || transportEquipmentManager == null ||
                currencyManager == null || cardTemplate == null)
            { Debug.LogError("CargoToolkitScreen의 Document, Manager, Card Template을 연결하세요.", this); return; }
            root = document.rootVisualElement; root.AddToClassList("harbor-document"); root.pickingMode = PickingMode.Ignore;
            previousTimeScale = Time.timeScale; Time.timeScale = 0; hasPaused = true;
            root.Q<Button>("closeButton").clicked += Close;
            root.Q<Button>("pickerCloseButton").clicked += ClosePicker;
            root.Q<Button>("detailCloseButton").clicked += CloseDetail;
            root.Q<Button>("confirmButton").clicked += Confirm;
            BuildCards();
            ClosePicker(); CloseDetail(); Refresh();
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += OnCargoChanged;
            Bus<CurrencyChangedEvent>.OnEvent += OnCurrencyChanged;
        }
        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= OnCargoChanged;
            Bus<CurrencyChangedEvent>.OnEvent -= OnCurrencyChanged;
            foreach (var hold in holds) hold.Dispose();
            holds.Clear(); cards.Clear(); emptySlots.Clear(); selectedSlots.Clear(); detailItem = null; isRefreshPending = false;
            if (root != null)
            {
                if (root.Q<Button>("closeButton") is Button closeButtonBinding) closeButtonBinding.clicked -= Close;
                if (root.Q<Button>("pickerCloseButton") is Button pickerCloseButtonBinding) pickerCloseButtonBinding.clicked -= ClosePicker;
                if (root.Q<Button>("detailCloseButton") is Button detailCloseButtonBinding) detailCloseButtonBinding.clicked -= CloseDetail;
                if (root.Q<Button>("confirmButton") is Button confirmButtonBinding) confirmButtonBinding.clicked -= Confirm;
            }
            if (hasPaused) { Time.timeScale = previousTimeScale; hasPaused = false; }
            root = null;
        }
        private void LateUpdate() { if (isRefreshPending) { isRefreshPending = false; Refresh(); } }
        public void Open()
        {
            if (IsOpen || gameFlowManager == null || !gameFlowManager.CanStartBossBattle) return;
            gameObject.SetActive(true);
        }
        public void Close() => gameObject.SetActive(false);
        private void Confirm()
        {
            if (!cargoManager.HasValidLoadout || !gameFlowManager.CanStartBossBattle) return;
            Close(); gameFlowManager.StartBossBattle();
        }
        private void OpenPicker() { CloseDetail(); root.Q("cargoPicker").RemoveFromClassList("hidden"); }
        private void ClosePicker() => root.Q("cargoPicker").AddToClassList("hidden");
        private void OpenDetail(TransportItemSO item)
        {
            foreach (var hold in holds) hold.Cancel();
            ClosePicker(); detailItem = item;
            root.Q("cargoDetail").RemoveFromClassList("hidden"); Refresh();
        }
        private void CloseDetail()
        {
            foreach (var hold in holds) hold.Cancel();
            detailItem = null;
            root.Q("cargoDetail").AddToClassList("hidden");
        }
        private void OnCargoChanged(ChangedCurrentCargoCapacityEvent evt) => isRefreshPending = true;
        private void OnCurrencyChanged(CurrencyChangedEvent evt) => RefreshBalances();

        private void BuildCards()
        {
            root.Q("selectedGrid").Clear(); root.Q("availableGrid").Clear(); root.Q("pickerGrid").Clear(); root.Q("detailGrid").Clear(); selectedSlots.Clear();
            foreach (CargoData cargo in cargoManager.CargoData)
            {
                if (cargo == null || cargo.Item == null) continue;
                CreateSlot(cargo.Item);
                CreateCard(cargo.Item, root.Q("detailGrid"), true, false);
                CreateCard(cargo.Item, root.Q("availableGrid"), false, false);
                CreateCard(cargo.Item, root.Q("pickerGrid"), false, true);
            }
            for (int i = 0; i < minimumSelectedSlots; i++)
            {
                var empty = new Button(OpenPicker) { text = "+" };
                empty.AddToClassList("harbor-button"); empty.AddToClassList("cargo-empty");
                root.Q("selectedGrid").Add(empty); emptySlots.Add(empty);
            }
        }
        private void CreateSlot(TransportItemSO item)
        {
            var slot = new Button(() => OpenDetail(item)) { tooltip = item.DisplayName + " · 수량 조절" };
            slot.AddToClassList("harbor-button"); slot.AddToClassList("cargo-slot");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore }; icon.AddToClassList("cargo-slot-icon");
            ShipCardPresentation.SetIcon(icon, item.Icon); slot.Add(icon);
            var name = new Label(item.DisplayName) { pickingMode = PickingMode.Ignore }; name.AddToClassList("cargo-slot-name"); slot.Add(name);
            var amount = new Label { pickingMode = PickingMode.Ignore }; amount.AddToClassList("cargo-slot-amount"); slot.Add(amount);
            slot.style.borderLeftColor = item.AccentColor; slot.style.borderRightColor = item.AccentColor;
            slot.style.borderTopColor = item.AccentColor; slot.style.borderBottomColor = item.AccentColor;
            root.Q("selectedGrid").Add(slot);
            selectedSlots.Add(new CargoSlot { item = item, button = slot, amount = amount });
        }
        private void CreateCard(TransportItemSO item, VisualElement container, bool isDetail, bool isPicker)
        {
            TemplateContainer instance = cardTemplate.Instantiate();
            VisualElement card = instance.Q(className: "cargo-card");
            card.RemoveFromHierarchy(); container.Add(card);
            card.EnableInClassList("is-detail-card", isDetail);
            cards.Add(new CargoCard { item = item, root = card, isDetail = isDetail, isPicker = isPicker });
            ShipCardPresentation.SetIcon(card.Q("icon"), item.Icon);
            card.Q<Label>("cargoName").text = item.DisplayName;
            card.style.borderLeftColor = item.AccentColor; card.style.borderRightColor = item.AccentColor;
            card.style.borderTopColor = item.AccentColor; card.style.borderBottomColor = item.AccentColor;
            Button load = card.Q<Button>("loadButton");
            if (isPicker)
            {
                load.clicked += () => { if (cargoManager.LoadCargo(item.Type)) { ClosePicker(); Refresh(); } };
                load.text = "선택 · 1개 적재";
            }
            else holds.Add(new ToolkitHoldRepeatInput(load, () => cargoManager.LoadCargo(item.Type), true, repeatStartDelaySeconds, repeatIntervalSeconds, dragCancelDistance));
            card.Q<Button>("unloadButton").clicked += () => cargoManager.UnloadCargo(item.Type);
            card.Q<Button>("removeButton").clicked += () => cargoManager.RemoveCargo(item.Type);
            card.Q<Button>("unloadButton").EnableInClassList("hidden", !isDetail);
            card.Q<Button>("removeButton").EnableInClassList("hidden", !isDetail);
        }
        private void Refresh()
        {
            if (root == null) return;
            int selectedCount = 0;
            foreach (CargoSlot slot in selectedSlots)
            {
                int amount = cargoManager.GetCargoAmount(slot.item.Type);
                slot.button.EnableInClassList("hidden", amount <= 0);
                slot.amount.text = $"×{amount:N0}";
                if (amount > 0) selectedCount++;
            }
            foreach (CargoCard card in cards)
            {
                int amount = cargoManager.GetCargoAmount(card.item.Type);
                bool isVisible = !card.isDetail || card.item == detailItem;
                card.root.EnableInClassList("hidden", !isVisible);
                card.root.Q<Label>("amount").text = card.isDetail ? $"x{amount:N0}" : "보유 ∞";
                float weight = card.item.BaseWeight * (card.isDetail ? amount : 1);
                card.root.Q<Label>("weight").text = $"{weight:0.#} t";
                card.root.Q<Label>("rewards").text = DescribeRewards(card.item, card.isDetail ? amount : 1);
                card.root.Q<Button>("loadButton").SetEnabled(cargoManager.CanLoadCargo(card.item.Type));
                card.root.Q<Button>("unloadButton").SetEnabled(amount > 0);
                card.root.Q<Button>("removeButton").SetEnabled(amount != 0);
            }
            for (int i = 0; i < emptySlots.Count; i++) emptySlots[i].EnableInClassList("hidden", i >= Mathf.Max(0, minimumSelectedSlots - selectedCount));
            float loaded = cargoManager.GetCurrentCargoCapacity();
            float capacity = cargoManager.GetStatCargoCapacity();
            root.Q<Label>("capacityText").text = $"{loaded:0.#} / {capacity:0.#} t";
            root.Q("capacityFill").style.width = Length.Percent(capacity > 0 ? Mathf.Clamp01(loaded / capacity) * 100 : 0);
            root.Q("capacityFill").EnableInClassList("is-overloaded", !cargoManager.IsLoadoutWithinCapacity);
            root.Q<Label>("catalogHint").text = $"최대 적재량 {capacity:0.#} t · 적재 버튼을 길게 누르면 연속 적재";
            root.Q<Label>("status").text = cargoManager.HasValidLoadout ? "" : "적재량 또는 화물 설정을 확인하세요. 화물을 줄이거나 적재함을 강화하세요.";
            root.Q<Button>("confirmButton").SetEnabled(cargoManager.HasValidLoadout && gameFlowManager.CanStartBossBattle);
            RefreshBalances(); RefreshPreview();
        }
        private void RefreshBalances()
        {
            if (root == null) return;
            root.Q<Label>("balances").text = $"골드 {CurrencyTextFormatter.Format(currencyManager.GetCurrency(CurrencyType.Gold))}    다이아 {CurrencyTextFormatter.Format(currencyManager.GetCurrency(CurrencyType.Diamond))}    에메랄드 {CurrencyTextFormatter.Format(currencyManager.GetCurrency(CurrencyType.Emerald))}";
        }
        private static string DescribeRewards(TransportItemSO item, int units)
        {
            var text = new StringBuilder();
            if (item.Rewards != null)
                foreach (var reward in item.Rewards)
                {
                    if (reward.CurrencyAmount <= 0) continue;
                    if (text.Length > 0) text.Append("\n");
                    text.Append($"{CurrencyTextFormatter.FormatName(reward.CurrencyType)} {(long)reward.CurrencyAmount * units:N0}");
                }
            return text.ToString();
        }
        private void RefreshPreview()
        {
            var rewards = new Dictionary<CurrencyType, long> { [CurrencyType.Gold] = 0, [CurrencyType.Diamond] = 0, [CurrencyType.Emerald] = 0 };
            bool hasCargo = false;
            foreach (CargoData cargo in cargoManager.CargoData)
            {
                if (cargo == null || cargo.Item == null || cargo.Amount <= 0) continue;
                hasCargo = true;
                if (cargo.Item.Rewards == null) continue;
                foreach (var reward in cargo.Item.Rewards)
                {
                    if (reward.CurrencyAmount <= 0) continue;
                    long amount = (long)reward.CurrencyAmount * cargo.Amount;
                    rewards.TryGetValue(reward.CurrencyType, out long current);
                    rewards[reward.CurrencyType] = amount > long.MaxValue - current ? long.MaxValue : current + amount;
                }
            }
            if (hasCargo) transportEquipmentManager.ApplyPreviewModifiers(rewards);
            root.Q<Label>("rewardPreview").text = $"골드 {rewards[CurrencyType.Gold]:N0}    다이아 {rewards[CurrencyType.Diamond]:N0}    에메랄드 {rewards[CurrencyType.Emerald]:N0}";
        }
    }
}