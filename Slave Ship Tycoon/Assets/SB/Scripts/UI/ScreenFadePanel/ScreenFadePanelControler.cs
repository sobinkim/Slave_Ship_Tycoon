using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.UI.ScreenFadePanel
{
    public class ScreenFadePanelControler : MonoBehaviour
    {
        [SerializeField] ScreenFadePanelView _view;

        private int clearChapter;
        private int clearStage;
        private ChapterRouteType clearChapterType;

        private void OnEnable()
        {
            Bus<PlayStageClearEffectEvent>.OnEvent += ShowScreenFadePanel;

            if (_view != null)
                _view.OnFadeOut += AffterStageClearEffect;
        }

        private void OnDisable()
        {
            Bus<PlayStageClearEffectEvent>.OnEvent -= ShowScreenFadePanel;

            if (_view != null)
                _view.OnFadeOut -= AffterStageClearEffect;
        }

        private void ShowScreenFadePanel(PlayStageClearEffectEvent evt)
        {
            clearChapter = evt.Chapter;
            clearStage = evt.Stage;
            clearChapterType = evt.ClearChapterType;

            if (_view == null)
            {
                Debug.LogError($"{nameof(ScreenFadePanelControler)} needs a {nameof(ScreenFadePanelView)}.", this);
                return;
            }

            _view.PlayFade();
        }

        private void AffterStageClearEffect()
        {
            Bus<AffterStageClearEvent>.Raise(new AffterStageClearEvent(
                clearChapter,
                clearStage,
                clearChapterType));
        }
    }
}
