using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SB.Scripts.Visual
{
    public class PanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [FormerlySerializedAs("button")]
        [SerializeField] private Button _button;
        [SerializeField] private Color _selectedColor = new Color(0.16f, 0.48f, 0.38f);

        private Color _normalColor;
        private bool _hasNormalColor;

        public event Action OnClicked;

        private void OnEnable()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        public void PanelActive(bool active)
        {
            if (_panel != null)
                _panel.SetActive(active);

            if (_button != null)
            {
                if (_button.targetGraphic != null)
                {
                    if (_hasNormalColor == false)
                    {
                        _normalColor = _button.targetGraphic.color;
                        _hasNormalColor = true;
                    }

                    _button.targetGraphic.color = active ? _selectedColor : _normalColor;
                }

                _button.interactable = active == false;
            }
        }

        private void HandleClicked()
        {
            OnClicked?.Invoke();
        }
    }
}
