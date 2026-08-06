using SB.Scripts.Currency;

namespace SB.Core.EventBus
{
    public readonly struct CurrencyChangedEvent : IEvent
    {
        public readonly CurrencyType CurrencyType;
        public readonly int CurrentAmount;
        public readonly int PreviousAmount;

        public CurrencyChangedEvent(CurrencyType currencyType, int currentAmount, int previousAmount)
        {
            CurrencyType = currencyType;
            CurrentAmount = currentAmount;
            PreviousAmount = previousAmount;
        }
    }

    public readonly struct CurrencyAddedEvent : IEvent
    {
        public readonly CurrencyType CurrencyType;
        public readonly int AddedAmount;
        public readonly int CurrentAmount;

        public CurrencyAddedEvent(CurrencyType currencyType, int addedAmount, int currentAmount)
        {
            CurrencyType = currencyType;
            AddedAmount = addedAmount;
            CurrentAmount = currentAmount;
        }
    }

    public readonly struct CurrencySpentEvent : IEvent
    {
        public readonly CurrencyType CurrencyType;
        public readonly int SpentAmount;
        public readonly int CurrentAmount;

        public CurrencySpentEvent(CurrencyType currencyType, int spentAmount, int currentAmount)
        {
            CurrencyType = currencyType;
            SpentAmount = spentAmount;
            CurrentAmount = currentAmount;
        }
    }
}
