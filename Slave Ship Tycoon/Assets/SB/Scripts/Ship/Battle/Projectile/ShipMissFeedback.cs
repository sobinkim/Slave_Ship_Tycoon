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
        private Vector3 initialLocalPosition;
        private float remainingSeconds;
        private float nextAllowedTime;

        private void Awake()
        {
            if (label == null) { Debug.LogError("ShipMissFeedback requires a label.", this); enabled = false; return; }
            initialLocalPosition = label.transform.localPosition;
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
            label.alpha = 1f;
            label.enabled = true;
        }
        private void Update()
        {
            if (remainingSeconds <= 0f) return;
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            float progress = 1f - remainingSeconds /
                Mathf.Max(MinimumVisibleSeconds, visibleSeconds);
            label.transform.localPosition = initialLocalPosition + Vector3.up * (riseLocalUnits * progress);
            label.alpha = 1f - progress;
            if (remainingSeconds <= 0f) label.enabled = false;
        }
        private void Hide()
        {
            remainingSeconds = 0f;
            nextAllowedTime = 0f;
            if (label != null) { label.enabled = false; label.transform.localPosition = initialLocalPosition; }
        }
    }
}
