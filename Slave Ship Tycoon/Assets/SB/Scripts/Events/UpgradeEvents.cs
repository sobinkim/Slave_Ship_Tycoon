using SB.Scripts.Upgrade;

namespace SB.Core.EventBus
{
    public readonly struct UpgradeEvent : IEvent
    {
        public readonly MainShipUpgradeData _mainShipUpgradeData;

        public UpgradeEvent(MainShipUpgradeData mainShipUpgradeData)
        {
            _mainShipUpgradeData = mainShipUpgradeData;
        }
    }

    public readonly struct RefreshMainShipStatsEvent : IEvent
    {
        
    }
}
