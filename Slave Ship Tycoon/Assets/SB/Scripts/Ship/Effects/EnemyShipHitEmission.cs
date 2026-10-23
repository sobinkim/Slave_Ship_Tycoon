using SB.Core;
using UnityEngine;

namespace SB.Scripts
{
    /// <summary>Short hull emission flash when an enemy ship takes real damage.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyShipHitEmission : EntityComponent, IPoolable
    {
        [SerializeField] private EntityHealth health;
        [SerializeField, Tooltip("피격 이미션을 적용할 선체·돛 SpriteRenderer")]
        private SpriteRenderer[] emissionRenderers = new SpriteRenderer[0];
        [SerializeField, ColorUsage(true, true)] private Color emissionColor = Color.white;
        [SerializeField, Min(0f)] private float emissionIntensity = 1.8f;
        [SerializeField, Min(0.001f)] private float attackSeconds = 0.02f;
        [SerializeField, Min(0.001f)] private float releaseSeconds = 0.14f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_HitEmissionColor");
        private static readonly int EmissionIntensityId = Shader.PropertyToID("_HitEmissionIntensity");
        private static readonly int EmissionFlashId = Shader.PropertyToID("_HitEmissionFlash");
        private MaterialPropertyBlock properties;
        private float previousHealth;
        private float elapsed = -1f;
        private float startingFlash;
        private float flash;
        private bool pendingDamage;

        public override void Initialize(Entity entity)
        {
            Unsubscribe();
            base.Initialize(entity);
            if (health == null) health = Owner.GetCompo<EntityHealth>();
            previousHealth = health != null ? health.CurrentHealth : 0f;
            if (health != null) health.OnHealthChanged += HandleHealthChanged;
            Owner.OnHitEvent.AddListener(HandleHit);
            ApplyEmission(0f);
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            pendingDamage = currentHealth < previousHealth;
            previousHealth = currentHealth;
        }

        private void HandleHit()
        {
            if (!pendingDamage || !isActiveAndEnabled) return;
            pendingDamage = false;
            startingFlash = flash;
            elapsed = 0f;
            ApplyEmission(flash);
        }

        private void Update()
        {
            if (elapsed < 0f) return;
            elapsed += Time.deltaTime;
            float attack = Mathf.Max(0.001f, attackSeconds);
            float release = Mathf.Max(0.001f, releaseSeconds);
            if (elapsed < attack)
                ApplyEmission(Mathf.Lerp(startingFlash, 1f, Mathf.SmoothStep(0f, 1f, elapsed / attack)));
            else if (elapsed < attack + release)
                ApplyEmission(1f - Mathf.SmoothStep(0f, 1f, (elapsed - attack) / release));
            else
            {
                elapsed = -1f;
                ApplyEmission(0f);
            }
        }

        private void ApplyEmission(float value)
        {
            flash = value;
            if (properties == null) properties = new MaterialPropertyBlock();
            foreach (SpriteRenderer renderer in emissionRenderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetColor(EmissionColorId, emissionColor);
                properties.SetFloat(EmissionIntensityId, emissionIntensity);
                properties.SetFloat(EmissionFlashId, value);
                renderer.SetPropertyBlock(properties);
            }
        }

        public void OnSpawnedFromPool()
        {
            previousHealth = health != null ? health.CurrentHealth : 0f;
            pendingDamage = false;
            elapsed = -1f;
            startingFlash = 0f;
            ApplyEmission(0f);
        }

        public void OnDespawnedToPool()
        {
            pendingDamage = false;
            elapsed = -1f;
            startingFlash = 0f;
            ApplyEmission(0f);
        }

        private void OnDisable() => OnDespawnedToPool();

        private void Unsubscribe()
        {
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
            if (Owner != null) Owner.OnHitEvent.RemoveListener(HandleHit);
        }

        private void OnDestroy() => Unsubscribe();
    }
}
