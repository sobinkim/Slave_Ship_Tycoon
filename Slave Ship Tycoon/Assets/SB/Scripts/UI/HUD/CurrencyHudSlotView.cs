using TMPro;
using SB.Scripts.Currency;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.UI.HUD
{
    public enum CurrencyNumberDisplayMode
    {
        Compact,
        Full
    }

    public class CurrencyHudSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private CurrencyNumberDisplayMode numberDisplayMode;

        public Image Icon => icon;

        public void SetValue(long value)
        {
            if (valueText != null)
                valueText.text = numberDisplayMode == CurrencyNumberDisplayMode.Full
                    ? value.ToString("N0")
                    : CurrencyTextFormatter.Format(value);
        }
    }
}
