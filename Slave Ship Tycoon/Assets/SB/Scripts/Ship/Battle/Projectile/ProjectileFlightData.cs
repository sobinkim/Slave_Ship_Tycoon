using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [System.Serializable]
    public struct ProjectileFlightData
    {
        [Min(0.01f)] public float FlightSpeed;
        [Min(0f)] public float ArcHeight;
        [Min(0.01f)] public float MinimumFlightDuration;
        [Min(0.01f)] public float LifeTime;

        public static ProjectileFlightData Default => new ProjectileFlightData
        {
            FlightSpeed = 10f,
            ArcHeight = 2f,
            MinimumFlightDuration = 0.15f,
            LifeTime = 5f
        };

        public ProjectileFlightData GetValidated()
        {
            ProjectileFlightData data = this;

            if (data.FlightSpeed <= 0f)
                data.FlightSpeed = Default.FlightSpeed;

            if (data.ArcHeight < 0f)
                data.ArcHeight = Default.ArcHeight;

            if (data.MinimumFlightDuration <= 0f)
                data.MinimumFlightDuration = Default.MinimumFlightDuration;

            if (data.LifeTime <= 0f)
                data.LifeTime = Default.LifeTime;

            return data;
        }
    }
}
