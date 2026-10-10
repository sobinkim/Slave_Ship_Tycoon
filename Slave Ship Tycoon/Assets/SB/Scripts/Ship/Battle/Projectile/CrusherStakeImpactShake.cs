using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NormalProjectile))]
    public sealed class CrusherStakeImpactShake : MonoBehaviour
    {
        [Header("Screen Shake")]
        [SerializeField, Min(0f), Tooltip("명중 시 카메라가 움직이는 최대 월드 거리")]
        private float shakeDistanceUnits = 0.045f;
        [SerializeField, Min(0f), Tooltip("명중 후 화면 흔들림 지속 시간(초)")]
        private float shakeDurationSeconds = 0.16f;

        private NormalProjectile projectile;

        private void Awake()
        {
            projectile = GetComponent<NormalProjectile>();
        }

        private void OnEnable()
        {
            if (projectile == null)
                projectile = GetComponent<NormalProjectile>();

            projectile.TargetImpacted += HandleTargetImpacted;
        }

        private void OnDisable()
        {
            if (projectile != null)
                projectile.TargetImpacted -= HandleTargetImpacted;
        }

        private void HandleTargetImpacted()
        {
            if (shakeDistanceUnits <= 0f || shakeDurationSeconds <= 0f)
                return;

            Bus<CameraShakeRequestedEvent>.Raise(
                new CameraShakeRequestedEvent(shakeDistanceUnits, shakeDurationSeconds));
        }
    }
}
