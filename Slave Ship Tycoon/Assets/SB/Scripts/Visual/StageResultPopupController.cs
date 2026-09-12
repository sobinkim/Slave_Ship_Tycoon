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
            Bus<AffterStageClearEvent>.OnEvent += HandleChapterEnterPop;
        }

        private void OnDisable()
        {
            Bus<EvenStageClearedEvent>.OnEvent -= HandleClearPop;
            Bus<StageFailedEvent>.OnEvent -= HandleFailPop;
            Bus<AffterStageClearEvent>.OnEvent -= HandleChapterEnterPop;
        }

        private void HandleFailPop(StageFailedEvent evt)
        {
            resultPopupView.Show(evt.Chapter, evt.Stage, "스테이지 실패");
        }

        private void HandleClearPop(EvenStageClearedEvent evt)
        {
            resultPopupView.Show(evt.Chapter, evt.Stage, "스테이지 클리어");
        }

        private void HandleChapterEnterPop(AffterStageClearEvent evt)
        {
            int nextChapter = evt.Chapter + 1;

            if (evt.ClearChapterType == ChapterRouteType.Obtain)
                chapterEnterPopupView.Show(nextChapter, 1, "해군 순찰 해역 진입");
            else if (evt.ClearChapterType == ChapterRouteType.Sell)
                chapterEnterPopupView.Show(nextChapter, 1, "해적 출몰 해역 진입");
        }
    }
}
