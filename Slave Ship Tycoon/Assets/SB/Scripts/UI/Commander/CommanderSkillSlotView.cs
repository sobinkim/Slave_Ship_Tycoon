using System;
using SB.Scripts.Commander;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public sealed class CommanderSkillSlotView : MonoBehaviour
    {
        [SerializeField] private Button _skillButton;
        [SerializeField] private Image _skillIcon;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TMP_Text _cooldownText;
        [SerializeField] private TMP_Text _detailText;
        [SerializeField] private GameObject _activeEffect;
        [SerializeField] private GameObject _emptyEffect;
        [Header("Icon charge presentation")]
        [SerializeField] private Image chargeFill;
        [SerializeField] private Image stateBorder;
        [SerializeField] private Color chargingColor = new Color(.22f,.57f,.76f,.6f);
        [SerializeField] private Color readyColor = new Color(.25f,.85f,.66f,.65f);
        [SerializeField] private Color activeColor = new Color(1f,.72f,.3f,.7f);
        [SerializeField] private Color idleColor = new Color(.3f,.37f,.42f,.6f);

        private int _slotIndex;

        public event Action<int> OnSkillButtonClicked;

        private void Awake()
        {
            if (_skillButton != null)
                _skillButton.onClick.AddListener(HandleSkillButtonClicked);
        }

        private void OnDestroy()
        {
            if (_skillButton != null)
                _skillButton.onClick.RemoveListener(HandleSkillButtonClicked);
        }

        public void Initialize(int slotIndex)
        {
            _slotIndex = slotIndex;
        }

        public void Refresh(
            CommanderSkillData skillData,
            CommanderSkillRuntimeState runtimeState,
            bool canUse,
            float currentGauge = float.MaxValue,
            bool isBattleRunning = true)
        {
            bool hasSkill = skillData != null && runtimeState != null;

            if (_skillIcon != null)
            {
                _skillIcon.sprite = hasSkill ? skillData.Icon : null;
                _skillIcon.enabled = hasSkill && skillData.Icon != null;
            }

            if (_detailText != null)
                _detailText.text = hasSkill ? skillData.DisplayName : "빈 슬롯";

            if (_emptyEffect != null)
                _emptyEffect.SetActive(hasSkill == false);

            if (_skillButton != null)
                _skillButton.interactable = hasSkill && canUse;

            if (hasSkill == false)
            {
                SetCooldown(0f, 0f);
                if (chargeFill != null) chargeFill.fillAmount = 0f;
                if (stateBorder != null) stateBorder.color = idleColor;

                if (_activeEffect != null)
                    _activeEffect.SetActive(false);

                return;
            }

            SetCooldown(runtimeState.RemainingCooldown, skillData.Cooldown);

            if (_cooldownText != null)
            {
                string status = runtimeState.IsActive
                    ? $"{runtimeState.RemainingDuration:0.0}"
                    : runtimeState.RemainingCooldown > 0f
                        ? $"{runtimeState.RemainingCooldown:0.0}"
                        : string.Empty;
                _cooldownText.text = status;
                _cooldownText.gameObject.SetActive(string.IsNullOrEmpty(status) == false);
            }

            float gaugeProgress = skillData.GaugeCost > 0f ? Mathf.Clamp01(currentGauge / skillData.GaugeCost) : 1f;
            float cooldownProgress = skillData.Cooldown > 0f ? 1f - Mathf.Clamp01(runtimeState.RemainingCooldown / skillData.Cooldown) : 1f;
            bool isCharged = gaugeProgress >= 1f && cooldownProgress >= 1f;
            bool showReady = canUse || (!isBattleRunning && isCharged);
            Color stateColor = runtimeState.IsActive ? activeColor : showReady ? readyColor : chargingColor;
            if (chargeFill != null)
            {
                chargeFill.color = stateColor;
                chargeFill.fillAmount = runtimeState.IsActive
                    ? Mathf.Clamp01(runtimeState.RemainingDuration / skillData.Duration)
                    : Mathf.Min(gaugeProgress, cooldownProgress);
            }
            if (stateBorder != null) stateBorder.color = stateColor;
            if (_skillIcon != null) _skillIcon.color = showReady || runtimeState.IsActive ? Color.white : new Color(.45f,.5f,.55f);

            if (_activeEffect != null)
                _activeEffect.SetActive(runtimeState.IsActive);
        }

        private void SetCooldown(float remainingCooldown, float cooldown)
        {
            if (_cooldownFill != null)
            {
                _cooldownFill.fillAmount = cooldown > 0f
                    ? Mathf.Clamp01(remainingCooldown / cooldown)
                    : 0f;
                _cooldownFill.gameObject.SetActive(remainingCooldown > 0f);
            }

            if (_cooldownText != null)
            {
                _cooldownText.text = remainingCooldown > 0f
                    ? Mathf.CeilToInt(remainingCooldown).ToString()
                    : string.Empty;
                _cooldownText.gameObject.SetActive(remainingCooldown > 0f);
            }
        }

        private void HandleSkillButtonClicked()
        {
            OnSkillButtonClicked?.Invoke(_slotIndex);
        }
    }
}
