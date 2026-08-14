using System;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public sealed class CommanderSkillBarView : MonoBehaviour
    {
        [SerializeField] private CommanderSkillSlotView[] _skillSlots =
            Array.Empty<CommanderSkillSlotView>();
        [SerializeField] private Button _autoUseButton;
        [SerializeField] private GameObject _autoUseEnabledEffect;

        public event Action<int> OnSkillButtonClicked;
        public event Action OnAutoUseButtonClicked;

        public int SlotCount => _skillSlots?.Length ?? 0;

        private void Awake()
        {
            if (_autoUseButton != null)
                _autoUseButton.onClick.AddListener(HandleAutoUseButtonClicked);

            if (_skillSlots == null)
                return;

            for (int i = 0; i < _skillSlots.Length; i++)
            {
                CommanderSkillSlotView skillSlot = _skillSlots[i];

                if (skillSlot == null)
                    continue;

                skillSlot.Initialize(i);
                skillSlot.OnSkillButtonClicked += HandleSkillButtonClicked;
            }
        }

        private void OnDestroy()
        {
            if (_autoUseButton != null)
                _autoUseButton.onClick.RemoveListener(HandleAutoUseButtonClicked);

            if (_skillSlots == null)
                return;

            for (int i = 0; i < _skillSlots.Length; i++)
            {
                if (_skillSlots[i] != null)
                    _skillSlots[i].OnSkillButtonClicked -= HandleSkillButtonClicked;
            }
        }

        public CommanderSkillSlotView GetSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < SlotCount ? _skillSlots[slotIndex] : null;
        }

        public void SetAutoUseEnabled(bool isEnabled)
        {
            if (_autoUseEnabledEffect != null)
                _autoUseEnabledEffect.SetActive(isEnabled);
        }

        private void HandleSkillButtonClicked(int slotIndex)
        {
            OnSkillButtonClicked?.Invoke(slotIndex);
        }

        private void HandleAutoUseButtonClicked()
        {
            OnAutoUseButtonClicked?.Invoke();
        }
    }
}
