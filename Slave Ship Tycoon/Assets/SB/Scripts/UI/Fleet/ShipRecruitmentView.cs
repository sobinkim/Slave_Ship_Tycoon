using System.Collections;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using SB.Scripts.Fleet;
using TMPro;
using UnityEngine;

namespace SB.Scripts.UI.Fleet
{
    public sealed class ShipRecruitmentView : MonoBehaviour
    {
        [System.Serializable]
        private sealed class RewardCard
        {
            public RectTransform root;
            public CanvasGroup visibility;
            public UnityEngine.UI.Image icon;
            public UnityEngine.UI.Image surface;
            public TMP_Text title;
            public TMP_Text grade;
            public Vector2 gridPosition;
        }

        [Header("References")]
        [SerializeField] private EscortGachaManager gachaManager;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private GameObject backdrop;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text singleCostText;
        [SerializeField] private TMP_Text multiCostText;
        [SerializeField] private TMP_Text resultCountText;
        [SerializeField] private UnityEngine.UI.Button singleButton;
        [SerializeField] private UnityEngine.UI.Button multiButton;
        [SerializeField] private GameObject skipButton;
        [SerializeField] private GameObject confirmButton;
        [SerializeField] private UnityEngine.UI.Image revealFlash;
        [SerializeField] private RewardCard[] cards;
        [Header("Recruitment")]
        [SerializeField, Min(1)] private int multiCount = 11;
        [SerializeField] private Vector2 singleCardPosition = new Vector2(394, -100);
        [Header("Reveal timing (seconds)")]
        [SerializeField, Min(0.01f)] private float revealSeconds = 0.22f;
        [SerializeField, Min(0f)] private float cardDelaySeconds = 0.06f;
        [SerializeField, Range(0.1f, 1f)] private float initialScale = 0.65f;
        [SerializeField] private Color[] gradeColors = { new Color(.55f,.65f,.72f), new Color(.22f,.78f,.57f), new Color(.25f,.62f,.95f), new Color(.7f,.4f,.95f), new Color(1f,.73f,.3f) };
        private readonly List<EscortShipData> rewards = new List<EscortShipData>();
        private Coroutine revealRoutine;
        private bool isBusy;

        private void OnEnable() => Bus<CurrencyChangedEvent>.OnEvent += OnCurrencyChanged;
        private void OnDisable()
        {
            Bus<CurrencyChangedEvent>.OnEvent -= OnCurrencyChanged;
            SkipReveal();
        }
        private void Start() => Close();
        private void OnCurrencyChanged(CurrencyChangedEvent evt) => Refresh();

        public void Open()
        {
            shopPanel.SetActive(true);
            if (backdrop != null) backdrop.SetActive(true);
            resultPanel.SetActive(false);
            statusText.text = "같은 등급 3척을 합성해 더 높은 등급으로 성장하세요.";
            Refresh();
        }

        public void Close()
        {
            SkipReveal();
            shopPanel.SetActive(false);
            if (backdrop != null) backdrop.SetActive(false);
            resultPanel.SetActive(false);
        }

        public void Confirm()
        {
            SkipReveal();
            resultPanel.SetActive(false);
            Refresh();
        }

        public void SummonSingle() => Recruit(1);
        public void SummonMulti() => Recruit(multiCount);

        private void Refresh()
        {
            if (gachaManager == null || currencyManager == null) return;
            long balance = currencyManager.GetCurrency(gachaManager.CurrencyType);
            string unit = CurrencyTextFormatter.FormatName(gachaManager.CurrencyType);
            balanceText.text = $"보유 {unit}  {CurrencyTextFormatter.Format(balance)}";
            singleCostText.text = $"{gachaManager.SummonCost:N0} {unit}";
            multiCostText.text = $"{(long)gachaManager.SummonCost * multiCount:N0} {unit}";
            bool available = !isBusy && gachaManager.CanSummon(out _);
            singleButton.interactable = available;
            multiButton.interactable = available && balance >= (long)gachaManager.SummonCost * multiCount;
            if (!available && !isBusy)
                statusText.text = balance < gachaManager.SummonCost ? "뽑기 재화가 부족해요. 운송 정산으로 모아보세요." : "모집 설정을 확인해주세요.";
        }

        private void Recruit(int count)
        {
            if (isBusy || gachaManager == null || currencyManager == null) return;
            if (count > cards.Length || currencyManager.GetCurrency(gachaManager.CurrencyType) < (long)gachaManager.SummonCost * count)
            {
                statusText.text = "모집에 필요한 재화가 부족해요.";
                return;
            }
            isBusy = true;
            rewards.Clear();
            for (int i = 0; i < count; i++)
            {
                if (!gachaManager.TrySummon(out EscortGachaResult reward)) break;
                rewards.Add(reward.Reward);
            }
            if (rewards.Count == 0) { isBusy = false; Refresh(); return; }
            resultCountText.text = $"새로운 호위선 {rewards.Count}척 · 보유 함대에 추가되었습니다";
            for (int i = 0; i < cards.Length; i++)
            {
                RewardCard card = cards[i];
                card.root.gameObject.SetActive(i < rewards.Count);
                if (i >= rewards.Count) continue;
                EscortShipData reward = rewards[i];
                card.root.anchoredPosition = rewards.Count == 1 ? singleCardPosition : card.gridPosition;
                card.icon.sprite = reward.Icon;
                card.icon.enabled = reward.Icon != null;
                card.title.text = reward.DisplayName;
                card.grade.text = GradeName(reward.Grade);
                card.grade.color = gradeColors[(int)reward.Grade];
                card.surface.color = Color.Lerp(new Color(.045f,.08f,.13f), card.grade.color, .24f);
                card.root.GetComponent<UnityEngine.UI.Image>().color = card.grade.color;
                card.visibility.alpha = 0;
                card.root.localScale = Vector3.one * initialScale;
            }
            resultPanel.SetActive(true);
            skipButton.SetActive(true);
            confirmButton.SetActive(false);
            Refresh();
            revealRoutine = StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                float elapsed = 0;
                while (elapsed < revealSeconds)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / revealSeconds);
                    float eased = 1 - Mathf.Pow(1-progress, 3);
                    cards[i].visibility.alpha = eased;
                    cards[i].root.localScale = Vector3.one * Mathf.Lerp(initialScale, 1, eased);
                    Color glow = gradeColors[(int)rewards[i].Grade];
                    glow.a = (1-progress) * .18f;
                    revealFlash.color = glow;
                    yield return null;
                }
                cards[i].visibility.alpha = 1;
                cards[i].root.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(cardDelaySeconds);
            }
            revealRoutine = null;
            FinishReveal();
        }

        public void SkipReveal()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            FinishReveal();
        }

        private void FinishReveal()
        {
            if (cards != null) foreach (RewardCard card in cards)
            {
                if (card.visibility != null) card.visibility.alpha = 1;
                if (card.root != null) card.root.localScale = Vector3.one;
            }
            if (revealFlash != null) revealFlash.color = Color.clear;
            if (skipButton != null) skipButton.SetActive(false);
            if (confirmButton != null) confirmButton.SetActive(true);
            isBusy = false;
            Refresh();
        }

        private static string GradeName(EscortShipGrade grade)
        {
            switch (grade)
            {
                case EscortShipGrade.Common: return "일반";
                case EscortShipGrade.Uncommon: return "고급";
                case EscortShipGrade.Rare: return "희귀";
                case EscortShipGrade.Epic: return "영웅";
                default: return "전설";
            }
        }
    }
}
