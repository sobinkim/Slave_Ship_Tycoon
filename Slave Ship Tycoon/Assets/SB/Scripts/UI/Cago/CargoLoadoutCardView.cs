using SB.Scripts.Currency;
using SB.SO.TransportItemSOs;
using TMPro;
using UnityEngine;

namespace SB.Scripts
{
    public enum CargoLoadoutCardMode { SelectedCargo, AvailableCargo }

    public sealed class CargoLoadoutCardView : MonoBehaviour
    {
        [Header("Cargo")]
        [SerializeField] private CargoManager cargoManager;
        [SerializeField] private TransportItemSO cargoItem;
        [SerializeField] private CargoLoadoutCardMode displayMode;

        [Header("Contents")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private GameObject emptyRoot;
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text diamondText;
        [SerializeField] private TMP_Text emeraldText;

        [Header("Appearance")]
        [SerializeField] private UnityEngine.UI.Image borderImage;
        [SerializeField] private UnityEngine.UI.Image loadButtonSurface;
        [SerializeField] private UnityEngine.UI.Outline loadButtonOutline;
        [SerializeField, Tooltip("적재 버튼의 기본 배경색입니다.")]
        private Color loadButtonBaseColor = new Color(0.03f, 0.12f, 0.15f, 1f);
        [SerializeField, Range(0f, 1f), Tooltip("화물의 강조 색상을 적재 버튼에 섞는 비율입니다.")]
        private float loadButtonAccentStrength = 0.25f;

        [Header("Controls")]
        [SerializeField] private UnityEngine.UI.Button loadButton;
        [SerializeField] private UnityEngine.UI.Button emptyLoadButton;
        [SerializeField] private UnityEngine.UI.Button unloadButton;
        [SerializeField] private UnityEngine.UI.Button removeButton;
        [SerializeField, Tooltip("적재 버튼의 길게 누르기 반복 입력입니다. 같은 버튼에 있는 컴포넌트를 연결하세요.")]
        private HoldRepeatButton loadButtonRepeat;

        public UnityEngine.UI.Button LoadButton => loadButton;

        public void Configure(CargoManager manager, TransportItemSO item, CargoLoadoutCardMode mode)
        {
            cargoManager = manager;
            cargoItem = item;
            displayMode = mode;
            Refresh();
        }

        public void Refresh()
        {
            if (cargoManager == null || cargoItem == null)
                return;

            int amount = cargoManager.GetCargoAmount(cargoItem.Type);
            bool isSelectedCard = displayMode == CargoLoadoutCardMode.SelectedCargo;
            if (contentRoot != null) contentRoot.SetActive(!isSelectedCard || amount != 0);
            if (emptyRoot != null) emptyRoot.SetActive(isSelectedCard && amount == 0);
            if (icon != null) icon.sprite = cargoItem.Icon;
            if (nameText != null) nameText.text = cargoItem.DisplayName;
            if (borderImage != null) borderImage.color = cargoItem.AccentColor;
            if (loadButtonSurface != null)
                loadButtonSurface.color = Color.Lerp(loadButtonBaseColor, cargoItem.AccentColor, loadButtonAccentStrength);
            if (loadButtonOutline != null) loadButtonOutline.effectColor = cargoItem.AccentColor;
            if (amountText != null)
                amountText.text = isSelectedCard ? $"x{amount:N0}" : "보유 ∞";
            if (weightText != null)
                weightText.text = isSelectedCard
                    ? $"{amount * cargoItem.BaseWeight:0.#} t"
                    : $"{cargoItem.BaseWeight:0.#} t";

            int rewardUnits = isSelectedCard ? amount : 1;
            SetReward(goldText, CurrencyType.Gold, rewardUnits);
            SetReward(diamondText, CurrencyType.Diamond, rewardUnits);
            SetReward(emeraldText, CurrencyType.Emerald, rewardUnits);
            if (loadButton != null) loadButton.interactable = cargoManager.CanLoadCargo(cargoItem.Type);
            if (emptyLoadButton != null) emptyLoadButton.interactable = cargoManager.CanLoadCargo(cargoItem.Type);
            if (unloadButton != null) unloadButton.interactable = amount > 0;
            if (removeButton != null) removeButton.interactable = amount != 0;
        }

        public void AddCargo()
        {
            LoadCargoOnce();
        }

        public bool LoadCargoOnce()
        {
            return cargoManager != null && cargoItem != null && cargoManager.LoadCargo(cargoItem.Type);
        }

        public void SubtractCargo()
        {
            if (cargoManager != null && cargoItem != null)
                cargoManager.UnloadCargo(cargoItem.Type);
        }

        public void RemoveCargo()
        {
            if (cargoManager != null && cargoItem != null)
                cargoManager.RemoveCargo(cargoItem.Type);
        }

        private void SetReward(TMP_Text target, CurrencyType type, int units)
        {
            if (target == null) return;
            long unitReward = 0;
            if (cargoItem.Rewards != null)
                foreach (CargoSalesRewardData reward in cargoItem.Rewards)
                    if (reward.CurrencyType == type && reward.CurrencyAmount > 0)
                        unitReward += reward.CurrencyAmount;
            target.text = (unitReward * units).ToString("N0");
        }

        private void Awake()
        {
            if (cargoManager == null || cargoItem == null)
                Debug.LogError("Cargo loadout card requires an explicit CargoManager and cargo item.", this);
        }

        private void OnEnable()
        {
            if (loadButtonRepeat != null)
                loadButtonRepeat.OnTriggered += LoadCargoOnce;
        }

        private void OnDisable()
        {
            if (loadButtonRepeat != null)
                loadButtonRepeat.OnTriggered -= LoadCargoOnce;
        }
    }
}
