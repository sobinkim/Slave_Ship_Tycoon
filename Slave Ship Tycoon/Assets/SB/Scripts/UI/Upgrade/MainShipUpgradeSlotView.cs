using System;
using SB.Scripts.Upgrade;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SB.Scripts.UI.Upgrade
{
    public class MainShipUpgradeSlotView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private MainShipUpgradeType _upgradeType;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _cost;
        [SerializeField] private TMP_Text _level;
        [SerializeField] private float _repeatStartDelay = 0.35f;
        [SerializeField] private float _repeatInterval = 0.1f;

        public event Func<MainShipUpgradeType, bool> OnUpgradeButtonClicked;

        private Coroutine _repeatUpgradeCoroutine;

        private void OnDisable()
        {
            StopRepeatUpgrade();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            StopRepeatUpgrade();

            if (!TryUpgrade())
                return;

            _repeatUpgradeCoroutine = StartCoroutine(RepeatUpgrade());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopRepeatUpgrade();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopRepeatUpgrade();
        }

        public void Refresh(int level, float cost)
        {
            _level.text = level.ToString();
            _cost.text = cost.ToString();
        }

        private System.Collections.IEnumerator RepeatUpgrade()
        {
            yield return new WaitForSecondsRealtime(_repeatStartDelay);

            while (true)
            {
                if (!TryUpgrade())
                {
                    _repeatUpgradeCoroutine = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(_repeatInterval);
            }
        }

        private bool TryUpgrade()
        {
            return OnUpgradeButtonClicked?.Invoke(_upgradeType) ?? false;
        }

        private void StopRepeatUpgrade()
        {
            if (_repeatUpgradeCoroutine == null)
                return;

            StopCoroutine(_repeatUpgradeCoroutine);
            _repeatUpgradeCoroutine = null;
        }
    }
}
