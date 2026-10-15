using SB.Core.EventBus;
using TMPro;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [DisallowMultipleComponent]
    public sealed class ShipMissFeedback : MonoBehaviour
    {
        private const float MinimumVisibleSeconds = 0.01f;

        [Header("References")]
        [SerializeField] private TMP_Text label;

        [Header("Presentation")]
        [SerializeField, Min(.01f)] private float visibleSeconds = .35f;
        [SerializeField, Min(0f), Tooltip("동일 함선의 MISS 문구 재표시 최소 간격. 실제 탄 집계에는 영향 없음")]
        private float minimumIntervalSeconds = .4f;
        [SerializeField, Min(0f)] private float riseLocalUnits = .08f;
        [SerializeField] private float driftLocalUnits = .04f;
        [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = .65f;
        [SerializeField, Min(.01f)] private float popSeconds = .08f;
        [SerializeField, Min(.01f)] private float settleSeconds = .12f;
        [SerializeField, Min(.01f)] private float entryScale = .8f;
        [SerializeField, Min(.01f)] private float peakScale = 1.08f;
        private Vector3 initialLocalPosition;
        private Vector3 initialLocalScale;
        private float remainingSeconds;
        private float nextAllowedTime;

        private void Awake()
        {
            if (label == null) { Debug.LogError("ShipMissFeedback requires a label.", this); enabled = false; return; }
            initialLocalPosition = label.transform.localPosition;
            initialLocalScale = label.transform.localScale;
            label.text = "MISS";
            Hide();
        }
        private void OnEnable() => Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            Hide();
        }
        private void HandleBattleEnded(StageBattleEndedEvent evt) => Hide();
        public void Play()
        {
            if (!isActiveAndEnabled || label == null || Time.time < nextAllowedTime) return;
            nextAllowedTime = Time.time + minimumIntervalSeconds;
            remainingSeconds = visibleSeconds;
            label.transform.localPosition = initialLocalPosition;
            label.transform.localScale = initialLocalScale * entryScale;
            label.alpha = 1f;
            label.enabled = true;
        }
        private void Update()
        {
            if (remainingSeconds <= 0f) return;
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            float progress = 1f - remainingSeconds /
                Mathf.Max(MinimumVisibleSeconds, visibleSeconds);
            float elapsed = visibleSeconds - remainingSeconds;
            float pop = Mathf.Clamp01(elapsed / Mathf.Max(.01f, popSeconds));
            float settle = Mathf.Clamp01((elapsed - popSeconds) / Mathf.Max(.01f, settleSeconds));
            float scale = elapsed < popSeconds
                ? Mathf.Lerp(entryScale, peakScale, 1f - Mathf.Pow(1f - pop, 3f))
                : Mathf.Lerp(peakScale, 1f, Mathf.SmoothStep(0f, 1f, settle));
            label.transform.localScale = initialLocalScale * scale;
            label.transform.localPosition = initialLocalPosition +
                new Vector3(driftLocalUnits * progress, riseLocalUnits * progress, 0f);
            label.alpha = 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(fadeStartNormalized, 1f, progress));
            if (remainingSeconds <= 0f) label.enabled = false;
        }
        private void Hide()
        {
            remainingSeconds = 0f;
            nextAllowedTime = 0f;
            if (label != null)
            {
                label.enabled = false;
                label.transform.localPosition = initialLocalPosition;
                label.transform.localScale = initialLocalScale;
            }
        }
    }
}
