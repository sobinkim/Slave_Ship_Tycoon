using System;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.Visual
{
    public class PanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button button;

        public event Action OnClicked;

        private void OnEnable()
        {
            button.onClick.AddListener(() => OnClicked?.Invoke());
        }

        private void OnDisable()
        {
            button.onClick.RemoveAllListeners();
        }

        public void PanelActive(bool active)
        {
            _panel.SetActive(active);
        }
    }
}