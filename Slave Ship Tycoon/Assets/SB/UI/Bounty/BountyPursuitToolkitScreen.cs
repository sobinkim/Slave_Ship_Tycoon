using SB.Scripts;
using SB.Scripts.Bounty;
using SB.Scripts.Visual;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.Bounty
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class BountyPursuitToolkitScreen : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private UIDocument entryDocument;
        [SerializeField] private BountyPursuitController pursuit;
        [SerializeField] private PannelManager panelManager;
        [SerializeField] private Sprite mainPiratePortrait;
        private VisualElement root;
        private VisualElement mainRoot;
        private Button entry;
        private bool wasActive;
        private bool editingFormation;
        private float availableAt;
        private DisplayStyle previousBossDisplay;
        private DisplayStyle previousBountyDisplay;

        private void OnEnable()
        {
            if (document == null || entryDocument == null || pursuit == null || panelManager == null)
            { Debug.LogError("현상금 UI의 Document, Pursuit, Panel Manager를 연결하세요.", this); return; }
            root = document.rootVisualElement;
            mainRoot = entryDocument.rootVisualElement;
            root.Q("pursuitRoot").pickingMode = PickingMode.Ignore;
            root.Q("pursuitRoot").parent.pickingMode = PickingMode.Ignore;
            entry = mainRoot.Q<Button>("bountyButton");
            availableAt = Time.unscaledTime + .35f;
            entry.clicked += Enter;
            root.Q<Button>("pursuitRetreat").clicked += pursuit.Retreat;
            root.Q<Button>("pursuitRetry").clicked += Retry;
            root.Q<Button>("pursuitQuickRetry").clicked += Retry;
            root.Q<Button>("pursuitFormation").clicked += EditFormation;
            root.Q<Button>("pursuitNext").clicked += Next;
            root.Q<Button>("pursuitReturn").clicked += Return;
            root.Q("mainPiratePortrait").style.backgroundImage = new StyleBackground(mainPiratePortrait);
            root.Q("escapeShip").style.backgroundImage = new StyleBackground(mainPiratePortrait);
            pursuit.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (root == null) return;
            pursuit.Changed -= Refresh;
            entry.clicked -= Enter;
            root.Q<Button>("pursuitRetreat").clicked -= pursuit.Retreat;
            root.Q<Button>("pursuitRetry").clicked -= Retry;
            root.Q<Button>("pursuitQuickRetry").clicked -= Retry;
            root.Q<Button>("pursuitFormation").clicked -= EditFormation;
            root.Q<Button>("pursuitNext").clicked -= Next;
            root.Q<Button>("pursuitReturn").clicked -= Return;
            RestoreEntries();
            root = null;
        }

        private void Enter() { if (Time.unscaledTime >= availableAt) pursuit.EnterDefault(); }
        private void Retry() { editingFormation = false; panelManager.ClickPanelButton(PanelType.Upgrade); pursuit.Retry(); Refresh(); }
        private void Next() { editingFormation = false; pursuit.TryNextConvoy(); Refresh(); }
        private void Return() { editingFormation = false; pursuit.ReturnToFarming(); }
        private void EditFormation()
        {
            editingFormation = true;
            root.Q("pursuitResult").AddToClassList("pursuit-hidden");
            panelManager.ClickPanelButton(PanelType.Fleet);
            Refresh();
        }

        private void RestoreEntries()
        {
            if (!wasActive || root == null) return;
            mainRoot.Q("bossEntry").style.display = previousBossDisplay;
            mainRoot.Q("bountyEntry").style.display = previousBountyDisplay;
            wasActive = false;
        }

        private void Refresh()
        {
            if (root == null) return;
            bool active = pursuit.IsActive;
            if (active && !wasActive)
            {
                previousBossDisplay = mainRoot.Q("bossEntry").resolvedStyle.display;
                previousBountyDisplay = mainRoot.Q("bountyEntry").resolvedStyle.display;
                mainRoot.Q("bossEntry").style.display = DisplayStyle.None;
                mainRoot.Q("bountyEntry").style.display = DisplayStyle.None;
                wasActive = true;
            }
            if (!active) RestoreEntries();
            root.Q("pursuitRoot").EnableInClassList("pursuit-hidden", !active);
            bool result = pursuit.State == PursuitState.Result;
            root.Q("pursuitResult").EnableInClassList("pursuit-hidden", !result || editingFormation);
            root.Q("pursuitQuickRetry").EnableInClassList("pursuit-hidden", !result || !editingFormation);
            root.Q<Button>("pursuitRetreat").SetEnabled(pursuit.State == PursuitState.Chasing);
            if (!active) return;
            root.Q<Label>("pursuitTitle").text = "현상금 추적 · " + pursuit.Definition.DisplayName;
            if (!result) return;
            bool won = pursuit.Result == PursuitResult.Success;
            root.Q<Label>("pursuitResultTitle").text = won ? "메인 해적선 격침!" : pursuit.Result == PursuitResult.Escaped ? "해적선 도주" : pursuit.Result == PursuitResult.MainShipLost ? "본함 침몰" : "추적 중단";
            root.Q<Label>("pursuitResultDetail").text = won ? $"목적지 도착 {Mathf.CeilToInt(pursuit.RemainingSeconds)}초 전 격침" : $"메인 해적선 잔여 체력 {Mathf.CeilToInt(pursuit.TargetHealthRatio * 100)}%\n함선을 다시 편성하고 도전하세요";
            root.Q<Label>("pursuitReward").text = won ? $"현상금 · 다이아 {pursuit.Definition.DiamondReward:N0}" : "";
            root.Q("pursuitNext").EnableInClassList("pursuit-hidden", pursuit.PursuitCount < 2);
        }

        private void Update()
        {
            if (root == null) return;
            entry.SetEnabled(pursuit.CanEnter);
            if (!pursuit.IsActive) return;
            int seconds = Mathf.CeilToInt(pursuit.RemainingSeconds);
            root.Q<Label>("escapeTime").text = pursuit.State == PursuitState.Entering ? "추적 준비" : $"도주까지 {seconds / 60:00}:{seconds % 60:00}";
            root.Q("escapeFill").style.width = Length.Percent(pursuit.Progress * 100);
            float width = root.Q("escapeFill").parent.resolvedStyle.width;
            root.Q("escapeShip").style.left = Mathf.Max(0, width - 58) * pursuit.Progress;
            root.Q("pursuitHud").EnableInClassList("pursuit-danger", seconds <= 10);
            root.Q<Label>("pursuitHint").text = pursuit.State == PursuitState.Entering ? "해적 함대 발견 · 함대 진입 중" : $"메인 해적선 HP {Mathf.CeilToInt(pursuit.TargetHealthRatio * 100)}% · 목적지 도착 전 격침";
        }
    }
}
