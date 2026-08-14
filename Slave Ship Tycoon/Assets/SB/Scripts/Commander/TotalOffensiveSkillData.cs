using UnityEngine;

namespace SB.Scripts.Commander
{
    [CreateAssetMenu(fileName = "TotalOffensiveSkill", menuName = "SB/Commander/Skill/Total Offensive")]
    public sealed class TotalOffensiveSkillData : CommanderSkillData
    {
        [SerializeField, Min(0f)] private float _attackSpeedIncreasePercent;

        public override CommanderSkillType SkillType => CommanderSkillType.TotalOffensive;
        public override CommanderSkillCategory Category => CommanderSkillCategory.Support;
        public float AttackSpeedIncreasePercent => _attackSpeedIncreasePercent;

        protected override void OnValidate()
        {
            base.OnValidate();
            _attackSpeedIncreasePercent = Mathf.Max(0f, _attackSpeedIncreasePercent);
        }
    }
}
