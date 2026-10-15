using System;
using SB.Core;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class NormalProjectile : BaseProjectile
    {
        [SerializeField, Tooltip("요격선이 이 포탄을 공중에서 파괴할 수 있습니다.")]
        private bool interceptable = true;
        protected override bool SupportsInterception => interceptable;

        [Header("Impact Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject waterSplashEffectPrefab;
        [SerializeField, Tooltip("미스 물기둥이 함선 앞에 보이도록 사용할 정렬 오더")]
        private int waterSplashSortingOrder = 4050;
        [SerializeField, Tooltip("화면 깊이에 따른 물기둥 정렬 보정")]
        private float waterSplashSortingOrderPerUnit = 100f;

        public event Action TargetImpacted;

        protected override void OnTargetImpact(Ship target, float damage, Vector3 impactPosition)
        {
            TargetImpacted?.Invoke();
            EntityHealth health = target.GetCompo<EntityHealth>();
            float previousHealth = health != null ? health.CurrentHealth : 0f;
            health?.ApplyDamage(damage, impactPosition);
            if (health != null && health.CurrentHealth < previousHealth)
            {
                target.GetComponent<ShipWoodImpactFeedback>()?.Play(impactPosition);
            }
            SpawnEffect(hitEffectPrefab, impactPosition);
        }

        protected override void OnWaterImpact(Vector3 impactPosition)
        {
            SpawnWaterSplash(impactPosition);
        }

        private static void SpawnEffect(GameObject effectPrefab, Vector3 position)
        {
            if (effectPrefab == null || PoolingManager.Instance == null)
                return;

            PoolingManager.Instance.Get(effectPrefab, position, Quaternion.identity);
        }

        private void SpawnWaterSplash(Vector3 position)
        {
            if (waterSplashEffectPrefab == null || PoolingManager.Instance == null)
                return;

            GameObject instance = PoolingManager.Instance.Get(waterSplashEffectPrefab, position, Quaternion.identity);
            if (instance == null)
                return;

            int sortingOrder = waterSplashSortingOrder + Mathf.RoundToInt(-position.y * waterSplashSortingOrderPerUnit);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sortingOrder = sortingOrder;
        }
    }
}
