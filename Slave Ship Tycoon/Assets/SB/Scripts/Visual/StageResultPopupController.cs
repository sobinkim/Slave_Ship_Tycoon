using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class StageResultPopupController : MonoBehaviour
    {
        [SerializeField] private StageResultPopupView resultPopupView;
        [SerializeField] private StageResultPopupView chapterEnterPopupView;

        private void OnEnable()
        {
            Bus<EvenStageClearedEvent>.OnEvent += HandleClearPop;
            Bus<StageFailedEvent>.OnEvent += HandleFailPop;
            Bus<LoopClearEvent>.OnEvent += HandleChapterEnterPop;
        }

        private void OnDisable()
        {
            Bus<EvenStageClearedEvent>.OnEvent -= HandleClearPop;
            Bus<StageFailedEvent>.OnEvent -= HandleFailPop;
            Bus<LoopClearEvent>.OnEvent -= HandleChapterEnterPop;
        }

        private void HandleFailPop(StageFailedEvent evt)
        {
            resultPopupView.Show(evt.Chapter, evt.Stage, "Stage Failed");
        }

        private void HandleClearPop(EvenStageClearedEvent evt)
        {
            resultPopupView.Show(evt.Chapter, evt.Stage, "Stage Clear");
        }

        private void HandleChapterEnterPop(LoopClearEvent evt)
        {
            int nextChapter = evt.Chapter + 1;

            if (evt.ClearChapterType == ChapterRouteType.Obtain)
                chapterEnterPopupView.Show(nextChapter, 1, "해군 순찰 해역 진입");
            else if (evt.ClearChapterType == ChapterRouteType.Sell)
                chapterEnterPopupView.Show(nextChapter, 1, "해적 출몰 해역 진입");
        }
    }
}
