using System.Collections;
using System.Collections.Generic;
using SB.Scripts.Fleet;
using TMPro;
using UnityEngine;

namespace SB.Scripts.UI.Fleet
{
    public sealed class FleetMergeResultView : MonoBehaviour
    {
        [System.Serializable]
        private sealed class RewardRow
        {
            public GameObject root;
            public UnityEngine.UI.Image icon;
            public TMP_Text nameText;
            public TMP_Text amountText;
        }

        [Header("References")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private RewardRow[] rows;
        [Header("Presentation")]
        [SerializeField, Min(0.01f)] private float revealSeconds = 0.25f;
        [SerializeField, Range(0.8f, 1f)] private float entryScale = 0.94f;
        [SerializeField, Min(0f)] private float panelBaseHeight = 380f;
        [SerializeField, Min(1f)] private float rowHeight = 130f;
        private Coroutine revealRoutine;
        public bool IsOpen => gameObject.activeSelf;

        public void Show(long mergeCount, IReadOnlyList<KeyValuePair<EscortShipData, int>> rewards)
        {
            if (mergeCount <= 0 || rewards == null || rewards.Count == 0) return;
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            summaryText.text = $"총 {mergeCount:N0}회 합성 · 최종 획득 함선";
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].root.SetActive(i < rewards.Count);
                if (i >= rewards.Count) continue;
                var reward = rewards[i];
                rows[i].icon.sprite = reward.Key.Icon;
                rows[i].icon.enabled = reward.Key.Icon != null;
                rows[i].nameText.text = $"{EscortInventoryItemView.GradeLabel(reward.Key.Grade)}\n{reward.Key.DisplayName}";
                rows[i].amountText.text = $"+{reward.Value:N0}척";
            }
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                panelBaseHeight + rowHeight * rewards.Count);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            visibility.alpha = 0f;
            revealRoutine = StartCoroutine(Reveal());
        }

        public void Close() => gameObject.SetActive(false);

        private IEnumerator Reveal()
        {
            float elapsed = 0f;
            while (elapsed < revealSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / revealSeconds);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                visibility.alpha = eased;
                panel.localScale = Vector3.one * Mathf.Lerp(entryScale, 1f, eased);
                yield return null;
            }
            visibility.alpha = 1f;
            panel.localScale = Vector3.one;
            revealRoutine = null;
        }

        private void OnDisable()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            if (panel != null) panel.localScale = Vector3.one;
            if (visibility != null) visibility.alpha = 0f;
        }
    }
}
