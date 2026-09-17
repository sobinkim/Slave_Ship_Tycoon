using System;
using System.Collections.Generic;
using SB.Core;
using UnityEngine;

namespace SB.Scripts.Fleet.Effects
{
    [DisallowMultipleComponent]
    public sealed class FleetEffectReceiver : MonoBehaviour, IEntityComponent, IIncomingDamageModifier
    {
        [Serializable]
        public struct Presentation
        {
            public FleetEffectDefinition Effect;
            public FleetEffectVisual Visual;
        }
        [SerializeField] private Ship owner;
        [SerializeField] private Presentation[] presentations = Array.Empty<Presentation>();
        private sealed class Application
        {
            public FleetEffectProvider Source;
            public FleetEffectDefinition Effect;
            public int Generation;
            public bool IsSourceVisual;
        }
        private readonly List<Application> applications = new List<Application>();
        public Ship Owner => owner;
        public int ActiveApplicationCount => applications.Count;
        public event Action EffectsChanged;

        public void CollectReceivedEffects(List<FleetEffectDefinition> effects)
        {
            effects.Clear();
            foreach (Application entry in applications)
            {
                if (entry.IsSourceVisual || !IsActive(entry) || effects.Contains(entry.Effect)) continue;
                effects.Add(entry.Effect);
            }
        }
        public bool BlocksDirectTargeting
        {
            get
            {
                foreach (Application entry in applications)
                    if (!entry.IsSourceVisual && entry.Effect.BlocksDirectTargeting && IsActive(entry)) return true;
                return false;
            }
        }
        public float IncomingDamageMultiplier
        {
            get
            {
                float strongestMultiplier = 1f;
                foreach (Application entry in applications)
                {
                    if (entry.IsSourceVisual || !IsActive(entry))
                        continue;

                    strongestMultiplier = Mathf.Min(strongestMultiplier,
                        Mathf.Clamp01(entry.Effect.IncomingDamageMultiplier));
                }

                return strongestMultiplier;
            }
        }
        public void Initialize(Entity entity) { owner = (Ship)entity; }
        public float ModifyIncomingDamage(float incomingDamage)
        {
            return incomingDamage * IncomingDamageMultiplier;
        }
        private void OnEnable()
        {
            if (owner == null) { Debug.LogError("Effect receiver requires an explicit owner.", this); return; }
            owner.OnDeathEvent.AddListener(HandleDeath);
            owner.OnHitEvent.AddListener(HandleHit);
        }
        private void OnDisable()
        {
            if (owner != null)
            {
                owner.OnDeathEvent.RemoveListener(HandleDeath);
                owner.OnHitEvent.RemoveListener(HandleHit);
            }
            Clear(owner != null && owner.IsDead);
        }
        public void Add(FleetEffectProvider source, FleetEffectDefinition effect, bool isSourceVisual)
        {
            if (owner == null || owner.IsDead || !isActiveAndEnabled) return;
            foreach (Application entry in applications)
                if (entry.Source == source && entry.Effect == effect && entry.IsSourceVisual == isSourceVisual) return;
            applications.Add(new Application { Source = source, Effect = effect,
                Generation = source.Owner.SpawnGeneration, IsSourceVisual = isSourceVisual });
            if (!isSourceVisual) effect.OnApplied(this, source);
            RefreshPresentation(effect, false);
            EffectsChanged?.Invoke();
        }
        public void Remove(FleetEffectProvider source, FleetEffectDefinition effect, bool shouldBreak)
        {
            bool removed = false;
            for (int i = applications.Count - 1; i >= 0; i--)
            {
                Application entry = applications[i];
                if (entry.Source != source || entry.Effect != effect) continue;
                applications.RemoveAt(i);
                if (!entry.IsSourceVisual) effect.OnRemoved(this, source);
                removed = true;
            }
            if (removed)
            {
                RefreshPresentation(effect, shouldBreak);
                EffectsChanged?.Invoke();
            }
        }
        private void RefreshPresentation(FleetEffectDefinition effect, bool shouldBreak)
        {
            bool hasEffect = false, isSource = false;
            foreach (Application entry in applications)
                if (entry.Effect == effect) { hasEffect = true; isSource |= entry.IsSourceVisual; }
            foreach (Presentation entry in presentations)
            {
                if (entry.Effect != effect || entry.Visual == null) continue;
                if (hasEffect) entry.Visual.Show(isSource);
                else if (shouldBreak) entry.Visual.PlayBreak();
                else entry.Visual.Hide();
            }
        }
        private void HandleDeath() { Clear(true); }
        private void HandleHit()
        {
            foreach (Presentation entry in presentations)
                if (entry.Visual != null) entry.Visual.PlayHit();
        }
        private void Clear(bool shouldBreak)
        {
            while (applications.Count > 0)
            {
                Application entry = applications[applications.Count - 1];
                Remove(entry.Source, entry.Effect, shouldBreak);
            }
        }
        private static bool IsActive(Application entry)
        {
            return entry.Source != null && entry.Source.IsSupplying && entry.Source.Owner != null &&
                   entry.Source.Owner.SpawnGeneration == entry.Generation;
        }
    }
}
