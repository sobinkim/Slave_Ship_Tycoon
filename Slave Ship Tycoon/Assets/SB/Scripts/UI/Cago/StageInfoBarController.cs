using SB.Core.EventBus;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts
{
    public class StageInfoBarController : MonoBehaviour
    {
        [SerializeField] private StageInfoBarView view;
        [SerializeField] private CargoManager cargoManager;

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
