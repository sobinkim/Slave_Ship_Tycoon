using System.Collections;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.Currency;
using SB.Scripts.TransportEquipment;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.InGame
{
    public sealed partial class InGameToolkitScreen
    {
        [Header("Transport bonus references")]
        [SerializeField] private MarketPriceManager marketPriceManager;
        [SerializeField] private TransportEquipmentManager transportEquipmentManager;
        [Header("Transport goal and result feedback")]
        [SerializeField, Tooltip("출항 장비의 보상 통화가 하나면 그 통화를 목표로 표시합니다. 장비 미장착·효과 없음·여러 통화 장비에서는 이 기본 목표를 사용합니다. 실제 보상이나 적재 설정은 바꾸지 않습니다.")]
        private CurrencyType transportGoalCurrency = CurrencyType.Gold;
        [SerializeField, Min(0.1f), Tooltip("전체 화물의 일반 시세 결과를 동시에 공개하는 총시간입니다. Unscaled 초 기준입니다.")]
        private float transportResultSeconds = 1.6f;
        [SerializeField, Min(0.1f), Tooltip("목표 재화가 크게 오른 특별 결과의 총시간입니다. Unscaled 초 기준입니다.")]
        private float transportSpecialResultSeconds = 2.2f;
        [SerializeField, Min(0.1f), Tooltip("상승 결과를 가린 채 확대·응축하는 시간입니다. 일반/특별 결과에 동일하게 적용해 공개 전에 결과를 예고하지 않습니다. Unscaled 초 기준입니다.")]
        private float transportAnticipationSeconds = 0.6f;
        [SerializeField, Min(0f), Tooltip("응축을 멈춘 뒤 배율을 확정 공개하기 전의 짧은 정지 시간입니다. 전투 시간 배율은 바꾸지 않습니다.")]
        private float transportRevealPauseSeconds = 0.1f;
        [SerializeField, Min(0.01f), Tooltip("이번 구간 목표 재화 증가액이 화물 기본가치의 이 배수 이상이면 특별 연출 후보가 됩니다. 확률과 보상은 바꾸지 않습니다.")]
        private float transportSpecialGainBaseMultiple = 30f;
        [SerializeField, Min(0), Tooltip("한 번의 운송에서 크게 강조할 특별 시세 결과의 최대 횟수입니다. 0이면 일반 연출만 사용합니다.")]
        private int transportSpecialResultsPerVoyage = 1;
        [Header("Transport effects (unscaled seconds)")]
        [SerializeField, Min(0.01f)] private float cargoResolveSeconds = 0.28f;
        [SerializeField, Min(0.01f)] private float currencyFlightSeconds = 0.24f;
        [SerializeField, Min(0.01f)] private float transportCountSeconds = 0.36f;
        [SerializeField, Range(0f, 2f)] private float cargoImpactStrength = 1f;
        [SerializeField, Range(1, 8)] private int currencyStreamCount = 3;
        [SerializeField, Range(1, 8)] private int bonusCurrencyStreamCount = 5;
        [SerializeField, Min(0.01f)] private float currencyStreamIntervalSeconds = 0.045f;
        [SerializeField, Min(0.01f)] private float rewardImpactSeconds = 0.22f;
        [Header("Lucky upgrade sparkle")]
        [SerializeField, Min(0.1f)] private float bonusSparkleSeconds = 1.05f;
        [SerializeField, Range(3, 10)] private int bonusSparkleCount = 7;

        private sealed class TransportItemView
        {
            public CargoData cargo;
            public VisualElement cell, anchor, icon, halo, ring;
            public Label multiplier, pop;
            public readonly List<VisualElement> twinkles = new List<VisualElement>();
            public int appliedMultiplier = 1;
        }
        private sealed class TransportRewardView
        {
            public CurrencyType currency;
            public Sprite sprite;
            public VisualElement container, icon, impact;
            public Label value, delta;
            public long displayed, from, target, baseline;
            public float countElapsed, impactElapsed;
        }
        private sealed class CargoReveal
        {
            public TransportItemView view;
            public int before, target, streamCount;
        }
        private sealed class RevealMoneyView
        {
            public CurrencyType currency;
            public VisualElement row;
            public Label before, after, delta;
        }
        private sealed class CurrencyFlight
        {
            public VisualElement icon;
            public TransportRewardView reward;
            public Vector2 from, to;
            public float startedAt, bend;
        }
        private sealed class TransportSpark
        {
            public VisualElement element;
            public Vector2 origin, velocity;
            public float startedAt, seconds;
        }
        private sealed class BonusTwinkleElement : VisualElement
        {
            public BonusTwinkleElement()
            {
                pickingMode = PickingMode.Ignore;
                AddToClassList("transport-bonus-twinkle");
                generateVisualContent += DrawTwinkle;
            }

            private void DrawTwinkle(MeshGenerationContext context)
            {
                Vector2 center = contentRect.center;
                float radius = Mathf.Min(contentRect.width, contentRect.height) * 0.5f;
                if (radius < 1) return;
                DrawStar(context.painter2D, center, radius, resolvedStyle.color);
                DrawStar(context.painter2D, center, radius * 0.65f, Color.white);
            }

            private static void DrawStar(Painter2D painter, Vector2 center, float radius, Color color)
            {
                painter.fillColor = color;
                painter.BeginPath();
                painter.MoveTo(center + Vector2.up * radius);
                painter.BezierCurveTo(center + Vector2.up * radius * 0.14f,
                    center + Vector2.right * radius * 0.14f, center + Vector2.right * radius);
                painter.BezierCurveTo(center + Vector2.right * radius * 0.14f,
                    center + Vector2.down * radius * 0.14f, center + Vector2.down * radius);
                painter.BezierCurveTo(center + Vector2.down * radius * 0.14f,
                    center + Vector2.left * radius * 0.14f, center + Vector2.left * radius);
                painter.BezierCurveTo(center + Vector2.left * radius * 0.14f,
                    center + Vector2.up * radius * 0.14f, center + Vector2.up * radius);
                painter.ClosePath();
                painter.Fill();
            }
        }

        private readonly List<TransportItemView> transportItems = new List<TransportItemView>();
        private readonly List<TransportRewardView> transportRewards = new List<TransportRewardView>();
        private readonly Queue<Dictionary<ETransportItemType, TransportMarketPriceEntry>> pendingMarketPrices =
            new Queue<Dictionary<ETransportItemType, TransportMarketPriceEntry>>();
        private readonly List<CurrencyFlight> currencyFlights = new List<CurrencyFlight>();
        private readonly List<TransportSpark> transportSparks = new List<TransportSpark>();
        private readonly List<RevealMoneyView> revealMoneyViews = new List<RevealMoneyView>();
        private VisualElement transportCargoRoot, transportItemsRoot, transportFx;
        private VisualElement transportReveal, transportGoalIcon;
        private Label revealCargoName, revealRate, revealMultiplier;
        private Label transportGoal, transportGoalValue, transportGoalBreakdown, transportGoalHint;
        private Coroutine transportInitializeRoutine, transportRevealRoutine;
        private bool transportInitialized;
        private int transportSpecialResultsPlayed;
        private CurrencyType activeTransportGoalCurrency;

        public bool IsTransportBonusPlaying => transportRevealRoutine != null || pendingMarketPrices.Count > 0;
        public CurrencyType TransportGoalCurrency => transportInitialized ? activeTransportGoalCurrency : transportGoalCurrency;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializePortraitWindow()
        {
            // Previously saved desktop resolutions must not stretch the portrait HUD.
            Resolution desktop = Screen.currentResolution;
            int units = Mathf.Clamp(Mathf.Min((desktop.height - 120) / 16, (desktop.width - 80) / 9), 20, 60);
            Screen.SetResolution(units * 9, units * 16, FullScreenMode.Windowed);
        }
#endif

        private static VisualElement TransportElement(string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            return element;
        }
        private static Label TransportLabel(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }

        private void BindTransportPresentation()
        {
            transportCargoRoot = root.Q("transportCargo");
            transportItemsRoot = root.Q("transportItems");
            transportFx = root.Q("transportFx");
            transportReveal = root.Q("transportReveal");
            revealCargoName = root.Q<Label>("revealCargoName");
            revealRate = root.Q<Label>("revealRate");
            revealMultiplier = root.Q<Label>("revealMultiplier");
            transportGoal = root.Q<Label>("transportGoal");
            transportGoalValue = root.Q<Label>("transportGoalValue");
            transportGoalBreakdown = root.Q<Label>("transportGoalBreakdown");
            transportGoalHint = root.Q<Label>("transportGoalHint");
            transportGoalIcon = root.Q("transportGoalIcon");
            foreach (CurrencySlot slot in currencySlots)
                if (slot.currencyType == TransportGoalCurrency && transportGoalIcon != null) SetCurrencyIcon(transportGoalIcon, slot);
            VisualElement revealMoneyRoot = root.Q("revealMoneyRows");
            revealMoneyRoot.Clear(); revealMoneyViews.Clear();
            VisualElement rewardsRoot = root.Q("transportRewardRows");
            rewardsRoot.Clear();
            transportRewards.Clear();
            var currencies = new HashSet<CurrencyType>();
            foreach (CurrencySlot slot in currencySlots)
            {
                if (!currencies.Add(slot.currencyType)) continue;
                var container = TransportElement("transport-reward");
                container.AddToClassList("currency-" + slot.currencyType.ToString().ToLowerInvariant());
                var icon = TransportElement("currency-icon");
                SetCurrencyIcon(icon, slot);
                var iconAnchor = TransportElement("transport-reward-icon-anchor");
                var impact = TransportElement("transport-reward-impact");
                iconAnchor.Add(impact); iconAnchor.Add(icon);
                var value = TransportLabel("0", "transport-reward-value");
                var delta = TransportLabel("", "transport-reward-delta");
                container.Add(iconAnchor); container.Add(value); container.Add(delta); rewardsRoot.Add(container);
                transportRewards.Add(new TransportRewardView
                {
                    currency = slot.currencyType, sprite = slot.icon, container = container,
                    icon = icon, impact = impact, value = value, delta = delta
                });
                var revealRow = TransportElement("reveal-money-row");
                revealRow.AddToClassList("currency-" + slot.currencyType.ToString().ToLowerInvariant());
                var revealIcon = TransportElement("reveal-money-icon");
                SetCurrencyIcon(revealIcon, slot);
                var before = TransportLabel("", "reveal-money-before");
                var arrow = TransportLabel("→", "reveal-money-arrow");
                var after = TransportLabel("", "reveal-money-after");
                var moneyDelta = TransportLabel("", "reveal-money-delta");
                revealRow.Add(revealIcon); revealRow.Add(before); revealRow.Add(arrow);
                revealRow.Add(after); revealRow.Add(moneyDelta); revealMoneyRoot.Add(revealRow);
                revealMoneyViews.Add(new RevealMoneyView
                { currency = slot.currencyType, row = revealRow, before = before, after = after, delta = moneyDelta });
            }
            ClearTransportPresentation();
        }

        private void ClearTransportPresentation()
        {
            if (transportInitializeRoutine != null) StopCoroutine(transportInitializeRoutine);
            if (transportRevealRoutine != null) StopCoroutine(transportRevealRoutine);
            transportInitializeRoutine = transportRevealRoutine = null;
            transportInitialized = false;
            transportSpecialResultsPlayed = 0;
            pendingMarketPrices.Clear(); currencyFlights.Clear(); transportSparks.Clear(); transportItems.Clear();
            transportItemsRoot?.Clear(); transportFx?.Clear();
            foreach (TransportRewardView reward in transportRewards)
            {
                reward.displayed = reward.from = reward.target = reward.baseline = 0;
                reward.value.text = "0"; reward.delta.text = "";
                reward.delta.style.opacity = 0;
                ResetTransportRewardImpact(reward);
                reward.container.RemoveFromClassList("is-gaining");
                reward.container.RemoveFromClassList("is-decreasing");
            }
            transportCargoRoot?.AddToClassList("hidden");
            transportCargoRoot?.RemoveFromClassList("is-market-reveal");
            transportReveal?.AddToClassList("hidden");
            if (transportGoalValue != null) transportGoalValue.text = "0";
            if (transportGoalValue != null) transportGoalValue.style.scale = new Scale(Vector3.one);
            if (transportGoalIcon != null) transportGoalIcon.style.scale = new Scale(Vector3.one);
            if (transportGoalBreakdown != null) transportGoalBreakdown.text = "";
            if (transportGoalHint != null) transportGoalHint.text = "";
        }

        private void OnObtainStarted(ObtainChapterStartedEvent evt) => ClearTransportPresentation();
        private void OnSellStarted(SellChapterStartedEvent evt) => PrepareTransportPresentation();

        private void PrepareTransportPresentation()
        {
            ClearTransportPresentation();
            foreach (CargoData cargo in cargoManager.TransportCargoData)
            {
                if (cargo == null || cargo.Item == null || cargo.Amount <= 0) continue;
                var cell = TransportElement("transport-item");
                cell.tooltip = $"{cargo.Item.DisplayName} ×{cargo.Amount:N0}";
                var tile = TransportElement("transport-tile");
                var anchor = TransportElement("transport-icon-anchor");
                var halo = TransportElement("transport-halo");
                var ring = TransportElement("transport-ring");
                var icon = TransportElement("transport-icon");
                ShipCardPresentation.SetIcon(icon, cargo.Item.Icon);
                var multiplier = TransportLabel("×1", "transport-multiplier");
                var pop = TransportLabel("", "transport-pop");
                anchor.Add(halo); anchor.Add(ring); anchor.Add(icon); anchor.Add(multiplier);
                tile.Add(anchor); tile.Add(pop); cell.Add(tile);
                var meta = TransportElement("transport-item-meta");
                meta.Add(TransportLabel(cargo.Item.DisplayName, "transport-item-name"));
                meta.Add(TransportLabel($"×{cargo.Amount:N0}", "transport-quantity"));
                cell.Add(meta);
                transportItemsRoot.Add(cell);
                var itemView = new TransportItemView
                {
                    cargo = new CargoData { Item = cargo.Item, Amount = cargo.Amount }, cell = cell,
                    anchor = anchor, icon = icon, halo = halo, ring = ring,
                    multiplier = multiplier, pop = pop
                };
                for (int i = 0; i < Mathf.Clamp(bonusSparkleCount, 3, 10); i++)
                {
                    var twinkle = new BonusTwinkleElement();
                    anchor.Add(twinkle);
                    itemView.twinkles.Add(twinkle);
                }
                transportItems.Add(itemView);
            }
            isRouteDirty = true;
            transportInitializeRoutine = StartCoroutine(InitializeTransportValues());
        }

        private IEnumerator InitializeTransportValues()
        {
            // Sell-entry price and equipment snapshots may be raised after this subscriber.
            yield return null;
            Dictionary<ETransportItemType, TransportMarketPriceEntry> initialPrices =
                pendingMarketPrices.Count > 0 ? pendingMarketPrices.Dequeue() : null;
            foreach (TransportItemView item in transportItems)
            {
                TransportMarketPriceEntry price;
                if ((initialPrices != null && initialPrices.TryGetValue(item.cargo.Item.Type, out price)) ||
                    marketPriceManager.TryGetMarketPrice(item.cargo.Item.Type, out price))
                    item.appliedMultiplier = price.GetAppliedMultiplier();
                item.multiplier.text = "×" + item.appliedMultiplier;
            }
            SetTransportRewardTargets(true);
            activeTransportGoalCurrency = ResolveTransportGoalCurrency(transportEquipmentManager.SellSnapshot);
            transportInitialized = true;
            transportInitializeRoutine = null;
            RefreshTransportGoal();
            if (pendingMarketPrices.Count > 0 && transportRevealRoutine == null)
                transportRevealRoutine = StartCoroutine(RevealTransportPrices());
        }

        private void OnMarketPrice(GetMarketPriceEvent evt)
        {
            if (evt.MarketPrices == null || transportItems.Count == 0 ||
                (!transportInitialized && transportInitializeRoutine == null)) return;
            var snapshot = new Dictionary<ETransportItemType, TransportMarketPriceEntry>();
            foreach (var price in evt.MarketPrices) snapshot[price.Key] = price.Value;
            pendingMarketPrices.Enqueue(snapshot);
            if (transportInitialized && transportRevealRoutine == null)
                transportRevealRoutine = StartCoroutine(RevealTransportPrices());
        }

        private IEnumerator RevealTransportPrices()
        {
            yield return null;
            while (pendingMarketPrices.Count > 0)
                yield return RevealTransportSnapshot(pendingMarketPrices.Dequeue());
            transportRevealRoutine = null;
        }

        private IEnumerator RevealTransportSnapshot(Dictionary<ETransportItemType, TransportMarketPriceEntry> prices)
        {
            Dictionary<CurrencyType, long> before = CalculateTransportRewards();
            Dictionary<CurrencyType, long> after = CalculateTransportRewards(prices);
            before.TryGetValue(TransportGoalCurrency, out long goalBefore);
            after.TryGetValue(TransportGoalCurrency, out long goalAfter);
            decimal goalGain = (decimal)goalAfter - goalBefore;
            Dictionary<CurrencyType, long> baseRewards = CalculateTransportCargoRewards(null, true);
            baseRewards.TryGetValue(TransportGoalCurrency, out long baseValue);
            bool special = baseValue > 0 && goalGain > 0 &&
                goalGain / baseValue >= (decimal)Mathf.Max(0.01f, transportSpecialGainBaseMultiple) &&
                transportSpecialResultsPlayed < Mathf.Max(0, transportSpecialResultsPerVoyage);
            var reveals = new List<CargoReveal>();
            foreach (TransportItemView item in transportItems)
            {
                if (!prices.TryGetValue(item.cargo.Item.Type, out TransportMarketPriceEntry price)) continue;
                int target = price.GetAppliedMultiplier();
                if (target == item.appliedMultiplier) continue;
                reveals.Add(new CargoReveal
                {
                    view = item, before = item.appliedMultiplier, target = target,
                    streamCount = target > item.appliedMultiplier ?
                        Mathf.Clamp(special ? bonusCurrencyStreamCount : currencyStreamCount, 1, 8) : 0
                });
            }
            // A zero-change roll is still one result; each cargo never adds another wait.
            if (special) transportSpecialResultsPlayed++;
            bool hasGain = reveals.Exists(reveal => reveal.target > reveal.before);
            transportCargoRoot.EnableInClassList("is-market-reveal", hasGain);
            foreach (TransportRewardView reward in transportRewards) reward.baseline = reward.target;
            foreach (CargoReveal reveal in reveals)
            {
                SetTransportPhase(reveal.view, "is-charging");
                if (hasGain) reveal.view.multiplier.text = "× ?";
            }
            ShowTransportReveal(before, after, special, false);
            float anticipation = hasGain ? Mathf.Max(0.1f, transportAnticipationSeconds) : 0;
            float resolveAt = hasGain ? anticipation + Mathf.Max(0, transportRevealPauseSeconds) : 0;
            float resolve = Mathf.Max(0.01f, cargoResolveSeconds);
            float flight = Mathf.Max(0.01f, currencyFlightSeconds);
            float interval = Mathf.Max(0.01f, currencyStreamIntervalSeconds);
            int streams = Mathf.Clamp(special ? bonusCurrencyStreamCount : currencyStreamCount, 1, 8);
            // Leave enough time for the last packet to arrive and for the amount to finish counting.
            float finishAt = Mathf.Max(special ? transportSpecialResultSeconds : transportResultSeconds,
                resolveAt + flight + (streams - 1) * interval + Mathf.Max(transportCountSeconds, rewardImpactSeconds));
            float sparkle = Mathf.Min(Mathf.Max(0.1f, bonusSparkleSeconds), finishAt - resolveAt);
            float elapsed = 0;
            bool resolved = false;
            bool rewardsArrived = false;
            while (elapsed < finishAt)
            {
                elapsed += Time.unscaledDeltaTime;
                if (!resolved && elapsed >= resolveAt)
                {
                    resolved = true;
                    // Apply the whole display snapshot together. Flights are feedback, never payment.
                    foreach (CargoReveal reveal in reveals) reveal.view.appliedMultiplier = reveal.target;
                    ShowTransportReveal(before, after, special, true);
                    foreach (CargoReveal reveal in reveals)
                    {
                        TransportItemView item = reveal.view;
                        bool grows = reveal.target > reveal.before;
                        SetTransportPhase(item, grows ? special ? "is-bonus" : "is-success" : "is-failure");
                        item.multiplier.text = "×" + reveal.target;
                        item.pop.text = (grows ? "+" : "−") + System.Math.Abs(reveal.target - reveal.before) + "배";
                        if (!grows) continue;
                        StartTransportSparks(item, elapsed, special ? 10 : 4);
                        StartCurrencyFlights(reveal, elapsed, interval);
                    }
                }
                float resultAge = Mathf.Max(0, elapsed - resolveAt);
                foreach (CargoReveal reveal in reveals)
                {
                    TransportItemView item = reveal.view;
                    if (!resolved)
                    {
                        float charge = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, anticipation));
                        float squeeze = Mathf.SmoothStep(0, 1, charge);
                        bool holding = elapsed >= anticipation;
                        float shake = holding ? 0 : Mathf.Sin(elapsed * Mathf.Lerp(20, 65, squeeze)) * squeeze * 2.5f;
                        item.anchor.style.scale = new Scale(new Vector3(1 - squeeze * 0.22f, 1 + squeeze * 0.08f, 1));
                        item.anchor.style.translate = new Translate(shake, -squeeze * 6, 0);
                        item.halo.style.opacity = 0.15f + squeeze * 0.65f;
                        item.halo.style.scale = new Scale(Vector3.one * Mathf.Lerp(1.5f, 0.85f, squeeze));
                        item.ring.style.opacity = squeeze * 0.8f;
                        item.ring.style.scale = new Scale(Vector3.one * Mathf.Lerp(1.8f, 0.9f, squeeze));
                        continue;
                    }
                    float p = Mathf.Clamp01(resultAge / resolve);
                    bool grows = reveal.target > reveal.before;
                    float strength = cargoImpactStrength * (special ? 1.5f : 1f);
                    float bounce = Mathf.Sin(p * Mathf.PI * 2.5f) * Mathf.Pow(1 - p, 2);
                    item.anchor.style.scale = new Scale(Vector3.one * (1 + bounce * (grows ? 0.4f * strength : 0.08f)));
                    item.anchor.style.translate = new Translate(0, (grows ? -1 : 1) * Mathf.Sin(p * Mathf.PI) * 5 * strength, 0);
                    item.multiplier.style.scale = new Scale(Vector3.one * (1 + Mathf.Sin(p * Mathf.PI) * 0.3f));
                    item.halo.style.opacity = 1 - p;
                    item.halo.style.scale = new Scale(Vector3.one * (0.8f + p * 1.2f));
                    item.ring.style.opacity = grows ? 1 - p : 0;
                    item.ring.style.scale = new Scale(Vector3.one * (0.8f + p * (special ? 1.2f : 0.7f)));
                    item.pop.style.opacity = 1 - Mathf.SmoothStep(0.35f, 1, p);
                    item.pop.style.scale = new Scale(Vector3.one * (1 + Mathf.Pow(1 - p, 3) * 0.65f * strength));
                    item.pop.style.translate = new Translate(0, -8 - 20 * p, 0);
                    if (special && grows)
                    {
                        AnimateBonusTwinkles(item, resultAge, sparkle);
                        if (resultAge < sparkle)
                        {
                            float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(resultAge * Mathf.PI * 8)), 2);
                            item.halo.style.opacity = Mathf.Max(1 - p, 0.15f + pulse * 0.5f);
                            item.halo.style.scale = new Scale(Vector3.one * (1.1f + pulse * 0.16f));
                        }
                    }
                }
                transportReveal.style.opacity = Mathf.Min(Mathf.Clamp01(elapsed / 0.08f),
                    Mathf.Clamp01((finishAt - elapsed) / Mathf.Min(0.15f, finishAt * 0.2f)));
                transportReveal.style.translate = new Translate(0, 0, 0);
                float punch = resolved ? Mathf.Pow(1 - Mathf.Clamp01(resultAge / resolve), 3) : 0;
                revealRate.style.scale = new Scale(Vector3.one * (1 + punch * (special ? 0.3f : 0.18f)));
                bool packetArrived = AnimateCurrencyFlights(elapsed, flight);
                if (resolved && !rewardsArrived &&
                    (packetArrived || (currencyFlights.Count == 0 && resultAge >= flight)))
                {
                    rewardsArrived = true;
                    SetTransportRewardTargets(false);
                    RefreshTransportGoal();
                }
                AnimateTransportSparks(elapsed);
                AnimateTransportRewards(Time.unscaledDeltaTime);
                yield return null;
            }
            foreach (CargoReveal reveal in reveals) reveal.view.appliedMultiplier = reveal.target;
            SetTransportRewardTargets(true);
            RefreshTransportGoal();
            currencyFlights.Clear(); transportSparks.Clear(); transportFx.Clear();
            foreach (TransportItemView item in transportItems)
            {
                item.anchor.style.scale = item.multiplier.style.scale = new Scale(Vector3.one);
                item.anchor.style.translate = new Translate(0, 0, 0);
                item.halo.style.opacity = item.pop.style.opacity = item.ring.style.opacity = 0;
                foreach (VisualElement twinkle in item.twinkles) twinkle.style.opacity = 0;
                SetTransportPhase(item, "is-settled");
            }
            transportReveal.AddToClassList("hidden");
            transportCargoRoot.RemoveFromClassList("is-market-reveal");
            transportGoalValue.style.scale = transportGoalIcon.style.scale = new Scale(Vector3.one);
        }

        private void ShowTransportReveal(Dictionary<CurrencyType, long> before,
            Dictionary<CurrencyType, long> after, bool special, bool resolved)
        {
            before.TryGetValue(TransportGoalCurrency, out long goalBefore);
            after.TryGetValue(TransportGoalCurrency, out long goalAfter);
            decimal goalDelta = (decimal)goalAfter - goalBefore;
            bool decreasing = false;
            foreach (var reward in before)
                if (after.TryGetValue(reward.Key, out long next) && next < reward.Value) decreasing = true;
            transportReveal.RemoveFromClassList("hidden");
            transportReveal.EnableInClassList("is-charging", !resolved);
            transportReveal.EnableInClassList("is-success", resolved && !decreasing && goalDelta > 0);
            transportReveal.EnableInClassList("is-bonus", resolved && special);
            transportReveal.EnableInClassList("is-failure", resolved && decreasing);
            revealCargoName.text = !resolved ? "구간 돌파 · 새 시세 확인 중" : decreasing ? "보너스 감소 · 기본가치 유지" : special ? "시세 대박!" : goalDelta > 0 ? "시세 보너스" : "시세 결과";
            revealRate.text = !resolved ? "× ?" : goalDelta == 0 ? "목표 재화 변동 없음" :
                CurrencyTextFormatter.FormatName(TransportGoalCurrency) + " " + SignedTransportAmount(goalDelta);
            revealRate.tooltip = resolved ? $"목표 재화 예상액 {goalBefore:N0} → {goalAfter:N0}" : "";
            revealMultiplier.text = resolved ? $"도착 예상 {FormatTransportAmount(goalBefore)} → {FormatTransportAmount(goalAfter)}" : $"도착 예상 {FormatTransportAmount(goalBefore)} → ?";
            revealRate.style.opacity = 1;
            revealRate.style.scale = new Scale(Vector3.one);
            foreach (RevealMoneyView row in revealMoneyViews)
            {
                before.TryGetValue(row.currency, out long previous);
                after.TryGetValue(row.currency, out long next);
                row.row.EnableInClassList("hidden", !resolved || previous == next || row.currency == TransportGoalCurrency);
                row.before.text = FormatTransportAmount(previous);
                row.after.text = resolved ? FormatTransportAmount(next) : "?";
                decimal delta = (decimal)next - previous;
                row.delta.text = resolved ? (delta >= 0 ? "+" : "−") + FormatTransportAmount((long)System.Math.Abs(delta)) : "";
                row.row.tooltip = resolved ? $"예상 전체 {CurrencyTextFormatter.FormatName(row.currency)} {previous:N0} → {next:N0}" : "";
            }
        }

        private static string FormatTransportAmount(long amount) =>
            amount < CompactBalanceThreshold ? amount.ToString("N0") : CurrencyTextFormatter.Format(amount);

        private static string SignedTransportAmount(decimal amount) =>
            (amount >= 0 ? "+" : "−") + FormatTransportAmount((long)System.Math.Abs(amount));

        private static void AnimateBonusTwinkles(TransportItemView item, float elapsed, float seconds)
        {
            float lifetime = seconds * 0.32f;
            float interval = seconds * 0.68f / Mathf.Max(1, item.twinkles.Count - 1);
            for (int i = 0; i < item.twinkles.Count; i++)
            {
                VisualElement twinkle = item.twinkles[i];
                float p = (elapsed - i * interval) / lifetime;
                if (p < 0 || p >= 1) { twinkle.style.opacity = 0; continue; }
                float pulse = Mathf.Pow(Mathf.Sin(p * Mathf.PI), 0.65f);
                float angle = -Mathf.PI * 0.7f + i * 2.4f;
                float radius = 36 + i % 3 * 7;
                twinkle.style.translate = new Translate(Mathf.Cos(angle) * radius - 18,
                    Mathf.Sin(angle) * radius * 0.72f - 18 - p * 3, 0);
                twinkle.style.opacity = pulse;
                twinkle.style.scale = new Scale(Vector3.one * (0.25f + pulse * (i % 2 == 0 ? 1.15f : 0.8f)));
                twinkle.style.rotate = new Rotate(Angle.Degrees(i % 2 * 30 + p * 12));
            }
        }

        private static void SetTransportPhase(TransportItemView item, string phase)
        {
            item.cell.EnableInClassList("is-charging", phase == "is-charging");
            item.cell.EnableInClassList("is-bonus", phase == "is-bonus");
            item.cell.EnableInClassList("is-success", phase == "is-success");
            item.cell.EnableInClassList("is-failure", phase == "is-failure");
            item.cell.EnableInClassList("is-settled", phase == "is-settled");
        }

        private Dictionary<CurrencyType, long> CalculateTransportCargoRewards(
            Dictionary<ETransportItemType, TransportMarketPriceEntry> prices = null, bool baseValuesOnly = false)
        {
            var rewards = new Dictionary<CurrencyType, long>();
            foreach (TransportItemView item in transportItems)
            {
                if (item.cargo.Item.Rewards == null) continue;
                int multiplier = baseValuesOnly ? 1 : item.appliedMultiplier;
                if (!baseValuesOnly && prices != null && prices.TryGetValue(item.cargo.Item.Type, out TransportMarketPriceEntry price))
                    multiplier = price.GetAppliedMultiplier();
                foreach (var reward in item.cargo.Item.Rewards)
                {
                    if (reward.CurrencyAmount <= 0) continue;
                    decimal value = (decimal)item.cargo.Amount * reward.CurrencyAmount * multiplier;
                    rewards.TryGetValue(reward.CurrencyType, out long current);
                    rewards[reward.CurrencyType] = (long)System.Math.Min(long.MaxValue, (decimal)current + value);
                }
            }
            return rewards;
        }

        private Dictionary<CurrencyType, long> CalculateTransportRewards(
            Dictionary<ETransportItemType, TransportMarketPriceEntry> prices = null)
        {
            Dictionary<CurrencyType, long> rewards = CalculateTransportCargoRewards(prices);
            if (transportItems.Count > 0) transportEquipmentManager.ApplySnapshotModifiers(rewards);
            return rewards;
        }

        /// <summary>Current manifest value, market-only gain, and the Sell-entry equipment contribution, in one currency.</summary>
        public bool TryGetTransportValueBreakdown(CurrencyType currency, out long baseValue,
            out long marketBonus, out long equipmentBonus)
        {
            baseValue = marketBonus = equipmentBonus = 0;
            if (!transportInitialized || transportItems.Count == 0) return false;
            Dictionary<CurrencyType, long> baseRewards = CalculateTransportCargoRewards(null, true);
            Dictionary<CurrencyType, long> marketRewards = CalculateTransportCargoRewards();
            baseRewards.TryGetValue(currency, out baseValue);
            marketRewards.TryGetValue(currency, out long marketValue);
            // Match the actual order: saturated cargo sum, then the frozen equipment modifiers.
            transportEquipmentManager.ApplySnapshotModifiers(marketRewards);
            marketRewards.TryGetValue(currency, out long finalValue);
            marketBonus = marketValue - baseValue;
            equipmentBonus = finalValue - marketValue;
            return true;
        }

        private CurrencyType ResolveTransportGoalCurrency(TransportEquipmentSnapshot snapshot)
        {
            if (!snapshot.HasEquipment || snapshot.SettlementModifiers.Count == 0) return transportGoalCurrency;
            CurrencyType currency = snapshot.SettlementModifiers[0].CurrencyType;
            for (int i = 1; i < snapshot.SettlementModifiers.Count; i++)
                if (snapshot.SettlementModifiers[i].CurrencyType != currency) return transportGoalCurrency;
            return currency;
        }

        private void RefreshTransportGoal()
        {
            CurrencyType currency = TransportGoalCurrency;
            if (!TryGetTransportValueBreakdown(currency, out long baseValue,
                out long marketBonus, out long equipmentBonus)) return;
            long total = (long)((decimal)baseValue + marketBonus + equipmentBonus);
            string goalName = currency == CurrencyType.Diamond ? "호위선 모집" :
                currency == CurrencyType.Emerald ? "지휘 강화" : "함대 강화";
            if (transportGoal != null) transportGoal.text = "도착 예상";
            if (transportGoalIcon != null)
                foreach (CurrencySlot slot in currencySlots)
                    if (slot.currencyType == currency) { SetCurrencyIcon(transportGoalIcon, slot); break; }
            if (transportGoalValue != null)
            {
                transportGoalValue.text = FormatTransportAmount(total);
                transportGoalValue.tooltip = $"도착 후 지급될 예상 {CurrencyTextFormatter.FormatName(currency)} {total:N0}";
            }
            if (transportGoalBreakdown != null)
            {
                transportGoalBreakdown.text = $"기본 {FormatTransportAmount(baseValue)}  ·  시세 {SignedTransportAmount(marketBonus)}  ·  장비 {SignedTransportAmount(equipmentBonus)}";
                transportGoalBreakdown.tooltip = $"기본 화물 {baseValue:N0} + 누적 시세 보너스 {marketBonus:N0} + 운송 장비 기여 {equipmentBonus:N0} = {total:N0}";
            }
            TransportEquipmentSnapshot snapshot = transportEquipmentManager.SellSnapshot;
            if (transportGoalHint != null)
                transportGoalHint.text = (snapshot.HasEquipment ? "출항 장비 · " + snapshot.DisplayName : "출항 장비 · 미장착") +
                    "  |  도착 후 지급 · 기본가치 보존";
            if (transportGoal != null)
                transportGoal.tooltip = goalName + " · " + transportGoalHint?.text;
        }

        private void SetTransportRewardTargets(bool immediate)
        {
            Dictionary<CurrencyType, long> rewards = CalculateTransportRewards();
            foreach (TransportRewardView reward in transportRewards)
            {
                rewards.TryGetValue(reward.currency, out long next);
                if (immediate)
                {
                    reward.displayed = reward.from = reward.target = next;
                    reward.countElapsed = Mathf.Max(0.01f, transportCountSeconds);
                    reward.delta.style.opacity = 0;
                    ResetTransportRewardImpact(reward);
                    reward.container.RemoveFromClassList("is-gaining");
                    reward.container.RemoveFromClassList("is-decreasing");
                    UpdateTransportRewardText(reward);
                    continue;
                }
                if (next == reward.target) continue;
                reward.from = reward.displayed; reward.target = next; reward.countElapsed = 0;
                reward.impactElapsed = 0;
                decimal delta = (decimal)next - reward.baseline;
                long deltaAmount = (long)System.Math.Abs(delta);
                string deltaText = deltaAmount < CompactBalanceThreshold ? deltaAmount.ToString("N0") : CurrencyTextFormatter.Format(deltaAmount);
                reward.delta.text = (delta > 0 ? "+" : "−") + deltaText;
                reward.container.EnableInClassList("is-gaining", delta > 0);
                reward.container.EnableInClassList("is-decreasing", delta < 0);
            }
        }
        private static void UpdateTransportRewardText(TransportRewardView reward)
        {
            reward.value.text = reward.displayed < CompactBalanceThreshold ? reward.displayed.ToString("N0") : CurrencyTextFormatter.Format(reward.displayed);
            reward.container.tooltip = CurrencyTextFormatter.FormatName(reward.currency) + " " + reward.displayed.ToString("N0");
        }
        private void AnimateTransportRewards(float deltaTime)
        {
            float seconds = Mathf.Max(0.01f, transportCountSeconds);
            foreach (TransportRewardView reward in transportRewards)
            {
                reward.countElapsed += deltaTime;
                float p = Mathf.Clamp01(reward.countElapsed / seconds);
                float eased = 1 - Mathf.Pow(1 - p, 3);
                reward.displayed = p >= 1 ? reward.target : (long)((decimal)reward.from + ((decimal)reward.target - reward.from) * (decimal)eased);
                UpdateTransportRewardText(reward);
                reward.delta.style.opacity = 1 - Mathf.SmoothStep(0, 1, p);
                reward.delta.style.translate = new Translate(0, -12 * p, 0);
                reward.impactElapsed += deltaTime;
                float impact = Mathf.Clamp01(reward.impactElapsed / Mathf.Max(0.01f, rewardImpactSeconds));
                float punch = Mathf.Sin(impact * Mathf.PI) * (1 - impact);
                reward.icon.style.scale = new Scale(Vector3.one * (1 + punch * 0.6f * cargoImpactStrength));
                reward.value.style.scale = new Scale(Vector3.one * (1 + punch * 0.14f * cargoImpactStrength));
                reward.container.style.translate = new Translate(0, -punch * 4 * cargoImpactStrength, 0);
                reward.impact.style.opacity = 1 - impact;
                reward.impact.style.scale = new Scale(Vector3.one * (0.65f + impact * 1.1f));
                if (reward.currency == TransportGoalCurrency && transportGoalValue != null)
                {
                    transportGoalValue.text = FormatTransportAmount(reward.displayed);
                    transportGoalValue.style.scale = new Scale(Vector3.one * (1 + punch * 0.14f * cargoImpactStrength));
                    if (transportGoalIcon != null)
                        transportGoalIcon.style.scale = new Scale(Vector3.one * (1 + punch * 0.6f * cargoImpactStrength));
                }
            }
        }

        private void ResetTransportRewardImpact(TransportRewardView reward)
        {
            reward.impactElapsed = Mathf.Max(0.01f, rewardImpactSeconds);
            reward.icon.style.scale = reward.value.style.scale = new Scale(Vector3.one);
            reward.container.style.translate = new Translate(0, 0, 0);
            reward.impact.style.opacity = 0;
        }

        private void StartCurrencyFlights(CargoReveal reveal, float elapsed, float interval)
        {
            TransportItemView item = reveal.view;
            foreach (TransportRewardView reward in transportRewards)
            {
                bool contributes = false;
                if (item.cargo.Item.Rewards != null)
                    foreach (var entry in item.cargo.Item.Rewards)
                        if (entry.CurrencyType == reward.currency && entry.CurrencyAmount > 0) contributes = true;
                if (!contributes || reward.currency != TransportGoalCurrency) continue;
                for (int packet = 0; packet < reveal.streamCount; packet++)
                {
                    var icon = TransportElement("transport-fly-icon");
                    icon.style.opacity = 0;
                    ShipCardPresentation.SetIcon(icon, reward.sprite);
                    transportFx.Add(icon);
                    currencyFlights.Add(new CurrencyFlight
                    {
                        icon = icon, reward = reward,
                        from = transportFx.WorldToLocal(item.icon.worldBound.center) +
                            new Vector2(Mathf.Sin(packet * 2.4f) * 12, Mathf.Cos(packet * 2.4f) * 8),
                        to = transportFx.WorldToLocal(transportGoalIcon.worldBound.center),
                        startedAt = elapsed + packet * interval, bend = 30 + packet * 6
                    });
                }
            }
        }
        private bool AnimateCurrencyFlights(float elapsed, float duration)
        {
            bool arrived = false;
            for (int i = currencyFlights.Count - 1; i >= 0; i--)
            {
                CurrencyFlight flight = currencyFlights[i];
                if (elapsed < flight.startedAt) continue;
                float p = Mathf.Clamp01((elapsed - flight.startedAt) / duration);
                float eased = p * p;
                Vector2 position = Vector2.Lerp(flight.from, flight.to, eased);
                position.y -= Mathf.Sin(p * Mathf.PI) * flight.bend;
                flight.icon.style.translate = new Translate(position.x - 11, position.y - 11, 0);
                flight.icon.style.opacity = Mathf.Clamp01((1 - p) * 10);
                flight.icon.style.scale = new Scale(Vector3.one * (0.7f + Mathf.Sin(p * Mathf.PI) * 0.5f));
                if (p < 1) continue;
                arrived = true;
                flight.reward.impactElapsed = 0;
                flight.icon.RemoveFromHierarchy(); currencyFlights.RemoveAt(i);
            }
            return arrived;
        }

        private void StartTransportSparks(TransportItemView item, float elapsed, int count)
        {
            Vector2 origin = transportFx.WorldToLocal(item.icon.worldBound.center);
            for (int i = 0; i < count; i++)
            {
                var spark = TransportElement("transport-spark");
                if (i % 3 == 1) spark.AddToClassList("currency-diamond");
                if (i % 3 == 2) spark.AddToClassList("currency-emerald");
                transportFx.Add(spark);
                float angle = i * Mathf.PI * 2 / count + 0.2f;
                float speed = 100 + i % 3 * 24;
                transportSparks.Add(new TransportSpark
                {
                    element = spark, origin = origin,
                    velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    startedAt = elapsed, seconds = 0.38f + i % 3 * 0.04f
                });
            }
        }

        private void AnimateTransportSparks(float elapsed)
        {
            for (int i = transportSparks.Count - 1; i >= 0; i--)
            {
                TransportSpark spark = transportSparks[i];
                float age = elapsed - spark.startedAt;
                float p = Mathf.Clamp01(age / spark.seconds);
                Vector2 position = spark.origin + spark.velocity * (age * (1 - p * 0.35f));
                position.y += age * age * 65;
                spark.element.style.translate = new Translate(position.x - 3, position.y - 3, 0);
                spark.element.style.opacity = 1 - p;
                spark.element.style.scale = new Scale(Vector3.one * (1.3f - p));
                if (p < 1) continue;
                spark.element.RemoveFromHierarchy(); transportSparks.RemoveAt(i);
            }
        }
    }
}
