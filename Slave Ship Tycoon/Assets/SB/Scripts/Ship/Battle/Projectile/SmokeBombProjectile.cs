using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class SmokeBombProjectile : BaseProjectile
    {
        [SerializeField] private SmokeCloudEffect smokeCloudPrefab;
        [SerializeField] private GameObject waterSplashEffectPrefab;
        protected override bool SupportsInterception => true;
        protected override bool RegistersIncomingDamage => false;

        protected override void OnTargetImpact(Ship target, float amount, Vector3 impactPosition)
        {
            if (target == null || target.IsDead || smokeCloudPrefab == null || PoolingManager.Instance == null) return;
            // A second hit refreshes the same cloud rather than stacking accuracy penalties.
            SmokeCloudEffect cloud = target.SmokeCloud;
            if (cloud == null)
                cloud = PoolingManager.Instance.Get(smokeCloudPrefab, impactPosition, Quaternion.identity);
            cloud?.Play(target);
        }

        protected override void OnWaterImpact(Vector3 impactPosition)
        {
            if (waterSplashEffectPrefab != null && PoolingManager.Instance != null)
                PoolingManager.Instance.Get(waterSplashEffectPrefab, impactPosition, Quaternion.identity);
        }
    }
}
