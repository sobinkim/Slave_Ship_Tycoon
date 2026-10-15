using System.Collections.Generic;
using SB.Core;
using UnityEngine;

namespace SB.Scripts
{
    public enum SeaCreatureKind { Swarmer, Charger }
    public enum SeaCreatureState { Swimming, Biting, WindingUp, Charging, Recovering, Dying, Cruising, Entering }

    internal readonly struct SeaCreatureTarget
    {
        public readonly Ship Ship;
        public readonly EntityHealth Health;
        public bool IsAlive => Ship != null && Ship.isActiveAndEnabled && !Ship.IsDead;
        public SeaCreatureTarget(Ship ship)
        {
            Ship = ship;
            Health = ship.GetCompo<EntityHealth>();
        }
    }

    public sealed class SeaCreatureEnemy : Enemy
    {
        [Header("Creature")]
        [SerializeField] private SeaCreatureKind kind;
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer chargeWarning;
        [SerializeField] private SeaCreatureSurfacePresentation surfacePresentation;
        [SerializeField, Tooltip("원본 그림의 머리가 오른쪽을 향할 때 켭니다.")]
        private bool artFacesRight;
        [SerializeField, Min(0f), Tooltip("조우 전에는 항로 위에 머무릅니다. 이 값은 그 자리에서 움직이는 유영 폭입니다.")]
        private float idleDriftAmplitude = 0.025f;
        [SerializeField, Min(1f)] private float maxHealth = 20f;

        [Header("Swimming and bite")]
        [SerializeField, Min(0.01f)] private float swimSpeed = 1.15f;
        [SerializeField, Min(0.01f)] private float bodyRadius = 0.22f;
        [SerializeField, Min(0.01f)] private float biteRange = 0.65f;
        [SerializeField, Min(0f)] private float biteDamage = 3f;
        [SerializeField, Min(0.1f)] private float biteIntervalSeconds = 1.1f;
        [SerializeField, Min(0.1f)] private float biteDurationSeconds = 0.42f;
        [SerializeField, Range(0f, 1f)] private float biteHitProgress = 0.55f;
        [SerializeField, Min(0f)] private float swimWeave = 0.18f;
        [SerializeField, Min(0.01f)] private float swimCyclesPerSecond = 1.5f;
        [SerializeField, Min(0f)] private float separationStrength = 1f;
        [SerializeField, Min(0f)] private float hitStunSeconds = 0.07f;
        [SerializeField, Min(0f)] private float hitKnockback = 0.09f;

        [Header("Charger")]
        [SerializeField, Min(0.1f)] private float chargeStartRange = 3.2f;
        [SerializeField, Min(0.1f)] private float chargeWindupSeconds = 0.7f;
        [SerializeField, Min(0.1f)] private float chargeSpeed = 5.5f;
        [SerializeField, Min(0.1f)] private float maximumChargeDistance = 3.6f;
        [SerializeField, Min(0f)] private float chargeDamage = 11f;
        [SerializeField, Min(0.1f)] private float chargeCooldownSeconds = 4f;
        [SerializeField, Min(0.01f)] private float chargeRecoverySeconds = 0.65f;
        [SerializeField, Min(0.01f)] private float shipContactRadius = 0.35f;

        [Header("Presentation")]
        [SerializeField, Min(0f)] private float bobAmplitude = 0.035f;
        [SerializeField, Min(0.01f)] private float deathSeconds = 0.28f;
        [SerializeField] private Color warningColor = new Color(1f, 0.38f, 0.15f, 0.4f);

        private IReadOnlyList<SeaCreatureTarget> fleet;
        private IReadOnlyList<SeaCreatureEnemy> creatures;
        private readonly HashSet<Ship> chargeVictims = new HashSet<Ship>();
        private SeaCreatureTarget target;
        private Vector2 verticalBounds;
        private Vector3 visualBasePosition;
        private Vector3 visualBaseScale;
        private Vector3 chargeDirection;
        private float chargeDistance;
        private float chargeTravelled;
        private float stateRemaining;
        private float biteCooldown;
        private float chargeCooldown;
        private float staggerRemaining;
        private float targetRefreshRemaining;
        private float swimPhase;
        private float deathElapsed;
        private bool encounterActive;
        private Vector3 encounterPosition;
        private Vector3 previousPosition;
        private bool biteApplied;
        private float hitPulse;
        private float entryDestinationX;
        private float entrySwimSpeed;

