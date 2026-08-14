using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Commander
{
    [CreateAssetMenu(fileName = "CommanderSkillLoadout", menuName = "SB/Commander/Skill Loadout")]
    public sealed class CommanderSkillLoadout : ScriptableObject
    {
        [SerializeField] private CommanderSkillData[] _equippedSkills = new CommanderSkillData[5];

        public IReadOnlyList<CommanderSkillData> EquippedSkills =>
            _equippedSkills ?? Array.Empty<CommanderSkillData>();

        private void OnValidate()
        {
            _equippedSkills ??= new CommanderSkillData[5];

            for (int i = 0; i < _equippedSkills.Length; i++)
            {
                CommanderSkillData skillData = _equippedSkills[i];

                if (skillData == null)
                    continue;

                for (int j = i + 1; j < _equippedSkills.Length; j++)
                {
                    if (_equippedSkills[j] != null &&
                        _equippedSkills[j].SkillType == skillData.SkillType)
                    {
                        Debug.LogWarning($"Duplicate equipped commander skill: {skillData.SkillType}", this);
                        break;
                    }
                }
            }
        }
    }
}
