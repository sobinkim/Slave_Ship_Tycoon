namespace SB.Core.EventBus
{
    public readonly struct CommanderGaugeChangedEvent : IEvent
    {
        public readonly float CurrentGauge;
        public readonly float MaxGauge;

        public CommanderGaugeChangedEvent(float currentGauge, float maxGauge)
        {
            CurrentGauge = currentGauge;
            MaxGauge = maxGauge;
        }
    }
}