        public SeaCreatureKind Kind => kind;
        public SeaCreatureState State { get; private set; }
        public float BodyRadius => bodyRadius;
        public float DeathSeconds => deathSeconds;
        public int ContactHitCount { get; private set; }
        public int ChargeCount { get; private set; }
        public bool IsEngaged { get; private set; }
        public float VisualHalfWidth => bodyRenderer != null && bodyRenderer.sprite != null
            ? bodyRenderer.sprite.bounds.extents.x * Mathf.Abs(bodyRenderer.transform.lossyScale.x) : bodyRadius;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            if (visual != null)
            {
                visualBasePosition = visual.localPosition;
                visualBaseScale = visual.localScale;
            }
            OnHitEvent.AddListener(HandleHit);
        }

        public override void OnSpawnedFromPool()
        {
            base.OnSpawnedFromPool();
            _shipStatCompo.SetBaseValue(_shipStatCompo.GetCommonStatSO(ShipCommonStatType.Health), maxHealth);
            _healthCompo.ResetHealth();
            State = SeaCreatureState.Cruising;
            target = default;
            encounterActive = false;
            IsEngaged = false;
            biteCooldown = 0f;
            chargeCooldown = 0f;
            staggerRemaining = 0f;
            targetRefreshRemaining = 0f;
            deathElapsed = 0f;
            hitPulse = 0f;
            entryDestinationX = 0f;
            entrySwimSpeed = 0f;
            swimPhase = Random.Range(0f, Mathf.PI * 2f);
            ContactHitCount = 0;
            ChargeCount = 0;
            chargeVictims.Clear();
            if (bodyRenderer != null) bodyRenderer.color = Color.white;
            if (chargeWarning != null) chargeWarning.enabled = false;
            ResetVisual();
            surfacePresentation?.ResetPresentation(Vector2.left);
        }

        internal void BeginEncounter(IReadOnlyList<SeaCreatureTarget> playerFleet,
            IReadOnlyList<SeaCreatureEnemy> reservedCreatures, Vector2 yBounds, bool engageImmediately = false)
        {
            fleet = playerFleet;
            creatures = reservedCreatures;
            verticalBounds = yBounds;
            encounterActive = true;
            encounterPosition = transform.position;
            previousPosition = transform.position;
            if (engageImmediately) Engage();
            UpdateVisual();
        }

        public void Engage()
        {
            if (IsDead || !encounterActive) return;
            IsEngaged = true;
            target = SelectTarget();
            State = SeaCreatureState.Swimming;
            targetRefreshRemaining = 0f;
        }

        public void BeginReinforcementEntry(float viewportRightX, float speed)
        {
            if (IsDead || !encounterActive) return;
            IsEngaged = false;
            State = SeaCreatureState.Entering;
            entryDestinationX = viewportRightX - VisualHalfWidth;
            entrySwimSpeed = Mathf.Max(0.1f, speed);
            UpdateVisual();
        }

        public void BeginDying()
        {
            State = SeaCreatureState.Dying;
            deathElapsed = 0f;
            if (chargeWarning != null) chargeWarning.enabled = false;
        }

