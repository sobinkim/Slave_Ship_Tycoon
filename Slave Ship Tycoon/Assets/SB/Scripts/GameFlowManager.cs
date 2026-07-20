using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    [Serializable]
    public struct ChapterData
    {
        public int MaxStage;
    }

    public enum StageRoomType
    {
        Normal,
        ChapterEnd
    }

    public class GameFlowManager : MonoBehaviour
    {
        private int currentChapter = 1;
        private int currentStage = 1;
        private StageRoomType currentStageType;
        //시작 하는 시점에 현재 상태를 저장하기

        public ChapterData[] Chapters;

        private void OnEnable()
        {
            Bus<StageStartedEvent>.OnEvent += CheckRoomType;
            Bus<StageBattleEndedEvent>.OnEvent += UpdateCurrentChapterData;
        }

        private void OnDisable()
        {
            Bus<StageStartedEvent>.OnEvent -= CheckRoomType;
            Bus<StageBattleEndedEvent>.OnEvent -= UpdateCurrentChapterData;
        }

        public void StartStage()
        {
            //입력 받은 정보에 맞는 전투 진행 시키기
            Bus<StageStartedEvent>.Raise(new StageStartedEvent(currentChapter, currentStage));
        }

        private void StageClear()
        {
            Bus<StageClearedEvent>.Raise(new StageClearedEvent(currentChapter, currentStage));
        }

        private void StageFail()
        {
            Bus<StageFailedEvent>.Raise(new StageFailedEvent(currentChapter, currentStage));
        }

        public void LoopClear()
        {
            Bus<LoopClearEvent>.Raise(new LoopClearEvent(currentChapter, currentStage));
        }

        private void CheckRoomType(StageStartedEvent evt)
        {
            if (evt.Stage == Chapters[currentChapter - 1].MaxStage)
                currentStageType = StageRoomType.ChapterEnd;
            else
                currentStageType = StageRoomType.Normal;
        }

        private void UpdateCurrentChapterData(StageBattleEndedEvent evt)
        {
            if (evt.IsClear)
            {
                StageClear();

                if (currentStageType == StageRoomType.Normal)
                    currentStage++;
                else if (currentStageType == StageRoomType.ChapterEnd)
                {
                    LoopClear();
                    currentChapter++;
                    currentStage = 1;
                }
            }
            else
            {
                StageFail();
            }

            //상황에 따라 재전투, 다음 스테이지 전투로 넘어가는 역할
            StartStage();
        }
    }
}
