namespace SB.Core.EventBus
{
    public readonly struct CameraShakeRequestedEvent : IEvent
    {
        public readonly float DistanceUnits;
        public readonly float DurationSeconds;

        public CameraShakeRequestedEvent(float distanceUnits, float durationSeconds)
        {
            DistanceUnits = distanceUnits;
            DurationSeconds = durationSeconds;
        }
    }
}
