using System.Collections.Generic;
using SB.Scripts;

namespace SB.Core.EventBus
{
    public readonly struct ChangedCurrentCargoCapacityEvent : IEvent
    {
        public readonly CargoData[] CargoData;

        public ChangedCurrentCargoCapacityEvent(CargoData[] _cargoData)
        {
            CargoData = _cargoData;
        }
    }

    public readonly struct GetMarketPriceEvent : IEvent
    {
        public readonly IReadOnlyDictionary<ETransportItemType, TransportMarketPriceEntry> MarketPrices;

        public GetMarketPriceEvent(
            IReadOnlyDictionary<ETransportItemType, TransportMarketPriceEntry> marketPrices)
        {
            MarketPrices = marketPrices;
        }
    }

    public readonly struct TransportCargoChangedEvent : IEvent
    {
        public readonly CargoManager CargoManager;
        public readonly IReadOnlyList<CargoData> CargoData;

        public TransportCargoChangedEvent(CargoManager cargoManager, IReadOnlyList<CargoData> cargoData)
        {
            CargoManager = cargoManager;
            CargoData = cargoData;
        }
    }
}
