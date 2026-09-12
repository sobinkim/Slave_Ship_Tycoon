using SB.Core.EventBus;
using System.Collections;
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
        private ChapterRouteType currentRouteType;
        private bool currentBattleIsBoss;
        private Coroutine _stageRestartRoutine;

        private void OnEnable()
        {
            Bus<StageBattleEndedEvent>.OnEvent += UpdateCurrentChapterData;
            Bus<AffterStageClearEvent>.OnEvent += HandleAfterStageClear;
        }

        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= UpdateCurrentChapterData;
            Bus<AffterStageClearEvent>.OnEvent -= HandleAfterStageClear;

            if (_stageRestartRoutine != null)
            {
                StopCoroutine(_stageRestartRoutine);
                _stageRestartRoutine = null;
            }
        }
        private void Update()
        {
            if (Time.timeScale > 0f && Input.GetKeyDown(KeyCode.Space))
                StartStage();
        }

        public void StartStage()
        {
            if (chapterDatabase == null ||
                chapterDatabase.TryGetChapter(currentChapter, out ChapterContainer chapter) == false)
            {
                Debug.Log($"Chapter {currentChapter} data was not found. Stage loop stopped.", this);
                return;
            }

            currentRouteType = chapter.RouteType;
            currentBattleIsBoss = false;

            if (currentRouteType == ChapterRouteType.Obtain)
            {
                if (chapterDatabase.TryGetRandomWave(currentChapter, out StageEnemyLayout wave) == false)
                {
                    Debug.LogError($"No wave layouts found for Obtain Chapter {currentChapter}.", this);
                    return;
                }

                currentStage = wave.Stage;
                currentStageType = StageRoomType.Normal;
            }
            else
            {
                if (chapterDatabase.TryGetStage(currentChapter, currentStage, out StageEnemyLayout stage) == false)
                {
                    Debug.LogError($"Stage {currentChapter}-{currentStage} data was not found.", this);
                    return;
                }

                currentStageType = chapter.IsFinalStage(currentStage)
                    ? StageRoomType.ChapterEnd
                    : StageRoomType.Normal;
            }

            Debug.Log($"{currentChapter}-{currentStage} {currentRouteType} started.", this);
            RaiseStageStartedEvent();
        }

        public void StartBossBattle()
        {
            if (chapterDatabase == null ||
                chapterDatabase.TryGetChapter(currentChapter, out ChapterContainer chapter) == false)
            {
                Debug.LogError($"Chapter {currentChapter} data was not found. Boss battle cannot start.", this);
                return;
            }

            if (chapter.RouteType != ChapterRouteType.Obtain)
            {
                Debug.LogWarning("Boss battle can only be started during an Obtain chapter.", this);
                return;
            }

            if (chapterDatabase.TryGetBoss(currentChapter, out StageEnemyLayout _) == false)
            {
                Debug.LogError($"Boss layout was not assigned for Chapter {currentChapter}.", this);
                return;
            }

            currentRouteType = chapter.RouteType;
            currentBattleIsBoss = true;
            currentStageType = StageRoomType.ChapterEnd;
            currentStage = 0;

            Debug.Log($"Chapter {currentChapter} boss battle started.", this);
            RaiseStageStartedEvent();
        }

        private void RaiseStageStartedEvent()
        {
            Bus<StageStartedEvent>.Raise(new StageStartedEvent(
                currentChapter,
                currentStage,
                currentRouteType,
                currentBattleIsBoss));
        }

        private void StageClear()
        {
            Bus<StageClearedEvent>.Raise(new StageClearedEvent(currentChapter, currentStage,currentBattleIsBoss));

            if (currentRouteType == ChapterRouteType.Obtain)
            {
                Bus<OddStageClearedEvent>.Raise(new OddStageClearedEvent(
                    currentChapter,
                    currentStage,
                    currentBattleIsBoss));
            }
            else
            {
                Bus<EvenStageClearedEvent>.Raise(new EvenStageClearedEvent(
                    currentChapter,
                    currentStage,
                    currentBattleIsBoss));
            }
        }

        private void StageFail()
        {
            Bus<StageFailedEvent>.Raise(new StageFailedEvent(currentChapter, currentStage,currentBattleIsBoss));

            if (currentRouteType == ChapterRouteType.Obtain)
            {
                Bus<OddStageFailedEvent>.Raise(new OddStageFailedEvent(
                    currentChapter,
                    currentStage,
                    currentBattleIsBoss));
            }
            else
            {
                Bus<EvenStageFailedEvent>.Raise(new EvenStageFailedEvent(
                    currentChapter,
                    currentStage,
                    currentBattleIsBoss));
            }
        }

        public void LoopClear()
        {
            Bus<PlayStageClearEffectEvent>.Raise(new PlayStageClearEffectEvent(currentChapter, currentStage, currentRouteType));
        }

        private void UpdateCurrentChapterData(StageBattleEndedEvent evt)
        {
            if (evt.IsClear)
            {
                StageClear();

                if (currentRouteType == ChapterRouteType.Obtain && currentBattleIsBoss == false)
                {
                    QueueStartStage();
                    return;
                }

                if (currentStageType == StageRoomType.ChapterEnd)
                {
                    LoopClear();
                    return;
                   
                }
                else
                {
                    currentStage++;
                }
            }
            else
            {
                StageFail();
            }

            QueueStartStage();
        }
        
        private void HandleAfterStageClear(AffterStageClearEvent evt)
        {
            if (evt.ClearChapterType == ChapterRouteType.Sell)
            {
                Bus<SellChapterCompletedEvent>.Raise(
                    new SellChapterCompletedEvent(evt.Chapter));
            }

            currentChapter++;
            currentStage = 1;

            if (chapterDatabase.TryGetChapter(currentChapter, out ChapterContainer chapter) &&
                chapter.RouteType == ChapterRouteType.Obtain)
            {
                Bus<ObtainChapterStartedEvent>.Raise(
                    new ObtainChapterStartedEvent(currentChapter));
            }
            else if (chapter != null && chapter.RouteType == ChapterRouteType.Sell)
            {
                Bus<SellChapterStartedEvent>.Raise(
                    new SellChapterStartedEvent(currentChapter));
            }

            QueueStartStage();
        }

        private void QueueStartStage()
        {
            if (_stageRestartRoutine != null)
                StopCoroutine(_stageRestartRoutine);

            _stageRestartRoutine = StartCoroutine(StartStageNextFrame());
        }

        private IEnumerator StartStageNextFrame()
        {
            yield return null;
            while (Time.timeScale == 0f)
                yield return null;
            _stageRestartRoutine = null;
            StartStage();
        }
    }
}
