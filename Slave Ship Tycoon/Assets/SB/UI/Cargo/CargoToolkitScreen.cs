using System.Collections.Generic;
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
        [SerializeField] private VisualTreeAsset cardTemplate;
        [Header("Currency icons (same sprites as the HUD)")]
        [SerializeField] private Sprite goldIcon;
        [SerializeField] private Sprite diamondIcon;
        [SerializeField] private Sprite emeraldIcon;
        [Header("Load button hold (unscaled seconds)")]
        [SerializeField, Min(0)] private float repeatStartDelaySeconds = 0.35f;
        [SerializeField, Min(0.01f)] private float repeatIntervalSeconds = 0.1f;
        [SerializeField, Min(0)] private float dragCancelDistance = 12f;
        [Header("Expected revenue feedback (unscaled seconds)")]
        [SerializeField, Min(0.01f)] private float rewardCountDurationSeconds = 0.5f;
        [SerializeField, Min(0.01f)] private float rewardDeltaDurationSeconds = 0.85f;
        [SerializeField, Min(0)] private float rewardDeltaRisePixels = 10f;

        private sealed class CargoCard
        {
            public TransportItemSO Item;
            public VisualElement Root;
            public Label Amount;
            public Button LoadButton;
            public Button UnloadButton;
        }
        private sealed class RewardPreview
        {
            public CurrencyType currency;
            public VisualElement container;
            public Label value;
            public Label delta;
            public long displayed;
            public long from;
            public long target;
            public float countElapsed;
            public float feedbackElapsed;
            public bool isCounting;
            public bool hasFeedback;
        }
        private readonly List<RewardPreview> rewardPreviews = new List<RewardPreview>();
        private bool hasRewardPreview;
        private readonly List<CargoCard> cards = new List<CargoCard>();
        private readonly List<ToolkitHoldRepeatInput> holds = new List<ToolkitHoldRepeatInput>();
        private VisualElement root;
        private float previousTimeScale;
        private bool hasPaused;
        private bool isRefreshPending;

        public bool IsOpen => gameObject.activeSelf;

        private void OnEnable()
        {
            if (document == null || cargoManager == null || gameFlowManager == null || transportEquipmentManager == null ||
                cardTemplate == null)
            { Debug.LogError("CargoToolkitScreen의 Document, Manager, Card Template을 연결하세요.", this); return; }
            root = document.rootVisualElement; root.AddToClassList("harbor-document"); root.pickingMode = PickingMode.Ignore;
            previousTimeScale = Time.timeScale; Time.timeScale = 0; hasPaused = true;
            root.Q<Button>("closeButton").clicked += Close;
            root.Q<Button>("confirmButton").clicked += Confirm;
            BindRewardPreviews(); BuildCards();
            Refresh();
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += OnCargoChanged;
        }
        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= OnCargoChanged;
            foreach (var hold in holds) hold.Dispose();
            holds.Clear(); cards.Clear(); isRefreshPending = false;
            rewardPreviews.Clear(); hasRewardPreview = false;
            if (root != null)
            {
                if (root.Q<Button>("closeButton") is Button closeButtonBinding) closeButtonBinding.clicked -= Close;
                if (root.Q<Button>("confirmButton") is Button confirmButtonBinding) confirmButtonBinding.clicked -= Confirm;
            }
            if (hasPaused) { Time.timeScale = previousTimeScale; hasPaused = false; }
            root = null;
        }
        private void LateUpdate()
        {
            if (isRefreshPending) { isRefreshPending = false; Refresh(); }
            AnimateRewardPreviews();
        }
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
        private void OnCargoChanged(ChangedCurrentCargoCapacityEvent evt) => isRefreshPending = true;

        private void BuildCards()
        {
            VisualElement container = root.Q("availableGrid");
            container.Clear();
            foreach (CargoData cargo in cargoManager.CargoData)
            {
                if (cargo == null || cargo.Item == null) continue;
                CreateCard(cargo.Item, container);
            }
        }
        private void CreateCard(TransportItemSO item, VisualElement container)
        {
            TemplateContainer instance = cardTemplate.Instantiate();
            VisualElement card = instance.Q(className: "cargo-card");
            card.RemoveFromHierarchy();
            card.EnableInClassList("is-first-cargo-card", container.childCount == 0);
            container.Add(card);
            ShipCardPresentation.SetIcon(card.Q("icon"), item.Icon);
            card.Q<Label>("cargoName").text = item.DisplayName;
            card.Q<Label>("weight").text = $"1개당 {item.BaseWeight:0.#} t";
            VisualElement rewards = card.Q("rewards");
            if (item.Rewards != null)
                foreach (var reward in item.Rewards)
                {
                    if (reward.CurrencyAmount <= 0) continue;
                    var chip = new VisualElement
                    {
                        tooltip = $"화물 1개당 {CurrencyTextFormatter.FormatName(reward.CurrencyType)} {reward.CurrencyAmount:N0}"
                    };
                    chip.AddToClassList("cargo-reward-chip");
                    AddCurrencyIcon(chip, reward.CurrencyType, "cargo-reward-icon");
                    var value = new Label(FormatAmount(reward.CurrencyAmount));
                    value.AddToClassList("cargo-reward-value");
                    chip.Add(value);
                    rewards.Add(chip);
                }
            Button load = card.Q<Button>("loadButton");
            Button unload = card.Q<Button>("unloadButton");
            load.tooltip = "1개 적재 · 길게 누르면 연속 적재";
            unload.tooltip = "1개 비우기 · 길게 누르면 연속으로 비우기";
            holds.Add(new ToolkitHoldRepeatInput(load, () => cargoManager.LoadCargo(item.Type), true,
                repeatStartDelaySeconds, repeatIntervalSeconds, dragCancelDistance));
            holds.Add(new ToolkitHoldRepeatInput(unload, () => cargoManager.UnloadCargo(item.Type), true,
                repeatStartDelaySeconds, repeatIntervalSeconds, dragCancelDistance));
            cards.Add(new CargoCard
            {
                Item = item, Root = card, Amount = card.Q<Label>("amount"),
                LoadButton = load, UnloadButton = unload
            });
        }
        private void Refresh()
        {
            if (root == null) return;
            foreach (CargoCard card in cards)
            {
                int amount = cargoManager.GetCargoAmount(card.Item.Type);
                card.Amount.text = amount.ToString("N0");
                card.Root.EnableInClassList("is-empty", amount <= 0);
                card.LoadButton.SetEnabled(cargoManager.CanLoadCargo(card.Item.Type));
                card.UnloadButton.SetEnabled(amount > 0);
            }
            float loaded = cargoManager.GetCurrentCargoCapacity();
            float capacity = cargoManager.GetStatCargoCapacity();
            root.Q<Label>("capacityText").text = $"{loaded:0.#} / {capacity:0.#} t";
            root.Q("capacityFill").style.width = Length.Percent(capacity > 0 ? Mathf.Clamp01(loaded / capacity) * 100 : 0);
            root.Q("capacityFill").EnableInClassList("is-overloaded", !cargoManager.IsLoadoutWithinCapacity);
            root.Q<Label>("status").text = !cargoManager.HasValidLoadout ? "적재 중량 또는 화물 설정을 확인하세요." :
                capacity <= 0 ? "적재함을 강화해 적재 공간을 확보하세요." :
                loaded <= 0 ? "화물을 적재해 보상을 준비하세요." : $"남은 적재 공간 {cargoManager.GetRemainingCapacity():0.#} t";
            root.Q<Button>("confirmButton").SetEnabled(cargoManager.HasValidLoadout && gameFlowManager.CanStartBossBattle);
            RefreshPreview();
        }
        private Sprite GetCurrencyIcon(CurrencyType currency)
        {
            switch (currency)
            {
                case CurrencyType.Gold: return goldIcon;
                case CurrencyType.Diamond: return diamondIcon;
                case CurrencyType.Emerald: return emeraldIcon;
                default: return null;
            }
        }
        private void AddCurrencyIcon(VisualElement container, CurrencyType currency, string className)
        {
            Sprite sprite = GetCurrencyIcon(currency);
            if (sprite != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList(className);
                ShipCardPresentation.SetIcon(icon, sprite);
                container.Add(icon);
            }
            else
            {
                var fallback = new Label(CurrencyTextFormatter.FormatName(currency));
                fallback.AddToClassList("cargo-currency-fallback");
                container.Add(fallback);
            }
        }
        private static string FormatAmount(long amount) =>
            amount >= 1000000 || amount <= -1000000 ? CurrencyTextFormatter.Format(amount) : amount.ToString("N0");

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
            foreach (RewardPreview preview in rewardPreviews)
            {
                long next = rewards[preview.currency];
                preview.container.tooltip = $"{CurrencyTextFormatter.FormatName(preview.currency)} {next:N0}";
                if (!hasRewardPreview)
                {
                    preview.displayed = preview.from = preview.target = next;
                    preview.value.text = FormatAmount(next);
                    continue;
                }
                if (next == preview.target) continue;
                decimal change = (decimal)next - preview.target;
                preview.from = preview.displayed;
                preview.target = next;
                preview.countElapsed = preview.feedbackElapsed = 0;
                preview.isCounting = preview.hasFeedback = true;
                preview.container.EnableInClassList("is-gaining", change > 0);
                preview.container.EnableInClassList("is-decreasing", change < 0);
                preview.delta.text = change > 0 ? "+" + FormatAmount((long)change) : FormatAmount((long)change);
                preview.delta.style.opacity = 1;
                preview.delta.style.translate = new Translate(0, 0, 0);
            }
            hasRewardPreview = true;
        }
        private void BindRewardPreviews()
        {
            rewardPreviews.Clear(); hasRewardPreview = false;
            BindRewardPreview(CurrencyType.Gold, "goldPreview");
            BindRewardPreview(CurrencyType.Diamond, "diamondPreview");
            BindRewardPreview(CurrencyType.Emerald, "emeraldPreview");
        }
        private void BindRewardPreview(CurrencyType currency, string elementName)
        {
            VisualElement container = root.Q(elementName);
            VisualElement icon = container.Q("icon");
            Sprite sprite = GetCurrencyIcon(currency);
            ShipCardPresentation.SetIcon(icon, sprite);
            icon.style.display = sprite != null ? DisplayStyle.Flex : DisplayStyle.None;
            container.Q("currencyFallback")?.RemoveFromHierarchy();
            if (sprite == null)
            {
                var fallback = new Label(CurrencyTextFormatter.FormatName(currency)) { name = "currencyFallback" };
                fallback.AddToClassList("cargo-currency-fallback");
                container.Insert(0, fallback);
            }
            Label delta = container.Q<Label>("delta");
            container.RemoveFromClassList("is-gaining");
            container.RemoveFromClassList("is-decreasing");
            delta.text = "";
            delta.style.opacity = 0;
            delta.style.translate = new Translate(0, 0, 0);
            rewardPreviews.Add(new RewardPreview
            {
                currency = currency, container = container,
                value = container.Q<Label>("value"), delta = delta
            });
        }
        private void AnimateRewardPreviews()
        {
            foreach (RewardPreview preview in rewardPreviews)
            {
                if (preview.isCounting)
                {
                    preview.countElapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(preview.countElapsed / Mathf.Max(0.01f, rewardCountDurationSeconds));
                    float eased = 1 - Mathf.Pow(1 - progress, 3);
                    long next = progress >= 1 ? preview.target :
                        (long)((decimal)preview.from + ((decimal)preview.target - preview.from) * (decimal)eased);
                    if (next != preview.displayed)
                    {
                        preview.displayed = next;
                        preview.value.text = FormatAmount(next);
                    }
                    preview.isCounting = progress < 1;
                }
                if (!preview.hasFeedback) continue;
                preview.feedbackElapsed += Time.unscaledDeltaTime;
                float feedback = Mathf.Clamp01(preview.feedbackElapsed / Mathf.Max(0.01f, rewardDeltaDurationSeconds));
                preview.delta.style.translate = new Translate(0, -rewardDeltaRisePixels * feedback, 0);
                preview.delta.style.opacity = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.4f, 1, feedback));
                if (feedback < 1) continue;
                preview.hasFeedback = false;
                preview.container.RemoveFromClassList("is-gaining");
                preview.container.RemoveFromClassList("is-decreasing");
            }
        }
    }
}
