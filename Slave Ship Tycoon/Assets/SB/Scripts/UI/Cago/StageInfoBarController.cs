using SB.Core.EventBus;
using System.Collections.Generic;
using UnityEngine;
using SB.Scripts.Currency;
using System.Text;

namespace SB.Scripts
{
    public class StageInfoBarController : MonoBehaviour
    {
        [SerializeField] private StageInfoBarView view;
        [SerializeField] private CargoManager cargoManager;
        [SerializeField] private ChapterDatabase chapterDatabase;
        private StageStartedEvent currentStage;
        private bool hasStage;
        private bool isDirty;

        private void LateUpdate()
        {
            if (!isDirty || !hasStage || view == null) return;
            isDirty = false;
            bool isTransport = currentStage.RouteType == ChapterRouteType.Sell;
            int total = chapterDatabase != null && chapterDatabase.TryGetChapter(currentStage.Chapter, out var chapter) ? chapter.MaxStage : currentStage.Stage;
            string title = isTransport ? $"운송 항로 {currentStage.Chapter}" :
                currentStage.IsBossBattle ? $"항로 {currentStage.Chapter} · 봉쇄 함대 돌파" : $"항로 {currentStage.Chapter} · 해적 해역";
            string detail = isTransport ? "목적지로 항해 중 · 화물을 지켜 정산하세요" : "자동 약탈 중 · 화물 준비 후 보스 도전";
            view.SetRoute(title, detail);
            view.SetProgress(currentStage.Stage,total,isTransport);
        }

        private void OnEnable()
        {
            Bus<StageStartedEvent>.OnEvent += HandleStageStarted;
            Bus<SellChapterStartedEvent>.OnEvent += HandleSellChapterStarted;
            Bus<ObtainChapterStartedEvent>.OnEvent += HandleObtainChapterStarted;
            Bus<GetMarketPriceEvent>.OnEvent += HandleMarketPriceChanged;
        }

        private void OnDisable()
        {
            Bus<StageStartedEvent>.OnEvent -= HandleStageStarted;
            Bus<SellChapterStartedEvent>.OnEvent -= HandleSellChapterStarted;
            Bus<ObtainChapterStartedEvent>.OnEvent -= HandleObtainChapterStarted;
            Bus<GetMarketPriceEvent>.OnEvent -= HandleMarketPriceChanged;
        }

        private void HandleStageStarted(StageStartedEvent evt)
        {
            if (view == null)
                return;

            currentStage = evt;
            hasStage = true;
            isDirty = true;
            StageInfoBarMode mode = evt.RouteType == ChapterRouteType.Obtain
                ? StageInfoBarMode.Odd
                : StageInfoBarMode.Even;

            view.SetStageInfoBarMode(mode);
        }

        private void HandleSellChapterStarted(SellChapterStartedEvent evt)
        {
            if (view == null || cargoManager == null)
                return;

            view.ClearCargoItems();
            view.AddCargoItems(cargoManager.CargoData);
        }

        private void HandleObtainChapterStarted(ObtainChapterStartedEvent evt)
        {
            view?.ClearCargoItems();
        }

        private void HandleMarketPriceChanged(GetMarketPriceEvent evt)
        {
            isDirty = true;
            if (view == null || evt.MarketPrices == null)
                return;

            foreach (KeyValuePair<ETransportItemType, TransportMarketPriceEntry> priceEntry in evt.MarketPrices)
            {
                view.SetMarketMultiplier(
                    priceEntry.Key,
                    priceEntry.Value.GetAppliedMultiplier());
            }
        }
    }
}
