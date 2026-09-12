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

        public event Func<bool> OnloadButtonClicked;
        public event Func<bool> OnunloadButtonClicked;

        private HoldRepeatButton _loadRepeatButton;
        private HoldRepeatButton _unLoadRepeatButton;

        private void Awake()
        {
            _loadRepeatButton = GetRepeatButton(_loadButton);
            _unLoadRepeatButton = GetRepeatButton(_unLoadButton);

            _loadRepeatButton.OnTriggered += HandleLoadButtonTriggered;
            _unLoadRepeatButton.OnTriggered += HandleUnloadButtonTriggered;
        }

        private void OnDestroy()
        {
            if (_loadRepeatButton != null)
                _loadRepeatButton.OnTriggered -= HandleLoadButtonTriggered;

            if (_unLoadRepeatButton != null)
                _unLoadRepeatButton.OnTriggered -= HandleUnloadButtonTriggered;
        }

        public void SetWeight(float weight)
        {
            _weightText.text = $"무게 {weight:0.##}";
        }


        public void SetIcon(Sprite icon)
        {
            _icon.sprite = icon;
        }

        private HoldRepeatButton GetRepeatButton(Button button)
        {
            HoldRepeatButton repeatButton = button.GetComponent<HoldRepeatButton>();
            return repeatButton != null ? repeatButton : button.gameObject.AddComponent<HoldRepeatButton>();
        }

        private bool HandleLoadButtonTriggered()
        {
            return OnloadButtonClicked?.Invoke() ?? false;
        }

        private bool HandleUnloadButtonTriggered()
        {
            return OnunloadButtonClicked?.Invoke() ?? false;
        }
    }
}
