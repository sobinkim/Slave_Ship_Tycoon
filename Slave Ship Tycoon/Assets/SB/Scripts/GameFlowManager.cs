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
        [SerializeField, Tooltip("운송 화물의 시세 연출을 마친 뒤 다음 전투를 시작합니다.")]
        private SB.UI.InGame.InGameToolkitScreen transportPresentation;
        [SerializeField, Tooltip("시세 연출 중 다음 항로의 출항 연출을 진행할 함대 스포너입니다.")]
        private StageSpawnManager stageSpawner;
        private int currentChapter = 1;
        private int currentStage = 1;
        private StageRoomType currentStageType;
        private ChapterRouteType currentRouteType;
        private bool currentBattleIsBoss;
        private Coroutine _stageRestartRoutine;
        private bool hasSuspendedTransport;
        private int suspendedTransportChapter;
        private int suspendedTransportStage;
        private bool isChapterTransitionPending;
        public bool IsPursuitActive { get; private set; }
        public bool CanStartPursuit => !IsPursuitActive && !currentBattleIsBoss &&
            !isChapterTransitionPending && currentRouteType == ChapterRouteType.Obtain;

        public bool TryEnterPursuit()
        {
            if (!CanStartPursuit) return false;
            CancelQueuedStageRestart();
            IsPursuitActive = true;
            return true;
        }

        public void LeavePursuit(bool resumeFarming)
        {
            IsPursuitActive = false;
            if (resumeFarming) QueueStartStage();
        }

        public bool HasSuspendedTransport => hasSuspendedTransport;
        public int SuspendedTransportChapter => suspendedTransportChapter;
        public int SuspendedTransportStage => suspendedTransportStage;

        public bool CanReturnToFarming => currentRouteType == ChapterRouteType.Sell &&
            !hasSuspendedTransport && !isChapterTransitionPending &&
            (transportPresentation == null || !transportPresentation.IsTransportBonusPlaying) &&
            HasFarmingChapter(currentChapter - 1);

        public bool CanResumeTransport => hasSuspendedTransport && !isChapterTransitionPending &&
            currentRouteType == ChapterRouteType.Obtain && currentChapter == suspendedTransportChapter - 1 &&
            chapterDatabase != null &&
            chapterDatabase.TryGetChapter(suspendedTransportChapter, out ChapterContainer chapter) &&
            chapter.RouteType == ChapterRouteType.Sell &&
            chapterDatabase.TryGetStage(suspendedTransportChapter, suspendedTransportStage, out StageEnemyLayout _);

        public bool CanStartBossBattle => !IsPursuitActive && !hasSuspendedTransport && !isChapterTransitionPending &&
            !currentBattleIsBoss && chapterDatabase != null &&
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

            CancelQueuedStageRestart();
        }
        private void Update()
        {
            if (!isChapterTransitionPending && _stageRestartRoutine == null && Time.timeScale > 0f && Input.GetKeyDown(KeyCode.Space) &&
                (transportPresentation == null || !transportPresentation.IsTransportBonusPlaying))
                StartStage();
        }

        public void StartStage()
        {
            if (IsPursuitActive) return;
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
            if (IsPursuitActive) return;
            if (currentBattleIsBoss || hasSuspendedTransport || isChapterTransitionPending)
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

            CancelQueuedStageRestart();

            currentRouteType = chapter.RouteType;
            currentBattleIsBoss = true;
            currentStageType = StageRoomType.ChapterEnd;
            currentStage = 0;

            Debug.Log($"Chapter {currentChapter} boss battle started.", this);
            RaiseStageStartedEvent();
        }

        public bool ReturnToFarming()
        {
            if (!CanReturnToFarming)
                return false;

            CancelQueuedStageRestart();
            suspendedTransportChapter = currentChapter;
            suspendedTransportStage = currentStage;
            hasSuspendedTransport = true;
            currentChapter = suspendedTransportChapter - 1;
            currentStage = 1;

            // Keep the manifest, market prices and departure equipment snapshot for resuming.
            StartStage();
            return true;
        }

        public bool ResumeTransport()
        {
            if (!CanResumeTransport)
                return false;

            CancelQueuedStageRestart();
            currentChapter = suspendedTransportChapter;
            currentStage = suspendedTransportStage;
            hasSuspendedTransport = false;
            suspendedTransportChapter = suspendedTransportStage = 0;

            // This resumes the existing departure; SellChapterStartedEvent would reset its value.
            StartStage();
            return true;
        }

        private bool HasFarmingChapter(int chapterNumber)
        {
            if (chapterDatabase == null ||
                !chapterDatabase.TryGetChapter(chapterNumber, out ChapterContainer chapter) ||
                chapter.RouteType != ChapterRouteType.Obtain)
                return false;

            foreach (StageEnemyLayout wave in chapter.Stages)
                if (wave != null)
                    return true;

            return false;
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
            isChapterTransitionPending = true;
            Bus<PlayStageClearEffectEvent>.Raise(new PlayStageClearEffectEvent(currentChapter, currentStage, currentRouteType));
        }

        private void UpdateCurrentChapterData(StageBattleEndedEvent evt)
        {
            if (IsPursuitActive) return;
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
            if (IsPursuitActive) return;
            isChapterTransitionPending = false;
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
            CancelQueuedStageRestart();
            _stageRestartRoutine = StartCoroutine(StartStageNextFrame());
        }

        private void CancelQueuedStageRestart()
        {
            stageSpawner?.CancelTransportDeparture();
            if (_stageRestartRoutine == null)
                return;

            StopCoroutine(_stageRestartRoutine);
            _stageRestartRoutine = null;
        }

        private IEnumerator StartStageNextFrame()
        {
            yield return null;
            if (currentRouteType == ChapterRouteType.Sell && transportPresentation != null &&
                transportPresentation.isActiveAndEnabled && transportPresentation.IsTransportBonusPlaying)
                stageSpawner?.PlayTransportDeparture();
            while (Time.timeScale == 0f ||
                (currentRouteType == ChapterRouteType.Sell && transportPresentation != null &&
                transportPresentation.isActiveAndEnabled && transportPresentation.IsTransportBonusPlaying))
                yield return null;
            _stageRestartRoutine = null;
            StartStage();
        }
    }
}
