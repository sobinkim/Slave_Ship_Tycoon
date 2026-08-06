using SB.Scripts.Projectiles;
using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public class ArcProjectileAttackCompo : Base_ShipAttackCompo
    {
        [SerializeField] private BaseProjectile projectilePrefab;
        [SerializeField] private Transform[] firePoints;
        [SerializeField] private ProjectileFlightData flightData = ProjectileFlightData.Default;

        private int currentFirePointIndex;

        public override void Initialize(SB.Core.Entity entity)
        {
            base.Initialize(entity);
        }

        protected override void Fire()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"{name} needs a projectile prefab.", this);
                return;
            }

            if (PoolingManager.Instance == null)
            {
                Debug.LogError($"{nameof(ArcProjectileAttackCompo)} needs a {nameof(PoolingManager)} in the scene.", this);
                return;
            }

            if (CurrentTarget == null || CurrentTarget.IsDead)
                return;

            ShipProjectileAnchorCompo targetAnchors =
                CurrentTarget.GetCompo<ShipProjectileAnchorCompo>();
            if (targetAnchors == null)
            {
                Debug.LogError($"{CurrentTarget.name} needs a {nameof(ShipProjectileAnchorCompo)}.", CurrentTarget);
                return;
            }

            if (TryGetFirePosition(out Vector3 startPosition) == false)
            {
                Debug.LogError($"{name} needs at least one fire point.", this);
                return;
            }

            Vector3 targetImpactPosition = targetAnchors.HitPosition;
            Vector3 waterImpactPosition = targetAnchors.WaterImpactPosition;

            BaseProjectile projectile = PoolingManager.Instance.Get(
                projectilePrefab,
                startPosition,
                Quaternion.identity);

            if (projectile == null)
                return;

            ProjectileLaunchData launchData = new ProjectileLaunchData(
                CurrentTarget,
                startPosition,
                targetImpactPosition,
                waterImpactPosition,
                flightData,
                FinalAttackDamage);

            projectile.Launch(launchData);
        }

        private bool TryGetFirePosition(out Vector3 firePosition)
        {
            Transform firePoint = GetNextFirePoint();
            if (firePoint == null)
            {
                firePosition = default;
                return false;
            }

            firePosition = firePoint.position;
            return true;
        }

        private Transform GetNextFirePoint()
        {
            if (firePoints == null || firePoints.Length == 0)
                return null;

            int startIndex = currentFirePointIndex;
            for (int i = 0; i < firePoints.Length; i++)
            {
                int index = (startIndex + i) % firePoints.Length;
                Transform firePoint = firePoints[index];
                if (firePoint == null)
                    continue;

                currentFirePointIndex = (index + 1) % firePoints.Length;
                return firePoint;
            }

            return null;
        }
    }
}
