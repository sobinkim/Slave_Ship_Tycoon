using SB.Core;
using SB.Scripts.Projectiles;
using UnityEngine;

namespace SB.Scripts
{
    /// <summary>Damage-only body emission, contact burst and water chips on each pooled creature.</summary>
    [DisallowMultipleComponent]
    public sealed class SeaCreatureHitFeedback : EntityComponent, IPoolable
    {
        [Header("Explicit references")]
        [SerializeField] private EntityHealth health;
        [SerializeField] private ShipProjectileAnchorCompo projectileAnchors;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private ParticleSystem contactBurst;
        [SerializeField] private ParticleSystemRenderer contactRenderer;
        [SerializeField] private ParticleSystem waterChips;
        [SerializeField] private ParticleSystemRenderer chipRenderer;

        [Header("Body hit emission")]
        [SerializeField, Tooltip("몸체 외에 함께 발광할 꼬리/지느러미/턱. 물살과 그림자는 제외합니다.")]
        private SpriteRenderer[] emissionBodyParts = new SpriteRenderer[0];
        [SerializeField, ColorUsage(true, true)] private Color emissionColor = Color.white;
        [SerializeField, Min(0f)] private float emissionIntensity = 1.8f;
        [SerializeField, Min(0.001f), Tooltip("실제 피해를 받은 뒤 발광이 최대가 되는 시간")]
        private float emissionAttackSeconds = 0.02f;
        [SerializeField, Min(0.001f), Tooltip("최대 발광 이후 원래 색으로 돌아오는 시간")]
        private float emissionReleaseSeconds = 0.14f;

        [Header("Contact burst")]
        [SerializeField, Min(0.01f)] private float burstSize = 0.32f;
        [SerializeField, Range(0.05f, 0.25f)] private float burstSeconds = 0.18f;
        [SerializeField] private Color burstColor = new Color(1f, 0.87f, 0.53f, 1f);

        [Header("Water chips")]
        [SerializeField, Range(4, 6)] private int chipCount = 5;
        [SerializeField, Min(0.01f)] private float chipSize = 0.085f;
        [SerializeField, Range(0.1f, 0.25f)] private float chipSeconds = 0.24f;
        [SerializeField, Min(0f)] private float chipSpeed = 1.1f;
        [SerializeField, Min(0f)] private float chipGravity = 0.5f;
        [SerializeField] private Color chipColor = new Color(0.4f, 0.85f, 0.88f, 1f);
        [SerializeField] private Color chipHighlight = new Color(0.92f, 1f, 0.96f, 1f);

        [Header("Sorting")]
        [SerializeField, Tooltip("몸체보다 앞에 표시할 피격 이펙트 정렬 오더 차이")]
        private int sortingOrderOffset = 12;

        private float previousHealth;
        private bool pendingDamage;
        private bool initialized;
        private MaterialPropertyBlock emissionProperties;
        private float emissionElapsed = -1f;
        private float emissionStartFlash;
        private float emissionFlash;

        private static readonly int EmissionColorId = Shader.PropertyToID("_HitEmissionColor");
        private static readonly int EmissionIntensityId = Shader.PropertyToID("_HitEmissionIntensity");
        private static readonly int EmissionFlashId = Shader.PropertyToID("_HitEmissionFlash");

        public float EmissionFlash => emissionFlash;

        public override void Initialize(Entity entity)
        {
            Unsubscribe();
            base.Initialize(entity);
            ConfigureParticles(contactBurst, false);
            ConfigureParticles(waterChips, true);
            if (health != null)
            {
                previousHealth = health.CurrentHealth;
                health.OnHealthChanged += HandleHealthChanged;
            }
            if (Owner != null) Owner.OnHitEvent.AddListener(HandleHit);
            initialized = true;
            ClearEffects();
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            // OnHit follows this notification only for real damage. Reset/heal alone never emits.
            pendingDamage = currentHealth < previousHealth;
            previousHealth = currentHealth;
        }

        private void HandleHit()
        {
            if (!pendingDamage || !isActiveAndEnabled) return;
            pendingDamage = false;
            // EntityHealth invokes OnHit before Die, so the final damaging hit still emits.
            BeginHitEmission();
            if (projectileAnchors != null) PlayHitAt(projectileAnchors.HitPosition);
        }

        private void BeginHitEmission()
        {
            emissionStartFlash = emissionFlash;
            emissionElapsed = 0f;
            ApplyEmission(emissionFlash);
        }

