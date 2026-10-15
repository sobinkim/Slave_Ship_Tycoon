using System.Collections.Generic;
using System.Text;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.Scripts.Fleet;
using SB.Scripts.Visual;
using SB.UI.Recruitment;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.Fleet
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class FleetToolkitScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument document;
        [SerializeField] private PlayerFleetManager fleetManager;
        [SerializeField] private PannelManager panelManager;
        [SerializeField] private RecruitmentToolkitScreen recruitmentScreen;
        [SerializeField] private FleetMergeToolkitScreen mergeScreen;
        [SerializeField] private VisualTreeAsset cardTemplate;
        [SerializeField] private Sprite mainShipIcon;
        [Header("Enemy attack preview")]
        [SerializeField, Min(1), Tooltip("공격 대상 미리보기의 열당 표시 수. 실제 적 배치 규칙은 변경하지 않습니다.")]
        private int enemyPreviewRows = 4;

        private readonly List<Button> slots = new List<Button>();
        private readonly List<VisualElement> enemyColumns = new List<VisualElement>();
        private readonly Dictionary<EscortShipData, Button> inventoryCards = new Dictionary<EscortShipData, Button>();
        private VisualElement screen;
        private VisualElement inventoryGrid;
        private Label statusLabel;
        private EscortShipData selectedEscort;
        private int selectedSlotIndex = -1;
        private float previousTimeScale;
        private bool hasPaused;

        private void OnEnable()
        {
            if (document == null || fleetManager == null || cardTemplate == null || panelManager == null)
            {
                Debug.LogError("FleetToolkitScreen의 Document, Fleet Manager, Panel Manager, Card Template을 연결하세요.", this);
                return;
            }
            recruitmentScreen?.Close();
            screen = document.rootVisualElement;
            screen.AddToClassList("harbor-document");
            screen.pickingMode = PickingMode.Ignore;
            inventoryGrid = screen.Q("inventoryGrid");
            statusLabel = screen.Q<Label>("statusLabel");
            inventoryCards.Clear();
            slots.Clear();
            enemyColumns.Clear();
            BuildFormation();
            screen.Q<Button>("closeButton").clicked += Close;
            screen.Q<Button>("removeAllButton").clicked += RemoveAllEscorts;
            screen.Q<Button>("mergeButton").clicked += OpenMerge;
            screen.Q<Button>("recruitButton").clicked += OpenRecruitment;
            Bus<EscortOwnershipChangedEvent>.OnEvent += OnOwnershipChanged;
            Bus<PlayerFleetFormationChangedEvent>.OnEvent += OnFormationChanged;
            selectedEscort = null;
            selectedSlotIndex = -1;
            statusLabel.text = string.Empty;
            if (Application.isPlaying)
            {
                previousTimeScale = Time.timeScale;
                hasPaused = true;
                Time.timeScale = 0f;
            }
            Refresh();
        }

        private void OnDisable()
        {
            Bus<EscortOwnershipChangedEvent>.OnEvent -= OnOwnershipChanged;
            Bus<PlayerFleetFormationChangedEvent>.OnEvent -= OnFormationChanged;
            mergeScreen?.Close();
            if (screen != null)
            {
                if (screen.Q<Button>("closeButton") is Button closeButtonBinding) closeButtonBinding.clicked -= Close;
                if (screen.Q<Button>("removeAllButton") is Button removeAllButtonBinding) removeAllButtonBinding.clicked -= RemoveAllEscorts;
                if (screen.Q<Button>("mergeButton") is Button mergeButtonBinding) mergeButtonBinding.clicked -= OpenMerge;
                if (screen.Q<Button>("recruitButton") is Button recruitButtonBinding) recruitButtonBinding.clicked -= OpenRecruitment;
            }
            if (hasPaused)
            {
                Time.timeScale = previousTimeScale;
                hasPaused = false;
            }
        }

        private void BuildFormation()
        {
            VisualElement playerRoot = screen.Q("playerColumns");
            VisualElement enemyRoot = screen.Q("enemyColumns");
            playerRoot.Clear();
            enemyRoot.Clear();
            for (int i = 0; i < fleetManager.FormationSlotCount; i++) slots.Add(null);
            screen.Q<Label>("playerTitle").text = $"아군 배치 · 호위선 {fleetManager.FormationSlotCount}칸";

            for (int columnIndex = 0; columnIndex < PlayerFleetLoadout.ColumnCount; columnIndex++)
            {
                var column = new VisualElement();
                column.AddToClassList("formation-column");
                var title = new Label(PlayerFleetLoadout.GetColumnLabel(columnIndex));
                title.AddToClassList("column-title");
                column.Add(title);
                var cells = new VisualElement();
                cells.AddToClassList("formation-cells");
                column.Add(cells);
                playerRoot.Add(column);
                for (int rowIndex = 0; rowIndex < PlayerFleetLoadout.RowCount; rowIndex++)
                {
                    int slotIndex = PlayerFleetLoadout.GetEscortSlotIndex(columnIndex, rowIndex);
                    var slot = new Button();
                    slot.AddToClassList("formation-slot");
                    var icon = new VisualElement { name = "shipIcon", pickingMode = PickingMode.Ignore };
                    icon.AddToClassList("ship-icon");
                    slot.Add(icon);
                    var nameLabel = new Label { name = "shipName", pickingMode = PickingMode.Ignore };
                    nameLabel.AddToClassList("ship-name");
                    slot.Add(nameLabel);
                    var numberLabel = new Label { name = "shipCount", pickingMode = PickingMode.Ignore };
                    numberLabel.AddToClassList("ship-count");
                    slot.Add(numberLabel);
                    cells.Add(slot);
                    if (slotIndex < 0)
                    {
                        slot.AddToClassList("main-ship-slot");
                        ShipCardPresentation.SetIcon(icon, mainShipIcon);
                        nameLabel.text = "메인 함선";
                        numberLabel.text = "고정";
                        slot.focusable = false;
                        slot.pickingMode = PickingMode.Ignore;
                    }
                    else
                    {
                        slots[slotIndex] = slot;
                        int capturedIndex = slotIndex;
                        slot.clicked += () => SelectSlot(capturedIndex);
                    }
                }

                var enemy = new VisualElement();
                enemy.AddToClassList("formation-column");
                var enemyTitle = new Label(PlayerFleetLoadout.GetColumnLabel(PlayerFleetLoadout.ColumnCount - 1 - columnIndex));
                enemyTitle.AddToClassList("column-title");
                enemy.Add(enemyTitle);
                var enemyCells = new VisualElement();
                enemyCells.AddToClassList("formation-cells");
                enemyCells.AddToClassList("enemy-column");
                enemy.Add(enemyCells);
                for (int rowIndex = 0; rowIndex < enemyPreviewRows; rowIndex++)
                {
                    var marker = new Label("◆");
                    marker.AddToClassList("enemy-marker");
                    enemyCells.Add(marker);
                }
                enemyRoot.Add(enemy);
                enemyColumns.Add(enemyCells);
            }
        }

        private void OnOwnershipChanged(EscortOwnershipChangedEvent evt) => Refresh();
        private void OnFormationChanged(PlayerFleetFormationChangedEvent evt) => Refresh();

        private void SelectEscort(EscortShipData ship)
        {
            selectedEscort = selectedEscort == ship ? null : ship;
            if (selectedEscort != null && selectedSlotIndex >= 0)
            {
                PlaceSelected(selectedSlotIndex);
                return;
            }
            statusLabel.text = selectedEscort != null ? "배치할 아군 슬롯을 선택하세요." : string.Empty;
            Refresh();
        }

        private void SelectSlot(int slotIndex)
        {
            if (selectedEscort != null) { PlaceSelected(slotIndex); return; }
            selectedSlotIndex = selectedSlotIndex == slotIndex ? -1 : slotIndex;
            statusLabel.text = selectedSlotIndex >= 0 ? $"{slotIndex + 1}번 슬롯 · 보유 함선을 누르면 배치됩니다." : string.Empty;
            Refresh();
        }

        private void PlaceSelected(int slotIndex)
        {
            EscortShipData ship = selectedEscort;
            if (!fleetManager.TryPlaceEscort(slotIndex, ship, out EscortFormationFailureReason failure))
            {
                statusLabel.text = failure == EscortFormationFailureReason.NotEnoughCopies
                    ? "미편성 함선이 부족합니다." : "이 함선을 배치할 수 없습니다.";
                Refresh();
                return;
            }
            selectedEscort = null;
            selectedSlotIndex = -1;
            statusLabel.text = $"{ship.DisplayName} · {slotIndex + 1}번 슬롯 배치\n다음 전투부터 적용됩니다.";
            Refresh();
        }

        private void RemoveAllEscorts()
        {
            for (int slotIndex = 0; slotIndex < fleetManager.FormationSlotCount; slotIndex++)
                fleetManager.TryRemoveEscort(slotIndex);
            selectedSlotIndex = -1;
            selectedEscort = null;
            statusLabel.text = "호위선 전체 편성 해제 · 다음 전투부터 적용됩니다.";
            Refresh();
        }

        private void Refresh()
        {
            if (screen == null || fleetManager == null) return;
            if (selectedEscort != null && fleetManager.GetOwnedCount(selectedEscort) <= 0) selectedEscort = null;
            for (int i = 0; i < slots.Count; i++)
            {
                EscortShipData ship = fleetManager.GetFormationAt(i);
                ShipCardPresentation.SetIcon(slots[i].Q("shipIcon"), ship != null ? ship.Icon : null);
                slots[i].Q<Label>("shipName").text = ship != null ? ship.DisplayName : "비어 있음";
                slots[i].Q<Label>("shipCount").text = $"{i + 1}";
                slots[i].EnableInClassList("is-selected", selectedSlotIndex == i);
            }
            var snapshots = fleetManager.GetOwnershipSnapshot();
            foreach (var card in inventoryCards.Values) card.AddToClassList("hidden");
            foreach (var snapshot in snapshots)
            {
                EscortShipData ship = snapshot.ShipData;
                if (!inventoryCards.TryGetValue(ship, out Button card))
                {
                    card = ShipCardPresentation.Create(cardTemplate, inventoryGrid);
                    card.clicked += () => SelectEscort(ship);
                    inventoryCards.Add(ship, card);
                }
                card.RemoveFromClassList("hidden");
                ShipCardPresentation.Bind(card, ship, $"대기 {snapshot.UnequippedCount:N0} / 보유 {snapshot.OwnedCount:N0}", selectedEscort == ship);
            }
            screen.Q<Label>("inventoryTitle").text = $"보유 함선 · {snapshots.Length}종";
            bool hasEscorts = false;
            for (int i = 0; i < fleetManager.FormationSlotCount; i++)
                hasEscorts |= fleetManager.GetFormationAt(i) != null;
            screen.Q<Button>("removeAllButton").SetEnabled(hasEscorts);
            RefreshAttackPreview(selectedSlotIndex >= 0 ? fleetManager.GetFormationAt(selectedSlotIndex) : selectedEscort);
        }

        private void RefreshAttackPreview(EscortShipData ship)
        {
            foreach (VisualElement column in enemyColumns) column.RemoveFromClassList("is-target");
            Label nameLabel = screen.Q<Label>("attackName");
            Label descriptionLabel = screen.Q<Label>("attackDescription");
            if (ship == null || ship.ShipPrefab == null)
            {
                nameLabel.text = selectedSlotIndex >= 0 ? "빈 슬롯" : "함선을 선택하세요";
                descriptionLabel.text = "배치된 함선을 선택하면 공격 우선순위를 확인할 수 있습니다.";
                return;
            }
            nameLabel.text = ship.DisplayName + (selectedSlotIndex >= 0 ? " · " + PlayerFleetLoadout.GetFormationPositionLabel(selectedSlotIndex) : "");
            var interceptor = ship.ShipPrefab.GetComponentInChildren<InterceptorAttackCompo>(true);
            if (interceptor != null)
            {
                descriptionLabel.text = "같은 가로 라인의 아군과 본선 보호 · 먼저 도착할 적 포탄 우선\n빠른 직선탄으로 한 발씩 요격 · 연막탄·회복탄 제외";
                return;
            }
            var healing = ship.ShipPrefab.GetComponentInChildren<HealingProjectileAttackCompo>(true);
            if (healing != null)
            {
                descriptionLabel.text = $"아군 전열 → 중열 → 후열 · 손상된 아군 무작위 회복 (회복선 제외)\nHP {healing.HealAmount:0.#} 회복 · 기본 발사 간격의 {healing.AttackIntervalMultiplier:0.#}배";
                return;
            }
            var selector = ship.ShipPrefab.GetComponentInChildren<TargetSelector>(true);
            var rules = selector != null ? selector.TargetRules : null;
            TargetLine primary = rules != null && rules.Count > 0 ? rules[0].Line : TargetLine.Any;
            for (int i = 0; i < enemyColumns.Count; i++)
                enemyColumns[i].EnableInClassList("is-target", primary == TargetLine.Any || (int)primary == i);
            var text = new StringBuilder();
            if (ship.ShipPrefab.GetComponentInChildren<ArcProjectileAttackCompo>(true) != null) text.Append("단일 대상 · 포탄 공격\n");
            if (rules == null || rules.Count == 0) text.Append("전열 제한 없이 생존 적 선택");
            else
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    if (i > 0) text.Append(" → ");
                    text.Append(LineName(rules[i].Line));
                    if (rules[i].Role != ShipRole.None) text.Append(" / " + RoleName(rules[i].Role));
                }
                text.Append("\n조건에 맞는 적이 없으면 다른 생존 적 공격");
            }
            if (selector != null && selector.SelectionMode == TargetSelectionMode.HighestCurrentHealth)
                text.Append("\n현재 체력이 높은 함선 우선 · 동률이면 가까운 함선");
            descriptionLabel.text = text.ToString();
        }

        private static string LineName(TargetLine line) => line switch
        {
            TargetLine.Front => "전열", TargetLine.Middle => "중열", TargetLine.Back => "후열", _ => "전체 전열"
        };
        private static string RoleName(ShipRole role) => role.ToString()
            .Replace("Tanker", "방어형").Replace("Dealer", "공격형").Replace("Support", "지원형")
            .Replace("Speedster", "속공형").Replace("AoE", "범위형").Replace("Piercer", "관통형");

        public void Close() => panelManager.CloseFleet();
        private void OpenRecruitment()
        {
            Close();
            recruitmentScreen.Open();
        }
        private void OpenMerge()
        {
            screen.Q(className: "screen").AddToClassList("hidden");
            mergeScreen.Open();
        }
        public void ReturnFromMerge()
        {
            if (!isActiveAndEnabled || screen == null) return;
            screen.Q(className: "screen").RemoveFromClassList("hidden");
            Refresh();
        }
    }
}


