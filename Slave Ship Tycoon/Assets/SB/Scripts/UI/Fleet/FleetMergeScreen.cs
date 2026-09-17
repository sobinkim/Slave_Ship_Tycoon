using System.Collections;
using System.Collections.Generic;
using SB.Scripts.Fleet;
using TMPro;
using UnityEngine;

namespace SB.Scripts.UI.Fleet
{
    public sealed class FleetMergeScreen : MonoBehaviour
    {
        [SerializeField] private PlayerFleetManager fleetManager;
        [SerializeField] private FleetMergeResultView resultView;
        [SerializeField] private EscortInventoryItemView cardPrefab;
        [SerializeField] private Transform inventoryRoot;
        [SerializeField] private UnityEngine.UI.Image sourceIcon;
        [SerializeField] private UnityEngine.UI.Image resultIcon;
        [SerializeField] private TMP_Text sourceLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private UnityEngine.UI.Image[] materialIcons;
        [SerializeField] private TMP_Text[] materialLabels;
        [SerializeField] private UnityEngine.UI.Button confirmButton;
        private readonly List<EscortShipData> selected = new List<EscortShipData>();
        private readonly List<EscortInventoryItemView> selectedCards = new List<EscortInventoryItemView>();
        private readonly List<EscortInventoryItemView> cards = new List<EscortInventoryItemView>();
        private readonly Dictionary<EscortInventoryItemView, bool> cardAvailability =
            new Dictionary<EscortInventoryItemView, bool>();
        [SerializeField, Min(0.1f)] private float revealSeconds = 0.4f;
        [SerializeField, Min(1f)] private float resultPeakScale = 1.2f;
        [Header("Unavailable feedback")]
        [SerializeField, Min(0.05f)] private float unavailablePulseSeconds = 0.34f;
        [SerializeField, Min(1f)] private float unavailablePeakScale = 1.08f;
        [SerializeField] private Color unavailableColor = new Color(.84f, .67f, .39f);
        private Coroutine revealRoutine;
        private Coroutine unavailablePulseRoutine;
        private Color defaultStatusColor;
        private float defaultStatusFontSize;
        private bool hasCachedStatusPresentation;
        private bool isCommitting;

        public void Open()
        {
            ClearSelection();
            if (resultView != null)
                resultView.Close();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Refresh();
        }
        public void Close() { ClearSelection(); gameObject.SetActive(false); }
        private void OnDisable()
        {
            ClearSelection();
            if (resultView != null) resultView.Close();
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            if (unavailablePulseRoutine != null) StopCoroutine(unavailablePulseRoutine);
            revealRoutine = null;
            unavailablePulseRoutine = null;
            resultIcon.rectTransform.localScale = Vector3.one;
            ResetStatusPresentation();
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            foreach (EscortInventoryItemView card in cards)
                if (card != null)
                    card.OnItemSelected -= SelectMaterial;
        }

        private void SelectMaterial(EscortInventoryItemView card)
        {
            if (card == null || card.ShipData == null)
                return;

            int selectedIndex = selectedCards.IndexOf(card);
            if (selectedIndex >= 0)
            {
                selectedCards.RemoveAt(selectedIndex);
                selected.RemoveAt(selectedIndex);
                Refresh();
                return;
            }

            EscortShipData ship = card.ShipData;
            if (selected.Count >= PlayerFleetManager.MergeMaterialCount || ship.Grade == EscortShipGrade.Legendary) return;
            if (selected.Count > 0 && selected[0].Grade != ship.Grade) return;
            if (cardAvailability.TryGetValue(card, out bool isAvailable) == false || isAvailable == false) return;

            selected.Add(ship);
            selectedCards.Add(card);
            Refresh();
        }

        public void RemoveFirst() => RemoveMaterial(0);
        public void RemoveSecond() => RemoveMaterial(1);
        public void RemoveThird() => RemoveMaterial(2);
        private void RemoveMaterial(int index)
        {
            if (index >= selected.Count)
                return;

            selected.RemoveAt(index);
            selectedCards.RemoveAt(index);
            Refresh();
        }
        private void Refresh()
        {
            int visibleCardCount = 0;

            foreach (var snapshot in fleetManager.GetOwnershipSnapshot())
            {
                for (int copyIndex = 0; copyIndex < snapshot.OwnedCount; copyIndex++)
                {
                    EscortInventoryItemView card = GetOrCreateCard(visibleCardCount);
                    bool isAvailable = copyIndex < snapshot.UnequippedCount;
                    bool isSelected = selectedCards.Contains(card);
                    bool canSelect = isSelected ||
                                     isAvailable &&
                                     selected.Count < PlayerFleetManager.MergeMaterialCount &&
                                     snapshot.ShipData.Grade != EscortShipGrade.Legendary &&
                                     (selected.Count == 0 || snapshot.ShipData.Grade == selected[0].Grade);

                    cardAvailability[card] = isAvailable;
                    card.gameObject.SetActive(true);
                    card.transform.SetSiblingIndex(visibleCardCount);
                    card.RefreshIndividual(snapshot, isSelected, isAvailable);
                    card.SetIndividualMaterialState(canSelect, isSelected);
                    visibleCardCount++;
                }
            }

            for (int i = visibleCardCount; i < cards.Count; i++)
            {
                cards[i].gameObject.SetActive(false);
                cardAvailability[cards[i]] = false;
            }

            EscortShipData source = selected.Count > 0 ? selected[0] : null;
            fleetManager.TryGetMergePreview(source, out var result);
            sourceIcon.sprite=source!=null?source.Icon:null;sourceIcon.enabled=sourceIcon.sprite!=null;
            resultIcon.sprite=result!=null?result.Icon:null;resultIcon.enabled=resultIcon.sprite!=null;
            sourceLabel.text=source!=null?EscortInventoryItemView.GradeLabel(source.Grade):"재료 선택";
            resultLabel.text=result!=null?EscortInventoryItemView.GradeLabel(result.Grade):"상위 등급";
            statusLabel.text=$"같은 등급의 미편성 함선 3척 필요  ({selected.Count}/3)\n선택한 재료를 누르면 해제됩니다";
            for(int i=0;i<materialIcons.Length;i++)
            {
                materialIcons[i].sprite=i<selected.Count?selected[i].Icon:null;
                materialIcons[i].enabled=materialIcons[i].sprite!=null;
                materialLabels[i].text=i<selected.Count?selected[i].DisplayName:"+";
            }
            confirmButton.interactable=selected.Count==PlayerFleetManager.MergeMaterialCount && !isCommitting;
        }

