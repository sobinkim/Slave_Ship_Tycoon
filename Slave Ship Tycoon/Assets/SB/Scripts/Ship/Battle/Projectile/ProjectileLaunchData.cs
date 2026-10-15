using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public enum ProjectileHitMode
    {
        Normal,
        Guaranteed
    }

    public readonly struct ProjectileLaunchData
    {
        public readonly Ship Target;
        public readonly int TargetSpawnGeneration;
        public readonly Vector3 StartPosition;
        public readonly Vector3 TargetImpactPosition;
        public readonly Vector3 WaterImpactPosition;
        public readonly ProjectileFlightData FlightData;
        public readonly float Damage;
        public readonly ProjectileHitMode HitMode;
        public readonly float HitChanceMultiplier;

        public ProjectileLaunchData(
            Ship target,
            Vector3 startPosition,
            Vector3 targetImpactPosition,
            Vector3 waterImpactPosition,
            ProjectileFlightData flightData,
            float damage,
            ProjectileHitMode hitMode = ProjectileHitMode.Normal,
            float hitChanceMultiplier = 1f)
        {
            Target = target;
            TargetSpawnGeneration = target != null ? target.SpawnGeneration : 0;
            StartPosition = startPosition;
            TargetImpactPosition = targetImpactPosition;
            WaterImpactPosition = waterImpactPosition;
            FlightData = flightData;
            Damage = damage;
            HitMode = hitMode;
            HitChanceMultiplier = Mathf.Clamp01(hitChanceMultiplier);
        }
    }
}
