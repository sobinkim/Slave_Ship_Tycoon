using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.UI.HUD
{
    public class CurrencyHudSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text valueText;

        public Image Icon => icon;

        public void SetValue(int value)
        {
            if (valueText != null)
                valueText.text = value.ToString();
        }
    }
}
