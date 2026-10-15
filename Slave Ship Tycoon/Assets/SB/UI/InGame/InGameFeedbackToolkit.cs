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
        [SerializeField] private InGameToolkitScreen inGameScreen;
        [SerializeField] private CurrencyType[] settlementCurrencies = { CurrencyType.Gold, CurrencyType.Diamond, CurrencyType.Emerald };
        [Header("Settlement (unscaled seconds)")]
        [SerializeField, Min(0.01f)] private float settlementFadeInSeconds = 0.3f;
        [SerializeField, Min(0)] private float settlementHoldSeconds = 2.5f;
        [SerializeField, Min(0), Tooltip("정산 후 강화·모집 버튼을 확인할 최소 시간입니다. 기존 정산 유지 시간과 더 긴 값을 사용합니다.")]
        private float settlementGrowthHoldSeconds = 6f;
        [SerializeField, Min(0.01f)] private float settlementFadeOutSeconds = 0.35f;
        [SerializeField, Min(0.1f)] private float rewardCountSeconds = 0.65f;
        [SerializeField, Min(0)] private float rewardRowDelaySeconds = 0.12f;
        [SerializeField, Range(0.8f,1)] private float settlementEntryScale = 0.94f;
        [SerializeField, Min(0.01f)] private float settlementPeakSeconds = 0.28f;
        [SerializeField, Range(1f, 1.16f)] private float settlementPeakScale = 1.06f;
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
        private VisualElement settlementPanel;
        private VisualElement settlementCard;
        private Label goalAmountLabel;
        private Label settlementStatus;
        private SaleSettlementResult lastResult;
        private CurrencyType lastGoalCurrency;
        private long lastBaseValue;
        private long lastMarketBonus;
        private long lastEquipmentBonus;
        private bool hasLastBreakdown;
        private ChapterRouteType currentRouteType;
        private Coroutine settlementRoutine;
        private Coroutine popupRoutine;
        private Coroutine stageFadeRoutine;

        private void OnEnable()
        {
            if (document == null) { Debug.LogError("InGameFeedbackToolkit의 Document를 연결하세요.", this); return; }
            root = document.rootVisualElement; root.AddToClassList("harbor-document"); root.pickingMode = PickingMode.Ignore;
            settlementPanel = root.Q("settlement");
            settlementCard = root.Q("settlementCard");
            goalAmountLabel = root.Q<Label>("settlementGoalAmount");
            settlementStatus = root.Q<Label>("settlementStatus");
            root.Q<Button>("settlementClose").clicked += CloseSettlement;
            root.Q<Button>("settlementUpgrade").clicked += OpenUpgrade;
            root.Q<Button>("settlementRecruit").clicked += OpenRecruitment;
            VisualElement rewards = root.Q("settlementRewards"); rewards.Clear(); rewardLabels.Clear();
            foreach (CurrencyType currency in settlementCurrencies)
            {
                var row = new VisualElement(); row.AddToClassList("settlement-reward"); row.userData = currency;
                var name = new Label(CurrencyTextFormatter.FormatName(currency)); name.AddToClassList("settlement-reward-name"); row.Add(name);
                var amount = new Label("0"); amount.AddToClassList("reward-amount"); row.Add(amount);
                rewards.Add(row); rewardLabels.Add(currency, amount);
            }
            Bus<SaleSettlementCompletedEvent>.OnEvent += OnSettlement;
            Bus<StageFailedEvent>.OnEvent += OnStageFail;
            Bus<AffterStageClearEvent>.OnEvent += OnChapterEnter;
            Bus<PlayStageClearEffectEvent>.OnEvent += OnStageTransition;
            Bus<StageStartedEvent>.OnEvent += OnStageStarted;
        }
        private void OnDisable()
        {
            Bus<SaleSettlementCompletedEvent>.OnEvent -= OnSettlement;
            Bus<StageFailedEvent>.OnEvent -= OnStageFail;
            Bus<AffterStageClearEvent>.OnEvent -= OnChapterEnter;
            Bus<PlayStageClearEffectEvent>.OnEvent -= OnStageTransition;
            Bus<StageStartedEvent>.OnEvent -= OnStageStarted;
            StopAllCoroutines(); settlementRoutine = null; popupRoutine = null; stageFadeRoutine = null;
            if (root != null)
            {
                if (root.Q<Button>("settlementClose") is Button settlementCloseBinding) settlementCloseBinding.clicked -= CloseSettlement;
                if (root.Q<Button>("settlementUpgrade") is Button settlementUpgradeBinding) settlementUpgradeBinding.clicked -= OpenUpgrade;
                if (root.Q<Button>("settlementRecruit") is Button settlementRecruitBinding) settlementRecruitBinding.clicked -= OpenRecruitment;
                goalAmountLabel.style.scale = new Scale(Vector3.one);
                root.Q("settlement").AddToClassList("hidden"); root.Q("stagePopup").AddToClassList("hidden"); root.Q("stageFade").AddToClassList("hidden");
            }
        }
        private void OnSettlement(SaleSettlementCompletedEvent evt)
        {
            if (evt.Result == null) return;
            lastResult = evt.Result;
            lastGoalCurrency = inGameScreen != null ? inGameScreen.TransportGoalCurrency : CurrencyType.Gold;
            hasLastBreakdown = inGameScreen != null && inGameScreen.TryGetTransportValueBreakdown(lastGoalCurrency,
                out lastBaseValue, out lastMarketBonus, out lastEquipmentBonus);
            long goalTotal = GetRewardAmount(evt.Result, lastGoalCurrency);
            hasLastBreakdown = hasLastBreakdown && lastBaseValue >= 0 && lastMarketBonus >= 0 &&
                (decimal)lastBaseValue + lastMarketBonus + lastEquipmentBonus == goalTotal;
            ShowSettlement(evt.Result);
        }
        public bool ReplayLastSettlement()
        {
            if (lastResult == null) return false;
            ShowSettlement(lastResult); return true;
        }
        private void ShowSettlement(SaleSettlementResult result)
        {
            if (result == null || root == null) return;
            if (settlementRoutine != null) StopCoroutine(settlementRoutine);
            root.Q<Label>("settlementTitle").text = $"항로 {result.SellChapter} 운송 완주";
            long totalCargo = 0; int highestMultiplier = 1;
            foreach (CargoSaleResult cargo in result.CargoResults) { totalCargo += cargo.CargoAmount; highestMultiplier = Mathf.Max(highestMultiplier, cargo.MarketMultiplier); }
            root.Q<Label>("cargoSummary").text = $"화물 {CurrencyTextFormatter.Format(totalCargo)}개 하역 완료 · 최고 시세 x{highestMultiplier}";
            root.Q<Label>("settlementGoalTitle").text = $"이번 운송 목표 · {CurrencyTextFormatter.FormatName(lastGoalCurrency)}";
            root.Q<Label>("settlementGrowthHint").text = lastGoalCurrency == CurrencyType.Diamond
                ? "지켜낸 수익으로 호위선을 모집해 다음 항로를 준비하세요."
                : "지켜낸 수익으로 함대를 강화해 다음 항로를 준비하세요.";
            goalAmountLabel.text = "0";
            goalAmountLabel.tooltip = $"최종 지급 {GetRewardAmount(result, lastGoalCurrency):N0}";
            goalAmountLabel.style.scale = new Scale(Vector3.one);
            settlementStatus.text = "항구 도착 · 운송 수익 정산";
            settlementCard.RemoveFromClassList("is-settled");
            VisualElement breakdown = root.Q("settlementBreakdown");
            breakdown.EnableInClassList("hidden", !hasLastBreakdown);
            if (hasLastBreakdown)
            {
                SetBreakdownValue("settlementBaseValue", lastBaseValue, false);
                SetBreakdownValue("settlementMarketBonus", lastMarketBonus, true);
                SetBreakdownValue("settlementEquipmentBonus", lastEquipmentBonus, true);
            }
            bool canOpenGrowth = inGameScreen != null;
            root.Q("settlementActions").EnableInClassList("hidden", !canOpenGrowth);
            root.Q<Label>("settlementGrowthHint").EnableInClassList("hidden", !canOpenGrowth);
            root.Q<Button>("settlementUpgrade").EnableInClassList("primary-button", lastGoalCurrency != CurrencyType.Diamond);
            root.Q<Button>("settlementRecruit").EnableInClassList("primary-button", lastGoalCurrency == CurrencyType.Diamond);
            rewardAmounts.Clear();
            foreach (var reward in result.TotalRewards) rewardAmounts[reward.CurrencyType] = reward.Amount;
            foreach (var pair in rewardLabels)
            {
                rewardAmounts.TryGetValue(pair.Key, out long amount);
                pair.Value.parent.EnableInClassList("hidden", amount <= 0);
                pair.Value.parent.EnableInClassList("is-goal", pair.Key == lastGoalCurrency);
                pair.Value.text = "0";
                pair.Value.tooltip = $"{amount:N0}";
            }
            root.Q("settlement").RemoveFromClassList("hidden");
            settlementRoutine = StartCoroutine(SettlementReveal());
        }
        private IEnumerator SettlementReveal()
        {
            VisualElement panel = settlementPanel;
            yield return Fade(panel, 0, 1, settlementFadeInSeconds, true, true);
            foreach (CurrencyType currency in settlementCurrencies)
            {
                if (!rewardAmounts.TryGetValue(currency, out long amount) || amount <= 0) continue;
                float elapsed = 0;
                while (elapsed < rewardCountSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = 1 - Mathf.Pow(1 - Mathf.Clamp01(elapsed / rewardCountSeconds), 3);
                    string counted = CurrencyTextFormatter.Format(progress >= 1f ? amount : (long)(amount * (double)progress));
                    rewardLabels[currency].text = counted;
                    if (currency == lastGoalCurrency) goalAmountLabel.text = counted;
                    yield return null;
                }
                rewardLabels[currency].text = CurrencyTextFormatter.Format(amount);
                if (currency == lastGoalCurrency) goalAmountLabel.text = CurrencyTextFormatter.Format(amount);
                if (rewardRowDelaySeconds > 0) yield return new WaitForSecondsRealtime(rewardRowDelaySeconds);
            }
            goalAmountLabel.text = CurrencyTextFormatter.Format(GetRewardAmount(lastResult, lastGoalCurrency));
            settlementStatus.text = "운송 수익 지급 완료";
            settlementCard.AddToClassList("is-settled");
            float peakElapsed = 0;
            while (peakElapsed < settlementPeakSeconds)
            {
                peakElapsed += Time.unscaledDeltaTime;
                float peak = Mathf.Sin(Mathf.Clamp01(peakElapsed / settlementPeakSeconds) * Mathf.PI);
                goalAmountLabel.style.scale = new Scale(Vector3.one * Mathf.Lerp(1f, settlementPeakScale, peak));
                yield return null;
            }
            goalAmountLabel.style.scale = new Scale(Vector3.one);
            float holdSeconds = Mathf.Max(settlementHoldSeconds, settlementGrowthHoldSeconds);
            if (holdSeconds > 0) yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Fade(panel, 1, 0, settlementFadeOutSeconds, true, true);
            panel.AddToClassList("hidden"); settlementRoutine = null;
        }
        private void CloseSettlement()
        {
            if (settlementRoutine != null) StopCoroutine(settlementRoutine);
            settlementRoutine = null;
            goalAmountLabel.style.scale = new Scale(Vector3.one);
            settlementPanel.AddToClassList("hidden");
        }
        private void OpenUpgrade()
        {
            CloseSettlement();
            if (inGameScreen != null) inGameScreen.OpenGrowth(lastGoalCurrency == CurrencyType.Emerald ? CurrencyType.Emerald : CurrencyType.Gold);
        }
        private void OpenRecruitment()
        {
            CloseSettlement();
            if (inGameScreen != null) inGameScreen.OpenGrowth(CurrencyType.Diamond);
        }
        private void SetBreakdownValue(string name, long amount, bool signed)
        {
            Label label = root.Q<Label>(name);
            label.text = (signed && amount > 0 ? "+" : "") + CurrencyTextFormatter.Format(amount);
            label.tooltip = $"{amount:N0}";
        }
        private static long GetRewardAmount(SaleSettlementResult result, CurrencyType currency)
        {
            foreach (SaleCurrencyReward reward in result.TotalRewards)
                if (reward.CurrencyType == currency) return reward.Amount;
            return 0;
        }
        private void OnStageStarted(StageStartedEvent evt) => currentRouteType = evt.RouteType;
        private void OnStageFail(StageFailedEvent evt) => ShowPopup(evt.Chapter, evt.Stage, currentRouteType == ChapterRouteType.Sell
            ? "운송 저지 · 같은 구간 재도전\n기본가치 유지 · 보너스 차감은 구간당 1회"
            : "스테이지 실패");
        private void OnChapterEnter(AffterStageClearEvent evt) => ShowPopup(evt.Chapter + 1, 1,
            evt.ClearChapterType == ChapterRouteType.Obtain ? "운송 항로 진입" : "해적 해역 진입");
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
            if (evt.ClearChapterType == ChapterRouteType.Sell)
                while (inGameScreen != null && inGameScreen.isActiveAndEnabled && inGameScreen.IsTransportBonusPlaying)
                    yield return null;
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
                if (scaleCard) settlementCard.style.scale = new Scale(Vector3.one * Mathf.Lerp(settlementEntryScale, 1, alpha));
                yield return null;
            }
            element.style.opacity = end;
        }
    }
}