        protected override void Update()
        {
            if (!encounterActive)
                return;

            float delta = Time.deltaTime;
            if (IsDead)
            {
                deathElapsed += delta;
                float fade = Mathf.Clamp01(deathElapsed / deathSeconds);
                if (surfacePresentation != null)
                {
                    surfacePresentation.TickPresentation(State, delta, swimPhase,
                        Vector2.zero, Vector2.left, fade);
                }
                else if (visual != null)
                {
                    visual.localPosition = visualBasePosition + Vector3.down * (fade * 0.18f);
                    visual.localScale = visualBaseScale * (1f - fade * 0.35f);
                }
                if (surfacePresentation == null && bodyRenderer != null) bodyRenderer.color = new Color(1f, 1f, 1f, 1f - fade);
                return;
            }

            biteCooldown -= delta;
            chargeCooldown -= delta;
            staggerRemaining -= delta;
            targetRefreshRemaining -= delta;
            if (State != SeaCreatureState.WindingUp && State != SeaCreatureState.Charging && State != SeaCreatureState.Biting &&
                (!target.IsAlive || targetRefreshRemaining <= 0f))
            {
                target = SelectTarget();
                targetRefreshRemaining = 0.3f;
            }

            switch (State)
            {
                case SeaCreatureState.Entering:
                    Vector3 entryPosition = transform.position;
                    entryPosition.x = Mathf.MoveTowards(entryPosition.x, entryDestinationX, entrySwimSpeed * delta);
                    transform.position = entryPosition;
                    if (Mathf.Approximately(entryPosition.x, entryDestinationX)) Engage();
                    break;
                case SeaCreatureState.Cruising:
                    transform.position = encounterPosition + Vector3.up * (Mathf.Sin(Time.time + swimPhase) * idleDriftAmplitude);
                    break;
                case SeaCreatureState.WindingUp:
                    stateRemaining -= delta;
                    UpdateWarning();
                    if (stateRemaining <= 0f)
                    {
                        State = SeaCreatureState.Charging;
                        if (chargeWarning != null) chargeWarning.enabled = false;
                    }
                    break;
                case SeaCreatureState.Charging:
                    AdvanceCharge(delta);
                    break;
                case SeaCreatureState.Biting:
                    stateRemaining -= delta;
                    if (!biteApplied && 1f - stateRemaining / biteDurationSeconds >= biteHitProgress)
                    {
                        biteApplied = true;
                        if (target.IsAlive && IsExposed(target) &&
                            (target.Ship.transform.position - transform.position).sqrMagnitude <= Mathf.Pow(biteRange + shipContactRadius, 2f))
                        {
                            ApplyContactDamage(target, biteDamage);
                            ContactHitCount++;
                        }
                    }
                    if (stateRemaining <= 0f) State = SeaCreatureState.Swimming;
                    break;
                case SeaCreatureState.Recovering:
                    stateRemaining -= delta;
                    if (stateRemaining <= 0f) State = SeaCreatureState.Swimming;
                    break;
                default:
                    if (target.IsAlive && staggerRemaining <= 0f) SwimOrAttack(delta);
                    break;
            }
            UpdateVisual();
        }

        private SeaCreatureTarget SelectTarget()
        {
            SeaCreatureTarget selected = default;
            float nearest = float.PositiveInfinity;
            bool hasLivingEscort = false;
            foreach (SeaCreatureTarget candidate in fleet)
                hasLivingEscort |= candidate.IsAlive && candidate.Ship.myShipData.ShipType != ShipType.MainShip;
            foreach (SeaCreatureTarget candidate in fleet)
            {
                if (!candidate.IsAlive || candidate.Ship.IsDirectTargetProtected || !IsExposed(candidate)) continue;
                bool isEscort = candidate.Ship.myShipData.ShipType != ShipType.MainShip;
                if (hasLivingEscort && !isEscort) continue;
                float distance = (candidate.Ship.transform.position - transform.position).sqrMagnitude;
                if (distance < nearest)
                {
                    selected = candidate;
                    nearest = distance;
                }
            }
            return selected;
        }

