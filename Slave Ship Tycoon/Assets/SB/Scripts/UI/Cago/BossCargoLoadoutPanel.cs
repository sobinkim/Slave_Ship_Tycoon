using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using SB.Scripts.TransportEquipment;
using SB.SO.TransportItemSOs;
using TMPro;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class BossCargoLoadoutPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CargoManager cargoManager;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private TransportEquipmentManager transportEquipmentManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject cargoPickerRoot;

        [Header("Loadout display")]
        [SerializeField] private CargoLoadoutCardView selectedCardPrefab;
        [SerializeField] private CargoLoadoutCardView availableCardPrefab;
        [SerializeField] private UnityEngine.UI.Button emptySlotPrefab;
        [SerializeField] private RectTransform selectedContent;
        [SerializeField] private RectTransform availableContent;
        [SerializeField] private RectTransform pickerContent;
        [SerializeField, Min(0), Tooltip("선택한 화물 카드와 빈 슬롯을 합친 최소 슬롯 수입니다.")]
        private int minimumSelectedSlots = 12;
        [SerializeField] private TMP_Text catalogHintText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private UnityEngine.UI.Image capacityFill;
        [SerializeField] private TMP_Text goldRewardText;
        [SerializeField] private TMP_Text diamondRewardText;
        [SerializeField] private TMP_Text emeraldRewardText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private UnityEngine.UI.Button confirmButton;

        [Header("Scroll areas")]
        [SerializeField] private UnityEngine.UI.ScrollRect selectedScroll;
        [SerializeField] private UnityEngine.UI.ScrollRect availableScroll;
        [SerializeField] private UnityEngine.UI.ScrollRect pickerScroll;

        private readonly Dictionary<CurrencyType, long> previewRewards = new Dictionary<CurrencyType, long>();
        private readonly List<CargoLoadoutCardView> selectedCards = new List<CargoLoadoutCardView>();
        private readonly List<CargoLoadoutCardView> availableCards = new List<CargoLoadoutCardView>();
        private readonly List<CargoLoadoutCardView> pickerCards = new List<CargoLoadoutCardView>();
        private readonly List<UnityEngine.UI.Button> emptySlots = new List<UnityEngine.UI.Button>();
        private readonly List<TransportItemSO> selectedItems = new List<TransportItemSO>();
        private readonly List<TransportItemSO> nextSelectedItems = new List<TransportItemSO>();
        private float previousTimeScale;
        private bool hasPaused;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (cargoManager == null || gameFlowManager == null || panelRoot == null ||
                selectedCardPrefab == null || availableCardPrefab == null || emptySlotPrefab == null ||
                selectedContent == null || availableContent == null || pickerContent == null)
            {
                Debug.LogError("Boss cargo preparation requires explicit managers, panel, card prefabs and content references.", this);
                enabled = false;
                return;
            }

            BuildCatalogCards(availableContent, availableCards, closePickerAfterLoad: false);
            BuildCatalogCards(pickerContent, pickerCards, closePickerAfterLoad: true);
            UpdateSelectedCards();
        }

        private void OnEnable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += HandleLoadoutChanged;
        }

        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= HandleLoadoutChanged;
            RestoreBattleTime();
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (panelRoot == null || cargoManager == null || gameFlowManager == null ||
                !gameFlowManager.CanStartBossBattle || IsOpen)
                return;

            previousTimeScale = Time.timeScale;
            hasPaused = Application.isPlaying;
            if (hasPaused) Time.timeScale = 0f;
            panelRoot.SetActive(true);
            CloseCargoPicker();
            Refresh();
            Canvas.ForceUpdateCanvases();
            ResetScroll(selectedScroll);
            ResetScroll(availableScroll);
        }

        public void Close()
        {
            CloseCargoPicker();
            if (panelRoot != null) panelRoot.SetActive(false);
            RestoreBattleTime();
        }

        public void ConfirmBossChallenge()
        {
            if (cargoManager == null || gameFlowManager == null ||
                !cargoManager.HasValidLoadout || !gameFlowManager.CanStartBossBattle)
                return;

            Close();
            gameFlowManager.StartBossBattle();
        }

        public void ShowCargoPicker()
        {
            if (cargoPickerRoot != null) cargoPickerRoot.SetActive(true);
            Refresh();
            Canvas.ForceUpdateCanvases();
            ResetScroll(pickerScroll);
        }

        public void CloseCargoPicker()
        {
            if (cargoPickerRoot != null) cargoPickerRoot.SetActive(false);
        }

        private void HandleLoadoutChanged(ChangedCurrentCargoCapacityEvent evt)
        {
            if (IsOpen) Refresh();
        }

        public void Refresh()
        {
            if (cargoManager == null) return;
            float loadedWeight = cargoManager.GetCurrentCargoCapacity();
            float capacity = cargoManager.GetStatCargoCapacity();
            if (capacityText != null) capacityText.text = $"{loadedWeight:0.#} / {capacity:0.#} t";
            if (capacityFill != null) capacityFill.fillAmount = capacity > 0f ? Mathf.Clamp01(loadedWeight / capacity) : 0f;
            if (catalogHintText != null) catalogHintText.text = $"적재할 화물을 선택하세요. (최대 {capacity:0.#} t)";
            UpdateSelectedCards();
            RefreshCards(selectedCards);
            RefreshCards(availableCards);
            RefreshCards(pickerCards);

            previewRewards.Clear();
            bool hasCargo = false;
            foreach (CargoData cargo in cargoManager.CargoData)
            {
                if (cargo == null || cargo.Item == null || cargo.Amount <= 0) continue;
                hasCargo = true;
                if (cargo.Item.Rewards == null) continue;
                foreach (CargoSalesRewardData reward in cargo.Item.Rewards)
                {
                    if (reward.CurrencyAmount <= 0) continue;
                    previewRewards.TryGetValue(reward.CurrencyType, out long currentReward);
                    long addedReward = (long)cargo.Amount * reward.CurrencyAmount;
                    previewRewards[reward.CurrencyType] = addedReward > long.MaxValue - currentReward
                        ? long.MaxValue : currentReward + addedReward;
                }
            }
            if (hasCargo && transportEquipmentManager != null)
                transportEquipmentManager.ApplyPreviewModifiers(previewRewards);

            SetReward(goldRewardText, CurrencyType.Gold);
            SetReward(diamondRewardText, CurrencyType.Diamond);
            SetReward(emeraldRewardText, CurrencyType.Emerald);
            if (confirmButton != null)
                confirmButton.interactable = cargoManager.HasValidLoadout && gameFlowManager != null && gameFlowManager.CanStartBossBattle;
            if (statusText != null)
                statusText.text = !cargoManager.IsLoadoutWithinCapacity
                    ? "적재 한도를 초과했습니다. 수량을 줄여주세요."
                    : !cargoManager.HasValidLoadout ? "화물 설정을 확인해주세요. 잘못된 수량은 × 버튼으로 제거할 수 있습니다."
                    : string.Empty;
        }

        private void SetReward(TMP_Text target, CurrencyType currency)
        {
            if (target == null) return;
            previewRewards.TryGetValue(currency, out long reward);
            target.text = reward.ToString("N0");
        }

        private void BuildCatalogCards(RectTransform content, List<CargoLoadoutCardView> cards, bool closePickerAfterLoad)
        {
            // The catalog is explicitly authored on CargoManager; prefab instances let its scroll content grow with it.
            foreach (CargoData cargo in cargoManager.CargoData)
            {
                if (cargo == null || cargo.Item == null) continue;
                CargoLoadoutCardView card = CreateCard(availableCardPrefab, content, cargo.Item, CargoLoadoutCardMode.AvailableCargo);
                if (closePickerAfterLoad && card.LoadButton != null)
                    card.LoadButton.onClick.AddListener(CloseCargoPicker);
                cards.Add(card);
            }
        }

        private void UpdateSelectedCards()
        {
            nextSelectedItems.Clear();
            foreach (CargoData cargo in cargoManager.CargoData)
                if (cargo != null && cargo.Item != null && cargo.Amount != 0)
                    nextSelectedItems.Add(cargo.Item);

            bool hasChanged = selectedItems.Count != nextSelectedItems.Count;
            for (int index = 0; !hasChanged && index < nextSelectedItems.Count; index++)
                hasChanged = selectedItems[index] != nextSelectedItems[index];
            int requiredEmptySlots = Mathf.Max(0, minimumSelectedSlots - nextSelectedItems.Count);
            if (!hasChanged && emptySlots.Count == requiredEmptySlots) return;

            foreach (CargoLoadoutCardView card in selectedCards) RemoveInstance(card.gameObject);
            foreach (UnityEngine.UI.Button emptySlot in emptySlots)
            {
                emptySlot.onClick.RemoveListener(ShowCargoPicker);
                RemoveInstance(emptySlot.gameObject);
            }
            selectedCards.Clear();
            emptySlots.Clear();
            selectedItems.Clear();
            selectedItems.AddRange(nextSelectedItems);

            foreach (TransportItemSO item in selectedItems)
                selectedCards.Add(CreateCard(selectedCardPrefab, selectedContent, item, CargoLoadoutCardMode.SelectedCargo));
            for (int index = 0; index < requiredEmptySlots; index++)
            {
                UnityEngine.UI.Button emptySlot = Instantiate(emptySlotPrefab, selectedContent, false);
                emptySlot.onClick.AddListener(ShowCargoPicker);
                emptySlot.gameObject.SetActive(true);
                emptySlots.Add(emptySlot);
            }
        }

        private CargoLoadoutCardView CreateCard(CargoLoadoutCardView prefab, RectTransform content,
            TransportItemSO item, CargoLoadoutCardMode mode)
        {
            CargoLoadoutCardView card = Instantiate(prefab, content, false);
            card.Configure(cargoManager, item, mode);
            card.gameObject.SetActive(true);
            return card;
        }

        private static void RefreshCards(List<CargoLoadoutCardView> cards)
        {
            foreach (CargoLoadoutCardView card in cards) card.Refresh();
        }

        private static void RemoveInstance(GameObject instance)
        {
            instance.SetActive(false);
            Destroy(instance);
        }

        private static void ResetScroll(UnityEngine.UI.ScrollRect scroll)
        {
            if (scroll == null) return;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        private void RestoreBattleTime()
        {
            if (!hasPaused) return;
            Time.timeScale = previousTimeScale;
            hasPaused = false;
        }
    }
}
