using SB.Scripts;

namespace SB.Core.EventBus
{
    public readonly struct StageStartedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;
        public readonly ChapterRouteType RouteType;
        public readonly bool IsBossBattle;

        public StageStartedEvent(int chapter, int stage)
            : this(chapter, stage, ChapterRouteType.Obtain, false)
        {
        }

        public StageStartedEvent(int chapter, int stage, ChapterRouteType routeType, bool isBossBattle)
        {
            Chapter = chapter;
            Stage = stage;
            RouteType = routeType;
            IsBossBattle = isBossBattle;
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
        public readonly bool IsBossBattle;

        public StageClearedEvent(int chapter, int stage, bool isBossBattle)
        {
            Chapter = chapter;
            Stage = stage;
            IsBossBattle = isBossBattle;
        }
    }

    public readonly struct StageFailedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;
        public readonly bool IsBossBattle;

        public StageFailedEvent(int chapter, int stage, bool isBossBattle)
        {
            Chapter = chapter;
            Stage = stage;
            IsBossBattle = isBossBattle;
        }
    }

    public readonly struct OddStageClearedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;
        public readonly bool IsBossBattle;

        public OddStageClearedEvent(int chapter, int stage, bool isBossBattle)
        {
            Chapter = chapter;
            Stage = stage;
            IsBossBattle = isBossBattle;
        }
    }

    public readonly struct OddStageFailedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;
        public readonly bool IsBossBattle;

        public OddStageFailedEvent(int chapter, int stage, bool isBossBattle)
        {
            Chapter = chapter;
            Stage = stage;
            IsBossBattle = isBossBattle;
        }
    }
   
    public readonly struct EvenStageClearedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;
        public readonly bool IsBossBattle;

        public EvenStageClearedEvent(int chapter, int stage, bool isBossBattle)
        {
            Chapter = chapter;
            Stage = stage;
            IsBossBattle = isBossBattle;
        }
    }

    public readonly struct EvenStageFailedEvent : IEvent
    {
        public readonly int Chapter;
        public readonly int Stage;
        public readonly bool IsBossBattle;

        public EvenStageFailedEvent(int chapter, int stage, bool isBossBattle)
        {
            Chapter = chapter;
            Stage = stage;
            IsBossBattle = isBossBattle;
        }
    }

    public readonly struct PlayStageClearEffectEvent : IEvent
    {
        public readonly ChapterRouteType ClearChapterType;
        public readonly int Chapter;
        public readonly int Stage;

        public PlayStageClearEffectEvent(int chapter, int stage, ChapterRouteType clearChapterType)
        {
            Chapter = chapter;
            Stage = stage;
            ClearChapterType = clearChapterType;
        }
    }
    
    
    public readonly struct AffterStageClearEvent : IEvent
    {
        public readonly ChapterRouteType ClearChapterType;
        public readonly int Chapter;
        public readonly int Stage;

        public AffterStageClearEvent(int chapter, int stage, ChapterRouteType clearChapterType)
        {
            Chapter = chapter;
            Stage = stage;
            ClearChapterType = clearChapterType;
        }
    }

    public readonly struct ObtainChapterStartedEvent : IEvent
    {
        public readonly int Chapter;

        public ObtainChapterStartedEvent(int chapter)
        {
            Chapter = chapter;
        }
    }

    public readonly struct SellChapterStartedEvent : IEvent
    {
        public readonly int Chapter;

        public SellChapterStartedEvent(int chapter)
        {
            Chapter = chapter;
        }
    }

    public readonly struct SellChapterCompletedEvent : IEvent
    {
        public readonly int Chapter;

        public SellChapterCompletedEvent(int chapter)
        {
            Chapter = chapter;
        }
    }
}
