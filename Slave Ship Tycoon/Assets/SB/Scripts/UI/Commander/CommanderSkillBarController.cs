using SB.Core.EventBus;
using SB.Scripts.Commander;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class CommanderSkillBarController : MonoBehaviour
    {
        [SerializeField] private CommanderSkillManager _commanderSkillManager;
        [SerializeField] private CommanderSkillBarView _commanderSkillBarView;

        private void OnEnable()
        {
            Bus<CommanderSkillStateChangedEvent>.OnEvent += HandleSkillStateChanged;
            Bus<CommanderSkillLoadoutChangedEvent>.OnEvent += HandleSkillLoadoutChanged;

            if (_commanderSkillBarView != null)
            {
                _commanderSkillBarView.OnSkillButtonClicked += HandleSkillButtonClicked;
                _commanderSkillBarView.OnAutoUseButtonClicked += HandleAutoUseButtonClicked;
            }
        }

        private void Start()
        {
            RefreshAllSlots();
            RefreshAutoUse();
        }

        private void OnDisable()
        {
            Bus<CommanderSkillStateChangedEvent>.OnEvent -= HandleSkillStateChanged;
            Bus<CommanderSkillLoadoutChangedEvent>.OnEvent -= HandleSkillLoadoutChanged;

            if (_commanderSkillBarView != null)
            {
                _commanderSkillBarView.OnSkillButtonClicked -= HandleSkillButtonClicked;
                _commanderSkillBarView.OnAutoUseButtonClicked -= HandleAutoUseButtonClicked;
            }
        }

        private void Update()
        {
            RefreshAllSlots();
        }

        private void HandleSkillButtonClicked(int slotIndex)
        {
            if (_commanderSkillManager == null)
                return;

            CommanderSkillData skillData = _commanderSkillManager.GetEquippedSkill(slotIndex);

            if (skillData != null)
                _commanderSkillManager.TryUseSkill(skillData.SkillType);
        }

        private void HandleAutoUseButtonClicked()
        {
            if (_commanderSkillManager == null)
                return;

            _commanderSkillManager.SetAutoUseEnabled(
                _commanderSkillManager.IsAutoUseEnabled == false);
            RefreshAutoUse();
        }

        private void HandleSkillStateChanged(CommanderSkillStateChangedEvent evt)
        {
            RefreshSkillSlot(evt.SkillType);
        }

        private void HandleSkillLoadoutChanged(CommanderSkillLoadoutChangedEvent evt)
        {
            RefreshAllSlots();
        }

        private void RefreshAllSlots()
        {
            if (_commanderSkillManager == null || _commanderSkillBarView == null)
                return;

            int slotCount = Mathf.Min(
                _commanderSkillManager.EquippedSlotCount,
                _commanderSkillBarView.SlotCount);

            for (int i = 0; i < _commanderSkillBarView.SlotCount; i++)
            {
                CommanderSkillSlotView slotView = _commanderSkillBarView.GetSlot(i);

                if (slotView == null)
                    continue;

                if (i >= slotCount)
                {
                    slotView.Refresh(null, null, false);
                    continue;
                }

                RefreshSkillSlot(i);
            }
        }

        private void RefreshSkillSlot(int slotIndex)
        {
            CommanderSkillData skillData = _commanderSkillManager.GetEquippedSkill(slotIndex);
            CommanderSkillRuntimeState runtimeState = null;

            if (skillData != null)
                _commanderSkillManager.TryGetRuntimeState(skillData.SkillType, out runtimeState);

            _commanderSkillBarView.GetSlot(slotIndex)?.Refresh(
                skillData,
                runtimeState,
                skillData != null && _commanderSkillManager.CanUseSkill(skillData.SkillType));
        }

        private void RefreshSkillSlot(CommanderSkillType skillType)
        {
            for (int i = 0; i < _commanderSkillManager.EquippedSlotCount; i++)
            {
                CommanderSkillData skillData = _commanderSkillManager.GetEquippedSkill(i);

                if (skillData != null && skillData.SkillType == skillType)
                {
                    RefreshSkillSlot(i);
                    return;
                }
            }
        }

        private void RefreshAutoUse()
        {
            if (_commanderSkillManager != null && _commanderSkillBarView != null)
            {
                _commanderSkillBarView.SetAutoUseEnabled(
                    _commanderSkillManager.IsAutoUseEnabled);
            }
        }
    }
}
