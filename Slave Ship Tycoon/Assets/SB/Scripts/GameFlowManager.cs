using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public enum StageRoomType
    {
        Normal,
        ChapterEnd
    }

    public class GameFlowManager : MonoBehaviour
    {
        [SerializeField] private ChapterDatabase chapterDatabase;

        private int currentChapter = 1;
        private int currentStage = 1;
        private StageRoomType currentStageType;

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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                StartStage();
        }

        public void StartStage()
        {
            if (HasCurrentChapter() == false)
            {
                Debug.Log($"Chapter {currentChapter} data was not found. Stage loop stopped.", this);
                return;
            }

            print(currentChapter + "-" + currentStage + " 시작 ");
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
            if (chapterDatabase == null)
            {
                Debug.LogError($"{nameof(GameFlowManager)} needs a chapter database.", this);
                currentStageType = StageRoomType.Normal;
                return;
            }

            if (chapterDatabase.IsFinalStage(evt.Chapter, evt.Stage, out bool isFinalStage) == false)
            {
                Debug.LogError($"Chapter data not found for Chapter {evt.Chapter}, Stage {evt.Stage}.", this);
                currentStageType = StageRoomType.Normal;
                return;
            }

            currentStageType = isFinalStage ? StageRoomType.ChapterEnd : StageRoomType.Normal;
            print("현재 방 상태 확인 결과>>> " + currentStageType);
        }

        private void UpdateCurrentChapterData(StageBattleEndedEvent evt)
        {
            if (evt.IsClear)
            {
                StageClear();
                print("스테이지 격파 성공");

                if (currentStageType == StageRoomType.Normal)
                {
                    currentStage++;
                }
                else if (currentStageType == StageRoomType.ChapterEnd)
                {
                    LoopClear();
                    currentChapter++;
                    currentStage = 1;
                }
            }
            else
            {
                print("스테이지 격파 실패");
                StageFail();
            }

            StartStage();
        }

        private bool HasCurrentChapter()
        {
            return chapterDatabase != null &&
                   chapterDatabase.TryGetChapter(currentChapter, out ChapterContainer _);
        }
    }
}
