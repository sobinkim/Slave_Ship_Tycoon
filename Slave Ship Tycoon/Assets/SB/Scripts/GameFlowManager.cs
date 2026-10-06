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
        [Header("References")]
        [SerializeField] private ChapterDatabase chapterDatabase;
        [SerializeField, Tooltip("보스 처치 후 운송 해역으로 진입할 때 저장된 적재 설정을 실제 운송 화물로 복사합니다.")]
        private CargoManager cargoManager;
        private int currentChapter = 1;
        private int currentStage = 1;
        private StageRoomType currentStageType;
        private ChapterRouteType currentRouteType;
        private bool currentBattleIsBoss;
        private Coroutine _stageRestartRoutine;

        public bool CanStartBossBattle => !currentBattleIsBoss && chapterDatabase != null &&
            chapterDatabase.TryGetChapter(currentChapter, out ChapterContainer chapter) &&
            chapter.RouteType == ChapterRouteType.Obtain &&
            chapterDatabase.TryGetBoss(currentChapter, out StageEnemyLayout _);

        private void Awake()
        {
            if (chapterDatabase == null || cargoManager == null)
                Debug.LogError("GameFlowManager requires a chapter database and cargo manager.", this);
        }

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
            if (currentBattleIsBoss)
                return;

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

            if (cargoManager == null || !cargoManager.HasValidLoadout)
            {
                Debug.LogWarning("Choose a valid cargo loadout within capacity before challenging the boss.", this);
                return;
            }

            if (_stageRestartRoutine != null)
            {
                StopCoroutine(_stageRestartRoutine);
                _stageRestartRoutine = null;
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

            if (chapterDatabase == null)
                return;

            if (chapterDatabase.TryGetChapter(currentChapter, out ChapterContainer chapter) &&
                chapter.RouteType == ChapterRouteType.Obtain)
            {
                Bus<ObtainChapterStartedEvent>.Raise(
                    new ObtainChapterStartedEvent(currentChapter));
            }
            else if (chapter != null && chapter.RouteType == ChapterRouteType.Sell)
            {
                // The manifest must exist before market prices and settlement snapshot subscribers run.
                if (cargoManager == null || !cargoManager.PrepareTransportCargo())
                {
                    Debug.LogError("Transport chapter could not prepare its cargo manifest.", this);
                    return;
                }

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
