namespace SB.Scripts.Commander
{
    public sealed class CommanderSkillRuntimeState
    {
        public CommanderSkillData SkillData { get; }
        public float RemainingCooldown { get; private set; }
        public float RemainingDuration { get; private set; }
        public bool IsActive => RemainingDuration > 0f;

        public CommanderSkillRuntimeState(CommanderSkillData skillData)
        {
            SkillData = skillData;
        }

        public void StartSkill()
        {
            RemainingCooldown = SkillData.Cooldown;
            RemainingDuration = SkillData.Duration;
        }

        public void Tick(float deltaTime)
        {
            if (RemainingCooldown > 0f)
                RemainingCooldown = UnityEngine.Mathf.Max(0f, RemainingCooldown - deltaTime);

            if (RemainingDuration > 0f)
                RemainingDuration = UnityEngine.Mathf.Max(0f, RemainingDuration - deltaTime);
        }

        public void StopSkill()
        {
            RemainingDuration = 0f;
        }

        public void SetRemainingCooldown(float remainingCooldown)
        {
            RemainingCooldown = UnityEngine.Mathf.Clamp(
                remainingCooldown,
                0f,
                SkillData.Cooldown);
        }
    }
}
