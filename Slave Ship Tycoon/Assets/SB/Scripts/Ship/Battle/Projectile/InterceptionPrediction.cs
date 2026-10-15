using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public static class InterceptionPrediction
    {
        private const int SearchSteps = 64;
        private const int RefinementSteps = 14;

        // Find the earliest meeting point on the actual committed curve, including acceleration.
        public static bool TrySolve(BaseProjectile target, Vector3 origin, float speed,
            float minimumFlightSeconds, float impactMarginSeconds, out Vector3 point, out float seconds)
        {
            point = default;
            seconds = 0f;
            if (target == null || !target.CanBeIntercepted || speed <= 0f) return false;
            float end = target.RemainingPredictionSeconds - impactMarginSeconds;
            if (end < minimumFlightSeconds) return false;
            float previousTime = 0f;
            for (int i = 0; i <= SearchSteps; i++)
            {
                float time = Mathf.Lerp(minimumFlightSeconds, end, i / (float)SearchSteps);
                if (!target.TryPredictPosition(time, out Vector3 candidate)) return false;
                if (Vector3.Distance(origin, candidate) > speed * time)
                {
                    previousTime = time;
                    continue;
                }
                // Reaching before the minimum visible flight is not an eligible shot.
                if (i == 0) return false;
                float lower = previousTime;
                float upper = time;
                for (int step = 0; step < RefinementSteps; step++)
                {
                    float middle = (lower + upper) * .5f;
                    target.TryPredictPosition(middle, out candidate);
                    if (Vector3.Distance(origin, candidate) > speed * middle) lower = middle;
                    else upper = middle;
                }
                seconds = upper;
                return target.TryPredictPosition(seconds, out point);
            }
            return false;
        }

        public static bool SweptHit(Vector3 projectileStart, Vector3 projectileEnd,
            Vector3 targetStart, Vector3 targetEnd, float radius, out float fraction)
        {
            Vector3 start = projectileStart - targetStart;
            Vector3 delta = (projectileEnd - targetEnd) - start;
            fraction = delta.sqrMagnitude > Mathf.Epsilon
                ? Mathf.Clamp01(-Vector3.Dot(start, delta) / delta.sqrMagnitude) : 0f;
            return (start + delta * fraction).sqrMagnitude <= radius * radius;
        }
    }
}
