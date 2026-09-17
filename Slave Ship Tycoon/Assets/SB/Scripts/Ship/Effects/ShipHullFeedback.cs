using System.Collections;
using SB.Core;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class ShipHullFeedback : MonoBehaviour
    {
        [Header("Explicit ship references")]
        [SerializeField] private EntityHealth health;
        [SerializeField] private SpriteRenderer hull;
        [SerializeField] private SpriteRenderer healthFill;
        [SerializeField] private GameObject healthBar;
        [Header("Hit and hull condition")]
        [SerializeField] private Color hitColor = new Color(1f, .91f, .68f);
        [SerializeField] private Color healthyColor = new Color(.22f, .78f, .62f);
        [SerializeField] private Color dangerColor = new Color(.91f, .39f, .21f);
        [SerializeField, Range(0f, 1f)] private float dangerThreshold = .3f;
        [SerializeField, Min(.01f)] private float hitSeconds = .16f;
        [SerializeField, Min(.01f), Tooltip("피격 후 체력바가 목표 체력까지 따라가는 시간")]
        private float healthDrainSeconds = .12f;
        [SerializeField, Min(.01f)] private float barWidth = .7f;
        [SerializeField, Min(.01f)] private float barHeight = .045f;
        [SerializeField, Tooltip("전투 상태와 관계없이 체력바를 계속 표시합니다.")]
        private bool alwaysShowHealth = true;
        private Color hullColor;
        private Vector3 healthFillBaseLocalPosition;
        private float displayedHealthRatio;
        private float previousHealth;
        private bool isBattleActive;
        private Coroutine hitRoutine;
        private Coroutine healthBarRoutine;

        private void Awake()
        {
            if (health == null || hull == null)
            {
                Debug.LogError("ShipHullFeedback requires health and hull references.", this);
                enabled = false;
                return;
            }
            hullColor = hull.color;

            if (healthFill != null)
                healthFillBaseLocalPosition = healthFill.transform.localPosition;
        }
        private void OnEnable()
        {
            if (health == null || hull == null) return;
            Bus<BattleStartEvent>.OnEvent += HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
            previousHealth = health.CurrentHealth;
            health.OnHealthChanged += OnHealthChanged;
            RefreshBar(health.CurrentHealth, health.MaxHealth, false);
        }
        private void OnDisable()
        {
            Bus<BattleStartEvent>.OnEvent -= HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            if (health != null) health.OnHealthChanged -= OnHealthChanged;
            if (hitRoutine != null) StopCoroutine(hitRoutine);
            if (healthBarRoutine != null) StopCoroutine(healthBarRoutine);
            hitRoutine = null;
            healthBarRoutine = null;
            if (hull != null) hull.color = hullColor;
        }
        private void HandleBattleStarted(BattleStartEvent evt)
        {
            isBattleActive = true;
            RefreshBar(health.CurrentHealth, health.MaxHealth, false);
        }
        private void HandleBattleEnded(StageBattleEndedEvent evt)
        {
            isBattleActive = false;
            RefreshBar(health.CurrentHealth, health.MaxHealth, false);
        }
        private void OnHealthChanged(float current, float maximum)
        {
            RefreshBar(current, maximum, true);
            if (current < previousHealth && isActiveAndEnabled)
            {
                if (hitRoutine != null) StopCoroutine(hitRoutine);
                hitRoutine = StartCoroutine(Hit());
            }
            else if (current >= maximum && hull != null)
            {
                if (hitRoutine != null) StopCoroutine(hitRoutine);
                hitRoutine = null;
                hull.color = hullColor;
            }
            previousHealth = current;
        }
        private void RefreshBar(float current, float maximum, bool shouldAnimate)
        {
            float targetRatio = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;

            if (healthBar != null)
                healthBar.SetActive(current > 0f && (isBattleActive || alwaysShowHealth || targetRatio < 1f));

            if (healthBarRoutine != null)
            {
                StopCoroutine(healthBarRoutine);
                healthBarRoutine = null;
            }

            if (!shouldAnimate || targetRatio >= displayedHealthRatio)
            {
                SetHealthFillRatio(targetRatio);
                return;
            }

            healthBarRoutine = StartCoroutine(DrainHealthBar(targetRatio));
        }

        private IEnumerator DrainHealthBar(float targetRatio)
        {
            float startRatio = displayedHealthRatio;
            float elapsed = 0f;

            while (elapsed < healthDrainSeconds)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / healthDrainSeconds);
                SetHealthFillRatio(Mathf.Lerp(startRatio, targetRatio, progress));
                yield return null;
            }

            SetHealthFillRatio(targetRatio);
            healthBarRoutine = null;
        }

        private void SetHealthFillRatio(float ratio)
        {
            displayedHealthRatio = ratio;

            if (healthFill == null)
                return;

            float currentWidth = barWidth * Mathf.Max(.001f, ratio);
            healthFill.size = new Vector2(currentWidth, barHeight);
            healthFill.transform.localPosition = new Vector3(
                healthFillBaseLocalPosition.x - (barWidth - currentWidth) * .5f,
                healthFillBaseLocalPosition.y,
                healthFillBaseLocalPosition.z);
            healthFill.color = ratio <= dangerThreshold ? dangerColor : healthyColor;
        }
        private IEnumerator Hit()
        {
            float elapsed = 0f;
            while (elapsed < hitSeconds)
            {
                elapsed += Time.deltaTime;
                hull.color = Color.Lerp(hitColor, hullColor, Mathf.Clamp01(elapsed / hitSeconds));
                yield return null;
            }
            hull.color = hullColor;
            hitRoutine = null;
        }
    }
}
