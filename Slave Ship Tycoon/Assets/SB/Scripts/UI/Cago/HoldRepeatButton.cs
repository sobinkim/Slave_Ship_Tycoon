using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SB.Scripts
{
    public class HoldRepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float _repeatStartDelay = 0.35f;
        [SerializeField] private float _repeatInterval = 0.1f;

        public event Func<bool> OnTriggered;

        private Coroutine _repeatCoroutine;

        private void OnDisable()
        {
            StopRepeat();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            StopRepeat();

            if (!TryTrigger())
                return;

            _repeatCoroutine = StartCoroutine(Repeat());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopRepeat();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopRepeat();
        }

        private IEnumerator Repeat()
        {
            yield return new WaitForSecondsRealtime(_repeatStartDelay);

            while (true)
            {
                if (!TryTrigger())
                {
                    _repeatCoroutine = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(_repeatInterval);
            }
        }

        private bool TryTrigger()
        {
            return OnTriggered?.Invoke() ?? false;
        }

        private void StopRepeat()
        {
            if (_repeatCoroutine == null)
                return;

            StopCoroutine(_repeatCoroutine);
            _repeatCoroutine = null;
        }
    }
}
