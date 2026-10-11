using System.Collections;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Fleet;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.Fleet
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class FleetMergeToolkitScreen : MonoBehaviour
    {
        private readonly struct Material
        {
            public readonly EscortShipData Ship;
            public readonly int CopyIndex;
            public Material(EscortShipData ship, int copyIndex) { Ship = ship; CopyIndex = copyIndex; }
        }

        [Header("References")]
        [SerializeField] private UIDocument document;
        [SerializeField] private PlayerFleetManager fleetManager;
        [SerializeField] private FleetToolkitScreen fleetScreen;
        [SerializeField] private VisualTreeAsset cardTemplate;
        [Header("Result presentation")]
        [SerializeField, Min(0.01f)] private float revealSeconds = 0.25f;
        [SerializeField, Range(0.8f, 1f)] private float entryScale = 0.94f;

        private readonly List<Material> selected = new List<Material>();
        private readonly List<Button> materialSlots = new List<Button>();
        private VisualElement root;
        private VisualElement resultPanel;
        private Label statusLabel;
        private Coroutine revealRoutine;
        private bool isCommitting;
        private bool isResultOpen;

        public void Open() => gameObject.SetActive(true);
        public void Close()
        {
            gameObject.SetActive(false);
            fleetScreen?.ReturnFromMerge();
        }

        private void OnEnable()
        {
            if (document == null || fleetManager == null || fleetScreen == null || cardTemplate == null)
            {
                Debug.LogError("FleetMergeToolkitScreen의 Document, Fleet Manager, Fleet Screen, Card Template을 연결하세요.", this);
                return;
            }
            root = document.rootVisualElement;
            root.AddToClassList("harbor-document");
            resultPanel = root.Q("resultPanel");
            statusLabel = root.Q<Label>("statusLabel");
            resultPanel.AddToClassList("hidden");
            selected.Clear();
            isResultOpen = false;
            materialSlots.Clear();
            VisualElement slotsRoot = root.Q("materialSlots");
            slotsRoot.Clear();
            for (int i = 0; i < PlayerFleetManager.MergeMaterialCount; i++)
            {
                Button slot = ShipCardPresentation.Create(cardTemplate, slotsRoot);
                int index = i;
                slot.clicked += () => RemoveMaterial(index);
                materialSlots.Add(slot);
            }
            root.Q<Button>("closeButton").clicked += Close;
            root.Q<Button>("confirmButton").clicked += Confirm;
            root.Q<Button>("mergeAllButton").clicked += MergeAll;
            root.Q<Button>("resultCloseButton").clicked += CloseResult;
            Bus<EscortOwnershipChangedEvent>.OnEvent += OnOwnershipChanged;
            Bus<PlayerFleetFormationChangedEvent>.OnEvent += OnFormationChanged;
            Refresh();
        }

        private void OnDisable()
        {
            Bus<EscortOwnershipChangedEvent>.OnEvent -= OnOwnershipChanged;
            Bus<PlayerFleetFormationChangedEvent>.OnEvent -= OnFormationChanged;
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            selected.Clear();
            isCommitting = false;
            isResultOpen = false;
            if (root == null) return;
            if (root.Q<Button>("closeButton") is Button closeButtonBinding) closeButtonBinding.clicked -= Close;
            if (root.Q<Button>("confirmButton") is Button confirmButtonBinding) confirmButtonBinding.clicked -= Confirm;
            if (root.Q<Button>("mergeAllButton") is Button mergeAllButtonBinding) mergeAllButtonBinding.clicked -= MergeAll;
            if (root.Q<Button>("resultCloseButton") is Button resultCloseButtonBinding) resultCloseButtonBinding.clicked -= CloseResult;
        }

        private void OnOwnershipChanged(EscortOwnershipChangedEvent evt) { if (!isCommitting) Refresh(); }
        private void OnFormationChanged(PlayerFleetFormationChangedEvent evt) { if (!isCommitting) Refresh(); }

        private int SelectedIndex(EscortShipData ship, int copyIndex)
        {
            return selected.FindIndex(material => material.Ship == ship && material.CopyIndex == copyIndex);
        }

        private void SelectMaterial(EscortShipData ship, int copyIndex)
        {
            if (isCommitting || isResultOpen) return;
            int index = SelectedIndex(ship, copyIndex);
            if (index >= 0) { RemoveMaterial(index); return; }
            if (copyIndex >= fleetManager.GetUnequippedCount(ship) ||
                selected.Count >= PlayerFleetManager.MergeMaterialCount ||
                ship.Grade == EscortShipGrade.Legendary ||
                selected.Count > 0 && selected[0].Ship.Grade != ship.Grade) return;
            selected.Add(new Material(ship, copyIndex));
            Refresh();
        }

        private void RemoveMaterial(int index)
        {
            if (index >= selected.Count || isCommitting || isResultOpen) return;
            selected.RemoveAt(index);
            Refresh();
        }

        private void Refresh()
        {
            if (root == null) return;
            selected.RemoveAll(material => material.CopyIndex >= fleetManager.GetUnequippedCount(material.Ship));
            VisualElement grid = root.Q("inventoryGrid");
            grid.Clear();
            foreach (EscortOwnershipSnapshot snapshot in fleetManager.GetOwnershipSnapshot())
            {
                EscortShipData ship = snapshot.ShipData;
                for (int i = 0; i < snapshot.OwnedCount; i++)
                {
                    int copyIndex = i;
                    bool isSelected = SelectedIndex(ship, copyIndex) >= 0;
                    bool available = copyIndex < snapshot.UnequippedCount;
                    bool canSelect = isSelected || available && selected.Count < PlayerFleetManager.MergeMaterialCount &&
                        ship.Grade != EscortShipGrade.Legendary && (selected.Count == 0 || selected[0].Ship.Grade == ship.Grade);
                    Button card = ShipCardPresentation.Create(cardTemplate, grid);
                    ShipCardPresentation.Bind(card, ship, isSelected ? "선택됨" : available ? "대기" : "편성됨", isSelected);
                    card.SetEnabled(canSelect);
                    card.clicked += () => SelectMaterial(ship, copyIndex);
                }
            }
            EscortShipData source = selected.Count > 0 ? selected[0].Ship : null;
            fleetManager.TryGetMergePreview(source, out EscortShipData target);
            ShipCardPresentation.SetIcon(root.Q("sourceIcon"), source != null ? source.Icon : null);
            ShipCardPresentation.SetIcon(root.Q("targetIcon"), target != null ? target.Icon : null);
            root.Q<Label>("sourceLabel").text = source != null ? ShipCardPresentation.GradeName(source.Grade) : "재료 선택";
            root.Q<Label>("targetLabel").text = target != null ? ShipCardPresentation.GradeName(target.Grade) : "상위 등급";
            for (int i = 0; i < materialSlots.Count; i++)
            {
                EscortShipData ship = i < selected.Count ? selected[i].Ship : null;
                ShipCardPresentation.Bind(materialSlots[i], ship, ship != null ? "눌러서 해제" : $"{i + 1}번 재료", ship != null);
                materialSlots[i].SetEnabled(ship != null);
            }
            statusLabel.text = $"같은 등급의 미편성 함선 {PlayerFleetManager.MergeMaterialCount}척 필요 ({selected.Count}/{PlayerFleetManager.MergeMaterialCount})\n선택한 재료를 누르면 해제됩니다.";
            root.Q<Button>("confirmButton").SetEnabled(!isCommitting && !isResultOpen && selected.Count == PlayerFleetManager.MergeMaterialCount);
            root.Q<Button>("mergeAllButton").SetEnabled(!isCommitting && !isResultOpen);
        }

        private void Confirm()
        {
            if (isCommitting || isResultOpen || selected.Count != PlayerFleetManager.MergeMaterialCount) return;
            isCommitting = true;
            try
            {
                var ships = new List<EscortShipData>();
                foreach (Material material in selected) ships.Add(material.Ship);
                bool success = fleetManager.TryMergeSelected(ships, out EscortShipData reward);
                selected.Clear();
                Refresh();
                if (success) ShowResult(1, new[] { new KeyValuePair<EscortShipData, int>(reward, 1) });
                else statusLabel.text = "재료가 변경됐어요. 다시 선택해주세요.";
            }
            finally
            {
                isCommitting = false;
                root.Q<Button>("confirmButton").SetEnabled(false);
                root.Q<Button>("mergeAllButton").SetEnabled(!isResultOpen);
            }
        }

        private void MergeAll()
        {
            if (isCommitting || isResultOpen) return;
            isCommitting = true;
            try
            {
                long count = fleetManager.MergeAllUnequipped(out var rewards);
                selected.Clear();
                Refresh();
                if (count > 0) ShowResult(count, rewards);
                else statusLabel.text = "합성 가능한 미편성 함선이 없어요.\n같은 등급 3척이 필요하며 최고 등급은 제외됩니다.";
            }
            finally
            {
                isCommitting = false;
                root.Q<Button>("mergeAllButton").SetEnabled(!isResultOpen);
            }
        }

        private void ShowResult(long count, IReadOnlyList<KeyValuePair<EscortShipData, int>> rewards)
        {
            isResultOpen = true;
            VisualElement grid = root.Q("resultGrid");
            grid.Clear();
            foreach (var reward in rewards)
            {
                Button card = ShipCardPresentation.Create(cardTemplate, grid);
                ShipCardPresentation.Bind(card, reward.Key, $"+{reward.Value:N0}척");
                card.pickingMode = PickingMode.Ignore;
                card.focusable = false;
            }
            root.Q<Label>("resultSummary").text = $"합성 {count:N0}회 완료 · 편성 함선 유지\n획득한 함선은 미편성 상태입니다.";
            resultPanel.RemoveFromClassList("hidden");
            root.Q<Button>("confirmButton").SetEnabled(false);
            root.Q<Button>("mergeAllButton").SetEnabled(false);
            revealRoutine = StartCoroutine(RevealResult());
        }

        private IEnumerator RevealResult()
        {
            float elapsed = 0f;
            while (elapsed < revealSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / revealSeconds);
                resultPanel.style.opacity = progress;
                resultPanel.style.scale = new Scale(Vector3.one * Mathf.Lerp(entryScale, 1f, progress));
                yield return null;
            }
            resultPanel.style.opacity = 1f;
            resultPanel.style.scale = new Scale(Vector3.one);
            revealRoutine = null;
        }

        private void CloseResult()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            resultPanel.AddToClassList("hidden");
            isResultOpen = false;
            Refresh();
        }
    }
}

