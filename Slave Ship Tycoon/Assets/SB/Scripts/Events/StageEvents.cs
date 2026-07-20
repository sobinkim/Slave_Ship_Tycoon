namespace SB.Core.EventBus
{
    public readonly struct StageStartedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;

        public StageStartedEvent(int chapter, int stage)
        {
            Chapter = chapter;
            Stage = stage;
        }
    }

    public readonly struct StageBattleEndedEvent : IEvent
    {
        public readonly bool IsClear;

        public StageBattleEndedEvent(bool isClear)
        {
            IsClear = isClear;
        }
    }

    public readonly struct StageClearedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;

        public StageClearedEvent(int chapter, int stage)
        {
            Chapter = chapter;
            Stage = stage;
        }
    }

    public readonly struct StageFailedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;

        public StageFailedEvent(int chapter, int stage)
        {
            Chapter = chapter;
            Stage = stage;
        }
    }

    public readonly struct LoopClearEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;

        public LoopClearEvent(int chapter, int stage)
        {
            Chapter = chapter;
            Stage = stage;
        }
    }
}
