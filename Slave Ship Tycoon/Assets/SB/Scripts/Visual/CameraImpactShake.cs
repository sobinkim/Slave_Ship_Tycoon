using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Visual
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraImpactShake : MonoBehaviour
    {
        [Header("Repeated Impacts")]
        [SerializeField, Min(0f), Tooltip("한 번 흔들린 뒤 다음 명중 흔들림까지 쉬는 시간(초)")]
        private float cooldownSeconds = 0.12f;

        private Vector3 lastLocalOffset;
        private float remainingSeconds;
        private float durationSeconds;
        private float distanceUnits;
        private float nextAllowedShakeTime;

        private void OnEnable()
        {
            Bus<CameraShakeRequestedEvent>.OnEvent += HandleShakeRequested;
        }

        private void OnDisable()
        {
            Bus<CameraShakeRequestedEvent>.OnEvent -= HandleShakeRequested;
            transform.localPosition -= lastLocalOffset;
            lastLocalOffset = Vector3.zero;
            remainingSeconds = 0f;
            nextAllowedShakeTime = 0f;
        }

        private void LateUpdate()
        {
            Vector3 baseLocalPosition = transform.localPosition - lastLocalOffset;
            lastLocalOffset = Vector3.zero;

            if (remainingSeconds > 0f)
            {
                remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
                float strength = durationSeconds > 0f ? remainingSeconds / durationSeconds : 0f;
                Vector2 offset = Random.insideUnitCircle * (distanceUnits * strength);
                lastLocalOffset = new Vector3(offset.x, offset.y, 0f);
            }

            transform.localPosition = baseLocalPosition + lastLocalOffset;
        }

        private void HandleShakeRequested(CameraShakeRequestedEvent evt)
        {
            if (evt.DistanceUnits <= 0f || evt.DurationSeconds <= 0f)
                return;

            // Overlapping hits do not extend or amplify the current shake.
            if (remainingSeconds > 0f || Time.time < nextAllowedShakeTime)
                return;

            distanceUnits = evt.DistanceUnits;
            durationSeconds = evt.DurationSeconds;
            remainingSeconds = evt.DurationSeconds;
            nextAllowedShakeTime = Time.time + durationSeconds + cooldownSeconds;
        }
    }
}
