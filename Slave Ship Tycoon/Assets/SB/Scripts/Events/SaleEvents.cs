using SB.Scripts;

namespace SB.Core.EventBus
{
    public readonly struct SaleSettlementCompletedEvent : IEvent
    {
        public readonly SaleSettlementResult Result;

        public SaleSettlementCompletedEvent(SaleSettlementResult result)
        {
            Result = result;
        }
    }
}
