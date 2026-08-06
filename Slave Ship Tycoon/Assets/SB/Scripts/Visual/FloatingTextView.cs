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

        private RectTransform rectTransform;
        private Coroutine playCoroutine;
        private bool spawnedFromPool;

        private void Awake()
        {
            rectTransform = transform as RectTransform;

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

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            playCoroutine = StartCoroutine(PlayRoutine(anchoredPosition));
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
        }

        private IEnumerator PlayRoutine(Vector2 startPosition)
        {
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float progress = Mathf.Clamp01(time / duration);

                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, startPosition + moveOffset, progress);

                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - progress;

                yield return null;
            }

            playCoroutine = null;

            if (spawnedFromPool && PoolingManager.Instance != null)
                PoolingManager.Instance.Release(this);
            else
                Destroy(gameObject);
        }
    }
}
