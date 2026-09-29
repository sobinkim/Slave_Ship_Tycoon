using SB.Core.EventBus;
using System.Collections.Generic;
using UnityEngine;
using SB.Scripts.Currency;

namespace SB.Scripts
{
    public class StageInfoBarController : MonoBehaviour
    {
        [SerializeField] private StageInfoBarView view;
        [SerializeField] private CargoManager cargoManager;
        [SerializeField] private ChapterDatabase chapterDatabase;
        [Header("Route Card Text")]
        [SerializeField] private string pirateTitle = "해적 해역";
        [SerializeField] private string transportTitle = "운송 해역";
        [SerializeField] private string bossTitle = "봉쇄 함대";
        [SerializeField] private string pirateDescription = "자동 약탈 중 · 화물을 모아 출항을 준비하세요";
        [SerializeField] private string transportDescription = "목적지까지 화물을 안전하게 운송하세요";
        [SerializeField] private string bossDescription = "보스를 격파하고 다음 항로를 여세요";
        private StageStartedEvent currentStage;
        private bool hasStage;
        private bool isDirty;

        private void LateUpdate()
        {
            if (!isDirty || !hasStage || view == null) return;
            isDirty = false;
            bool isTransport = currentStage.RouteType == ChapterRouteType.Sell;
            int total = chapterDatabase != null && chapterDatabase.TryGetChapter(currentStage.Chapter, out var chapter) ? chapter.MaxStage : currentStage.Stage;
            string title = isTransport ? transportTitle : currentStage.IsBossBattle ? bossTitle : pirateTitle;
            string detail = isTransport ? transportDescription : currentStage.IsBossBattle ? bossDescription : pirateDescription;
            view.SetRouteNumber(currentStage.Chapter);
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

            view.SetStageInfoBarMode(mode, evt.IsBossBattle);
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
