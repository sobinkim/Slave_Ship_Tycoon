using TMPro;
using SB.Scripts.Currency;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.UI.HUD
{
    public class CurrencyHudSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text valueText;

        public Image Icon => icon;

        public void SetValue(long value)
        {
            if (valueText != null)
                valueText.text = CurrencyTextFormatter.Format(value);
        }
    }
}
