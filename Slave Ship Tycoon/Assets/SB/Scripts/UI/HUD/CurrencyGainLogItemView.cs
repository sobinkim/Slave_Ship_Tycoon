using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts.UI.HUD
{
    public class CurrencyGainLogItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private RectTransform _rectTransform;
        private Coroutine _playCoroutine;
        private Coroutine _moveCoroutine;
        private Action _onFinished;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
        }

        private void OnDisable()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _onFinished = null;
        }

        public void SetReferences(Image icon, TMP_Text amountText, CanvasGroup canvasGroup)
        {
            _icon = icon;
            _amountText = amountText;
            _canvasGroup = canvasGroup;
        }

        public void Play(Sprite icon, int amount, float visibleDuration, float fadeOutDuration, Action onFinished)
        {
            if (_playCoroutine != null)
                StopCoroutine(_playCoroutine);

            _icon.sprite = icon;
            _amountText.text = $"+{amount:N0}";
            _canvasGroup.alpha = 0f;
            _rectTransform.localScale = Vector3.one * 0.9f;
            _onFinished = onFinished;
            _playCoroutine = StartCoroutine(PlayRoutine(visibleDuration, fadeOutDuration));
        }

        public void FadeOut(float duration, Action onFinished)
        {
            if (_playCoroutine != null)
                StopCoroutine(_playCoroutine);

            _onFinished = onFinished;
            _playCoroutine = StartCoroutine(FadeOutRoutine(duration));
        }

        public void MoveTo(Vector2 targetPosition, float duration)
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveRoutine(targetPosition, duration));
        }

        private IEnumerator PlayRoutine(float visibleDuration, float fadeOutDuration)
        {
            yield return FadeAndScale(0f, 1f, 0.9f, 1f, 0.1f);
            yield return new WaitForSecondsRealtime(visibleDuration);
            yield return FadeAndScale(1f, 0f, 1f, 1f, fadeOutDuration);

            Finish();
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            yield return FadeAndScale(_canvasGroup.alpha, 0f, _rectTransform.localScale.x, 1f, duration);
            Finish();
        }

        private IEnumerator MoveRoutine(Vector2 targetPosition, float duration)
        {
            Vector2 startPosition = _rectTransform.anchoredPosition;

            if (duration <= 0f)
            {
                _rectTransform.anchoredPosition = targetPosition;
                _moveCoroutine = null;
                yield break;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);
                yield return null;
            }

            _rectTransform.anchoredPosition = targetPosition;
            _moveCoroutine = null;
        }

        private IEnumerator FadeAndScale(float startAlpha, float endAlpha, float startScale, float endScale, float duration)
        {
            if (duration <= 0f)
            {
                _canvasGroup.alpha = endAlpha;
                _rectTransform.localScale = Vector3.one * endScale;
                yield break;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
                _rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, easedProgress);
                yield return null;
            }

            _canvasGroup.alpha = endAlpha;
            _rectTransform.localScale = Vector3.one * endScale;
        }

        private void Finish()
        {
            _playCoroutine = null;

            Action onFinished = _onFinished;
            _onFinished = null;
            onFinished?.Invoke();
        }
    }
}
