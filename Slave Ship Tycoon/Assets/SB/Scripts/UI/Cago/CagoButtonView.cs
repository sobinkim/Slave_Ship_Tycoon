using System;
using System.Text;
using SB.Scripts.Currency;
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

        public void RefreshCargo(CargoData cargo)
        {
            string title = cargo.Item.Type == ETransportItemType.Gold ? "금괴 화물" : "선박 자재";
            var rewards = new StringBuilder();
            foreach (var reward in cargo.Item.Rewards)
            {
                if (rewards.Length > 0) rewards.Append(" · ");
                rewards.Append(CurrencyTextFormatter.FormatName(reward.CurrencyType)).Append(" ").Append(CurrencyTextFormatter.Format(reward.CurrencyAmount));
            }
            _weightText.text = $"{title}  <color=#E3BD78>{cargo.Amount}개 적재</color>\n<size=80%>개당 무게 {cargo.Item.BaseWeight:0.#} · 기본 {rewards}</size>";
            _unLoadButton.interactable = cargo.Amount > 0;
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
