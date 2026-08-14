using SB.Scripts.Currency;

namespace SB.Core.EventBus
{
    public readonly struct CurrencyChangedEvent : IEvent
    {
        public readonly CurrencyType CurrencyType;
        public readonly long CurrentAmount;
        public readonly long PreviousAmount;

        public CurrencyChangedEvent(CurrencyType currencyType, long currentAmount, long previousAmount)
        {
            CurrencyType = currencyType;
            CurrentAmount = currentAmount;
            PreviousAmount = previousAmount;
        }
    }

    public readonly struct CurrencyAddRequestEvent : IEvent
    {
        public readonly CurrencyType CurrencyType;
        public readonly long AddedAmount;

        public CurrencyAddRequestEvent(CurrencyType currencyType, long addedAmount)
        {
            CurrencyType = currencyType;
            AddedAmount = addedAmount;
        }
    }

    public readonly struct CurrencyAddedEvent : IEvent
    {
        public readonly CurrencyType CurrencyType;
        public readonly long AddedAmount;
        public readonly long CurrentAmount;

        public CurrencyAddedEvent(CurrencyType currencyType, long addedAmount, long currentAmount)
        {
            CurrencyType = currencyType;
            AddedAmount = addedAmount;
            CurrentAmount = currentAmount;
        }
    }

    public readonly struct CurrencySpentEvent : IEvent
    {
        public readonly CurrencyType CurrencyType;
        public readonly long SpentAmount;
        public readonly long CurrentAmount;

        public CurrencySpentEvent(CurrencyType currencyType, long spentAmount, long currentAmount)
        {
            CurrencyType = currencyType;
            SpentAmount = spentAmount;
            CurrentAmount = currentAmount;
        }
    }
}
