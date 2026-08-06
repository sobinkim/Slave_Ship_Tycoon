using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Currency
{
    [Serializable]
    public struct CurrencyAmount
    {
        public CurrencyType currencyType;
        public int amount;
    }

    public class CurrencyManager : MonoBehaviour
    {
        [SerializeField] private CurrencyAmount[] initialCurrencies;

        private readonly int[] currencyAmounts = new int[Enum.GetValues(typeof(CurrencyType)).Length];

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

        public int GetCurrency(CurrencyType currencyType)
        {
            return currencyAmounts[(int)currencyType];
        }

        public void AddCurrency(CurrencyType currencyType, int amount)
        {
            if (amount <= 0)
                return;

            int previousAmount = GetCurrency(currencyType);
            currencyAmounts[(int)currencyType] = previousAmount + amount;

            Bus<CurrencyAddedEvent>.Raise(new CurrencyAddedEvent(currencyType, amount, currencyAmounts[(int)currencyType]));
            Bus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(currencyType, currencyAmounts[(int)currencyType], previousAmount));
        }

        public bool TrySpendCurrency(CurrencyType currencyType, int amount)
        {
            if (amount <= 0)
                return true;

            int previousAmount = GetCurrency(currencyType);
            if (previousAmount < amount)
                return false;

            currencyAmounts[(int)currencyType] = previousAmount - amount;

            Bus<CurrencySpentEvent>.Raise(new CurrencySpentEvent(currencyType, amount, currencyAmounts[(int)currencyType]));
            Bus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(currencyType, currencyAmounts[(int)currencyType], previousAmount));
            return true;
        }

        private void InitializeCurrencies()
        {
            Array.Clear(currencyAmounts, 0, currencyAmounts.Length);

            if (initialCurrencies == null)
                return;

            for (int i = 0; i < initialCurrencies.Length; i++)
                currencyAmounts[(int)initialCurrencies[i].currencyType] = Mathf.Max(0, initialCurrencies[i].amount);
        }
    }
}
