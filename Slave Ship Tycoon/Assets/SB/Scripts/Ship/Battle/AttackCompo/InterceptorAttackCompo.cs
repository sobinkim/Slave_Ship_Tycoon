using SB.Scripts.Projectiles;
using SB.Scripts;
using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public sealed class InterceptorAttackCompo : Base_ShipAttackCompo
    {
        [Header("Interception")]
        [SerializeField] private InterceptorProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField, Min(.1f)] private float projectileSpeed = 12f;
        [SerializeField, Min(.1f), Tooltip("발사점에서 예상 요격 지점까지의 최대 거리")]
        private float interceptionRange = 5f;
        [SerializeField, Min(.001f)] private float minimumFlightSeconds = .04f;
        [SerializeField, Min(0f), Tooltip("적 포탄 착탄 전 확보할 최소 여유 시간")]
        private float impactMarginSeconds = .06f;
        [SerializeField, Tooltip("같은 가로 라인에 배치된 호위함을 보호합니다.")]
        private bool protectsSameRowOnly = true;
        [SerializeField, Tooltip("본선을 향하는 포탄은 배치 라인에 관계없이 보호합니다.")]
        private bool protectsMainShip = true;

        private BaseProjectile selectedProjectile;
        private Vector3 meetingPoint;
        private float meetingSeconds;
        private bool animationCommitted;

        public override bool TicksCooldownWithoutTarget => true;
        public override bool FinishesAnimationWithoutTarget => true;
        public int ShotsFired { get; private set; }

        public override bool TryAcquireTarget() => EnsureTarget();

        public override bool EnsureTarget()
        {
            if (!IsBattleRunning || OwnerShip == null || !OwnerShip.isActiveAndEnabled || OwnerShip.IsDead)
                return false;
            if (animationCommitted) return true;
            selectedProjectile = null;
            if (!CanAttack || firePoint == null || projectilePrefab == null || PoolingManager.Instance == null)
                return false;
            int highestPriorityLine = int.MaxValue;
            float soonestImpact = float.MaxValue;
            var flights = BaseProjectile.InterceptableFlights;
            for (int i = 0; i < flights.Count; i++)
            {
                BaseProjectile candidate = flights[i];
                Ship protectedShip = candidate != null ? candidate.IntendedTarget : null;
                int linePriority = GetProtectionLinePriority(protectedShip);
                if (!ProtectsTarget(candidate) ||
                    !InterceptionPrediction.TrySolve(candidate, firePoint.position, projectileSpeed,
                        minimumFlightSeconds, impactMarginSeconds, out Vector3 point, out float seconds) ||
                    (point - firePoint.position).sqrMagnitude > interceptionRange * interceptionRange)
                    continue;

                float remainingImpact = candidate.RemainingPredictionSeconds;
                if (linePriority > highestPriorityLine ||
                    (linePriority == highestPriorityLine && remainingImpact >= soonestImpact))
                    continue;

                selectedProjectile = candidate;
                meetingPoint = point;
                meetingSeconds = seconds;
                highestPriorityLine = linePriority;
                soonestImpact = remainingImpact;
            }
            return selectedProjectile != null;
        }

        private static int GetProtectionLinePriority(Ship target)
        {
            if (target == null || target.myShipData.ShipType == ShipType.MainShip)
                return 0; // The main ship occupies the rear line.

            int column = target.myShipData.SpawnSlot.x;
            bool targetEnemy = target.myShipData.ShipType == ShipType.Enemy;
            TargetLine line = targetEnemy
                ? column switch
                {
                    0 => TargetLine.Front,
                    1 => TargetLine.Middle,
                    2 => TargetLine.Back,
                    _ => TargetLine.Any
                }
                : column switch
                {
                    0 => TargetLine.Back,
                    1 => TargetLine.Middle,
                    2 => TargetLine.Front,
                    _ => TargetLine.Any
                };

            return line switch
            {
                TargetLine.Back => 0,
                TargetLine.Front => 1,
                TargetLine.Middle => 2,
                _ => 3
            };
        }

        private bool ProtectsTarget(BaseProjectile projectile)
        {
            if (projectile == null || !projectile.CanBeIntercepted) return false;
            Ship ally = projectile.IntendedTarget;
            if (ally == null) return false;
            bool ownerEnemy = OwnerShip.myShipData.ShipType == ShipType.Enemy;
            bool targetEnemy = ally.myShipData.ShipType == ShipType.Enemy;
            if (ownerEnemy != targetEnemy) return false;
            if (ally.myShipData.ShipType == ShipType.MainShip) return protectsMainShip;
            return !protectsSameRowOnly || ally.myShipData.SpawnSlot.y == OwnerShip.myShipData.SpawnSlot.y;
        }

        // Fire on attack entry: a windup long enough for artillery would miss fast incoming shells.
        // The dedicated clip starts its recoil/flash on this same frame.
        protected override void OnAttackEnter()
        {
            Fire();
            animationCommitted = true;
        }

        protected override void Fire()
        {
            if (animationCommitted || selectedProjectile == null || !selectedProjectile.CanBeIntercepted) return;
            var projectile = PoolingManager.Instance.Get(projectilePrefab, firePoint.position, Quaternion.identity);
            if (projectile == null) return;
            if (!projectile.Launch(selectedProjectile, firePoint.position, meetingPoint, projectileSpeed, meetingSeconds))
            {
                PoolingManager.Instance.Release(projectile);
                return;
            }
            ShotsFired++;
            PlayMuzzleEffect(firePoint, meetingPoint - firePoint.position);
            selectedProjectile = null;
        }

        protected override void OnAttackExit()
        {
            animationCommitted = false;
            base.OnAttackExit();
        }

        public override void ResetAttackState()
        {
            selectedProjectile = null;
            animationCommitted = false;
            base.ResetAttackState();
        }
    }
}
