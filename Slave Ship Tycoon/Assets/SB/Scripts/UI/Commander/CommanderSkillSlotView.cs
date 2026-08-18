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
            bool canUse)
        {
            bool hasSkill = skillData != null && runtimeState != null;

            if (_skillIcon != null)
            {
                _skillIcon.sprite = hasSkill ? skillData.Icon : null;
                _skillIcon.enabled = hasSkill;
            }

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
