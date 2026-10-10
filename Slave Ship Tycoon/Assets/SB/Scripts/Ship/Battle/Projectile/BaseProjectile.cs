using SB.Core.EventBus;
using SB.Scripts.AttackCompo;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public enum ProjectileResolution
    {
        TargetHit,
        WaterHit,
        Expired,
        Cancelled
    }

    public abstract class BaseProjectile : MonoBehaviour, IPoolable
    {
        private const float ArcPeakMultiplier = 4f;
        private const float ArcTangentSampleDistance = 0.01f;

        [Header("Flight")]
        [SerializeField] private bool rotateAlongPath = true;
        [SerializeField] private float rotationOffset;
        [SerializeField, Min(0f), Tooltip("착탄 위치에서 투사체가 보이는 추가 시간")]
        private float impactVisualHoldDurationSeconds;

        [Header("Sorting")]
        [SerializeField] private int projectileBaseSortingOrder = 3000;
        [SerializeField] private float sortingOrderPerUnit = 100f;
        [SerializeField] private int trailSortingOrderOffset = -1;

        private ProjectileLaunchData launchData;
        private ProjectileFlightData flightData;
        private ProjectileTravelMode travelMode;
        private float travelStartSpeed;
        private float travelEndSpeed;
        private float ballisticApexHeight;
        private float ballisticTargetClearance;
        private float ballisticTimeToApex;
        private float ballisticDescentTimeMultiplier;
        private float ballisticHitRadius;
        private Vector3 travelDirection;
        private Vector3 pathStartPosition;
        private Vector3 pathEndPosition;
        private Vector3 previousFramePosition;
        private Vector3 missControlPoint;
        private float flightDuration;
        private float flightElapsed;
        private float lifeElapsed;
        private bool isFlying;
        private bool isImpactHolding;
        private bool hasReportedResolution;
        private float impactHoldRemainingSeconds;
        private ProjectileResolution impactResolution;
        private bool isMissPath;
        private bool isLuckMiss;
        private TrailRenderer[] trailRenderers;
        private Renderer projectileRenderer;
        private ShipIncomingFireDisplay incomingFireDisplay;

        public event System.Action<BaseProjectile, ProjectileResolution> Resolved;
        protected virtual bool RegistersIncomingDamage => true;

        public float RemainingFlightSeconds => Mathf.Max(0f, flightDuration - flightElapsed);
        public bool IsLuckMiss => isLuckMiss;
        internal bool IsFlying => isFlying && !isImpactHolding;
        internal Vector3 FlightStartPosition => pathStartPosition;

        private void Awake()
        {
            CacheRenderers();
        }

        private void OnEnable()
        {
            Bus<StageBattleEndedEvent>.OnEvent += HandleStageBattleEnded;
            Bus<MainShipFinalSalvoStartedEvent>.OnEvent += HandleFinalSalvoStarted;
        }

        private void OnDisable()
        {
            UnregisterIncomingFire();
            Bus<StageBattleEndedEvent>.OnEvent -= HandleStageBattleEnded;
            Bus<MainShipFinalSalvoStartedEvent>.OnEvent -= HandleFinalSalvoStarted;
            NotifyCancelledFlight();
        }

        public void Launch(ProjectileLaunchData data, ProjectileTravelMode travelMode = ProjectileTravelMode.Arc,
            float initialStraightSpeed = 0f, float maximumTravelSpeed = 0f,
            float ballisticApexHeight = 2.5f, float ballisticTimeToApex = 0.55f,
            float ballisticDescentTimeMultiplier = 1f, float ballisticHitRadius = 0.35f,
            float ballisticTargetClearance = 0.15f)
        {
            UnregisterIncomingFire();
            launchData = data;
            flightData = data.FlightData.GetValidated();
            this.travelMode = travelMode;
            travelStartSpeed = Mathf.Max(0f, initialStraightSpeed);
            travelEndSpeed = Mathf.Max(travelStartSpeed, maximumTravelSpeed);
            this.ballisticApexHeight = Mathf.Max(0.01f, ballisticApexHeight);
            this.ballisticTargetClearance = Mathf.Max(0.01f, ballisticTargetClearance);
            this.ballisticTimeToApex = Mathf.Max(0.01f, ballisticTimeToApex);
            this.ballisticDescentTimeMultiplier = Mathf.Clamp(ballisticDescentTimeMultiplier, 0.25f, 1f);
            this.ballisticHitRadius = Mathf.Max(0f, ballisticHitRadius);
            transform.position = data.StartPosition;
            previousFramePosition = data.StartPosition;
            ClearTrails();

            flightElapsed = 0f;
            lifeElapsed = 0f;
            isFlying = true;
            isImpactHolding = false;
            hasReportedResolution = false;
            impactHoldRemainingSeconds = 0f;
            isMissPath = false;

            isLuckMiss = false;
            Vector3 destination = data.TargetImpactPosition;
            if (data.HitMode == ProjectileHitMode.Normal && IsOriginalTargetAlive())
            {
                var evasion = data.Target.GetComponent<ShipProjectileEvasion>();
                if (evasion != null && evasion.TryResolveMiss(data.StartPosition, Random.value, out Vector3 waterPosition))
                {
                    isLuckMiss = true;
                    destination = waterPosition;
                }
            }
            // A miss continues along the committed path; the travel mode does not change mid-flight.
            ApplySortingOrder(data.StartPosition, destination);
            travelDirection = (destination - data.StartPosition).normalized;
            BuildFlightPath(data.StartPosition, destination);
            if (RegistersIncomingDamage && IsOriginalTargetAlive())
            {
                incomingFireDisplay = data.Target.GetComponent<ShipIncomingFireDisplay>();
                incomingFireDisplay?.Register(this, data.Damage, data.TargetSpawnGeneration);
            }
        }

        private void Update()
        {
            if (isFlying == false)
                return;

            if (isImpactHolding)
            {
                impactHoldRemainingSeconds -= Time.deltaTime;
                if (impactHoldRemainingSeconds <= 0f)
                    ReleaseToPool(impactResolution);
                return;
            }

            lifeElapsed += Time.deltaTime;
            if (lifeElapsed >= flightData.LifeTime)
            {
                Expire();
                return;
            }

            if (travelMode == ProjectileTravelMode.Arc &&
                isMissPath == false && IsOriginalTargetAlive() == false)
            {
                if (isLuckMiss)
                    UnregisterIncomingFire(); // Preserve the committed water arc after target loss.
                else
                    SwitchToMissPath();
            }

            flightElapsed += Time.deltaTime;
            float rawProgress;
            Vector3 previousPosition = transform.position;
            previousFramePosition = previousPosition;
            if (travelMode == ProjectileTravelMode.BallisticArc)
            {
                rawProgress = Mathf.Clamp01(flightElapsed / flightDuration);
                transform.position = EvaluateBallisticPosition(rawProgress);
                RotateToDirection(EvaluateBallisticTangent(rawProgress));
            }
            else if (IsAcceleratedTravel())
            {
                rawProgress = Mathf.Clamp01(flightElapsed / flightDuration);
                float easedProgress = EvaluateAcceleratedProgress(rawProgress);
                Vector3 basePosition = Vector3.Lerp(pathStartPosition, pathEndPosition, easedProgress);
                if (travelMode == ProjectileTravelMode.AcceleratingArc)
                {
                    float arcOffset = ArcPeakMultiplier * flightData.ArcHeight * rawProgress * (1f - rawProgress);
                    transform.position = basePosition + Vector3.up * arcOffset;
                    RotateToPath(rawProgress);
                }
                else
                {
                    transform.position = basePosition;
                    RotateToDirection(travelDirection);
                }
            }
            else
            {
                rawProgress = Mathf.Clamp01(flightElapsed / flightDuration);
                transform.position = EvaluateArcPosition(rawProgress);
                RotateToPath(rawProgress);
            }

            if (rawProgress >= 1f)
                ResolveImpact();
        }

        private void BuildFlightPath(Vector3 startPosition, Vector3 endPosition)
        {
            float distance = Vector3.Distance(startPosition, endPosition);

            pathStartPosition = startPosition;
            pathEndPosition = endPosition;

            if (travelMode == ProjectileTravelMode.BallisticArc)
            {
                float heightAtApex = GetBallisticApexHeight(startPosition, endPosition);
                float ascentHeight = heightAtApex - startPosition.y;
                float descentHeight = heightAtApex - endPosition.y;
                float timeFromApexToTarget = ballisticTimeToApex * Mathf.Sqrt(descentHeight / ascentHeight);
                flightDuration = Mathf.Max(flightData.MinimumFlightDuration,
                    ballisticTimeToApex + timeFromApexToTarget * ballisticDescentTimeMultiplier);
            }
            else if (IsAcceleratedTravel())
            {
                float averageSpeed = Mathf.Max(0.01f, (travelStartSpeed + travelEndSpeed) * 0.5f);
                flightDuration = Mathf.Max(flightData.MinimumFlightDuration, distance / averageSpeed);
            }
            else
            {
                flightDuration = Mathf.Max(flightData.MinimumFlightDuration, distance / flightData.FlightSpeed);
            }
        }

        private Vector3 EvaluateBallisticPosition(float progress)
        {
            float elapsedSeconds = progress * flightDuration;
            Vector3 horizontalDisplacement = Vector3.ProjectOnPlane(pathEndPosition - pathStartPosition, Vector3.up);
            Vector3 position = pathStartPosition + horizontalDisplacement * progress;

            float heightAtApex = GetBallisticApexHeight(pathStartPosition, pathEndPosition);
            if (elapsedSeconds <= ballisticTimeToApex)
            {
                float ascentProgress = elapsedSeconds / ballisticTimeToApex;
                float ascentHeight = heightAtApex - pathStartPosition.y;
                position.y = pathStartPosition.y + ascentHeight *
                    (2f * ascentProgress - ascentProgress * ascentProgress);
            }
            else
            {
                float descentSeconds = Mathf.Max(0f, elapsedSeconds - ballisticTimeToApex);
                float descentDuration = Mathf.Max(0.01f, flightDuration - ballisticTimeToApex);
                float descentProgress = Mathf.Clamp01(descentSeconds / descentDuration);
                float descentHeight = heightAtApex - pathEndPosition.y;
                position.y = heightAtApex - descentHeight * descentProgress * descentProgress;
            }

            return position;
        }

        private float GetBallisticApexHeight(Vector3 startPosition, Vector3 endPosition)
        {
            float configuredApexHeight = startPosition.y + ballisticApexHeight;
            float minimumHeightAboveTarget = endPosition.y + ballisticTargetClearance;
            return Mathf.Max(configuredApexHeight, minimumHeightAboveTarget);
        }

        private bool DidSegmentReachTarget(Vector3 segmentStart, Vector3 segmentEnd, Vector3 targetPosition)
        {
            Vector3 segment = segmentEnd - segmentStart;
            float segmentLengthSquared = segment.sqrMagnitude;
            float closestProgress = segmentLengthSquared > Mathf.Epsilon
                ? Mathf.Clamp01(Vector3.Dot(targetPosition - segmentStart, segment) / segmentLengthSquared)
                : 0f;
            Vector3 closestPosition = segmentStart + segment * closestProgress;
            return (closestPosition - targetPosition).sqrMagnitude <= ballisticHitRadius * ballisticHitRadius;
        }

        private Vector3 EvaluateBallisticTangent(float progress)
        {
            float previousProgress = Mathf.Clamp01(progress - ArcTangentSampleDistance);
            float nextProgress = Mathf.Clamp01(progress + ArcTangentSampleDistance);
            return EvaluateBallisticPosition(nextProgress) - EvaluateBallisticPosition(previousProgress);
        }

        private void SwitchToMissPath()
        {
            UnregisterIncomingFire();
            float currentProgress = Mathf.Clamp01(flightElapsed / flightDuration);
            Vector3 currentPosition = transform.position;
            Vector3 currentDirection = EvaluateArcTangent(currentProgress).normalized;
            Vector3 waterPosition = launchData.WaterImpactPosition;
            float distance = Vector3.Distance(currentPosition, waterPosition);

            if (currentDirection.sqrMagnitude <= Mathf.Epsilon)
                currentDirection = (waterPosition - currentPosition).normalized;

            pathStartPosition = currentPosition;
            pathEndPosition = waterPosition;
            missControlPoint = currentPosition + currentDirection * distance * 0.5f;

            flightElapsed = 0f;
            flightDuration = Mathf.Max(flightData.MinimumFlightDuration, distance / flightData.FlightSpeed);
            isMissPath = true;
        }

        private void ResolveImpact()
        {
            UnregisterIncomingFire();
            bool didHitTarget = isMissPath == false && !isLuckMiss && IsOriginalTargetAlive() &&
                                (!IsBallisticTravel() ||
                                 DidSegmentReachTarget(previousFramePosition, transform.position, launchData.TargetImpactPosition));
            if (didHitTarget)
                OnTargetImpact(launchData.Target, launchData.Damage, pathEndPosition);
            else
            {
                if (isLuckMiss && IsOriginalTargetAlive())
                    launchData.Target.GetComponent<ShipMissFeedback>()?.Play();
                OnWaterImpact(pathEndPosition);
            }

            ProjectileResolution resolution = didHitTarget
                ? ProjectileResolution.TargetHit
                : ProjectileResolution.WaterHit;
            ReportResolution(resolution);

            if (impactVisualHoldDurationSeconds > 0f)
            {
                impactResolution = resolution;
                impactHoldRemainingSeconds = impactVisualHoldDurationSeconds;
                isImpactHolding = true;
                return;
            }

            ReleaseToPool(resolution);
        }

        private bool IsOriginalTargetAlive()
        {
            return launchData.Target != null &&
                   launchData.Target.isActiveAndEnabled &&
                   launchData.Target.IsDead == false &&
                   launchData.Target.SpawnGeneration == launchData.TargetSpawnGeneration;
        }

        private void RotateToPath(float rawProgress)
        {
            if (rotateAlongPath == false)
                return;

            RotateToDirection(EvaluateArcTangent(rawProgress));
        }

        private void RotateToDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }

        internal Vector3 EvaluateArcPosition(float rawProgress)
        {
            if (isMissPath)
                return EvaluateMissPosition(rawProgress);

            Vector3 basePosition = Vector3.Lerp(pathStartPosition, pathEndPosition, rawProgress);
            float arcOffset = ArcPeakMultiplier * flightData.ArcHeight * rawProgress * (1f - rawProgress);
            return basePosition + Vector3.up * arcOffset;
        }

        private Vector3 EvaluateMissPosition(float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * pathStartPosition +
                   2f * inverse * progress * missControlPoint +
                   progress * progress * pathEndPosition;
        }

        private Vector3 EvaluateArcTangent(float rawProgress)
        {
            float previousRawProgress = Mathf.Clamp01(rawProgress - ArcTangentSampleDistance);
            float nextRawProgress = Mathf.Clamp01(rawProgress + ArcTangentSampleDistance);
            Vector3 previousPosition = EvaluateArcPosition(previousRawProgress);
            Vector3 nextPosition = EvaluateArcPosition(nextRawProgress);
            return nextPosition - previousPosition;
        }

        private bool IsAcceleratedTravel()
        {
            return travelMode == ProjectileTravelMode.AcceleratingStraight ||
                   travelMode == ProjectileTravelMode.AcceleratingArc;
        }

        private bool IsBallisticTravel() => travelMode == ProjectileTravelMode.BallisticArc;

        private float EvaluateAcceleratedProgress(float progress)
        {
            float speedRange = travelEndSpeed - travelStartSpeed;
            float averageSpeed = (travelStartSpeed + travelEndSpeed) * 0.5f;
            if (averageSpeed <= 0.01f || speedRange <= 0.01f)
                return progress;

            // Integrate a linearly increasing speed, then normalize so both ends land exactly.
            float distanceRatio =
                (travelStartSpeed * progress + 0.5f * speedRange * progress * progress) / averageSpeed;
            return Mathf.Clamp01(distanceRatio);
        }

        private void Expire()
        {
            OnExpired(transform.position);
            ReleaseToPool(ProjectileResolution.Expired);
        }

        private void HandleStageBattleEnded(StageBattleEndedEvent evt)
        {
            ReleaseToPool();
        }

        private void HandleFinalSalvoStarted(MainShipFinalSalvoStartedEvent evt)
        {
            CancelFlight();
        }

        public void CancelFlight()
        {
            ReleaseToPool();
        }

        private void ReleaseToPool(ProjectileResolution resolution = ProjectileResolution.Cancelled)
        {
            UnregisterIncomingFire();
            if (isFlying == false)
                return;

            isFlying = false;
            isImpactHolding = false;
            if (hasReportedResolution == false)
                ReportResolution(resolution);

            if (PoolingManager.Instance != null)
                PoolingManager.Instance.Release(this);
            else
                gameObject.SetActive(false);
        }

        protected abstract void OnTargetImpact(Ship target, float damage, Vector3 impactPosition);

        protected virtual void OnWaterImpact(Vector3 impactPosition)
        {
        }

        protected virtual void OnExpired(Vector3 lastPosition)
        {
        }

        public void OnSpawnedFromPool()
        {
            UnregisterIncomingFire();
            isFlying = false;
            isImpactHolding = false;
            hasReportedResolution = false;
            impactHoldRemainingSeconds = 0f;
            isMissPath = false;
            isLuckMiss = false;
            travelMode = ProjectileTravelMode.Arc;
            flightElapsed = 0f;
            lifeElapsed = 0f;
            ClearTrails();
        }

        public void OnDespawnedToPool()
        {
            UnregisterIncomingFire();
            NotifyCancelledFlight();
        }

        private void NotifyCancelledFlight()
        {
            if (!isFlying)
                return;

            isFlying = false;
            isImpactHolding = false;
            if (hasReportedResolution == false)
                ReportResolution(ProjectileResolution.Cancelled);
        }

        private void ReportResolution(ProjectileResolution resolution)
        {
            if (hasReportedResolution)
                return;

            hasReportedResolution = true;
            Resolved?.Invoke(this, resolution);
        }

        private void UnregisterIncomingFire()
        {
            if (incomingFireDisplay != null)
                incomingFireDisplay.Unregister(this);
            incomingFireDisplay = null;
        }

        private void ClearTrails()
        {
            CacheRenderers();

            if (trailRenderers == null)
                return;

            for (int i = 0; i < trailRenderers.Length; i++)
                trailRenderers[i].Clear();
        }

        private void ApplySortingOrder(Vector3 startPosition, Vector3 targetPosition)
        {
            CacheRenderers();

            float sortY = Mathf.Min(startPosition.y, targetPosition.y);
            int sortingOrder = projectileBaseSortingOrder + Mathf.RoundToInt(-sortY * sortingOrderPerUnit);

            if (projectileRenderer != null)
                projectileRenderer.sortingOrder = sortingOrder;

            if (trailRenderers == null)
                return;

            int trailSortingOrder = sortingOrder + trailSortingOrderOffset;
            int sortingLayerId = projectileRenderer != null
                ? projectileRenderer.sortingLayerID
                : 0;

            for (int i = 0; i < trailRenderers.Length; i++)
            {
                trailRenderers[i].sortingLayerID = sortingLayerId;
                trailRenderers[i].sortingOrder = trailSortingOrder;
            }
        }

        private void CacheRenderers()
        {
            if (projectileRenderer == null)
                projectileRenderer = GetComponent<Renderer>();

            if (trailRenderers == null)
                trailRenderers = GetComponentsInChildren<TrailRenderer>();
        }
    }
}