        private void SwimOrAttack(float delta)
        {
            Vector3 toTarget = target.Ship.transform.position - transform.position;
            toTarget.z = 0f;
            float distance = toTarget.magnitude;
            if (kind == SeaCreatureKind.Charger && chargeCooldown <= 0f && distance <= chargeStartRange)
            {
                chargeDirection = distance > 0.01f ? toTarget / distance : Vector3.left;
                chargeDistance = Mathf.Min(maximumChargeDistance, distance + bodyRadius + shipContactRadius);
                chargeTravelled = 0f;
                stateRemaining = chargeWindupSeconds;
                chargeVictims.Clear();
                State = SeaCreatureState.WindingUp;
                ChargeCount++;
                UpdateWarning();
                return;
            }
            if (distance <= biteRange)
            {
                Vector3 spacing = SeparationVelocity();
                transform.position += spacing * delta;
                if (biteCooldown <= 0f)
                {
                    biteCooldown = biteIntervalSeconds;
                    State = SeaCreatureState.Biting;
                    stateRemaining = biteDurationSeconds;
                    biteApplied = false;
                }
                return;
            }

            Vector3 direction = toTarget.normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
            float weave = Mathf.Sin(Time.time * swimCyclesPerSecond * Mathf.PI * 2f + swimPhase) * swimWeave;
            Vector3 velocity = direction * swimSpeed + perpendicular * weave + SeparationVelocity();
            Vector3 next = transform.position + velocity * delta;
            next.y = Mathf.Clamp(next.y, verticalBounds.x, verticalBounds.y);
            transform.position = next;
        }

        private Vector3 SeparationVelocity()
        {
            Vector3 separation = Vector3.zero;
            foreach (SeaCreatureEnemy other in creatures)
            {
                if (other == this || other == null || !other.isActiveAndEnabled || other.IsDead) continue;
                Vector3 offset = transform.position - other.transform.position;
                float distance = offset.magnitude;
                float spacing = bodyRadius + other.bodyRadius;
                if (distance <= 0.001f || distance >= spacing) continue;
                separation += offset / distance * (1f - distance / spacing);
            }
            return Vector3.ClampMagnitude(separation, 1f) * separationStrength;
        }

        private void AdvanceCharge(float delta)
        {
            Vector3 previous = transform.position;
            float step = Mathf.Min(chargeSpeed * delta, chargeDistance - chargeTravelled);
            transform.position += chargeDirection * step;
            chargeTravelled += step;
            SeaCreatureTarget firstHit = default;
            float firstProgress = float.PositiveInfinity;
            foreach (SeaCreatureTarget candidate in fleet)
            {
                if (!candidate.IsAlive || chargeVictims.Contains(candidate.Ship)) continue;
                Vector3 segment = transform.position - previous;
                float progress = segment.sqrMagnitude > 0f
                    ? Mathf.Clamp01(Vector3.Dot(candidate.Ship.transform.position - previous, segment) / segment.sqrMagnitude) : 0f;
                float radius = bodyRadius + shipContactRadius;
                float missSquared = (previous + segment * progress - candidate.Ship.transform.position).sqrMagnitude;
                if (missSquared > radius * radius) continue;
                float entry = segment.magnitude > 0.001f
                    ? Mathf.Clamp01(progress - Mathf.Sqrt(radius * radius - missSquared) / segment.magnitude) : 0f;
                if (entry < firstProgress) { firstProgress = entry; firstHit = candidate; }
            }
            if (firstHit.IsAlive)
            {
                transform.position = Vector3.Lerp(previous, transform.position, firstProgress);
                chargeVictims.Add(firstHit.Ship);
                ApplyContactDamage(firstHit, chargeDamage);
                ContactHitCount++;
                chargeTravelled = chargeDistance;
            }
            if (chargeTravelled >= chargeDistance)
            {
                State = SeaCreatureState.Recovering;
                stateRemaining = chargeRecoverySeconds;
                chargeCooldown = chargeCooldownSeconds;
                encounterPosition = transform.position;
            }
        }

