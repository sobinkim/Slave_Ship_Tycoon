using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Upgrade;
using SB.Scripts.Visual;
using SB.Scripts.Currency;
using SB.UI.Recruitment;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.InGame
{
    [DisallowMultipleComponent]
    public sealed class UpgradeToolkitPanel : MonoBehaviour
    {
        [Serializable] public sealed class UpgradeRow
        {
            public MainShipUpgradeType upgradeType;
            public string displayName;
        }
        [Header("References")]
        [SerializeField] private UIDocument document;
        [SerializeField] private ShipUpgradeManager upgradeManager;
        [SerializeField, Tooltip("강화 비용 충족 여부를 표시하는 재화 매니저")] private CurrencyManager currencyManager;
        [SerializeField] private CommanderToolkitController commanderController;
        [SerializeField] private PannelManager panelManager;
        [SerializeField] private RecruitmentToolkitScreen recruitmentScreen;
        [SerializeField] private UpgradeRow[] upgradeRows;
        [Header("Input (unscaled seconds)")]
        [SerializeField, Min(0)] private float repeatStartDelaySeconds = 0.35f;
        [SerializeField, Min(0.01f)] private float repeatIntervalSeconds = 0.1f;
        [SerializeField, Min(0)] private float dragCancelDistance = 12f;
        [SerializeField, Min(0.01f)] private float feedbackSeconds = 0.25f;

        private readonly Dictionary<MainShipUpgradeType, VisualElement> rows = new Dictionary<MainShipUpgradeType, VisualElement>();
        private readonly List<ToolkitHoldRepeatInput> holds = new List<ToolkitHoldRepeatInput>();
        private readonly Dictionary<MainShipUpgradeType, IVisualElementScheduledItem> flashes = new Dictionary<MainShipUpgradeType, IVisualElementScheduledItem>();
        private VisualElement root;

        private void OnEnable()
        {
            if (document == null || upgradeManager == null || currencyManager == null || commanderController == null ||
                panelManager == null || recruitmentScreen == null)
            { Debug.LogError("UpgradeToolkitPanel의 참조를 연결하세요.", this); return; }
            root = document.rootVisualElement;
            VisualElement container = root.Q("upgradeRows");
            container.Clear(); rows.Clear();
            foreach (UpgradeRow definition in upgradeRows)
            {
                var row = new VisualElement(); row.AddToClassList("upgrade-row");
                var copy = new VisualElement(); copy.AddToClassList("upgrade-copy");
                var heading = new VisualElement(); heading.AddToClassList("upgrade-heading");
                var nameLabel = new Label(definition.displayName); nameLabel.AddToClassList("upgrade-name"); heading.Add(nameLabel);
                var level = new Label { name = "level" }; level.AddToClassList("upgrade-level"); heading.Add(level);
                copy.Add(heading);
                var effect = new Label { name = "effect" }; effect.AddToClassList("upgrade-effect"); copy.Add(effect);
                row.Add(copy);
                var action = new Button { name = "upgradeAction", tooltip = "짧게 누르면 1회, 길게 누르면 연속 강화" };
                action.AddToClassList("harbor-button"); action.AddToClassList("upgrade-action");
                var cost = new Label { name = "cost", pickingMode = PickingMode.Ignore }; cost.AddToClassList("upgrade-cost"); action.Add(cost);
                var caption = new Label { name = "actionLabel", pickingMode = PickingMode.Ignore }; caption.AddToClassList("upgrade-action-label"); action.Add(caption);
                row.Add(action); container.Add(row); rows.Add(definition.upgradeType, row);
                MainShipUpgradeType type = definition.upgradeType;
                holds.Add(new ToolkitHoldRepeatInput(action, () => TryUpgrade(type), false, repeatStartDelaySeconds, repeatIntervalSeconds, dragCancelDistance));
            }
            root.Q<Button>("maintenanceTab").clicked += ShowMaintenance;
            root.Q<Button>("commanderTab").clicked += ShowCommander;
            root.Q<Button>("commandSettingsButton").clicked += OpenCommander;
            Bus<UpgradeEvent>.OnEvent += OnUpgradeChanged;
            Bus<CurrencyChangedEvent>.OnEvent += OnCurrencyChanged;
            ShowMaintenance(); Refresh();
        }
        private void Start() => Refresh();
        private void OnDisable()
        {
            Bus<UpgradeEvent>.OnEvent -= OnUpgradeChanged;
            Bus<CurrencyChangedEvent>.OnEvent -= OnCurrencyChanged;
            foreach (var hold in holds) hold.Dispose();
            holds.Clear();
            foreach (var flash in flashes.Values) flash.Pause();
            flashes.Clear();
            if (root == null) return;
            if (root.Q<Button>("maintenanceTab") is Button maintenanceTabBinding) maintenanceTabBinding.clicked -= ShowMaintenance;
            if (root.Q<Button>("commanderTab") is Button commanderTabBinding) commanderTabBinding.clicked -= ShowCommander;
            if (root.Q<Button>("commandSettingsButton") is Button commandSettingsButtonBinding) commandSettingsButtonBinding.clicked -= OpenCommander;
        }
        public void SetVisible(bool visible)
        {
            if (document == null) return;
            document.rootVisualElement.Q("upgradePanel")?.EnableInClassList("hidden", !visible);
            if (!visible) foreach (var hold in holds) hold.Cancel();
        }
        private void ShowMaintenance()
        {
            root.Q("maintenanceScroll").RemoveFromClassList("hidden");
            root.Q("commanderSettings").AddToClassList("hidden");
            root.Q<Button>("maintenanceTab").AddToClassList("primary-button");
            root.Q<Button>("commanderTab").RemoveFromClassList("primary-button");
            root.Q<Label>("upgradeHint").text = "강화 버튼 길게 누르기 · 목록을 위로 밀어 더 보기";
        }
        private void ShowCommander()
        {
            foreach (var hold in holds) hold.Cancel();
            root.Q("maintenanceScroll").AddToClassList("hidden");
            root.Q("commanderSettings").RemoveFromClassList("hidden");
            root.Q<Button>("maintenanceTab").RemoveFromClassList("primary-button");
            root.Q<Button>("commanderTab").AddToClassList("primary-button");
            root.Q<Label>("upgradeHint").text = "슬롯 선택 → 스킬 장착 · 재사용 대기 유지";
            commanderController.RefreshSettings();
        }
        private void OpenCommander()
        {
            recruitmentScreen.Close();
            panelManager.ClickPanelButton(PanelType.Upgrade);
            ShowCommander();
        }
        private bool TryUpgrade(MainShipUpgradeType type)
        {
            bool success = upgradeManager.TryUpgrade(type);
            VisualElement row = rows[type];
            row.EnableInClassList("is-success", success);
            row.EnableInClassList("is-failure", !success);
            if (!success) row.Q<Label>("actionLabel").text = "강화 불가";
            if (flashes.TryGetValue(type, out var previous)) previous.Pause();
            flashes[type] = row.schedule.Execute(() =>
            {
                row.RemoveFromClassList("is-success"); row.RemoveFromClassList("is-failure"); RefreshRow(type, row);
            }).StartingIn((long)(feedbackSeconds * 1000));
            return success;
        }
        private void OnUpgradeChanged(UpgradeEvent evt) => Refresh();
        private void OnCurrencyChanged(CurrencyChangedEvent evt) => Refresh();
        private void Refresh()
        {
            if (upgradeManager == null) return;
            foreach (var row in rows) RefreshRow(row.Key, row.Value);
        }
        private void RefreshRow(MainShipUpgradeType type, VisualElement row)
        {
            UpgradeGrowthData growth = upgradeManager.GetTargetUpdateData(type);
            Button action = row.Q<Button>("upgradeAction");
            if (growth == null)
            {
                action.SetEnabled(false); row.Q<Label>("actionLabel").text = "설정 없음";
                return;
            }
            bool canAfford = growth.CurrentCost > 0 && currencyManager.GetCurrency(growth.currencyType) >= growth.CurrentCost;
            action.SetEnabled(canAfford);
            row.Q<Label>("actionLabel").text = canAfford ? "강화 +" : "재화 부족";
            row.Q<Label>("level").text = $"Lv. {growth.level}";
            row.Q<Label>("cost").text = $"{CurrencyTextFormatter.Format(growth.CurrentCost)} {CurrencyTextFormatter.FormatName(growth.currencyType)}";
            MainShipUpgradeData data = upgradeManager.MainShipUpgradeData;
            float current = type switch
            {
                MainShipUpgradeType.Health => data.health,
                MainShipUpgradeType.AttackPowerPercent => data.attackPowerPercent,
                MainShipUpgradeType.AttackSpeedPercent => data.attackSpeedPercent,
                MainShipUpgradeType.CargoCapacity => data.cargoCapacity,
                MainShipUpgradeType.Luck => data.luck,
                MainShipUpgradeType.CommanderGaugeMax => data.commanderGaugeMax,
                MainShipUpgradeType.CommanderGaugeRecoveryPerSecond => data.commanderGaugeRecoveryPerSecond,
                MainShipUpgradeType.HealthRecoveryPerSecond => data.healthRecoveryPerSecond,
                _ => 0
            };
            bool isPercent = type == MainShipUpgradeType.AttackPowerPercent || type == MainShipUpgradeType.AttackSpeedPercent || type == MainShipUpgradeType.Luck;
            bool isPerSecond = type == MainShipUpgradeType.CommanderGaugeRecoveryPerSecond || type == MainShipUpgradeType.HealthRecoveryPerSecond;
            bool isAdditional = type == MainShipUpgradeType.AttackPowerPercent || type == MainShipUpgradeType.AttackSpeedPercent || type == MainShipUpgradeType.HealthRecoveryPerSecond;
            string unit = isPercent ? "%" : isPerSecond ? "/초" : "";
            row.Q<Label>("effect").text = $"{(isAdditional ? "추가 " : "")}{current:0.##}{unit} → {current + growth.IncreaseValue:0.##}{unit}";
        }
    }
}