using System.Collections;
using UnityEngine;

namespace SB.Scripts.UI.Fleet
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PanelFadeIn : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.01f)] private float durationSeconds = 0.24f;
        private Coroutine fadeRoutine;
        private void Awake() { if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>(); }
        private void OnEnable()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
            fadeRoutine = StartCoroutine(Fade());
        }
        private IEnumerator Fade()
        {
            float elapsed = 0;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationSeconds);
                canvasGroup.alpha = t * t * (3 - 2 * t);
                yield return null;
            }
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            fadeRoutine = null;
        }
        private void OnDisable()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = null;
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
        }
    }
}
