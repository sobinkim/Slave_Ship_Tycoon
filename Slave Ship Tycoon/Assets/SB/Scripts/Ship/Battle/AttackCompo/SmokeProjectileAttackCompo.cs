using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public sealed class SmokeProjectileAttackCompo : ArcProjectileAttackCompo
    {
        [SerializeField, Min(1f), Tooltip("기본 공격 주기 대비 배율. 회복 지원함과 같은 방식으로 조절합니다.")]
        private float attackIntervalMultiplier = 3f;
        [SerializeField, Min(0f)] private float baselineAttackAnimationDurationSeconds = 0.45833334f;
        protected override float ProjectileAmount => 0f;
        public override bool TicksCooldownWithoutTarget => true;
        public override bool FinishesAnimationWithoutTarget => true;
        protected override float FinalAttackCooldown => base.FinalAttackCooldown * attackIntervalMultiplier +
            baselineAttackAnimationDurationSeconds / FinalAttackSpeed * (attackIntervalMultiplier - 1f);

        protected override void Fire()
        {
            TryAcquireTarget();
            base.Fire();
        }
    }
}
