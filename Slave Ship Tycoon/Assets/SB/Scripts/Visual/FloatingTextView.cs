using System.Collections;
using TMPro;
using UnityEngine;

namespace SB.Scripts.Visual
{
    [RequireComponent(typeof(RectTransform))]
    public class FloatingTextView : MonoBehaviour, IPoolable
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.01f)] private float duration = 0.7f;
        [SerializeField] private Vector2 moveOffset = new Vector2(0f, 70f);
        [Header("Impact presentation")]
        [SerializeField] private bool useImpactAnimation;
        [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = 0.75f;
        [SerializeField, Min(0.01f)] private float popSeconds = 0.08f;
        [SerializeField, Min(0.01f)] private float settleSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float entryScale = 0.72f;
        [SerializeField, Min(0.01f)] private float peakScale = 1.18f;
        [SerializeField, Min(0f)] private float entryRisePixels = 14f;

        private RectTransform rectTransform;
        private Coroutine playCoroutine;
        private bool spawnedFromPool;
        private Vector3 initialScale;
        private float currentAlpha = 1f;

        public bool IsPlaying => playCoroutine != null && gameObject.activeInHierarchy;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            initialScale = transform.localScale;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Play(string message, Vector2 anchoredPosition)
        {
            if (text == null)
            {
                Debug.LogError($"{nameof(FloatingTextView)} needs a TMP text.", this);
                return;
            }

            if (playCoroutine != null)
                StopCoroutine(playCoroutine);

            rectTransform.anchoredPosition = anchoredPosition;
            text.text = message;
            currentAlpha = 1f;
            transform.localScale = initialScale * (useImpactAnimation ? entryScale : 1f);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            playCoroutine = StartCoroutine(PlayRoutine(anchoredPosition));
        }

        public void Release()
        {
            if (spawnedFromPool && PoolingManager.Instance != null)
                PoolingManager.Instance.Release(this);
            else
                Destroy(gameObject);
        }

        public void SetReferences(TMP_Text targetText, CanvasGroup targetCanvasGroup)
        {
            text = targetText;
            canvasGroup = targetCanvasGroup;
        }

        public void OnSpawnedFromPool()
        {
            spawnedFromPool = true;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        public void OnDespawnedToPool()
        {
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }

            spawnedFromPool = false;
            transform.localScale = initialScale;
        }

        private IEnumerator PlayRoutine(Vector2 startPosition)
        {
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float progress = Mathf.Clamp01(time / duration);

                if (useImpactAnimation)
                {
                    float pop = Mathf.Clamp01(time / Mathf.Max(0.01f, popSeconds));
                    float settle = Mathf.Clamp01((time - popSeconds) / Mathf.Max(0.01f, settleSeconds));
                    float scale = time < popSeconds
                        ? Mathf.Lerp(entryScale, peakScale, 1f - Mathf.Pow(1f - pop, 3f))
                        : Mathf.Lerp(peakScale, 1f, Mathf.SmoothStep(0f, 1f, settle));
                    transform.localScale = initialScale * scale;
                    rectTransform.anchoredPosition = startPosition + moveOffset * progress +
                        Vector2.up * (entryRisePixels * (1f - Mathf.Pow(1f - pop, 3f)));
                    currentAlpha = 1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(fadeStartNormalized, 1f, progress));
                }
                else
                {
                    rectTransform.anchoredPosition = Vector2.Lerp(startPosition, startPosition + moveOffset, progress);
                    currentAlpha = 1f - progress;
                }
                if (canvasGroup != null) canvasGroup.alpha = currentAlpha;

                yield return null;
            }

            playCoroutine = null;

            Release();
        }
    }
}
