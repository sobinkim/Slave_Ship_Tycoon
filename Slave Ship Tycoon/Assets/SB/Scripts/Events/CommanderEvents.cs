using SB.Scripts.Commander;

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

    public readonly struct CommanderSkillStateChangedEvent : IEvent
    {
        public readonly CommanderSkillType SkillType;

        public CommanderSkillStateChangedEvent(CommanderSkillType skillType)
        {
            SkillType = skillType;
        }
    }

    public readonly struct CommanderSkillUsedEvent : IEvent
    {
        public readonly CommanderSkillType SkillType;

        public CommanderSkillUsedEvent(CommanderSkillType skillType)
        {
            SkillType = skillType;
        }
    }

    public readonly struct CommanderSkillLoadoutChangedEvent : IEvent
    {
    }
}
