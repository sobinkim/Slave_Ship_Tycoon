using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public class ItemElementView : MonoBehaviour
    {
        [SerializeField] private TMP_Text AmountText;
        [SerializeField] private Image IconImage;

        public void SettingItemElementView(int amount, Sprite icon)
        {
            AmountText.text = amount.ToString();
            IconImage.sprite = icon;
        }

        public void UpdateItemAmountText(int amount)
        {
            AmountText.text = amount.ToString();
        }
    }
}