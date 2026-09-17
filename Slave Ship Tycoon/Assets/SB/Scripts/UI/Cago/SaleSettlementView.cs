using System;
using System.Collections;
using SB.Scripts.Currency;
using TMPro;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class SaleSettlementView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _cargoSummaryText;
        [SerializeField] private SaleRewardSlotView[] _rewardSlots = Array.Empty<SaleRewardSlotView>();
        [SerializeField, Min(0.01f)] private float _fadeInDuration = 0.3f;
        [SerializeField, Min(0f)] private float _holdDuration = 2.5f;
        [SerializeField, Min(0.01f)] private float _fadeOutDuration = 0.35f;

        [Header("Harbor ledger reveal")]
        [SerializeField, Min(0.1f)] private float rewardCountSeconds = 0.65f;
        [SerializeField, Min(0f)] private float rewardRowDelaySeconds = 0.12f;
        [SerializeField, Range(0.8f, 1f)] private float entryScale = 0.94f;
        private SaleSettlementResult lastResult;
        private Coroutine _showRoutine;

        public void ReplayLastSettlement()
        {
            if (lastResult != null) Show(lastResult);
        }

        private void OnDisable()
        {
            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = null;
            SetAlpha(0f);
            if (_panel != null) _panel.SetActive(false);
        }

        private void Awake()
        {
            SetAlpha(0f);

            if (_panel != null)
                _panel.SetActive(false);
        }

        public void Show(SaleSettlementResult result)
        {
            if (result == null)
                return;

            if (_showRoutine != null)
                StopCoroutine(_showRoutine);

            lastResult = result;
            Refresh(result);
            foreach (var slot in _rewardSlots) if (slot != null) slot.ShowProgress(0f);

            if (_panel != null)
                _panel.SetActive(true);

            SetAlpha(0f);
            _showRoutine = StartCoroutine(ShowRoutine());
        }

        private void Refresh(SaleSettlementResult result)
        {
            if (_titleText != null)
                _titleText.text = $"항로 {result.SellChapter} · 목적지 도착";

            long totalCargo = 0;
            int highestMultiplier = 1;

            for (int i = 0; i < result.CargoResults.Count; i++)
            {
                CargoSaleResult cargoResult = result.CargoResults[i];
                totalCargo += cargoResult.CargoAmount;
                highestMultiplier = Mathf.Max(highestMultiplier, cargoResult.MarketMultiplier);
            }

            if (_cargoSummaryText != null)
                _cargoSummaryText.text = $"화물 {CurrencyTextFormatter.Format(totalCargo)}개 하역  ·  최고 시세 x{highestMultiplier}\n운송 장비 효과를 포함한 최종 수익";

            for (int slotIndex = 0; slotIndex < _rewardSlots.Length; slotIndex++)
            {
                SaleRewardSlotView rewardSlot = _rewardSlots[slotIndex];

                if (rewardSlot == null)
                    continue;

                long amount = 0;

                for (int rewardIndex = 0; rewardIndex < result.TotalRewards.Count; rewardIndex++)
                {
                    SaleCurrencyReward reward = result.TotalRewards[rewardIndex];

                    if (reward.CurrencyType == rewardSlot.CurrencyType)
                    {
                        amount = reward.Amount;
                        break;
                    }
                }

                rewardSlot.SetReward(amount);
            }
        }

        private IEnumerator ShowRoutine()
        {
            yield return Fade(0f, 1f, _fadeInDuration);
            foreach (var slot in _rewardSlots)
            {
                if (slot == null || !slot.gameObject.activeSelf) continue;
                float elapsed = 0f;
                while (elapsed < rewardCountSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / rewardCountSeconds);
                    slot.ShowProgress(1f - Mathf.Pow(1f - t, 3f));
                    yield return null;
                }
                slot.ShowProgress(1f);
                if (rewardRowDelaySeconds > 0f) yield return new WaitForSecondsRealtime(rewardRowDelaySeconds);
            }

            if (_holdDuration > 0f)
                yield return new WaitForSecondsRealtime(_holdDuration);

            yield return Fade(1f, 0f, _fadeOutDuration);

            if (_panel != null)
                _panel.SetActive(false);

            _showRoutine = null;
        }

        private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsedTime / duration)));
                yield return null;
            }

            SetAlpha(endAlpha);
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = alpha;
            if (_panel != null) _panel.transform.localScale = Vector3.one * Mathf.Lerp(entryScale, 1f, alpha);
        }
    }
}
