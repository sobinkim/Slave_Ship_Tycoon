using SB.Core;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class NormalProjectile : BaseProjectile
    {
        [Header("Impact Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject waterSplashEffectPrefab;

        protected override void OnTargetImpact(Ship target, float damage, Vector3 impactPosition)
        {
            target.GetCompo<EntityHealth>()?.ApplyDamage(damage);
            SpawnEffect(hitEffectPrefab, impactPosition);
        }

        protected override void OnWaterImpact(Vector3 impactPosition)
        {
            SpawnEffect(waterSplashEffectPrefab, impactPosition);
        }

        private static void SpawnEffect(GameObject effectPrefab, Vector3 position)
        {
            if (effectPrefab == null || PoolingManager.Instance == null)
                return;

            PoolingManager.Instance.Get(effectPrefab, position, Quaternion.identity);
        }
    }
}
