using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class InterceptorProjectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer projectileRenderer;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private InterceptionBurst impactEffect;
        [SerializeField, Min(.001f), Tooltip("상대 운동 선분으로 판정하는 요격 반경")]
        private float hitRadius = .09f;
        [SerializeField, Min(.01f)] private float missGraceSeconds = .15f;
        [Header("Successful interception shake")]
        [SerializeField, Min(0f), Tooltip("요격 성공 시 화면이 움직이는 최대 월드 거리")]
        private float successShakeDistanceUnits = .03f;
        [SerializeField, Min(0f), Tooltip("요격 성공 시 화면 흔들림 시간")]
        private float successShakeDurationSeconds = .14f;

        private BaseProjectile target;
        private int targetGeneration;
        private Vector3 origin;
        private Vector3 velocity;
        private Vector3 previousTargetPosition;
        private float startTime;
        private float lifeSeconds;
        private bool isFlying;
        private int lastSweepFrame = -1;

        public bool IsFlying => isFlying;

        private void OnEnable()
        {
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
            Bus<MainShipFinalSalvoStartedEvent>.OnEvent += HandleFinalSalvo;
        }

        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            Bus<MainShipFinalSalvoStartedEvent>.OnEvent -= HandleFinalSalvo;
            DetachTarget();
            isFlying = false;
        }

        public bool Launch(BaseProjectile incoming, Vector3 start, Vector3 meetingPoint, float speed, float flightSeconds)
        {
            DetachTarget();
            if (incoming == null || !incoming.TryReserveInterception(this)) return false;
            target = incoming;
            targetGeneration = incoming.FlightGeneration;
            target.Resolved += HandleTargetResolved;
            previousTargetPosition = target.transform.position;
            origin = start;
            velocity = (meetingPoint - start).normalized * speed;
            transform.SetPositionAndRotation(start,
                Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg));
            startTime = Time.time;
            lifeSeconds = flightSeconds + missGraceSeconds;
            lastSweepFrame = -1;
            isFlying = true;
            if (projectileRenderer != null)
                projectileRenderer.sortingOrder = 3100 + Mathf.RoundToInt(-meetingPoint.y * 100f);
            if (trail != null)
            {
                trail.Clear();
                trail.sortingOrder = projectileRenderer != null ? projectileRenderer.sortingOrder - 1 : 3099;
            }
            return true;
        }

        private Vector3 PositionAt(float time) => origin + velocity * Mathf.Max(0f, time - startTime);

        // Called by the incoming projectile before damage; LateUpdate provides the visual step.
        internal bool TryInterceptStep(Vector3 targetStart, Vector3 targetEnd, bool sampleCommittedCurve = false)
        {
            if (!isFlying || target == null || target.FlightGeneration != targetGeneration) return false;
            lastSweepFrame = Time.frameCount;
            previousTargetPosition = targetEnd;
            float frameStart = Time.time - Time.deltaTime;
            float lower = Time.deltaTime > 0f ? Mathf.Clamp01((startTime - frameStart) / Time.deltaTime) : 0f;
            float upper = sampleCommittedCurve ? target.LastStepImpactFraction : 1f;
            if (lower > upper) return false;
            int steps = Mathf.Clamp(Mathf.CeilToInt(Time.deltaTime / .01f), 1, 64);
            Vector3 contact = default;
            bool hit = false;
            for (int i = 0; i < steps; i++)
            {
                float a = Mathf.Lerp(lower, upper, i / (float)steps);
                float b = Mathf.Lerp(lower, upper, (i + 1f) / steps);
                Vector3 start = PositionAt(frameStart + Time.deltaTime * a);
                Vector3 end = PositionAt(frameStart + Time.deltaTime * b);
                Vector3 enemyStart = sampleCommittedCurve ? target.EvaluateLastStepPosition(a) : Vector3.Lerp(targetStart, targetEnd, a);
                Vector3 enemyEnd = sampleCommittedCurve ? target.EvaluateLastStepPosition(b) : Vector3.Lerp(targetStart, targetEnd, b);
                if (!InterceptionPrediction.SweptHit(start, end, enemyStart, enemyEnd, hitRadius, out float fraction)) continue;
                contact = (Vector3.Lerp(start, end, fraction) +
                           Vector3.Lerp(enemyStart, enemyEnd, fraction)) * .5f;
                hit = true;
                break;
            }
            if (!hit) return false;
            BaseProjectile intercepted = target;
            intercepted.Resolved -= HandleTargetResolved;
            bool succeeded = intercepted.TryIntercept(this, targetGeneration);
            if (succeeded && impactEffect != null && PoolingManager.Instance != null)
                PoolingManager.Instance.Get(impactEffect, contact, Quaternion.identity);
            if (succeeded && successShakeDistanceUnits > 0f && successShakeDurationSeconds > 0f)
                Bus<CameraShakeRequestedEvent>.Raise(
                    new CameraShakeRequestedEvent(successShakeDistanceUnits, successShakeDurationSeconds));
            Release();
            return succeeded;
        }

        private void LateUpdate()
        {
            if (!isFlying) return;
            if (target == null || !target.IsFlying || target.FlightGeneration != targetGeneration)
            {
                Release();
                return;
            }
            if (lastSweepFrame != Time.frameCount &&
                TryInterceptStep(previousTargetPosition, target.transform.position)) return;
            transform.position = PositionAt(Time.time);
            if (Time.time - startTime >= lifeSeconds) Release();
        }

        private void HandleTargetResolved(BaseProjectile projectile, ProjectileResolution resolution) => Release();
        private void HandleBattleEnded(StageBattleEndedEvent evt) => Release();
        private void HandleFinalSalvo(MainShipFinalSalvoStartedEvent evt) => Release();

        private void DetachTarget()
        {
            if (target != null)
            {
                target.Resolved -= HandleTargetResolved;
                target.ReleaseInterceptionReservation(this);
            }
            target = null;
        }

        private void Release()
        {
            if (!isFlying) return;
            isFlying = false;
            DetachTarget();
            if (PoolingManager.Instance != null) PoolingManager.Instance.Release(this);
            else gameObject.SetActive(false);
        }

        public void OnSpawnedFromPool()
        {
            DetachTarget();
            isFlying = false;
            trail?.Clear();
        }

        public void OnDespawnedToPool()
        {
            DetachTarget();
            isFlying = false;
            trail?.Clear();
        }
    }
}
