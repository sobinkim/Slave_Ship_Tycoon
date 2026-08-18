using UnityEngine;

namespace SB.Scripts.Commander
{
    [CreateAssetMenu(fileName = "FocusFireSkill", menuName = "SB/Commander/Skill/Focus Fire")]
    public sealed class FocusFireSkillData : CommanderSkillData
    {
        public override CommanderSkillType SkillType => CommanderSkillType.FocusFire;
        public override CommanderSkillCategory Category => CommanderSkillCategory.Attack;
    }
}
