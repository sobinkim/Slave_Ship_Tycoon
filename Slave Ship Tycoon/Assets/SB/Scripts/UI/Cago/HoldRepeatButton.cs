using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SB.Scripts
{
    public class HoldRepeatButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
    {
        [Header("References")]
        [SerializeField, Tooltip("버튼이 비활성화되면 반복 입력을 멈춥니다. 기존 화물 UI의 미할당 상태도 유지할 수 있습니다.")]
        private UnityEngine.UI.Button button;

        [Header("Hold Repeat")]
        [SerializeField, Min(0f), Tooltip("첫 입력 이후 반복을 시작할 때까지 기다리는 실제 시간입니다.")]
        private float _repeatStartDelay = 0.35f;
        [SerializeField, Min(0.01f), Tooltip("누르고 있는 동안 화물 1개를 적재하는 실제 시간 간격입니다.")]
        private float _repeatInterval = 0.1f;

        public event Func<bool> OnTriggered;

        private Coroutine _repeatCoroutine;
        private PointerEventData activePointer;

        private void OnDisable()
        {
            StopRepeat();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !CanTrigger())
                return;

            StopRepeat();
            activePointer = eventData;

            if (!TryTrigger())
            {
                activePointer = null;
                return;
            }

            _repeatCoroutine = StartCoroutine(Repeat());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activePointer != null && activePointer.pointerId == eventData.pointerId)
                StopRepeat();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (activePointer != null && activePointer.pointerId == eventData.pointerId)
                StopRepeat();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            TryTrigger();
        }

        private IEnumerator Repeat()
        {
            yield return new WaitForSecondsRealtime(_repeatStartDelay);

            while (true)
            {
                if (activePointer == null || !activePointer.eligibleForClick || !TryTrigger())
                {
                    _repeatCoroutine = null;
                    activePointer = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(_repeatInterval);
            }
        }

        private bool TryTrigger()
        {
            return CanTrigger() && (OnTriggered?.Invoke() ?? false);
        }

        private bool CanTrigger()
        {
            return isActiveAndEnabled && (button == null || (button.IsActive() && button.IsInteractable()));
        }

        private void StopRepeat()
        {
            activePointer = null;
            if (_repeatCoroutine == null)
                return;

            StopCoroutine(_repeatCoroutine);
            _repeatCoroutine = null;
        }
    }
}
