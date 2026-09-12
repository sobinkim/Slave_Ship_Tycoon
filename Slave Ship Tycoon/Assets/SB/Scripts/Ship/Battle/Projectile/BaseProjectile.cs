using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public abstract class BaseProjectile : MonoBehaviour, IPoolable
    {
        [Header("Flight")]
        [SerializeField] private bool rotateAlongPath = true;
        [SerializeField] private float rotationOffset;

        [Header("Sorting")]
        [SerializeField] private int projectileBaseSortingOrder = 3000;
        [SerializeField] private float sortingOrderPerUnit = 100f;
        [SerializeField] private int trailSortingOrderOffset = -1;

        private ProjectileLaunchData launchData;
        private ProjectileFlightData flightData;
        private Vector3 pathStartPosition;
        private Vector3 pathEndPosition;
        private Vector3 missControlPoint;
        private float flightDuration;
        private float flightElapsed;
        private float lifeElapsed;
        private bool isFlying;
        private bool isMissPath;
        private TrailRenderer[] trailRenderers;
        private Renderer projectileRenderer;

        private void Awake()
        {
            CacheRenderers();
        }

        public void Launch(ProjectileLaunchData data)
        {
            launchData = data;
            flightData = data.FlightData.GetValidated();
            transform.position = data.StartPosition;
            ClearTrails();

            flightElapsed = 0f;
            lifeElapsed = 0f;
            isFlying = true;
            isMissPath = false;

            ApplySortingOrder(data.StartPosition, data.TargetImpactPosition);
            BuildFlightPath(data.StartPosition, data.TargetImpactPosition);
        }

        private void Update()
        {
            if (isFlying == false)
                return;

            lifeElapsed += Time.deltaTime;
            if (lifeElapsed >= flightData.LifeTime)
            {
                Expire();
                return;
            }

            if (isMissPath == false && IsOriginalTargetAlive() == false)
                SwitchToMissPath();

            flightElapsed += Time.deltaTime;
            float rawProgress = Mathf.Clamp01(flightElapsed / flightDuration);
            transform.position = EvaluateArcPosition(rawProgress);
            RotateToPath(rawProgress);

            if (rawProgress >= 1f)
                ResolveImpact();
        }

        private void BuildFlightPath(Vector3 startPosition, Vector3 endPosition)
        {
            float distance = Vector3.Distance(startPosition, endPosition);

            pathStartPosition = startPosition;
            pathEndPosition = endPosition;

            flightDuration = Mathf.Max(flightData.MinimumFlightDuration, distance / flightData.FlightSpeed);
        }

        private void SwitchToMissPath()
        {
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
            if (isMissPath == false && IsOriginalTargetAlive())
                OnTargetImpact(launchData.Target, launchData.Damage, pathEndPosition);
            else
                OnWaterImpact(pathEndPosition);

            ReleaseToPool();
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

            Vector3 direction = EvaluateArcTangent(rawProgress);
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }

        private Vector3 EvaluateArcPosition(float rawProgress)
        {
            if (isMissPath)
                return EvaluateMissPosition(rawProgress);

            Vector3 basePosition = Vector3.Lerp(pathStartPosition, pathEndPosition, rawProgress);
            float arcOffset = 4f * flightData.ArcHeight * rawProgress * (1f - rawProgress);
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
            const float sampleDistance = 0.01f;

            float previousRawProgress = Mathf.Clamp01(rawProgress - sampleDistance);
            float nextRawProgress = Mathf.Clamp01(rawProgress + sampleDistance);
            Vector3 previousPosition = EvaluateArcPosition(previousRawProgress);
            Vector3 nextPosition = EvaluateArcPosition(nextRawProgress);
            return nextPosition - previousPosition;
        }

        private void Expire()
        {
            OnExpired(transform.position);
            ReleaseToPool();
        }

        private void ReleaseToPool()
        {
            if (isFlying == false)
                return;

            isFlying = false;

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
            isFlying = false;
            isMissPath = false;
            flightElapsed = 0f;
            lifeElapsed = 0f;
            ClearTrails();
        }

        public void OnDespawnedToPool()
        {
            isFlying = false;
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
