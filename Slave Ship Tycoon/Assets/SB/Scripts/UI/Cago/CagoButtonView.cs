using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public class CagoButtonView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _unLoadButton;
        [SerializeField] private TMP_Text _weightText;

        public event Action OnloadButtonClicked;
        public event Action OnunloadButtonClicked;

        private void Awake()
        {
            _loadButton.onClick.AddListener(() => OnloadButtonClicked?.Invoke());
            _unLoadButton.onClick.AddListener(() => OnunloadButtonClicked?.Invoke());
        }

        public void SetWeight(float weight)
        {
            _weightText.text = weight.ToString();
        }


        public void SetIcon(Sprite icon)
        {
            _icon.sprite = icon;
        }
    }
}