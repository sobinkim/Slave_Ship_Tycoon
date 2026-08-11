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
        public readonly Dictionary<ETransportItemType, TransportMarketPriceEntry> _marketPrices;

        public GetMarketPriceEvent(Dictionary<ETransportItemType, TransportMarketPriceEntry> marketPrices)
        {
            _marketPrices = marketPrices;
        }
    }

}
