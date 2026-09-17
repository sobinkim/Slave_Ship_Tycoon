using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Core
{
    [DisallowMultipleComponent]
    public class EntityHealth : EntityComponent, IAfterInitialize
    {
        public delegate void HealthChangedHandler(float currentHealth, float maxHealth);

        public event HealthChangedHandler OnHealthChanged;

        [SerializeField] private StatSO maxHealthStat;
        [SerializeField, Min(1f)] private float fallbackMaxHealth = 100f;
        [SerializeField] private bool resetHealthOnInitialize = true;
        [SerializeField] private Transform hitDamageTexPos;

        private EntityStatCompo statCompo;
        private StatSO runtimeMaxHealthStat;
        private float maxHealth;
        private float currentHealth;
        private IIncomingDamageModifier[] incomingDamageModifiers = Array.Empty<IIncomingDamageModifier>();

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        public bool IsAlive => Owner != null && !Owner.IsDead;
        public double LastDamageTimeSeconds { get; private set; } = double.NegativeInfinity;

        public override void Initialize(Entity entity)
        {
            base.Initialize(entity);
            statCompo = entity.GetCompo<EntityStatCompo>();
            incomingDamageModifiers = entity.GetComponentsInChildren<IIncomingDamageModifier>(true);
        }

        public void AfterInitialize()
        {
            InitializeMaxHealthStat();

            if (resetHealthOnInitialize)
                ResetHealth();
        }

        private void OnDestroy()
        {
            if (runtimeMaxHealthStat != null)
                runtimeMaxHealthStat.OnValueChanged -= HandleMaxHealthChanged;
        }

        public void ResetHealth()
        {
            ResetHealth(1f);
        }

        public void ResetHealth(float initialHealthRatio)
        {
            if (Owner != null)
                Owner.IsDead = false;

            maxHealth = ResolveMaxHealth();
            currentHealth = maxHealth * Mathf.Clamp01(initialHealthRatio);
            LastDamageTimeSeconds = double.NegativeInfinity;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public virtual void ApplyDamage(float damage)
        {
            ApplyDamage(damage, hitDamageTexPos.position);
        }

        public virtual void ApplyDamage(float damage, Vector3 hitPosition, bool isCritical = false)
        {
            if (Owner == null || Owner.IsDead || Owner.IsInvincible)
                return;

            if (damage <= 0f)
                return;

            damage = ApplyIncomingDamageModifiers(damage);
            if (damage <= 0f)
                return;

            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
            LastDamageTimeSeconds = Time.timeAsDouble;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Owner.OnHitEvent.Invoke();
            Bus<DamageAppliedEvent>.Raise(
                new DamageAppliedEvent(Mathf.CeilToInt(damage), hitPosition, isCritical)
            );

            if (currentHealth <= 0f)
                Die();
        }

        public virtual void ApplyHeal(float amount)
        {
            if (Owner == null || Owner.IsDead || amount <= 0f)
                return;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);

            if (!Mathf.Approximately(previousHealth, currentHealth))
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void Kill()
        {
            ApplyDamage(maxHealth);
        }

        protected virtual void Die()
        {
            if (Owner == null || Owner.IsDead)
                return;

            Owner.IsDead = true;
            Owner.OnDeathEvent.Invoke();
        }

        private void InitializeMaxHealthStat()
        {
            if (runtimeMaxHealthStat != null)
                runtimeMaxHealthStat.OnValueChanged -= HandleMaxHealthChanged;

            runtimeMaxHealthStat = null;

            if (statCompo != null && maxHealthStat != null)
            {
                runtimeMaxHealthStat = statCompo.GetStat(maxHealthStat);
                if (runtimeMaxHealthStat != null)
                    runtimeMaxHealthStat.OnValueChanged += HandleMaxHealthChanged;
            }

            maxHealth = ResolveMaxHealth();
        }

        private float ApplyIncomingDamageModifiers(float damage)
        {
            foreach (IIncomingDamageModifier modifier in incomingDamageModifiers)
            {
                if (modifier == null)
                    continue;

                damage = Mathf.Max(0f, modifier.ModifyIncomingDamage(damage));
                if (damage <= 0f)
                    return 0f;
            }

            return damage;
        }

        private float ResolveMaxHealth()
        {
            if (runtimeMaxHealthStat != null)
                return Mathf.Max(1f, runtimeMaxHealthStat.Value);

            return Mathf.Max(1f, fallbackMaxHealth);
        }

        private void HandleMaxHealthChanged(StatSO stat, float currentValue, float previousValue)
        {
            float previousMaxHealth = maxHealth;
            maxHealth = Mathf.Max(1f, currentValue);

            float changedAmount = maxHealth - previousMaxHealth;
            if (changedAmount > 0f)
                currentHealth += changedAmount;

            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
