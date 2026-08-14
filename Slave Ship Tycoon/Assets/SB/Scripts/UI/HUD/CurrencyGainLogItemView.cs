using System;
using System.Collections;
using TMPro;
using SB.Scripts.Currency;
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
        private Vector3 _defaultScale;
        private Coroutine _playCoroutine;
        private Coroutine _moveCoroutine;
        private Action _onFinished;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _defaultScale = _rectTransform.localScale;
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

        public void Play(Sprite icon, long amount, float visibleDuration, float fadeOutDuration, Action onFinished)
        {
            if (_playCoroutine != null)
                StopCoroutine(_playCoroutine);

            _icon.sprite = icon;
            _amountText.text = $"+{CurrencyTextFormatter.Format(amount)}";
            _canvasGroup.alpha = 0f;
            _rectTransform.localScale = _defaultScale * 1.1f;
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
            yield return FadeAndScale(0f, 1f, _defaultScale * 1.1f, _defaultScale, 0.1f);
            yield return new WaitForSecondsRealtime(visibleDuration);
            yield return FadeAndScale(1f, 0f, _defaultScale, _defaultScale, fadeOutDuration);

            Finish();
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            yield return FadeAndScale(_canvasGroup.alpha, 0f, _rectTransform.localScale, _defaultScale, duration);
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

        private IEnumerator FadeAndScale(
            float startAlpha,
            float endAlpha,
            Vector3 startScale,
            Vector3 endScale,
            float duration)
        {
            if (duration <= 0f)
            {
                _canvasGroup.alpha = endAlpha;
                _rectTransform.localScale = endScale;
                yield break;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
                _rectTransform.localScale = Vector3.Lerp(startScale, endScale, easedProgress);
                yield return null;
            }

            _canvasGroup.alpha = endAlpha;
            _rectTransform.localScale = endScale;
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
