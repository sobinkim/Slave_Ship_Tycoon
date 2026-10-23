using System;
using System.Collections;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using SB.Scripts.Fleet;
using SB.Scripts.Visual;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.Recruitment
{
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class RecruitmentToolkitScreen : MonoBehaviour
    {
        private sealed class RewardCard
        {
            public Button Root;
            public VisualElement Flash;
            public VisualElement Ring;
            public bool RevealFirstAttempt;
            public bool RevealSecondAttempt;
        }

        [Header("References")]
        [SerializeField] private UIDocument document;
        [SerializeField] private EscortGachaManager gachaManager;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private PannelManager panelManager;
        [SerializeField] private VisualTreeAsset cardTemplate;
        [SerializeField] private Sprite recruitmentIcon;
        [SerializeField, Min(1), Tooltip("묶음 모집 횟수")]
        private int multiSummonCount = 15;
        [Header("Reveal timing (unscaled seconds)")]
        [SerializeField, Min(0.01f)] private float revealSeconds = 0.12f;
        [SerializeField, Min(0f)] private float cardDelaySeconds = 0.025f;
        [Header("Promotion reveal (unscaled seconds)")]
        [SerializeField, Min(0f)] private float promotionLeadInSeconds = 0.18f;
        [SerializeField, Min(0.01f)] private float firstAnticipationSeconds = 1.2f;
        [SerializeField, Min(0.01f)] private float secondAnticipationSeconds = 1.65f;
        [SerializeField, Min(0f)] private float secondPromotionPauseSeconds = 0.35f;
        [SerializeField, Min(0.01f)] private float promotionBurstSeconds = 0.5f;
        [SerializeField, Min(0f)] private float promotionHoldSeconds = 0.25f;
        [SerializeField, Min(0f)] private float promotionShakePixels = 5f;
        [SerializeField, Min(1f)] private float promotionShakeFrequency = 24f;
        [Header("Failed promotion reveal (visual only)")]
        [SerializeField, Range(0f, 1f), Tooltip("실제 1차 승급 실패 카드 중 전조를 보여줄 비율. 실제 보상 확률에는 영향이 없습니다.")]
        private float firstFailureRevealChance = 0.12f;
        [SerializeField, Range(0f, 1f), Tooltip("실제 2차 승급 실패 카드 중 전조를 보여줄 비율. 실제 보상 확률에는 영향이 없습니다.")]
        private float secondFailureRevealChance = 0.15f;
        [SerializeField, Range(0.25f, 0.85f), Tooltip("실패한 경우 전체 예고 시간 중 떨림을 멈출 지점")]
        private float failedPromotionStopProgress = 0.6f;
        [SerializeField, Min(0.01f)] private float promotionFailureFadeSeconds = 0.35f;
        [SerializeField, Min(0f)] private float promotionFailureHoldSeconds = 0.25f;

        private readonly System.Random revealRandom = new System.Random();
        private readonly List<EscortGachaResult> rewards = new List<EscortGachaResult>();
        private readonly List<RewardCard> rewardCards = new List<RewardCard>();
        private VisualElement root;
        private VisualElement resultPanel;
        private VisualElement resultGrid;
        private ScrollView resultScroll;
        private VisualElement resultFlash;
        private Label statusLabel;
        private Coroutine revealRoutine;
        private bool isBusy;
        private int lastSummonCount;

        public event Action<bool> OpenStateChanged;

        private void OnEnable()
        {
            if (document == null || gachaManager == null || currencyManager == null || panelManager == null || cardTemplate == null)
            {
                Debug.LogError("RecruitmentToolkitScreen의 Document, Manager, Card Template을 연결하세요.", this);
                return;
            }
            root = document.rootVisualElement;
            root.AddToClassList("harbor-document");
            root.pickingMode = PickingMode.Ignore;
            resultPanel = root.Q("resultPanel");
            resultGrid = root.Q("resultGrid");
            resultScroll = root.Q<ScrollView>("resultScroll");
            resultFlash = root.Q("resultFlash");
            statusLabel = root.Q<Label>("statusLabel");
            ResetRevealEffects();
            resultPanel.AddToClassList("hidden");
            root.Q<Button>("closeButton").clicked += Close;
            root.Q<Button>("singleButton").clicked += SummonSingle;
            root.Q<Button>("multiButton").clicked += SummonMulti;
            root.Q<Button>("skipButton").clicked += SkipReveal;
            root.Q<Button>("confirmButton").clicked += Confirm;
            root.Q<Button>("repeatButton").clicked += RepeatLastRecruitment;
            root.Q<Button>("formationButton").clicked += OpenFormation;
            root.Q<Button>("resultFormationButton").clicked += OpenFormation;
            ShipCardPresentation.SetIcon(root.Q("recruitmentIcon"), recruitmentIcon);
            Bus<CurrencyChangedEvent>.OnEvent += OnCurrencyChanged;
            panelManager.PanelChanged += OnPanelChanged;
            statusLabel.text = string.Empty;
            Refresh();
            OpenStateChanged?.Invoke(true);
        }

        private void OnDisable()
        {
            Bus<CurrencyChangedEvent>.OnEvent -= OnCurrencyChanged;
            if (panelManager != null) panelManager.PanelChanged -= OnPanelChanged;
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            ResetRevealEffects();
            isBusy = false;
            OpenStateChanged?.Invoke(false);
            if (root == null) return;
            if (root.Q<Button>("closeButton") is Button closeButtonBinding) closeButtonBinding.clicked -= Close;
            if (root.Q<Button>("singleButton") is Button singleButtonBinding) singleButtonBinding.clicked -= SummonSingle;
            if (root.Q<Button>("multiButton") is Button multiButtonBinding) multiButtonBinding.clicked -= SummonMulti;
            if (root.Q<Button>("skipButton") is Button skipButtonBinding) skipButtonBinding.clicked -= SkipReveal;
            if (root.Q<Button>("confirmButton") is Button confirmButtonBinding) confirmButtonBinding.clicked -= Confirm;
            if (root.Q<Button>("repeatButton") is Button repeatButtonBinding) repeatButtonBinding.clicked -= RepeatLastRecruitment;
            if (root.Q<Button>("formationButton") is Button formationButtonBinding) formationButtonBinding.clicked -= OpenFormation;
            if (root.Q<Button>("resultFormationButton") is Button resultFormationButtonBinding) resultFormationButtonBinding.clicked -= OpenFormation;
        }

        public void Open()
        {
            if (panelManager != null && panelManager.CurrentPanel == PanelType.Fleet)
                panelManager.CloseFleet();
            gameObject.SetActive(true);
        }
        public void Close() => gameObject.SetActive(false);
        private void OpenFormation()
        {
            Close();
            panelManager.ClickPanelButton(PanelType.Fleet);
        }
        private void OnPanelChanged(PanelType type) => Close();
        private void OnCurrencyChanged(CurrencyChangedEvent evt)
        {
            if (evt.CurrencyType == gachaManager.CurrencyType) Refresh();
        }

        private void Refresh()
        {
            if (root == null) return;
            long balance = currencyManager.GetCurrency(gachaManager.CurrencyType);
            string unit = CurrencyTextFormatter.FormatName(gachaManager.CurrencyType);
            root.Q<Label>("singleCost").text = $"{gachaManager.SummonCost:N0} {unit}";
            long multiCost = (long)gachaManager.SummonCost * multiSummonCount;
            root.Q<Label>("multiTitle").text = $"{multiSummonCount}회 모집";
            root.Q<Label>("multiCost").text = $"{multiCost:N0} {unit}";
            bool canSummon = gachaManager.CanSummon(out EscortGachaFailureReason failure);
            root.Q<Button>("singleButton").SetEnabled(!isBusy && canSummon);
            root.Q<Button>("multiButton").SetEnabled(!isBusy && canSummon && balance >= multiCost);
            int repeatCount = Mathf.Max(1, lastSummonCount);
            long repeatCost = (long)gachaManager.SummonCost * repeatCount;
            root.Q<Label>("repeatTitle").text = $"{repeatCount}회 다시 모집";
            root.Q<Label>("repeatCost").text = $"{repeatCost:N0} {unit}";
            root.Q<Button>("repeatButton").SetEnabled(!isBusy && canSummon && balance >= repeatCost);
            if (!canSummon && !isBusy)
                statusLabel.text = failure == EscortGachaFailureReason.InsufficientCurrency
                    ? "모집 재화가 부족해요. 운송 정산으로 모아보세요." : "모집 설정을 확인해주세요.";
        }

        private void SummonSingle() => Recruit(1);
        private void SummonMulti() => Recruit(multiSummonCount);
        private void RepeatLastRecruitment() => Recruit(lastSummonCount > 0 ? lastSummonCount : 1);

        private void Recruit(int count)
        {
            if (isBusy || resultPanel == null) return;
            if (!gachaManager.CanSummon(out EscortGachaFailureReason failure) ||
                currencyManager.GetCurrency(gachaManager.CurrencyType) < (long)gachaManager.SummonCost * count)
            {
                statusLabel.text = failure == EscortGachaFailureReason.InsufficientCurrency || failure == EscortGachaFailureReason.None
                    ? "모집에 필요한 재화가 부족해요." : "모집 설정을 확인해주세요.";
                Refresh();
                return;
            }
            isBusy = true;
            lastSummonCount = count;
            var recruitedRewards = new List<EscortGachaResult>(count);
            for (int i = 0; i < count; i++)
            {
                if (!gachaManager.TrySummon(out EscortGachaResult result)) break;
                recruitedRewards.Add(result);
            }
            if (recruitedRewards.Count == 0) { isBusy = false; Refresh(); return; }
            rewards.Clear();
            rewards.AddRange(recruitedRewards);
            rewardCards.Clear();
            resultGrid.Clear();
            root.Q<Label>("resultSummary").text = rewards.Count == 1
                ? "새 호위선 1척이 합류했습니다.\n함대에 배치하면 다음 전투부터 출전합니다."
                : $"새 호위선 {rewards.Count}척이 합류했습니다.\n함대에 배치하면 다음 전투부터 출전합니다.";
            foreach (EscortGachaResult reward in rewards)
            {
                Button card = ShipCardPresentation.Create(cardTemplate, resultGrid);
                ShipCardPresentation.Bind(card, reward.InitialReward, string.Empty);
                card.AddToClassList("recruitment-reward-card");
                card.pickingMode = PickingMode.Ignore;
                card.focusable = false;
                card.style.opacity = 0f;
                var flash = new VisualElement { pickingMode = PickingMode.Ignore };
                flash.AddToClassList("recruitment-card-flash");
                card.Add(flash);
                var ring = new VisualElement { pickingMode = PickingMode.Ignore };
                ring.AddToClassList("recruitment-card-ring");
                card.Add(ring);
                rewardCards.Add(new RewardCard
                {
                    Root = card,
                    Flash = flash,
                    Ring = ring,
                    RevealFirstAttempt = reward.PromotionCount > 0 ||
                        (reward.PromotionAttemptCount > 0 && revealRandom.NextDouble() < firstFailureRevealChance),
                    RevealSecondAttempt = reward.PromotionCount == 2 ||
                        (reward.PromotionCount == 1 && reward.PromotionAttemptCount > 1 && revealRandom.NextDouble() < secondFailureRevealChance)
                });
            }
            resultPanel.RemoveFromClassList("hidden");
            SetResultActions(false);
            Refresh();
            revealRoutine = StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            resultScroll.scrollOffset = Vector2.zero;
            for (int i = 0; i < rewards.Count; i++)
            {
                EscortGachaResult reward = rewards[i];
                RewardCard card = rewardCards[i];
                resultScroll.ScrollTo(card.Root);
                float elapsed = 0f;
                while (elapsed < revealSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / revealSeconds);
                    float eased = 1f - Mathf.Pow(1f - progress, 3f);
                    card.Root.style.opacity = eased;
                    yield return null;
                }
                card.Root.style.opacity = StyleKeyword.Null;
                if (card.RevealFirstAttempt)
                {
                    foreach (RewardCard other in rewardCards)
                        other.Root.EnableInClassList("recruitment-card-muted", other != card);
                    if (promotionLeadInSeconds > 0f)
                        yield return new WaitForSecondsRealtime(promotionLeadInSeconds);
                    yield return RevealPromotion(card, reward.FirstPromotionReward, false);
                    if (card.RevealSecondAttempt)
                    {
                        if (secondPromotionPauseSeconds > 0f)
                            yield return new WaitForSecondsRealtime(secondPromotionPauseSeconds);
                        yield return RevealPromotion(card, reward.PromotionCount == 2 ? reward.Reward : null, true);
                    }
                    foreach (RewardCard other in rewardCards)
                        other.Root.RemoveFromClassList("recruitment-card-muted");
                }
                if (cardDelaySeconds > 0f) yield return new WaitForSecondsRealtime(cardDelaySeconds);
            }
            revealRoutine = null;
            FinishReveal();
        }

        private IEnumerator RevealPromotion(RewardCard card, EscortShipData promoted, bool second)
        {
            card.Root.AddToClassList("recruitment-card-charging");
            float duration = second ? secondAnticipationSeconds : firstAnticipationSeconds;
            bool succeeded = promoted != null;
            float chargeSeconds = succeeded ? duration : duration * failedPromotionStopProgress;
            float elapsed = 0f;
            while (elapsed < chargeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                ApplyPromotionShake(card, elapsed, Mathf.Lerp(0.3f, 1f, progress), second);
                card.Flash.style.opacity = 0.06f + 0.3f * progress + Mathf.Sin(elapsed * Mathf.PI * 3f) * 0.025f;
                card.Ring.style.opacity = 0.12f + progress * 0.65f;
                yield return null;
            }

            card.Root.RemoveFromClassList("recruitment-card-charging");
            if (!succeeded)
            {
                yield return RevealPromotionFailure(card);
                yield break;
            }
            ShipCardPresentation.Bind(card.Root, promoted, string.Empty);
            elapsed = 0f;
            while (elapsed < promotionBurstSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / promotionBurstSeconds);
                ApplyPromotionShake(card, duration + elapsed, (1f - progress) * 1.6f, second);
                card.Flash.style.opacity = (1f - progress) * (second ? 0.85f : 0.65f);
                card.Ring.style.opacity = 1f - progress;
                resultFlash.style.opacity = (1f - progress) * (second ? 0.16f : 0.08f);
                yield return null;
            }
            ResetCardEffects(card);
            if (promotionHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(promotionHoldSeconds);
        }

        private void ApplyPromotionShake(RewardCard card, float elapsed, float strength, bool second)
        {
            float amplitude = promotionShakePixels * strength * (second ? 1.35f : 1f);
            float phase = elapsed * promotionShakeFrequency * Mathf.PI * 2f;
            card.Root.style.translate = new Translate(Mathf.Sin(phase) * amplitude,
                Mathf.Sin(phase * 0.73f) * amplitude * 0.25f, 0f);
        }

        private IEnumerator RevealPromotionFailure(RewardCard card)
        {
            card.Root.style.translate = StyleKeyword.Null;
            card.Root.AddToClassList("recruitment-card-failed");
            float flashOpacity = card.Flash.style.opacity.value;
            float ringOpacity = card.Ring.style.opacity.value;
            float elapsed = 0f;
            while (elapsed < promotionFailureFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / promotionFailureFadeSeconds);
                card.Flash.style.opacity = flashOpacity * (1f - progress);
                card.Ring.style.opacity = ringOpacity * (1f - progress);
                yield return null;
            }
            card.Flash.style.opacity = 0f;
            card.Ring.style.opacity = 0f;
            if (promotionFailureHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(promotionFailureHoldSeconds);
            ResetCardEffects(card);
        }

        private void SkipReveal()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            FinishReveal();
        }

        private void FinishReveal()
        {
            for (int i = 0; i < rewardCards.Count; i++)
            {
                EscortGachaResult reward = rewards[i];
                ShipCardPresentation.Bind(rewardCards[i].Root, reward.Reward, string.Empty);
            }
            ResetRevealEffects();
            isBusy = false;
            SetResultActions(true);
            Refresh();
        }

        private void ResetRevealEffects()
        {
            foreach (RewardCard card in rewardCards)
            {
                ResetCardEffects(card);
                card.Root.style.opacity = StyleKeyword.Null;
                card.Root.RemoveFromClassList("recruitment-card-muted");
            }
            if (resultFlash != null) resultFlash.style.opacity = 0f;
        }

        private void ResetCardEffects(RewardCard card)
        {
            card.Root.RemoveFromClassList("recruitment-card-charging");
            card.Root.RemoveFromClassList("recruitment-card-failed");
            card.Root.style.translate = StyleKeyword.Null;
            card.Flash.style.opacity = 0f;
            card.Ring.style.opacity = 0f;
            if (resultFlash != null) resultFlash.style.opacity = 0f;
        }

        private void SetResultActions(bool complete)
        {
            root.Q<Button>("skipButton").EnableInClassList("hidden", complete);
            root.Q<Button>("confirmButton").EnableInClassList("hidden", !complete);
            root.Q<Button>("repeatButton").EnableInClassList("hidden", !complete);
            root.Q<Button>("resultFormationButton").EnableInClassList("hidden", !complete);
        }

        private void Confirm()
        {
            SkipReveal();
            resultPanel.AddToClassList("hidden");
            statusLabel.text = "입항 완료 · 함대에 배치해 전투에 합류시키세요.";
            Refresh();
        }
    }
}

