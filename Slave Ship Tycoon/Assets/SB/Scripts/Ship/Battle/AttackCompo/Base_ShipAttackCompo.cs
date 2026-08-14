using SB.Core;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public abstract class Base_ShipAttackCompo : EntityComponent
    {
        [SerializeField, Min(0.01f)] private float attackCooldown = 1f;

        private Ship ownerShip;
        private TargetSelector targetSelector;
        private ShipCombatStatCompo combatStatCompo;
        private EntityAnimator animator;
        private EntityAnimatorTrigger animatorTrigger;
        private float currentCooldown;

        private static readonly int AttackSpeedMultiplierHash = UnityEngine.Animator.StringToHash("AttackSpeedMultiplier");

        protected TargetSelector TargetSelector => targetSelector;
        protected ShipCombatStatCompo CombatStatCompo => combatStatCompo;
        protected EntityAnimator Animator => animator;
        protected EntityAnimatorTrigger AnimatorTrigger => animatorTrigger;
        protected Ship CurrentTarget { get; private set; }
        protected float FinalAttackDamage => combatStatCompo != null ? combatStatCompo.FinalAttackDamage : 0f;
        protected float FinalAttackSpeed => combatStatCompo != null ? combatStatCompo.FinalAttackSpeed : 1f;
        protected float FinalAttackCooldown => attackCooldown / FinalAttackSpeed;
        public bool HasAliveTarget => CurrentTarget != null && CurrentTarget.IsDead == false;
        public bool CanAttack => currentCooldown <= 0f;

        private void OnEnable()
        {
            Bus<BattleStartEvent>.OnEvent += RequestTarget;
        }

        private void OnDisable()
        {
            Bus<BattleStartEvent>.OnEvent -= RequestTarget;
        }

        public override void Initialize(Entity entity)
        {
            base.Initialize(entity);

            ownerShip = entity as Ship;
            targetSelector = GetCompo<TargetSelector>();
            combatStatCompo = GetCompo<ShipCombatStatCompo>();
            animator = GetCompo<EntityAnimator>();
            animatorTrigger = GetCompo<EntityAnimatorTrigger>();

            if (animatorTrigger != null)
            {
                animatorTrigger.OnAttackStartTrigger += HandleAttackStartTrigger;
                animatorTrigger.OnAttackEndTrigger += HandleAttackEndTrigger;
            }
        }

        private void OnDestroy()
        {
            if (animatorTrigger == null)
                return;

            animatorTrigger.OnAttackStartTrigger -= HandleAttackStartTrigger;
            animatorTrigger.OnAttackEndTrigger -= HandleAttackEndTrigger;
        }

        public void ResetAttackState()
        {
            CurrentTarget = null;
            currentCooldown = 0f;
            ResetAttackAnimationSpeed();
        }

        private void RequestTarget(BattleStartEvent evt)
        {
            targetSelector?.SetBattleSpawnData(evt.BattleSpawnData);
            ResetAttackState();
            TryAcquireTarget();
            ownerShip?.ChangeState(ShipStateType.Idle);
        }

        public bool TryAcquireTarget()
        {
            CurrentTarget = targetSelector != null ? targetSelector.GetTarget() : null;
            return HasAliveTarget;
        }

        public void ForceRetarget()
        {
            CurrentTarget = null;
            TryAcquireTarget();
        }

        public bool EnsureTarget()
        {
            if (HasAliveTarget)
                return true;

            return TryAcquireTarget();
        }

        public void TickCooldown(float deltaTime)
        {
            if (currentCooldown > 0f)
                currentCooldown -= deltaTime;
        }

        public void ResetCooldown()
        {
            currentCooldown = FinalAttackCooldown;
        }

        public void RefreshAttackSpeed(float previousAttackSpeed)
        {
            float currentAttackSpeed = FinalAttackSpeed;

            if (currentCooldown > 0f && previousAttackSpeed > 0f && currentAttackSpeed > 0f)
                currentCooldown *= previousAttackSpeed / currentAttackSpeed;

            ApplyAttackAnimationSpeed();
        }

        public bool EnterAttack()
        {
            if (EnsureTarget() == false)
                return false;

            ApplyAttackAnimationSpeed();
            OnAttackEnter();
            return true;
        }

        private void HandleAttackStartTrigger()
        {
            if (EnsureTarget() == false)
                return;

            Fire();
        }

        private void HandleAttackEndTrigger()
        {
            OnAttackExit();
        }

        protected virtual void OnAttackEnter()
        {
        }

        protected abstract void Fire();

        protected virtual void OnAttackExit()
        {
            ResetAttackAnimationSpeed();
            ResetCooldown();
            ownerShip?.ChangeState(ShipStateType.Idle);
        }

        public void ApplyAttackAnimationSpeed()
        {
            SetAttackAnimationSpeed(FinalAttackSpeed);
        }

        public void ResetAttackAnimationSpeed()
        {
            SetAttackAnimationSpeed(1f);
        }

        private void SetAttackAnimationSpeed(float speed)
        {
            animator?.SetParam(AttackSpeedMultiplierHash, speed);
        }
    }
}
