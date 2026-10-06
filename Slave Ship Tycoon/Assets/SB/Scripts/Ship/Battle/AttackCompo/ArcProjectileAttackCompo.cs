using SB.Scripts.Projectiles;
using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public enum ProjectileTravelMode
    {
        Arc,
        AcceleratingStraight,
        AcceleratingArc,
        BallisticArc
    }

    public class ArcProjectileAttackCompo : Base_ShipAttackCompo
    {
        [SerializeField] private BaseProjectile projectilePrefab;
        [SerializeField] private Transform[] firePoints;
        [SerializeField] private ProjectileFlightData flightData = ProjectileFlightData.Default;

        [Header("Travel Mode")]
        [SerializeField] private ProjectileTravelMode travelMode = ProjectileTravelMode.Arc;
        [SerializeField, Min(0f), Tooltip("가속 직선 이동 시작 속도 (월드 단위/초)")]
        private float initialStraightSpeed = 2f;
        [SerializeField, Min(0f), Tooltip("직선 이동이 도달할 목표 속도 (월드 단위/초)")]
        private float straightMaximumSpeed = 12f;
        [SerializeField, Min(0f), Tooltip("가속 포물선 이동의 시작 속도 (월드 단위/초)")]
        private float initialArcSpeed = 1.5f;
        [SerializeField, Min(0f), Tooltip("가속 포물선 이동이 도달할 목표 속도 (월드 단위/초)")]
        private float arcMaximumSpeed = 8f;
        [SerializeField, Min(0.01f), Tooltip("발사 지점을 기준으로 한 포물선 정점 높이")]
        private float ballisticApexHeight = 2.5f;
        [SerializeField, Min(0.01f), Tooltip("타겟이 설정된 정점보다 높을 때 타겟 위로 확보할 최소 높이")]
        private float ballisticTargetClearance = 0.15f;
        [SerializeField, Min(0.01f), Tooltip("작살이 정점에 도달하기까지 걸리는 시간")]
        private float ballisticTimeToApex = 0.55f;
        [SerializeField, Range(0.25f, 1f), Tooltip("하강 구간 시간 배율. 작을수록 더 빨리 내려옵니다.")]
        private float ballisticDescentTimeMultiplier = 0.7f;
        [SerializeField, Min(0f), Tooltip("작살이 이 거리 안에서 타겟을 지나가면 적중 처리합니다.")]
        private float ballisticHitRadius = 0.35f;

        private int currentFirePointIndex;

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

            if (HasAliveTarget == false)
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

            bool isStraightMode = travelMode == ProjectileTravelMode.AcceleratingStraight;
            float launchSpeed = isStraightMode ? initialStraightSpeed : initialArcSpeed;
            float maximumTravelSpeed = isStraightMode ? straightMaximumSpeed : arcMaximumSpeed;
            projectile.Launch(
                launchData,
                travelMode,
                launchSpeed,
                maximumTravelSpeed,
                ballisticApexHeight,
                ballisticTimeToApex,
                ballisticDescentTimeMultiplier,
                ballisticHitRadius,
                ballisticTargetClearance);
        }

        public bool TryGetFirePosition(out Vector3 firePosition)
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