        private void UpdateWarning()
        {
            if (chargeWarning == null) return;
            chargeWarning.enabled = true;
            chargeWarning.transform.position = transform.position + chargeDirection * (chargeDistance * 0.5f);
            chargeWarning.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg);
            Vector3 spriteSize = chargeWarning.sprite.bounds.size;
            chargeWarning.transform.localScale = new Vector3(chargeDistance / spriteSize.x,
                bodyRadius * 1.3f / spriteSize.y, 1f);
            Color tint = warningColor;
            tint.a *= 0.65f + Mathf.Sin(Time.time * 20f) * 0.25f;
            chargeWarning.color = tint;
        }

        private void UpdateVisual()
        {
            Vector3 facing = State == SeaCreatureState.Cruising || State == SeaCreatureState.Entering ? Vector3.left
                : State == SeaCreatureState.Charging || State == SeaCreatureState.WindingUp
                ? chargeDirection : target.IsAlive ? target.Ship.transform.position - transform.position : Vector3.left;
            if (surfacePresentation != null)
            {
                float progress = State == SeaCreatureState.WindingUp ? 1f - stateRemaining / chargeWindupSeconds
                    : State == SeaCreatureState.Biting ? 1f - stateRemaining / biteDurationSeconds
                    : State == SeaCreatureState.Recovering ? 1f - stateRemaining / chargeRecoverySeconds : 0f;
                Vector2 velocity = Time.deltaTime > 0f ? (Vector2)(transform.position - previousPosition) / Time.deltaTime : Vector2.zero;
                surfacePresentation.TickPresentation(State, Time.deltaTime, swimPhase,
                    velocity, facing, Mathf.Clamp01(progress), hitPulse);
                previousPosition = transform.position;
                hitPulse = 0f;
                return;
            }
            if (visual == null) return;
            Vector3 offset = Vector3.up * (Mathf.Sin(Time.time * swimCyclesPerSecond * Mathf.PI * 2f + swimPhase) * bobAmplitude);
            if (State == SeaCreatureState.Biting && target.IsAlive)
                offset += (target.Ship.transform.position - transform.position).normalized * (stateRemaining * 0.5f);
            visual.localPosition = visualBasePosition + offset;
            float squash = State == SeaCreatureState.WindingUp ? 0.8f : 1f;
            visual.localScale = Vector3.Scale(visualBaseScale, new Vector3(1f / squash, squash, 1f));
            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = artFacesRight ? facing.x < 0f : facing.x > 0f;
            }
        }

        private void ApplyContactDamage(SeaCreatureTarget victim, float damage)
        {
            if (victim.Ship is MainShip mainShip && mainShip.Destruction != null)
            {
                foreach (SeaCreatureTarget escort in fleet)
                    if (escort.IsAlive && escort.Ship.myShipData.ShipType != ShipType.MainShip)
                        return;
                mainShip.Destruction.PlayImpact(mainShip.transform.position);
                mainShip.Destruction.PlayDefeatShake();
                mainShip.Destruction.CompleteDefeat();
                return;
            }
            victim.Health?.ApplyDamage(damage, victim.Ship.transform.position);
        }

        private void HandleHit()
        {
            if (!encounterActive || IsDead || State == SeaCreatureState.Charging) return;
            staggerRemaining = hitStunSeconds;
            hitPulse = 1f;
            Vector3 away = target.IsAlive ? (transform.position - target.Ship.transform.position).normalized : Vector3.right;
            transform.position += away * hitKnockback;
        }

        private bool IsExposed(SeaCreatureTarget candidate)
        {
            Vector2 from = transform.position;
            Vector2 segment = (Vector2)candidate.Ship.transform.position - from;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.001f) return true;
            foreach (SeaCreatureTarget blocker in fleet)
            {
                if (!blocker.IsAlive || blocker.Ship == candidate.Ship) continue;
                Vector2 offset = (Vector2)blocker.Ship.transform.position - from;
                float progress = Vector2.Dot(offset, segment) / lengthSquared;
                if (progress <= 0f || progress >= 1f) continue;
                if ((offset - segment * progress).sqrMagnitude <= shipContactRadius * shipContactRadius) return false;
            }
            return true;
        }

        private void ResetVisual()
        {
            if (visual == null) return;
            visual.localPosition = visualBasePosition;
            visual.localScale = visualBaseScale;
        }

        public override void OnDespawnedToPool()
        {
            base.OnDespawnedToPool();
            encounterActive = false;
            IsEngaged = false;
            entryDestinationX = 0f;
            entrySwimSpeed = 0f;
            fleet = null;
            creatures = null;
            if (chargeWarning != null) chargeWarning.enabled = false;
            ResetVisual();
            surfacePresentation?.ResetPresentation(Vector2.left);
        }
    }
}
