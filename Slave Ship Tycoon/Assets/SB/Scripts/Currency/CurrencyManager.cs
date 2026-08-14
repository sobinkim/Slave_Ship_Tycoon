using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Currency
{
    [Serializable]
    public struct CurrencyAmount
    {
        public CurrencyType currencyType;
        public long amount;
    }

    public class CurrencyManager : MonoBehaviour
    {
        [SerializeField] private CurrencyAmount[] initialCurrencies;

        private readonly long[] currencyAmounts = new long[Enum.GetValues(typeof(CurrencyType)).Length];

        public static CurrencyManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{nameof(CurrencyManager)} already exists. Duplicate disabled.", this);
                enabled = false;
                return;
            }

            Instance = this;
            InitializeCurrencies();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            Bus<CurrencyAddRequestEvent>.OnEvent += HandleAddCurrencyRequest;
        }

        private void OnDisable()
        {
            Bus<CurrencyAddRequestEvent>.OnEvent -= HandleAddCurrencyRequest;
        }

        public long GetCurrency(CurrencyType currencyType)
        {
            return currencyAmounts[(int)currencyType];
        }

        private void HandleAddCurrencyRequest(CurrencyAddRequestEvent evt)
        {
            AddCurrency(evt.CurrencyType, evt.AddedAmount);
        }

        public void AddCurrency(CurrencyType currencyType, long amount)
        {
            if (amount <= 0)
                return;

            long previousAmount = GetCurrency(currencyType);
            long currentAmount = amount > long.MaxValue - previousAmount
                ? long.MaxValue
                : previousAmount + amount;
            long addedAmount = currentAmount - previousAmount;

            if (addedAmount <= 0)
                return;

            currencyAmounts[(int)currencyType] = currentAmount;

            Bus<CurrencyAddedEvent>.Raise(new CurrencyAddedEvent(currencyType, addedAmount,
                currentAmount));
            Bus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(currencyType, currencyAmounts[(int)currencyType],
                previousAmount));
        }

        public bool TrySpendCurrency(CurrencyType currencyType, long amount)
        {
            if (amount <= 0)
                return true;

            long previousAmount = GetCurrency(currencyType);
            if (previousAmount < amount)
                return false;

            currencyAmounts[(int)currencyType] = previousAmount - amount;

            Bus<CurrencySpentEvent>.Raise(new CurrencySpentEvent(currencyType, amount,
                currencyAmounts[(int)currencyType]));
            Bus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(currencyType, currencyAmounts[(int)currencyType],
                previousAmount));
            return true;
        }

        private void InitializeCurrencies()
        {
            Array.Clear(currencyAmounts, 0, currencyAmounts.Length);

            if (initialCurrencies == null)
                return;

            for (int i = 0; i < initialCurrencies.Length; i++)
                currencyAmounts[(int)initialCurrencies[i].currencyType] = Math.Max(0L, initialCurrencies[i].amount);
        }
    }
}
