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

        private EntityStatCompo statCompo;
        private StatSO runtimeMaxHealthStat;
        private float maxHealth;
        private float currentHealth;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        public bool IsAlive => Owner != null && !Owner.IsDead;

        public override void Initialize(Entity entity)
        {
            base.Initialize(entity);
            statCompo = entity.GetCompo<EntityStatCompo>();
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
            if (Owner != null)
                Owner.IsDead = false;

            maxHealth = ResolveMaxHealth();
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public virtual void ApplyDamage(float damage)
        {
            if (Owner == null || Owner.IsDead || Owner.IsInvincible)
                return;

            if (damage <= 0f)
                return;

            currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Owner.OnHitEvent.Invoke();

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
