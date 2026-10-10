using SB.Core;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HealingProjectile : BaseProjectile
    {
        [Header("Healing effects")]
        [SerializeField] private HealingImpactEffect healingImpactEffectPrefab;
        [SerializeField] private GameObject waterSplashEffectPrefab;

        private bool isEnemySource;
        private bool hasSource;
        protected override bool RegistersIncomingDamage => false;

        public void ConfigureSource(Ship source)
        {
            hasSource = source != null;
            isEnemySource = source != null && (source is Enemy || source.myShipData.ShipType == ShipType.Enemy);
        }

        protected override void OnTargetImpact(Ship target, float amount, Vector3 impactPosition)
        {
            if (!hasSource || target == null || target.IsDead)
                return;
            bool isEnemyTarget = target is Enemy || target.myShipData.ShipType == ShipType.Enemy;
            bool isSameTeam = isEnemySource == isEnemyTarget;
            // Line priority belongs to TargetSelector; a committed capsule can heal any allied line.
            if (!isSameTeam)
                return;

            EntityHealth health = target.GetCompo<EntityHealth>();
            if (health == null)
                return;
            float previousHealth = health.CurrentHealth;
            health.ApplyHeal(amount);
            float appliedHealing = health.CurrentHealth - previousHealth;
            if (appliedHealing <= 0f || healingImpactEffectPrefab == null || PoolingManager.Instance == null)
                return;

            HealingImpactEffect effect = PoolingManager.Instance.Get(
                healingImpactEffectPrefab, impactPosition, Quaternion.identity);
            effect?.Play(appliedHealing);
        }

        protected override void OnWaterImpact(Vector3 impactPosition)
        {
            if (waterSplashEffectPrefab != null && PoolingManager.Instance != null)
                PoolingManager.Instance.Get(waterSplashEffectPrefab, impactPosition, Quaternion.identity);
        }
    }
}

