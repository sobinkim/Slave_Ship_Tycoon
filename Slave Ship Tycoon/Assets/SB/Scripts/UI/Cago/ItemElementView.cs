using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public class ItemElementView : MonoBehaviour
    {
        [SerializeField] private TMP_Text AmountText;
        [SerializeField] private Image IconImage;
        [SerializeField] private TMP_Text _marketMultiplierText;

        public void SettingItemElementView(int amount, Sprite icon)
        {
            AmountText.text = amount.ToString();
            IconImage.sprite = icon;
        }

        public void UpdateItemAmountText(int amount)
        {
            AmountText.text = amount.ToString();
        }

        public void SetMarketMultiplier(int multiplier)
        {
            if (_marketMultiplierText == null)
                return;

            _marketMultiplierText.text = $"x{Mathf.Max(1, multiplier)}";
            _marketMultiplierText.gameObject.SetActive(true);
        }
    }
}
