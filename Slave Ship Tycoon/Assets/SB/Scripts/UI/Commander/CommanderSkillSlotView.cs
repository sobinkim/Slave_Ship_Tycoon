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
                _detailText.text = hasSkill ? $"{skillData.DisplayName}\n{skillData.GaugeCost:0.#} 지휘력" : string.Empty;

            if (_emptyEffect != null)
                _emptyEffect.SetActive(hasSkill == false);

            if (_skillButton != null)
                _skillButton.interactable = hasSkill && canUse;

            if (hasSkill == false)
            {
                SetCooldown(0f, 0f);

                if (_activeEffect != null)
                    _activeEffect.SetActive(false);

                return;
            }

            SetCooldown(runtimeState.RemainingCooldown, skillData.Cooldown);

            if (_cooldownText != null)
            {
                string status = runtimeState.IsActive
                    ? $"발동\n{Mathf.CeilToInt(runtimeState.RemainingDuration)}초"
                    : runtimeState.RemainingCooldown > 0f
                        ? $"{Mathf.CeilToInt(runtimeState.RemainingCooldown)}초"
                        : isBattleRunning == false ? "대기"
                        : currentGauge < skillData.GaugeCost ? "지휘력 부족"
                        : canUse ? string.Empty : "대기";
                _cooldownText.text = status;
                _cooldownText.gameObject.SetActive(string.IsNullOrEmpty(status) == false);
            }

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
