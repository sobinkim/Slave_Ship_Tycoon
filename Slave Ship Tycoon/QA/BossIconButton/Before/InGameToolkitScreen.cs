using System;
using System.Collections;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.Currency;
using SB.Scripts.Visual;
using SB.UI.Cargo;
using SB.UI.Recruitment;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.InGame
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed partial class InGameToolkitScreen : MonoBehaviour
    {
        private const long CompactBalanceThreshold = 1000000;

        [Serializable] public sealed class CurrencySlot
        {
            public CurrencyType currencyType;
            public Sprite icon;
            public Color iconColor = Color.white;
            public string hint;
        }
        [Header("References")]
        [SerializeField] private UIDocument document;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private CargoManager cargoManager;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private ChapterDatabase chapterDatabase;
        [SerializeField] private PannelManager panelManager;
        [SerializeField] private RecruitmentToolkitScreen recruitmentScreen;
        [SerializeField] private CargoToolkitScreen cargoScreen;
        [SerializeField] private UpgradeToolkitPanel upgradePanel;
        [SerializeField] private CurrencySlot[] currencySlots;
        [Header("Route copy")]
        [SerializeField] private string pirateTitle = "해적 해역";
        [SerializeField] private string transportTitle = "운송 해역";
        [SerializeField] private string bossTitle = "봉쇄 함대";
        [SerializeField] private string pirateDescription = "자동 약탈 중 · 화물을 모아 출항을 준비하세요";
        [SerializeField] private string transportDescription = "목적지까지 화물을 안전하게 운송하세요";
        [SerializeField] private string bossDescription = "보스를 격파하고 다음 항로를 여세요";
        [Header("Gain logs (unscaled seconds)")]
        [SerializeField, Min(1)] private int maximumVisibleLogs = 5;
        [SerializeField, Min(0)] private float logVisibleSeconds = 1f;
        [SerializeField, Min(0.01f)] private float logFadeSeconds = 0.2f;
        [SerializeField, Min(0.01f)] private float logOverflowFadeSeconds = 0.1f;
        [SerializeField, Min(0)] private float logSpawnIntervalSeconds = 0.1f;

        private sealed class GainLog { public VisualElement row; public float expiresAt; public float fadeSeconds; }
        private readonly Dictionary<CurrencyType, Label> amounts = new Dictionary<CurrencyType, Label>();
        private readonly Queue<CurrencyAddedEvent> pendingLogs = new Queue<CurrencyAddedEvent>();
        private readonly List<GainLog> logs = new List<GainLog>();
        private VisualElement root;
        private Coroutine spawnRoutine;
        private StageStartedEvent currentStage;
        private bool hasStage;
        private bool isRouteDirty;
        private int completedTransportStages;
        private readonly List<Label> transportStops = new List<Label>();
        private Button transportReturnButton;

        private void OnEnable()
        {
            if (document == null || currencyManager == null || cargoManager == null || gameFlowManager == null ||
                chapterDatabase == null || panelManager == null || recruitmentScreen == null || cargoScreen == null || upgradePanel == null ||
                marketPriceManager == null || transportEquipmentManager == null)
            { Debug.LogError("InGameToolkitScreen의 명시적 참조를 연결하세요.", this); return; }
            root = document.rootVisualElement;
            root.AddToClassList("harbor-document");
            root.pickingMode = PickingMode.Ignore;
            root.Q("hud").pickingMode = PickingMode.Ignore;
            transportStops.Clear();
            BuildCurrencies();
            BindTransportPresentation();
            transportReturnButton = root.Q<Button>("transportReturn");
            transportReturnButton.clicked += ReturnToFarming;
            root.Q<Button>("bossButton").clicked += OpenCargo;
            root.Q<Button>("upgradeNav").clicked += OpenUpgrade;
            root.Q<Button>("fleetNav").clicked += OpenFleet;
            root.Q<Button>("skillsNav").clicked += OpenSkills;
            root.Q<Button>("recruitNav").clicked += OpenRecruitment;
            panelManager.PanelChanged += OnPanelChanged;
            recruitmentScreen.OpenStateChanged += OnRecruitmentChanged;
            Bus<CurrencyChangedEvent>.OnEvent += OnCurrencyChanged;
            Bus<CurrencyAddedEvent>.OnEvent += OnCurrencyAdded;
            Bus<StageStartedEvent>.OnEvent += OnStageStarted;
            Bus<EvenStageClearedEvent>.OnEvent += OnTransportStageCleared;
            Bus<SellChapterStartedEvent>.OnEvent += OnSellStarted;
            Bus<ObtainChapterStartedEvent>.OnEvent += OnObtainStarted;
            Bus<GetMarketPriceEvent>.OnEvent += OnMarketPrice;
            RefreshCurrencies();
            OnPanelChanged(panelManager.CurrentPanel);
            isRouteDirty = hasStage;
        }

        private void Start() => RefreshCurrencies();

        private void OnDisable()
        {
            Bus<CurrencyChangedEvent>.OnEvent -= OnCurrencyChanged;
            Bus<CurrencyAddedEvent>.OnEvent -= OnCurrencyAdded;
            Bus<StageStartedEvent>.OnEvent -= OnStageStarted;
            Bus<EvenStageClearedEvent>.OnEvent -= OnTransportStageCleared;
            Bus<SellChapterStartedEvent>.OnEvent -= OnSellStarted;
            Bus<ObtainChapterStartedEvent>.OnEvent -= OnObtainStarted;
            Bus<GetMarketPriceEvent>.OnEvent -= OnMarketPrice;
            if (panelManager != null) panelManager.PanelChanged -= OnPanelChanged;
            if (recruitmentScreen != null) recruitmentScreen.OpenStateChanged -= OnRecruitmentChanged;
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
            spawnRoutine = null;
            pendingLogs.Clear();
            logs.Clear();
            ClearTransportPresentation();
            if (root == null) return;
            if (transportReturnButton != null) transportReturnButton.clicked -= ReturnToFarming;
            transportReturnButton = null;
            root.Q("gainLogs")?.Clear();
            if (root.Q<Button>("bossButton") is Button bossButtonBinding) bossButtonBinding.clicked -= OpenCargo;
            if (root.Q<Button>("upgradeNav") is Button upgradeNavBinding) upgradeNavBinding.clicked -= OpenUpgrade;
            if (root.Q<Button>("fleetNav") is Button fleetNavBinding) fleetNavBinding.clicked -= OpenFleet;
            if (root.Q<Button>("skillsNav") is Button skillsNavBinding) skillsNavBinding.clicked -= OpenSkills;
            if (root.Q<Button>("recruitNav") is Button recruitNavBinding) recruitNavBinding.clicked -= OpenRecruitment;
        }

        private void Update()
        {
            if (transportReturnButton != null) transportReturnButton.SetEnabled(gameFlowManager.CanReturnToFarming);
            for (int i = logs.Count - 1; i >= 0; i--)
            {
                GainLog log = logs[i];
                float remaining = log.expiresAt - Time.unscaledTime;
                if (remaining <= 0f) { log.row.RemoveFromHierarchy(); logs.RemoveAt(i); }
                else log.row.style.opacity = Mathf.Clamp01(remaining / log.fadeSeconds);
            }
        }
        private void LateUpdate()
        {
            if (!isRouteDirty || !hasStage || root == null) return;
            isRouteDirty = false;
            bool isTransport = currentStage.RouteType == ChapterRouteType.Sell;
            bool hasChapter = chapterDatabase.TryGetChapter(currentStage.Chapter, out ChapterContainer chapter);
            int total = hasChapter ? chapter.MaxStage : currentStage.Stage;
            bool isBlockade = isTransport && hasChapter && chapter.IsFinalStage(currentStage.Stage);
            root.Q("routeCard").EnableInClassList("is-transport", isTransport);
            root.Q("routeCard").EnableInClassList("is-blockade", isBlockade);
            root.Q<Label>("routeNumber").text = $"항로 {currentStage.Chapter:00}";
            root.Q<Label>("routeTitle").text = isBlockade ? "마지막 봉쇄선" : isTransport ? transportTitle : currentStage.IsBossBattle ? bossTitle : pirateTitle;
            root.Q<Label>("routeDetail").text = isBlockade ? "봉쇄 함대를 돌파하면 항구에서 정산합니다" : isTransport ? transportDescription : currentStage.IsBossBattle ? bossDescription : pirateDescription;
            root.Q<Label>("stageProgress").text = $"{currentStage.Stage} / {total} 구간";
            root.Q("stageProgress").EnableInClassList("hidden", !isTransport);
            root.Q("transportCargo").EnableInClassList("hidden", !isTransport || transportItems.Count == 0);
            root.Q("stageTrack").EnableInClassList("hidden", !isTransport);
            root.Q("stageFill").style.width = Length.Percent(total > 0 ? Mathf.Clamp01((float)completedTransportStages / total) * 100f : 0);
            if (isTransport)
            {
                RefreshTransportStops(total);
                root.Q<Label>("transportBeat").text = completedTransportStages >= total ? "항구 도착 · 화물 하역 준비" :
                    isBlockade ? "봉쇄선을 돌파하면 정산" : $"항구까지 {Mathf.Max(0, total - completedTransportStages)}전 · 승리할 때마다 시세 상승";
            }
            root.Q("bossEntry").EnableInClassList("hidden", isTransport);
            transportReturnButton.EnableInClassList("hidden", !isTransport);
            bool canResume = !isTransport && gameFlowManager.HasSuspendedTransport;
            root.Q<Button>("bossButton").text = canResume ? "운송 재개" : currentStage.IsBossBattle ? "전투 중" : "보스 도전";
            root.Q<Button>("bossButton").SetEnabled(canResume ? gameFlowManager.CanResumeTransport : gameFlowManager.CanStartBossBattle);
            root.Q<Label>("bossHint").text = canResume ? $"항로 {gameFlowManager.SuspendedTransportChapter} · {gameFlowManager.SuspendedTransportStage}구간부터 계속" :
                currentStage.IsBossBattle ? "봉쇄 함대를 격파하세요" : "승리 시 운송 항로 개방";
        }

        private void BuildCurrencies()
        {
            VisualElement bar = root.Q("currencyBar");
            bar.Clear();
            amounts.Clear();
            foreach (CurrencySlot slot in currencySlots)
            {
                var cell = new VisualElement { tooltip = slot.hint };
                cell.AddToClassList("currency-slot");
                cell.AddToClassList("currency-" + slot.currencyType.ToString().ToLowerInvariant());
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("currency-icon");
                SetCurrencyIcon(icon, slot); cell.Add(icon);
                var copy = new VisualElement(); copy.AddToClassList("currency-copy");
                var name = new Label(CurrencyTextFormatter.FormatName(slot.currencyType));
                name.AddToClassList("currency-name"); copy.Add(name);
                var amount = new Label(); amount.AddToClassList("currency-amount"); copy.Add(amount);
                cell.Add(copy); amounts.Add(slot.currencyType, amount); bar.Add(cell);
            }
        }
        private static void SetCurrencyIcon(VisualElement icon, CurrencySlot slot)
        {
            ShipCardPresentation.SetIcon(icon, slot.icon);
            icon.style.unityBackgroundImageTintColor = slot.iconColor;
        }
        private void RefreshCurrencies()
        {
            if (currencyManager == null) return;
            foreach (var pair in amounts) SetBalance(pair.Key, currencyManager.GetCurrency(pair.Key));
        }
        private void SetBalance(CurrencyType type, long amount)
        {
            if (!amounts.TryGetValue(type, out Label label)) return;
            string exactAmount = amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            label.text = amount < CompactBalanceThreshold ? exactAmount : CurrencyTextFormatter.Format(amount);
            label.tooltip = CurrencyTextFormatter.FormatName(type) + " " + exactAmount;
        }
        private void OnCurrencyChanged(CurrencyChangedEvent evt) => SetBalance(evt.CurrencyType, evt.CurrentAmount);
        private void OnCurrencyAdded(CurrencyAddedEvent evt)
        {
            if (evt.AddedAmount <= 0) return;
            pendingLogs.Enqueue(evt);
            if (spawnRoutine == null) spawnRoutine = StartCoroutine(SpawnLogs());
        }
        private IEnumerator SpawnLogs()
        {
            // Yield once so an empty queue cannot leave a completed Coroutine stored as the active routine.
            yield return null;
            while (pendingLogs.Count > 0)
            {
                CurrencyAddedEvent evt = pendingLogs.Dequeue();
                var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.AddToClassList("gain-log");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore }; icon.AddToClassList("currency-icon");
                foreach (CurrencySlot slot in currencySlots) if (slot.currencyType == evt.CurrencyType) { SetCurrencyIcon(icon, slot); break; }
                row.Add(icon);
                row.Add(new Label("+" + CurrencyTextFormatter.Format(evt.AddedAmount)) { pickingMode = PickingMode.Ignore });
                root.Q("gainLogs").Add(row);
                logs.Add(new GainLog { row = row, expiresAt = Time.unscaledTime + logVisibleSeconds + logFadeSeconds, fadeSeconds = logFadeSeconds });
                int visibleCount = 0;
                for (int i = logs.Count - 1; i >= 0; i--)
                {
                    visibleCount++;
                    if (visibleCount <= maximumVisibleLogs) continue;
                    logs[i].expiresAt = Mathf.Min(logs[i].expiresAt, Time.unscaledTime + logOverflowFadeSeconds);
                    logs[i].fadeSeconds = logOverflowFadeSeconds;
                }
                if (logSpawnIntervalSeconds > 0) yield return new WaitForSecondsRealtime(logSpawnIntervalSeconds);
            }
            spawnRoutine = null;
        }

        private void OnStageStarted(StageStartedEvent evt)
        {
            currentStage = evt; hasStage = true; isRouteDirty = true;
            completedTransportStages = evt.RouteType == ChapterRouteType.Sell ? Mathf.Max(0, evt.Stage - 1) : 0;
            if (evt.RouteType == ChapterRouteType.Sell && !transportInitialized && transportInitializeRoutine == null)
                PrepareTransportPresentation();
        }

        private void OnTransportStageCleared(EvenStageClearedEvent evt)
        {
            if (!hasStage || currentStage.RouteType != ChapterRouteType.Sell || evt.Chapter != currentStage.Chapter) return;
            completedTransportStages = Mathf.Max(completedTransportStages, evt.Stage);
            isRouteDirty = true;
        }

        private void RefreshTransportStops(int total)
        {
            if (transportStops.Count != total)
            {
                VisualElement stops = root.Q("transportStops");
                stops.Clear(); transportStops.Clear();
                for (int i = 0; i < total; i++)
                {
                    var stop = new Label(i == total - 1 ? "항구" : (i + 1).ToString()) { pickingMode = PickingMode.Ignore };
                    stop.AddToClassList("transport-stop");
                    stops.Add(stop); transportStops.Add(stop);
                }
            }
            for (int i = 0; i < transportStops.Count; i++)
            {
                transportStops[i].EnableInClassList("is-complete", i < completedTransportStages);
                transportStops[i].EnableInClassList("is-current", i == currentStage.Stage - 1 && i >= completedTransportStages);
                transportStops[i].EnableInClassList("is-port", i == total - 1);
            }
        }

        public void OpenGrowth(CurrencyType currency)
        {
            if (currency == CurrencyType.Diamond) OpenRecruitment();
            else OpenUpgrade();
        }

        private void OpenCargo()
        {
            if (gameFlowManager.HasSuspendedTransport) gameFlowManager.ResumeTransport();
            else cargoScreen.Open();
        }
        private void ReturnToFarming() => gameFlowManager.ReturnToFarming();
        private void OpenUpgrade() { recruitmentScreen.Close(); panelManager.ClickPanelButton(PanelType.Upgrade); }
        private void OpenSkills() { recruitmentScreen.Close(); panelManager.ClickPanelButton(PanelType.CommanderSkills); }
        private void OpenFleet() => panelManager.ClickPanelButton(PanelType.Fleet);
        private void OpenRecruitment() => recruitmentScreen.Open();
        private void OnPanelChanged(PanelType type)
        {
            upgradePanel.SetVisible(type == PanelType.Upgrade);
            root.Q("commanderSettings").EnableInClassList("hidden", type != PanelType.CommanderSkills);
            RefreshNavigation();
        }
        private void OnRecruitmentChanged(bool isOpen) => RefreshNavigation();
        private void RefreshNavigation()
        {
            bool isRecruitOpen = recruitmentScreen.gameObject.activeInHierarchy;
            root.Q<Button>("upgradeNav").EnableInClassList("primary-button", panelManager.CurrentPanel == PanelType.Upgrade && !isRecruitOpen);
            root.Q<Button>("skillsNav").EnableInClassList("primary-button", panelManager.CurrentPanel == PanelType.CommanderSkills && !isRecruitOpen);
            root.Q<Button>("fleetNav").EnableInClassList("primary-button", panelManager.CurrentPanel == PanelType.Fleet && !isRecruitOpen);
            root.Q<Button>("recruitNav").EnableInClassList("primary-button", isRecruitOpen);
        }
    }
}
