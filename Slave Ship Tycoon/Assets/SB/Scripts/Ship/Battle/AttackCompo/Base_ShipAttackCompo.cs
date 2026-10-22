using System;
using System.Collections.Generic;
using SB.Core;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.AttackCompo
{
    public abstract class Base_ShipAttackCompo : EntityComponent
    {
        [SerializeField, Min(0.01f)] private float attackCooldown = 1f;

        [Header("Muzzle")]
        [SerializeField] private SB.Scripts.Projectiles.ShipMuzzleEffect muzzleEffectPrefab;
        [SerializeField, Min(0.01f), Tooltip("포구 이펙트의 크기 배율")]
        private float muzzleEffectScale = 1f;

        [Header("Allied fire lanes")]
        [SerializeField, Tooltip("살아 있는 아군 함선이 포격 경로를 가리면 사격을 보류합니다. 항해 테스트 씬에서만 켭니다.")]
        private bool blocksFireThroughAlliedShips;
        [SerializeField, Min(0.01f), Tooltip("포격 경로를 막는 아군 함체의 반경(월드 단위). 회복과 요격에는 적용하지 않습니다.")]
        private float alliedHullBlockingRadius = 0.38f;

        private Ship ownerShip;
        private TargetSelector targetSelector;
        private ShipCombatStatCompo combatStatCompo;
        private EntityAnimator animator;
        private EntityAnimatorTrigger animatorTrigger;
        private float currentCooldown;
        private bool _isBattleRunning;
        private int _currentTargetSpawnGeneration;
        private IReadOnlyList<Ship> alliedEscorts = Array.Empty<Ship>();
        private MainShip alliedMainShip;
        private bool attackBlockedByAlly;
        private Predicate<Ship> clearFireLaneFilter;

        private static readonly int AttackSpeedMultiplierHash = UnityEngine.Animator.StringToHash("AttackSpeedMultiplier");

        protected TargetSelector TargetSelector => targetSelector;
        protected Ship OwnerShip => ownerShip;
        protected ShipCombatStatCompo CombatStatCompo => combatStatCompo;
        protected EntityAnimator Animator => animator;
        protected EntityAnimatorTrigger AnimatorTrigger => animatorTrigger;
        protected bool IsBattleRunning => _isBattleRunning;
        public Ship CurrentTarget { get; private set; }
        public event System.Action<Ship> TargetChanged;
        protected float FinalAttackDamage => combatStatCompo != null ? combatStatCompo.FinalAttackDamage : 0f;
        protected float FinalAttackSpeed => combatStatCompo != null ? combatStatCompo.FinalAttackSpeed : 1f;
        protected virtual float FinalAttackCooldown => attackCooldown / FinalAttackSpeed;
        public virtual bool TicksCooldownWithoutTarget => false;
        public virtual bool FinishesAnimationWithoutTarget => false;
        public bool HasAliveTarget => CurrentTarget != null && CurrentTarget.isActiveAndEnabled &&
                                      CurrentTarget.IsDead == false &&
                                      (targetSelector == null || targetSelector.IsValidTarget(CurrentTarget)) &&
                                      CurrentTarget.SpawnGeneration == _currentTargetSpawnGeneration;
        public bool CanAttack => _isBattleRunning && currentCooldown <= 0f && !IsFireLaneBlocked;
        public bool IsFireLaneBlocked => TryGetBlockingShip(CurrentTarget, out _);
        public Ship CurrentBlockingShip => TryGetBlockingShip(CurrentTarget, out Ship blocker) ? blocker : null;
        public bool BlocksFireThroughAlliedShips => blocksFireThroughAlliedShips;
        public float AlliedHullBlockingRadius => alliedHullBlockingRadius;

        private void OnEnable()
        {
            Bus<BattleStartEvent>.OnEvent += RequestTarget;
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        }

        private void OnDisable()
        {
            Bus<BattleStartEvent>.OnEvent -= RequestTarget;
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            _isBattleRunning = false;
            ClearFireLaneRoster();
            SetTarget(null);
        }

        public override void Initialize(Entity entity)
        {
            base.Initialize(entity);

            ownerShip = entity as Ship;
            targetSelector = GetCompo<TargetSelector>();
            clearFireLaneFilter = HasClearFireLaneTo;
            combatStatCompo = GetCompo<ShipCombatStatCompo>();
            animator = GetCompo<EntityAnimator>();
            animatorTrigger = GetCompo<EntityAnimatorTrigger>();

            if (animatorTrigger != null)
            {
                animatorTrigger.OnAttackStartTrigger += HandleAttackStartTrigger;
                animatorTrigger.OnAttackEndTrigger += HandleAttackEndTrigger;
            }
        }

        protected virtual void OnDestroy()
        {
            if (animatorTrigger == null)
                return;

            animatorTrigger.OnAttackStartTrigger -= HandleAttackStartTrigger;
            animatorTrigger.OnAttackEndTrigger -= HandleAttackEndTrigger;
        }

        public void SuspendCombat()
        {
            _isBattleRunning = false;
            ResetAttackState();
            ownerShip?.ChangeState(ShipStateType.Idle);
        }

        public void PlayFinalSalvoAnimation(string stateName)
        {
            Animator runtimeAnimator = animator != null ? animator.RuntimeAnimator : null;
            if (runtimeAnimator == null)
                return;

            animator.SetParam("ATTACK", true);
            int stateHash = UnityEngine.Animator.StringToHash(stateName);
            if (runtimeAnimator.HasState(0, stateHash))
                runtimeAnimator.Play(stateHash, 0, 0f);
        }

        public virtual void ResetAttackState()
        {
            animator?.SetParam("ATTACK", false);
            SetTarget(null);
            _currentTargetSpawnGeneration = 0;
            currentCooldown = 0f;
            attackBlockedByAlly = false;
            ResetAttackAnimationSpeed();
        }

        private void RequestTarget(BattleStartEvent evt)
        {
            targetSelector?.SetBattleSpawnData(evt.BattleSpawnData);
            alliedEscorts = evt.BattleSpawnData.EscortShips;
            alliedMainShip = evt.BattleSpawnData.MainShip;
            ResetAttackState();
            _isBattleRunning = true;
            TryAcquireTarget();
            ownerShip?.ChangeState(ShipStateType.Idle);
        }

        private void HandleBattleEnded(StageBattleEndedEvent evt)
        {
            _isBattleRunning = false;
            ResetAttackState();
            ClearFireLaneRoster();
        }

        public virtual bool TryAcquireTarget()
        {
            Ship selectedTarget = null;
            if (_isBattleRunning && ownerShip != null && ownerShip.isActiveAndEnabled &&
                !ownerShip.IsDead && targetSelector != null)
            {
                bool needsClearLane = blocksFireThroughAlliedShips && targetSelector.Team == TargetTeam.Opponent;
                selectedTarget = targetSelector.GetTarget(needsClearLane ? clearFireLaneFilter : null);
                // Keep the blocked target visible for feedback when every valid lane is closed.
                if (selectedTarget == null && needsClearLane) selectedTarget = targetSelector.GetTarget();
            }
            SetTarget(selectedTarget);
            return HasAliveTarget && HasClearFireLane();
        }

        private bool HasClearFireLaneTo(Ship target) => !TryGetBlockingShip(target, out _);

        private void SetTarget(Ship target)
        {
            int generation = target != null ? target.SpawnGeneration : 0;
            if (CurrentTarget == target && _currentTargetSpawnGeneration == generation) return;
            CurrentTarget = target;
            _currentTargetSpawnGeneration = generation;
            TargetChanged?.Invoke(target);
        }

        public void ForceRetarget()
        {
            SetTarget(null);
            TryAcquireTarget();
        }

        public virtual bool EnsureTarget()
        {
            if (_isBattleRunning == false || ownerShip == null ||
                ownerShip.isActiveAndEnabled == false || ownerShip.IsDead)
            {
                SetTarget(null);
                return false;
            }

            if (HasAliveTarget && !IsFireLaneBlocked)
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
            attackBlockedByAlly = false;
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
            if (_isBattleRunning == false || ownerShip == null ||
                ownerShip.isActiveAndEnabled == false || ownerShip.IsDead)
                return;

            OnAttackExit();
        }

        protected virtual void OnAttackEnter()
        {
        }

        protected abstract void Fire();

        protected void PlayMuzzleEffect(Transform firePoint, Vector3 direction)
        {
            if (muzzleEffectPrefab == null || firePoint == null || PoolingManager.Instance == null)
                return;

            var effect = PoolingManager.Instance.Get(muzzleEffectPrefab, firePoint.position, Quaternion.identity);
            if (effect != null)
                effect.Play(firePoint, direction, muzzleEffectScale);
        }

        protected virtual void OnAttackExit()
        {
            ResetAttackAnimationSpeed();
            if (!attackBlockedByAlly)
                ResetCooldown();
            ownerShip?.ChangeState(ShipStateType.Idle);
        }

        public void ConfigureFireOcclusion(bool enabled, float hullBlockingRadius)
        {
            blocksFireThroughAlliedShips = enabled;
            alliedHullBlockingRadius = Mathf.Max(0.01f, hullBlockingRadius);
        }

        protected bool HasClearFireLane()
        {
            if (!IsFireLaneBlocked)
                return true;

            attackBlockedByAlly = true;
            return false;
        }

        public bool TryGetBlockingShip(Ship target, out Ship blockingShip)
        {
            blockingShip = null;
            if (!blocksFireThroughAlliedShips || ownerShip == null || target == null ||
                ownerShip.myShipData.ShipType == ShipType.Enemy ||
                (targetSelector != null && targetSelector.Team == TargetTeam.SameTeam))
                return false;

            Vector2 origin = ownerShip.transform.position;
            Vector2 path = (Vector2)target.transform.position - origin;
            float distanceSquared = path.sqrMagnitude;
            if (distanceSquared <= Mathf.Epsilon)
                return false;

            float nearestProgress = float.PositiveInfinity;
            for (int i = 0; i < alliedEscorts.Count; i++)
                CheckFireLaneBlocker(alliedEscorts[i], origin, path, distanceSquared,
                    ref blockingShip, ref nearestProgress);
            CheckFireLaneBlocker(alliedMainShip, origin, path, distanceSquared,
                ref blockingShip, ref nearestProgress);
            return blockingShip != null;
        }

        private void CheckFireLaneBlocker(Ship candidate, Vector2 origin, Vector2 path,
            float pathDistanceSquared, ref Ship blockingShip, ref float nearestProgress)
        {
            if (candidate == null || candidate == ownerShip || !candidate.isActiveAndEnabled || candidate.IsDead)
                return;

            Vector2 offset = (Vector2)candidate.transform.position - origin;
            float progress = Vector2.Dot(offset, path) / pathDistanceSquared;
            if (progress <= 0f || progress >= 1f || progress >= nearestProgress)
                return;
            Vector2 distanceFromLane = offset - path * progress;
            if (distanceFromLane.sqrMagnitude > alliedHullBlockingRadius * alliedHullBlockingRadius)
                return;

            nearestProgress = progress;
            blockingShip = candidate;
        }

        private void ClearFireLaneRoster()
        {
            alliedEscorts = Array.Empty<Ship>();
            alliedMainShip = null;
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


