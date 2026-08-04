using System.Collections;
using TMPro;
using UnityEngine;

namespace SB.Scripts
{
    public class StageResultPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject parents;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI currentStageText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField, Min(0.01f)] private float fadeInSpeed = 8f;
        [SerializeField, Min(0f)] private float holdTime = 1f;
        [SerializeField, Min(0.01f)] private float fadeOutSpeed = 5f;

        private Coroutine popupRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = parents != null
                    ? parents.GetComponent<CanvasGroup>()
                    : GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = parents != null
                    ? parents.AddComponent<CanvasGroup>()
                    : gameObject.AddComponent<CanvasGroup>();
            }

            SetAlpha(0f);

            if (parents != null)
                parents.SetActive(false);
        }

        public void Show(int currentChapter, int currentStage, string text)
        {
            if (popupRoutine != null)
                StopCoroutine(popupRoutine);

            if (currentStageText != null)
                currentStageText.text = $"{currentChapter}-{currentStage}";

            if (resultText != null)
                resultText.text = text;

            if (parents != null)
                parents.SetActive(true);

            SetAlpha(0f);
            popupRoutine = StartCoroutine(PopupRoutine());
        }

        private IEnumerator PopupRoutine()
        {
            while (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                SetAlpha(Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeInSpeed * Time.deltaTime));
                yield return null;
            }

            SetAlpha(1f);

            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            while (canvasGroup != null && canvasGroup.alpha > 0f)
            {
                SetAlpha(Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeOutSpeed * Time.deltaTime));
                yield return null;
            }

            SetAlpha(0f);

            if (parents != null)
                parents.SetActive(false);

            popupRoutine = null;
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }
    }
}
