using System.Collections;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.Currency;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.InGame
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class InGameFeedbackToolkit : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument document;
        [SerializeField] private CurrencyType[] settlementCurrencies = { CurrencyType.Gold, CurrencyType.Diamond, CurrencyType.Emerald };
        [Header("Settlement (unscaled seconds)")]
        [SerializeField, Min(0.01f)] private float settlementFadeInSeconds = 0.3f;
        [SerializeField, Min(0)] private float settlementHoldSeconds = 2.5f;
        [SerializeField, Min(0.01f)] private float settlementFadeOutSeconds = 0.35f;
        [SerializeField, Min(0.1f)] private float rewardCountSeconds = 0.65f;
        [SerializeField, Min(0)] private float rewardRowDelaySeconds = 0.12f;
        [SerializeField, Range(0.8f,1)] private float settlementEntryScale = 0.94f;
        [Header("Stage popup (scaled seconds)")]
        [SerializeField, Min(0.01f)] private float popupFadeInSpeed = 8f;
        [SerializeField, Min(0)] private float popupHoldSeconds = 1f;
        [SerializeField, Min(0.01f)] private float popupFadeOutSpeed = 5f;
        [Header("Stage transition (scaled seconds)")]
        [SerializeField, Min(0)] private float stageFadeInSeconds = 0.15f;
        [SerializeField, Min(0)] private float stageFadeHoldSeconds = 0.1f;
        [SerializeField, Min(0)] private float stageFadeOutSeconds = 0.2f;

        private readonly Dictionary<CurrencyType, Label> rewardLabels = new Dictionary<CurrencyType, Label>();
        private readonly Dictionary<CurrencyType, long> rewardAmounts = new Dictionary<CurrencyType, long>();
        private VisualElement root;
        private SaleSettlementResult lastResult;
        private Coroutine settlementRoutine;
        private Coroutine popupRoutine;
        private Coroutine stageFadeRoutine;

        private void OnEnable()
        {
            if (document == null) { Debug.LogError("InGameFeedbackToolkit의 Document를 연결하세요.", this); return; }
            root = document.rootVisualElement; root.AddToClassList("harbor-document"); root.pickingMode = PickingMode.Ignore;
            root.Q<Button>("settlementClose").clicked += CloseSettlement;
            VisualElement rewards = root.Q("settlementRewards"); rewards.Clear(); rewardLabels.Clear();
            foreach (CurrencyType currency in settlementCurrencies)
            {
                var row = new VisualElement(); row.AddToClassList("settlement-reward"); row.userData = currency;
                row.Add(new Label(CurrencyTextFormatter.FormatName(currency)));
                var amount = new Label("0"); amount.AddToClassList("reward-amount"); row.Add(amount);
                rewards.Add(row); rewardLabels.Add(currency, amount);
            }
            Bus<SaleSettlementCompletedEvent>.OnEvent += OnSettlement;
            Bus<EvenStageClearedEvent>.OnEvent += OnStageClear;
            Bus<StageFailedEvent>.OnEvent += OnStageFail;
            Bus<AffterStageClearEvent>.OnEvent += OnChapterEnter;
            Bus<PlayStageClearEffectEvent>.OnEvent += OnStageTransition;
        }
        private void OnDisable()
        {
            Bus<SaleSettlementCompletedEvent>.OnEvent -= OnSettlement;
            Bus<EvenStageClearedEvent>.OnEvent -= OnStageClear;
            Bus<StageFailedEvent>.OnEvent -= OnStageFail;
            Bus<AffterStageClearEvent>.OnEvent -= OnChapterEnter;
            Bus<PlayStageClearEffectEvent>.OnEvent -= OnStageTransition;
            StopAllCoroutines(); settlementRoutine = null; popupRoutine = null; stageFadeRoutine = null;
            if (root != null)
            {
                if (root.Q<Button>("settlementClose") is Button settlementCloseBinding) settlementCloseBinding.clicked -= CloseSettlement;
                root.Q("settlement").AddToClassList("hidden"); root.Q("stagePopup").AddToClassList("hidden"); root.Q("stageFade").AddToClassList("hidden");
            }
        }
        private void OnSettlement(SaleSettlementCompletedEvent evt) => ShowSettlement(evt.Result);
        public bool ReplayLastSettlement()
        {
            if (lastResult == null) return false;
            ShowSettlement(lastResult); return true;
        }
        private void ShowSettlement(SaleSettlementResult result)
        {
            if (result == null || root == null) return;
            if (settlementRoutine != null) StopCoroutine(settlementRoutine);
            lastResult = result;
            root.Q<Label>("settlementTitle").text = $"항로 {result.SellChapter} · 목적지 도착";
            long totalCargo = 0; int highestMultiplier = 1;
            foreach (CargoSaleResult cargo in result.CargoResults) { totalCargo += cargo.CargoAmount; highestMultiplier = Mathf.Max(highestMultiplier, cargo.MarketMultiplier); }
            root.Q<Label>("cargoSummary").text = $"화물 {CurrencyTextFormatter.Format(totalCargo)}개 하역 · 최고 시세 x{highestMultiplier}\n운송 장비 효과를 포함한 최종 수익";
            rewardAmounts.Clear();
            foreach (var reward in result.TotalRewards) rewardAmounts[reward.CurrencyType] = reward.Amount;
            foreach (var pair in rewardLabels)
            {
                rewardAmounts.TryGetValue(pair.Key, out long amount);
                pair.Value.parent.EnableInClassList("hidden", amount <= 0);
                pair.Value.text = "0";
            }
            root.Q("settlement").RemoveFromClassList("hidden");
            settlementRoutine = StartCoroutine(SettlementReveal());
        }
        private IEnumerator SettlementReveal()
        {
            VisualElement panel = root.Q("settlement");
            yield return Fade(panel, 0, 1, settlementFadeInSeconds, true, true);
            foreach (CurrencyType currency in settlementCurrencies)
            {
                if (!rewardAmounts.TryGetValue(currency, out long amount) || amount <= 0) continue;
                float elapsed = 0;
                while (elapsed < rewardCountSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = 1 - Mathf.Pow(1 - Mathf.Clamp01(elapsed / rewardCountSeconds), 3);
                    rewardLabels[currency].text = CurrencyTextFormatter.Format((long)(amount * (double)progress));
                    yield return null;
                }
                rewardLabels[currency].text = CurrencyTextFormatter.Format(amount);
                if (rewardRowDelaySeconds > 0) yield return new WaitForSecondsRealtime(rewardRowDelaySeconds);
            }
            if (settlementHoldSeconds > 0) yield return new WaitForSecondsRealtime(settlementHoldSeconds);
            yield return Fade(panel, 1, 0, settlementFadeOutSeconds, true, true);
            panel.AddToClassList("hidden"); settlementRoutine = null;
        }
        private void CloseSettlement()
        {
            if (settlementRoutine != null) StopCoroutine(settlementRoutine);
            settlementRoutine = null; root.Q("settlement").AddToClassList("hidden");
        }
        private void OnStageClear(EvenStageClearedEvent evt) => ShowPopup(evt.Chapter, evt.Stage, "스테이지 클리어");
        private void OnStageFail(StageFailedEvent evt) => ShowPopup(evt.Chapter, evt.Stage, "스테이지 실패");
        private void OnChapterEnter(AffterStageClearEvent evt) => ShowPopup(evt.Chapter + 1, 1,
            evt.ClearChapterType == ChapterRouteType.Obtain ? "해군 순찰 해역 진입" : "해적 출몰 해역 진입");
        private void ShowPopup(int chapter, int stage, string message)
        {
            if (popupRoutine != null) StopCoroutine(popupRoutine);
            root.Q<Label>("stageNumber").text = $"{chapter}-{stage}"; root.Q<Label>("stageResult").text = message;
            popupRoutine = StartCoroutine(Popup());
        }
        private IEnumerator Popup()
        {
            VisualElement popup = root.Q("stagePopup"); popup.RemoveFromClassList("hidden");
            yield return Fade(popup, 0, 1, 1 / popupFadeInSpeed, false);
            if (popupHoldSeconds > 0) yield return new WaitForSeconds(popupHoldSeconds);
            yield return Fade(popup, 1, 0, 1 / popupFadeOutSpeed, false);
            popup.AddToClassList("hidden"); popupRoutine = null;
        }
        private void OnStageTransition(PlayStageClearEffectEvent evt)
        {
            if (stageFadeRoutine != null) StopCoroutine(stageFadeRoutine);
            stageFadeRoutine = StartCoroutine(StageTransition(evt));
        }
        private IEnumerator StageTransition(PlayStageClearEffectEvent evt)
        {
            VisualElement fade = root.Q("stageFade"); fade.RemoveFromClassList("hidden");
            yield return Fade(fade, 0, 1, stageFadeInSeconds, false);
            if (stageFadeHoldSeconds > 0) yield return new WaitForSeconds(stageFadeHoldSeconds);
            yield return Fade(fade, 1, 0, stageFadeOutSeconds, false);
            fade.AddToClassList("hidden"); stageFadeRoutine = null;
            // This completion event advances the existing GameFlowManager. Retire the legacy fade controller to avoid two advances.
            Bus<AffterStageClearEvent>.Raise(new AffterStageClearEvent(evt.Chapter, evt.Stage, evt.ClearChapterType));
        }
        private IEnumerator Fade(VisualElement element, float start, float end, float seconds, bool unscaled, bool scaleCard = false)
        {
            float elapsed = 0; element.style.opacity = start;
            while (elapsed < seconds)
            {
                elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float alpha = Mathf.Lerp(start, end, seconds > 0 ? Mathf.Clamp01(elapsed / seconds) : 1);
                element.style.opacity = alpha;
                if (scaleCard) root.Q("settlementCard").style.scale = new Scale(Vector3.one * Mathf.Lerp(settlementEntryScale, 1, alpha));
                yield return null;
            }
            element.style.opacity = end;
        }
    }
}