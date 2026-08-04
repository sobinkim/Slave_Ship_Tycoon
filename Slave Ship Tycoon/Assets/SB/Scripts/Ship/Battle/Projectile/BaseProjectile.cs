using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public abstract class BaseProjectile : MonoBehaviour, IPoolable
    {
        [Header("Flight")]
        [SerializeField, Min(0.01f)] private float flightSpeed = 10f;
        [SerializeField, Min(0f)] private float arcHeight = 2f;
        [SerializeField, Range(0.05f, 0.45f)] private float controlPointRatio = 0.25f;
        [SerializeField, Min(0.01f)] private float minimumFlightDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float lifeTime = 5f;
        [SerializeField] private bool rotateAlongPath = true;
        [SerializeField] private float rotationOffset;

        [Header("Sorting")]
        [SerializeField] private int projectileBaseSortingOrder = 3000;
        [SerializeField] private float sortingOrderPerUnit = 100f;
        [SerializeField] private int trailSortingOrderOffset = -1;

        private ProjectileLaunchData launchData;
        private Vector3 point0;
        private Vector3 point1;
        private Vector3 point2;
        private Vector3 point3;
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
            transform.position = data.StartPosition;

            flightElapsed = 0f;
            lifeElapsed = 0f;
            isFlying = true;
            isMissPath = false;

            ApplySortingOrder(data.StartPosition, data.TargetImpactPosition);
            BuildInitialPath(data.StartPosition, data.TargetImpactPosition);
        }

        private void Update()
        {
            if (isFlying == false)
                return;

            lifeElapsed += Time.deltaTime;
            if (lifeElapsed >= lifeTime)
            {
                Expire();
                return;
            }

            if (isMissPath == false && IsOriginalTargetAlive() == false)
                SwitchToMissPath();

            flightElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(flightElapsed / flightDuration);

            transform.position = EvaluateBezier(progress);
            RotateToPath(progress);

            if (progress >= 1f)
                ResolveImpact();
        }

        private void BuildInitialPath(Vector3 startPosition, Vector3 endPosition)
        {
            float direction = ResolveHorizontalDirection(startPosition, endPosition);
            float distance = Vector3.Distance(startPosition, endPosition);
            float controlDistance = Mathf.Max(0.1f, Mathf.Abs(endPosition.x - startPosition.x) * controlPointRatio);
            float apexY = Mathf.Max(startPosition.y, endPosition.y) + arcHeight;

            point0 = startPosition;
            point1 = startPosition + Vector3.right * direction * controlDistance;
            point2 = new Vector3(
                endPosition.x - direction * controlDistance,
                apexY,
                Mathf.Lerp(startPosition.z, endPosition.z, 0.75f));
            point3 = endPosition;

            flightDuration = Mathf.Max(minimumFlightDuration, distance / flightSpeed);
        }

        private void SwitchToMissPath()
        {
            Vector3 currentPosition = transform.position;
            Vector3 currentDirection = EvaluateBezierTangent(
                Mathf.Clamp01(flightElapsed / flightDuration)).normalized;
            Vector3 waterPosition = launchData.WaterImpactPosition;
            float distance = Vector3.Distance(currentPosition, waterPosition);
            float controlDistance = Mathf.Max(0.1f, distance * controlPointRatio);

            if (currentDirection.sqrMagnitude <= Mathf.Epsilon)
                currentDirection = (waterPosition - currentPosition).normalized;

            point0 = currentPosition;
            point1 = currentPosition + currentDirection * controlDistance;
            point2 = Vector3.Lerp(currentPosition, waterPosition, 0.7f) + Vector3.up * arcHeight * 0.2f;
            point3 = waterPosition;

            flightElapsed = 0f;
            flightDuration = Mathf.Max(minimumFlightDuration, distance / flightSpeed);
            isMissPath = true;
        }

        private void ResolveImpact()
        {
            if (isMissPath == false && IsOriginalTargetAlive())
                OnTargetImpact(launchData.Target, launchData.Damage, point3);
            else
                OnWaterImpact(point3);

            ReleaseToPool();
        }

        private bool IsOriginalTargetAlive()
        {
            return launchData.Target != null &&
                   launchData.Target.IsDead == false &&
                   launchData.Target.SpawnGeneration == launchData.TargetSpawnGeneration;
        }

        private void RotateToPath(float progress)
        {
            if (rotateAlongPath == false)
                return;

            Vector3 direction = EvaluateBezierTangent(progress);
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }

        private Vector3 EvaluateBezier(float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * inverse * point0 +
                   3f * inverse * inverse * progress * point1 +
                   3f * inverse * progress * progress * point2 +
                   progress * progress * progress * point3;
        }

        private Vector3 EvaluateBezierTangent(float progress)
        {
            float inverse = 1f - progress;
            return 3f * inverse * inverse * (point1 - point0) +
                   6f * inverse * progress * (point2 - point1) +
                   3f * progress * progress * (point3 - point2);
        }

        private static float ResolveHorizontalDirection(Vector3 startPosition, Vector3 endPosition)
        {
            float direction = Mathf.Sign(endPosition.x - startPosition.x);
            return Mathf.Approximately(direction, 0f) ? 1f : direction;
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