        private void Update()
        {
            if (emissionElapsed < 0f) return;
            emissionElapsed += Time.deltaTime;
            float attackSeconds = Mathf.Max(0.001f, emissionAttackSeconds);
            float releaseSeconds = Mathf.Max(0.001f, emissionReleaseSeconds);
            if (emissionElapsed < attackSeconds)
            {
                float attack = Mathf.SmoothStep(0f, 1f, emissionElapsed / attackSeconds);
                ApplyEmission(Mathf.Lerp(emissionStartFlash, 1f, attack));
            }
            else if (emissionElapsed < attackSeconds + releaseSeconds)
            {
                float release = Mathf.SmoothStep(0f, 1f, (emissionElapsed - attackSeconds) / releaseSeconds);
                ApplyEmission(1f - release);
            }
            else
            {
                emissionElapsed = -1f;
                ApplyEmission(0f);
            }
        }

        private void ApplyEmission(float flash)
        {
            emissionFlash = flash;
            if (emissionProperties == null) emissionProperties = new MaterialPropertyBlock();
            ApplyEmissionTo(bodyRenderer, flash);
            if (emissionBodyParts == null) return;
            foreach (SpriteRenderer part in emissionBodyParts)
            {
                if (part != bodyRenderer) ApplyEmissionTo(part, flash);
            }
        }

        private void ApplyEmissionTo(SpriteRenderer renderer, float flash)
        {
            if (renderer == null) return;
            // The surface animation owns waterline/swim/sink overrides on this same renderer.
            renderer.GetPropertyBlock(emissionProperties);
            emissionProperties.SetColor(EmissionColorId, emissionColor);
            emissionProperties.SetFloat(EmissionIntensityId, emissionIntensity);
            emissionProperties.SetFloat(EmissionFlashId, flash);
            renderer.SetPropertyBlock(emissionProperties);
        }

        /// <summary>Emit at an explicit world point; normal creature hits use the projectile hit anchor.</summary>
        public void PlayHitAt(Vector3 hitPosition)
        {
            if (!initialized || !isActiveAndEnabled) return;
            ApplySorting(contactRenderer, 1);
            ApplySorting(chipRenderer, 0);

            if (contactBurst != null)
            {
                contactBurst.Play(false);
                var burst = new ParticleSystem.EmitParams
                {
                    position = hitPosition,
                    velocity = Vector3.zero,
                    startLifetime = burstSeconds,
                    startSize = burstSize * Random.Range(0.92f, 1.08f),
                    startColor = burstColor,
                    rotation = Random.Range(-22.5f, 22.5f)
                };
                contactBurst.Emit(burst, 1);
            }

            if (waterChips == null) return;
            waterChips.Play(false);
            for (int i = 0; i < chipCount; i++)
            {
                // A broad upward fan reads as water spray rather than blood or an explosion.
                float angle = Mathf.Lerp(18f, 162f, (i + Random.Range(0.15f, 0.85f)) / chipCount) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                float size = chipSize * Random.Range(0.8f, 1.2f);
                var chip = new ParticleSystem.EmitParams
                {
                    position = hitPosition + direction * (burstSize * 0.12f),
                    velocity = direction * (chipSpeed * Random.Range(0.75f, 1.2f)),
                    startLifetime = chipSeconds * Random.Range(0.85f, 1f),
                    startSize3D = new Vector3(size * 0.65f, size * 1.45f, size),
                    startColor = Color.Lerp(chipColor, chipHighlight, Random.value),
                    rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f
                };
                waterChips.Emit(chip, 1);
            }
        }

        private void ConfigureParticles(ParticleSystem particles, bool chips)
        {
            if (particles == null) return;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startSpeed = 0f;
            main.startSize3D = chips;
            main.gravityModifier = chips ? chipGravity : 0f;
            main.maxParticles = chips ? 64 : 16;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;

            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            colors.color = fade;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.separateAxes = false;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, chips ? 0.8f : 0.45f), new Keyframe(0.18f, 1f),
                new Keyframe(0.5f, 0.9f), new Keyframe(1f, 0.2f)));
            var rotation = particles.rotationOverLifetime;
            rotation.enabled = chips;
            if (chips) rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
        }

        private void ApplySorting(ParticleSystemRenderer renderer, int additionalOrder)
        {
            if (renderer == null || bodyRenderer == null) return;
            renderer.sortingLayerID = bodyRenderer.sortingLayerID;
            renderer.sortingOrder = bodyRenderer.sortingOrder + sortingOrderOffset + additionalOrder;
        }

        public void OnSpawnedFromPool()
        {
            previousHealth = health != null ? health.CurrentHealth : 0f;
            pendingDamage = false;
            ClearEffects();
        }

        public void OnDespawnedToPool() => ClearEffects();
        private void OnDisable() => ClearEffects();

        private void ClearEffects()
        {
            pendingDamage = false;
            emissionElapsed = -1f;
            emissionStartFlash = 0f;
            ApplyEmission(0f);
            if (contactBurst != null) contactBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (waterChips != null) waterChips.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Unsubscribe()
        {
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
            if (Owner != null) Owner.OnHitEvent.RemoveListener(HandleHit);
        }

        private void OnDestroy() => Unsubscribe();
    }
}
