using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Commander
{
    [CreateAssetMenu(fileName = "CommanderSkillDatabase", menuName = "SB/Commander/Skill Database")]
    public sealed class CommanderSkillDatabase : ScriptableObject
    {
        [SerializeField] private CommanderSkillData[] _skills = Array.Empty<CommanderSkillData>();

        public IReadOnlyList<CommanderSkillData> Skills => _skills ?? Array.Empty<CommanderSkillData>();

        public bool TryGetSkill(CommanderSkillType skillType, out CommanderSkillData skillData)
        {
            if (_skills != null)
            {
                for (int i = 0; i < _skills.Length; i++)
                {
                    CommanderSkillData candidate = _skills[i];

                    if (candidate != null && candidate.SkillType == skillType)
                    {
                        skillData = candidate;
                        return true;
                    }
                }
            }

            skillData = null;
            return false;
        }

        public bool TryGetSkill(string skillId, out CommanderSkillData skillData)
        {
            if (_skills != null && string.IsNullOrWhiteSpace(skillId) == false)
            {
                for (int i = 0; i < _skills.Length; i++)
                {
                    CommanderSkillData candidate = _skills[i];

                    if (candidate != null && candidate.SkillId == skillId)
                    {
                        skillData = candidate;
                        return true;
                    }
                }
            }

            skillData = null;
            return false;
        }

        private void OnValidate()
        {
            _skills ??= Array.Empty<CommanderSkillData>();

            HashSet<CommanderSkillType> skillTypes = new HashSet<CommanderSkillType>();
            HashSet<string> skillIds = new HashSet<string>();

            for (int i = 0; i < _skills.Length; i++)
            {
                CommanderSkillData skillData = _skills[i];

                if (skillData == null)
                    continue;

                if (skillTypes.Add(skillData.SkillType) == false)
                    Debug.LogWarning($"Duplicate commander skill type: {skillData.SkillType}", this);

                if (skillIds.Add(skillData.SkillId) == false)
                    Debug.LogWarning($"Duplicate commander skill id: {skillData.SkillId}", this);
            }
        }
    }
}
