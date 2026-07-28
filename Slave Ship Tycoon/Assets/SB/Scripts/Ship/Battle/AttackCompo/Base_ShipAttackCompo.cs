using SB.Core;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public enum AttackStateType
    {
        Idle,
        Attack,
        Dead
    }

    public abstract class Base_ShipAttackCompo : EntityComponent
    {
        [SerializeField, Min(0.01f)] private float attackCooldown = 1f;

        private ShipAttackStateMachine stateMachine;

        private TargetSelector targetSelector;
        private EntityAnimator animator;
        private EntityAnimatorTrigger animatorTrigger;
        private float currentCooldown;

        protected TargetSelector TargetSelector => targetSelector;
        protected EntityAnimator Animator => animator;
        protected EntityAnimatorTrigger AnimatorTrigger => animatorTrigger;
        protected Ship CurrentTarget { get; private set; }
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

            targetSelector = GetCompo<TargetSelector>();
            animator = GetCompo<EntityAnimator>();
            animatorTrigger = GetCompo<EntityAnimatorTrigger>();

            stateMachine = new ShipAttackStateMachine(this);

            if (animatorTrigger != null)
            {
                animatorTrigger.OnAttackStartTrigger += AttackStart;
                animatorTrigger.OnAttackEndTrigger += AttackEnd;
            }
        }

        private void OnDestroy()
        {
            if (animatorTrigger == null)
                return;

            animatorTrigger.OnAttackStartTrigger -= AttackStart;
            animatorTrigger.OnAttackEndTrigger -= AttackEnd;
        }

        private void Update()
        {
            stateMachine?.Update();
        }

        public void ChangeState(AttackStateType state)
        {
            stateMachine?.ChangeState(state);
        }

        public void ResetAttackState()
        {
            CurrentTarget = null;
            currentCooldown = 0f;
            ChangeState(AttackStateType.Idle);
        }

        private void RequestTarget(BattleStartEvent evt)
        {
            ResetAttackState();
            TryAcquireTarget();
        }

        public bool TryAcquireTarget()
        {
            CurrentTarget = targetSelector != null ? targetSelector.GetTarget() : null;
            return HasAliveTarget;
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
            currentCooldown = attackCooldown;
        }

        protected virtual void AttackStart()
        {
            if (HasAliveTarget == false)
                TryAcquireTarget();
        }

        protected virtual void AttackEnd()
        {
            ResetCooldown();
            ChangeState(AttackStateType.Idle);
        }
    }
}
