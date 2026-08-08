using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadePanelView : MonoBehaviour
{
    [SerializeField] private Image ScreenFadePanel;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.15f;
    [SerializeField, Min(0f)] private float holdDuration = 0.1f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;

    public event Action OnFadeOut;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        SetAlpha(0f);
        SetActivePanel(false);
    }

    public void SetActivePanel(bool active)
    {
        ScreenFadePanel.enabled = active;
        ScreenFadePanel.raycastTarget = active;
    }

    public void PlayFade()
    {
        PlayFade(fadeInDuration, holdDuration, fadeOutDuration);
    }

    public void PlayFade(float fadeInTime, float holdTime, float fadeOutTime)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(fadeInTime, holdTime, fadeOutTime));
    }

    public void StopFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        SetAlpha(0f);
        SetActivePanel(false);
    }

    private IEnumerator FadeRoutine(float fadeInTime, float holdTime, float fadeOutTime)
    {
        SetActivePanel(true);

        yield return FadeAlpha(0f, 1f, fadeInTime);

        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

        yield return FadeAlpha(1f, 0f, fadeOutTime);

        SetActivePanel(false);
        fadeCoroutine = null;
        OnFadeOut?.Invoke();
    }

    private IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(endAlpha);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            SetAlpha(Mathf.Lerp(startAlpha, endAlpha, t));
            yield return null;
        }

        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = ScreenFadePanel.color;
        color.a = alpha;
        ScreenFadePanel.color = color;
    }
}
