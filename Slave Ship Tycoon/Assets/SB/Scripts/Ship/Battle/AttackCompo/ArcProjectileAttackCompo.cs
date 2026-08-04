using SB.Scripts.Projectiles;
using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public class ArcProjectileAttackCompo : Base_ShipAttackCompo
    {
        [SerializeField] private BaseProjectile projectilePrefab;

        private Ship ownerShip;
        private ShipProjectileAnchorCompo ownerAnchors;

        public override void Initialize(SB.Core.Entity entity)
        {
            base.Initialize(entity);

            ownerShip = entity as Ship;
            ownerAnchors = GetCompo<ShipProjectileAnchorCompo>();
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

            Vector3 startPosition = ownerAnchors != null
                ? ownerAnchors.FirePosition
                : ownerShip.transform.position;
            Vector3 targetImpactPosition = targetAnchors != null
                ? targetAnchors.HitPosition
                : CurrentTarget.transform.position;
            Vector3 waterImpactPosition = targetAnchors != null
                ? targetAnchors.WaterImpactPosition
                : CurrentTarget.transform.position;

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
                FinalAttackDamage);

            projectile.Launch(launchData);
        }
    }
}
