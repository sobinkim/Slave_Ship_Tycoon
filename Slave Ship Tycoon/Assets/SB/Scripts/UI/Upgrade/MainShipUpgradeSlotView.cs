using System;
using SB.Scripts.Currency;
using SB.Scripts.Upgrade;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SB.Scripts.UI.Upgrade
{
    public class MainShipUpgradeSlotView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private MainShipUpgradeType _upgradeType;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _cost;
        [SerializeField] private TMP_Text _level;
        [SerializeField] private ShipUpgradeManager upgradeManager;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private UnityEngine.UI.Image feedbackSurface;
        [SerializeField] private Color successColor = new Color(0.13f, 0.48f, 0.43f);
        [SerializeField] private Color failureColor = new Color(0.55f, 0.2f, 0.16f);
        [SerializeField, Min(0.05f)] private float feedbackSeconds = 0.25f;
        private Coroutine feedbackRoutine;
        private Color surfaceColor;
        private bool hasSurfaceColor;
        [SerializeField] private float _repeatStartDelay = 0.35f;
        [SerializeField] private float _repeatInterval = 0.1f;

        public event Func<MainShipUpgradeType, bool> OnUpgradeButtonClicked;

        private Coroutine _repeatUpgradeCoroutine;
        private bool isPointerHeld;
        private bool hasRepeatedUpgrade;

        private void OnDisable()
        {
            StopRepeatUpgrade();
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
            if (feedbackSurface != null && hasSurfaceColor) feedbackSurface.color = surfaceColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            StopRepeatUpgrade();
            if (eventData.button != PointerEventData.InputButton.Left) return;
            isPointerHeld = true;
            hasRepeatedUpgrade = false;
            _repeatUpgradeCoroutine = StartCoroutine(RepeatUpgrade());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            bool shouldUpgrade = isPointerHeld && !hasRepeatedUpgrade && !eventData.dragging;
            StopRepeatUpgrade();
            if (shouldUpgrade) TryUpgrade();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopRepeatUpgrade();
        }

        public void Refresh(int level, long cost)
        {
            _level.text = $"Lv. {level}";
            UpgradeGrowthData growth = upgradeManager != null ? upgradeManager.GetTargetUpdateData(_upgradeType) : null;
            _cost.text = growth == null ? CurrencyTextFormatter.Format(cost) : $"{CurrencyTextFormatter.Format(cost)} {CurrencyTextFormatter.FormatName(growth.currencyType)}";
            if (effectText == null || growth == null) return;
            MainShipUpgradeData data = upgradeManager.MainShipUpgradeData;
            float current = 0f;
            string unit = "";
            switch (_upgradeType)
            {
                case MainShipUpgradeType.Health: current = data.health; break;
                case MainShipUpgradeType.AttackPowerPercent: current = data.attackPowerPercent; unit = "%"; break;
                case MainShipUpgradeType.AttackSpeedPercent: current = data.attackSpeedPercent; unit = "%"; break;
                case MainShipUpgradeType.CargoCapacity: current = data.cargoCapacity; break;
                case MainShipUpgradeType.Luck: current = data.luck; unit = "%"; break;
                case MainShipUpgradeType.CommanderGaugeMax: current = data.commanderGaugeMax; break;
                case MainShipUpgradeType.CommanderGaugeRecoveryPerSecond: current = data.commanderGaugeRecoveryPerSecond; unit = "/초"; break;
                case MainShipUpgradeType.HealthRecoveryPerSecond: current = data.healthRecoveryPerSecond; unit = "/초"; break;
            }
            string prefix = _upgradeType == MainShipUpgradeType.AttackPowerPercent ||
                            _upgradeType == MainShipUpgradeType.AttackSpeedPercent ||
                            _upgradeType == MainShipUpgradeType.HealthRecoveryPerSecond
                ? "추가 "
                : "";
            effectText.text = $"{prefix}{current:0.##}{unit} → {current + growth.IncreaseValue:0.##}{unit}";
        }

        private System.Collections.IEnumerator RepeatUpgrade()
        {
            yield return new WaitForSecondsRealtime(_repeatStartDelay);

            while (true)
            {
                hasRepeatedUpgrade = true;
                if (!TryUpgrade())
                {
                    _repeatUpgradeCoroutine = null;
                    yield break;
                }

                yield return new WaitForSecondsRealtime(_repeatInterval);
            }
        }

        private bool TryUpgrade()
        {
            bool success = OnUpgradeButtonClicked?.Invoke(_upgradeType) ?? false;
            if (feedbackSurface != null)
            {
                if (!hasSurfaceColor) { surfaceColor = feedbackSurface.color; hasSurfaceColor = true; }
                if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
                feedbackRoutine = StartCoroutine(Flash(success ? successColor : failureColor));
            }
            if (!success && effectText != null) effectText.text = "재화 부족 · 운송과 전투로 획득";
            return success;
        }

        private System.Collections.IEnumerator Flash(Color color)
        {
            float elapsed = 0f;
            while (elapsed < feedbackSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                feedbackSurface.color = Color.Lerp(color, surfaceColor, Mathf.Clamp01(elapsed / feedbackSeconds));
                yield return null;
            }
            feedbackSurface.color = surfaceColor;
            feedbackRoutine = null;
            var growth = upgradeManager != null ? upgradeManager.GetTargetUpdateData(_upgradeType) : null;
            if (growth != null) Refresh(growth.level, growth.CurrentCost);
        }

        private void StopRepeatUpgrade()
        {
            isPointerHeld = false;
            if (_repeatUpgradeCoroutine == null)
                return;

            StopCoroutine(_repeatUpgradeCoroutine);
            _repeatUpgradeCoroutine = null;
        }
    }
}
