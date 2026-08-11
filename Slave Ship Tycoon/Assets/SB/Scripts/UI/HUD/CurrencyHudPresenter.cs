using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.Scripts.UI.HUD
{
    public class CurrencyHudPresenter : MonoBehaviour
    {
        [System.Serializable]
        private struct CurrencyHudViewBinding
        {
            public CurrencyType currencyType;
            public CurrencyHudSlotView view;
        }

        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private CurrencyHudViewBinding[] currencyViews;

        private readonly Dictionary<CurrencyType, CurrencyHudSlotView> slotViews = new();

        private void Awake()
        {
            ResolveReferences();
            BuildSlotViewDictionary();
        }

        private void OnEnable()
        {
            Bus<CurrencyChangedEvent>.OnEvent += HandleCurrencyChanged;
        }

        private void Start()
        {
            RefreshAll();
        }

        private void OnDisable()
        {
            Bus<CurrencyChangedEvent>.OnEvent -= HandleCurrencyChanged;
        }

        private void HandleCurrencyChanged(CurrencyChangedEvent evt)
        {
            Refresh(evt.CurrencyType);
        }

        private void RefreshAll()
        {
            Refresh(CurrencyType.Gold);
            Refresh(CurrencyType.Diamond);
            Refresh(CurrencyType.Emerald);
        }

        private void Refresh(CurrencyType currencyType)
        {
            if (currencyManager == null)
                return;

            if (slotViews.TryGetValue(currencyType, out CurrencyHudSlotView slotView) == false)
                return;

            if (slotView == null)
                return;

            slotView.SetValue(currencyManager.GetCurrency(currencyType));
        }

        private void BuildSlotViewDictionary()
        {
            slotViews.Clear();

            if (currencyViews == null)
                return;

            for (int i = 0; i < currencyViews.Length; i++)
            {
                CurrencyHudViewBinding binding = currencyViews[i];
                if (binding.view == null)
                    continue;

                slotViews[binding.currencyType] = binding.view;
            }
        }

        private void ResolveReferences()
        {
            if (currencyManager == null)
                currencyManager = CurrencyManager.Instance;
        }

        public bool TryGetCurrencyIcon(CurrencyType currencyType, out Sprite icon)
        {
            icon = null;

            if (slotViews.TryGetValue(currencyType, out CurrencyHudSlotView slotView) == false || slotView == null)
                return false;

            icon = slotView.Icon != null ? slotView.Icon.sprite : null;
            return icon != null;
        }
    }
}