        private EscortInventoryItemView GetOrCreateCard(int index)
        {
            if (index < cards.Count)
                return cards[index];

            EscortInventoryItemView card = Instantiate(cardPrefab, inventoryRoot);
            card.OnItemSelected += SelectMaterial;
            cards.Add(card);
            return card;
        }

        private void ClearSelection()
        {
            selected.Clear();
            selectedCards.Clear();
        }

private System.Collections.IEnumerator RevealResult()
        {
            float elapsed = 0f;
            while (elapsed < revealSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / revealSeconds);
                resultIcon.rectTransform.localScale = Vector3.one * Mathf.Lerp(resultPeakScale, 1f, t);
                yield return null;
            }
            resultIcon.rectTransform.localScale = Vector3.one;
            revealRoutine = null;
        }

        public void MergeAll()
        {
            if (isCommitting || (resultView != null && resultView.IsOpen)) return;
            isCommitting = true;
            try
            {
                long count = fleetManager.MergeAllUnequipped(out var rewards);
                ClearSelection();
                Refresh();
                if (count == 0)
                {
                    ShowUnavailableStatus("합성 가능한 미편성 함선이 없어요.\n같은 등급 3척이 필요합니다. 최고 등급은 제외됩니다.");
                    return;
                }
                var summary = new System.Text.StringBuilder();
                summary.Append($"일괄 합성 {count:N0}회 완료 · 편성 함선 유지\n");
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (i > 0) summary.Append(" · ");
                    summary.Append($"{EscortInventoryItemView.GradeLabel(rewards[i].Key.Grade)} +{rewards[i].Value:N0}");
                }
                statusLabel.text = summary.ToString();
                if (resultView != null) resultView.Show(count, rewards);
                if (rewards.Count > 0)
                {
                    var reward = rewards[rewards.Count - 1].Key;
                    resultIcon.sprite = reward.Icon;
                    resultIcon.enabled = reward.Icon != null;
                    resultLabel.text = EscortInventoryItemView.GradeLabel(reward.Grade);
                    if (revealRoutine != null) StopCoroutine(revealRoutine);
                    revealRoutine = StartCoroutine(RevealResult());
                }
            }
            finally
            {
                isCommitting = false;
                confirmButton.interactable = selected.Count == PlayerFleetManager.MergeMaterialCount;
            }
        }

        private void ShowUnavailableStatus(string message)
        {
            if (statusLabel == null) return;
            statusLabel.text = message;
            CacheStatusPresentation();
            if (unavailablePulseRoutine != null) StopCoroutine(unavailablePulseRoutine);
            if (!isActiveAndEnabled) return;
            unavailablePulseRoutine = StartCoroutine(PulseUnavailableStatus());
        }

        private IEnumerator PulseUnavailableStatus()
        {
            float elapsed = 0f;
            while (elapsed < unavailablePulseSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / unavailablePulseSeconds);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                statusLabel.fontSize = Mathf.Lerp(defaultStatusFontSize, defaultStatusFontSize * unavailablePeakScale, pulse);
                statusLabel.color = Color.Lerp(defaultStatusColor, unavailableColor, pulse);
                yield return null;
            }
            ResetStatusPresentation();
            unavailablePulseRoutine = null;
        }

        private void CacheStatusPresentation()
        {
            if (hasCachedStatusPresentation || statusLabel == null) return;
            defaultStatusColor = statusLabel.color;
            defaultStatusFontSize = statusLabel.fontSize;
            hasCachedStatusPresentation = true;
        }

        private void ResetStatusPresentation()
        {
            if (statusLabel == null || !hasCachedStatusPresentation) return;
            statusLabel.rectTransform.localScale = Vector3.one;
            statusLabel.fontSize = defaultStatusFontSize;
            statusLabel.color = defaultStatusColor;
        }

        public void Confirm()
        {
            if (isCommitting || (resultView != null && resultView.IsOpen) || selected.Count != PlayerFleetManager.MergeMaterialCount) return;
            isCommitting=true;
            bool success=fleetManager.TryMergeSelected(selected,out var reward);
            ClearSelection();
            isCommitting=false;
            Refresh();
            statusLabel.text=success?$"{reward.DisplayName} 획득!\n미편성 함선입니다. 함대에서 배치하세요.":"재료가 변경됐어요. 다시 선택해주세요.";
            if (success && resultView != null) resultView.Show(1, new[] { new KeyValuePair<EscortShipData, int>(reward, 1) });
            if(success){if (revealRoutine != null) StopCoroutine(revealRoutine); revealRoutine = StartCoroutine(RevealResult());resultIcon.sprite=reward.Icon;resultIcon.enabled=reward.Icon!=null;resultLabel.text=EscortInventoryItemView.GradeLabel(reward.Grade);}
        }
    }
}
