using SB.Scripts.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public sealed class SaleRewardSlotView : MonoBehaviour
    {
        [SerializeField] private CurrencyType _currencyType;
        [SerializeField] private Image _currencyIcon;
        [SerializeField] private TMP_Text _currencyNameText;
        [SerializeField] private TMP_Text _amountText;

        public CurrencyType CurrencyType => _currencyType;

        public void SetReward(long amount)
        {
            if (_currencyNameText != null)
                _currencyNameText.text = _currencyType.ToString();

            if (_amountText != null)
                _amountText.text = $"+{CurrencyTextFormatter.Format(amount)}";

            gameObject.SetActive(amount > 0);
        }
    }
}
