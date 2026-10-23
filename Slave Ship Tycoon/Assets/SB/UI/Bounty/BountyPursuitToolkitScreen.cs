using SB.Scripts.Bounty;
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
        private VisualElement root;
        private VisualElement mainRoot;
        private VisualElement hud;
        private VisualElement fill;
        private Label title;
        private Label time;
        private Label hint;
        private Button entry;
        private Button retreat;
        private bool wasActive;
        private float availableAt;
        private DisplayStyle previousBossDisplay;
        private DisplayStyle previousBountyDisplay;

        private void OnEnable()
        {
            if (document == null || entryDocument == null || pursuit == null)
            { Debug.LogError("현상금 UI의 Document, Entry Document, Pursuit를 연결하세요.", this); return; }
            root = document.rootVisualElement;
            mainRoot = entryDocument.rootVisualElement;
            root.Q("pursuitRoot").pickingMode = PickingMode.Ignore;
            root.Q("pursuitRoot").parent.pickingMode = PickingMode.Ignore;
            hud = root.Q("pursuitHud");
            fill = root.Q("escapeFill");
            title = root.Q<Label>("pursuitTitle");
            time = root.Q<Label>("escapeTime");
            hint = root.Q<Label>("pursuitHint");
            entry = mainRoot.Q<Button>("bountyButton");
            retreat = root.Q<Button>("pursuitRetreat");
            availableAt = Time.unscaledTime + .35f;
            entry.clicked += Enter;
            retreat.clicked += pursuit.Retreat;
            pursuit.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (root == null) return;
            pursuit.Changed -= Refresh;
            entry.clicked -= Enter;
            retreat.clicked -= pursuit.Retreat;
            RestoreEntries();
            root = null;
        }

        private void Enter() { if (Time.unscaledTime >= availableAt) pursuit.EnterDefault(); }

        private void RestoreEntries()
        {
            if (!wasActive) return;
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
            retreat.SetEnabled(active);
            if (active) RefreshProgress();
        }

        private void Update()
        {
            if (root == null) return;
            entry.SetEnabled(pursuit.CanEnter);
            if (pursuit.IsActive) RefreshProgress();
        }

        private void RefreshProgress()
        {
            title.text = "현상금 추적";
            fill.style.width = Length.Percent(pursuit.Progress * 100);
            if (pursuit.State == PursuitState.Result)
            {
                bool won = pursuit.Result == PursuitResult.Success;
                time.text = "해역 복귀 중";
                hint.text = won ? $"메인 해적선 격침 · 다이아 {pursuit.Definition.DiamondReward:N0} 획득"
                    : pursuit.Result == PursuitResult.Escaped ? "해적선 도주 · 원래 해역으로 복귀합니다"
                    : "본함 침몰 · 원래 해역으로 복귀합니다";
                return;
            }
            int seconds = Mathf.CeilToInt(pursuit.RemainingSeconds);
            time.text = pursuit.State == PursuitState.Entering ? "추적 시작" : $"도주까지 {seconds / 60:00}:{seconds % 60:00}";
            hint.text = pursuit.State == PursuitState.Entering ? "해적 함대에 접근 중"
                : $"메인 해적선 HP {Mathf.CeilToInt(pursuit.TargetHealthRatio * 100)}% · 목적지 도착 전 격침";
        }
    }
}
