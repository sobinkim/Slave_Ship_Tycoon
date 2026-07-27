using System.Collections.Generic;
using UnityEngine;

namespace SB.Core
{
    [DisallowMultipleComponent]
    public class EntityStatCompo : EntityComponent
    {
        [SerializeField] private StatContainer statContainer;

        private readonly Dictionary<string, StatSO> stats = new();

        public IReadOnlyDictionary<string, StatSO> Stats => stats;

        public override void Initialize(Entity entity)
        {
            base.Initialize(entity);
            ResetStats();
        }

        public void SetStatContainer(StatContainer container, bool resetImmediately = true)
        {
            statContainer = container;

            if (resetImmediately)
                ResetStats();
        }

        public void ResetStats()
        {
            stats.Clear();

            if (statContainer.StatOverrides == null)
                return;

            foreach (StatOverride statOverride in statContainer.StatOverrides)
            {
                if (statOverride == null || statOverride.Stat == null)
                    continue;

                StatSO stat = statOverride.CreateStat();
                if (stat == null || string.IsNullOrWhiteSpace(stat.StatName))
                    continue;

                stats[stat.StatName] = stat;
            }
        }

        public StatSO GetStat(string statName)
        {
            if (TryGetStat(statName, out StatSO stat))
                return stat;

            Debug.LogError($"{Owner.name} does not have stat: {statName}.", Owner);
            return null;
        }

        public StatSO GetStat(StatSO stat)
        {
            if (stat == null)
            {
                Debug.LogError("Stat cannot be null.", this);
                return null;
            }

            return GetStat(stat.StatName);
        }

        public bool TryGetStat(string statName, out StatSO stat)
        {
            if (string.IsNullOrWhiteSpace(statName))
            {
                stat = null;
                return false;
            }

            return stats.TryGetValue(statName, out stat);
        }

        public bool TryGetStat(StatSO stat, out StatSO outStat)
        {
            if (stat == null)
            {
                outStat = null;
                return false;
            }

            return TryGetStat(stat.StatName, out outStat);
        }

        public float GetValue(string statName, float defaultValue = 0f)
        {
            return TryGetStat(statName, out StatSO stat) ? stat.Value : defaultValue;
        }

        public float GetValue(StatSO stat, float defaultValue = 0f)
        {
            return TryGetStat(stat, out StatSO targetStat) ? targetStat.Value : defaultValue;
        }

        public void SetBaseValue(StatSO stat, float value)
        {
            StatSO targetStat = GetStat(stat);
            if (targetStat != null)
                targetStat.BaseValue = value;
        }

        public void SetBaseValue(string statName, float value)
        {
            StatSO targetStat = GetStat(statName);
            if (targetStat != null)
                targetStat.BaseValue = value;
        }

        public float GetBaseValue(StatSO stat, float defaultValue = 0f)
        {
            return TryGetStat(stat, out StatSO targetStat) ? targetStat.BaseValue : defaultValue;
        }

        public float GetBaseValue(string statName, float defaultValue = 0f)
        {
            return TryGetStat(statName, out StatSO stat) ? stat.BaseValue : defaultValue;
        }

        public void IncreaseBaseValue(StatSO stat, float value)
        {
            StatSO targetStat = GetStat(stat);
            if (targetStat != null)
                targetStat.BaseValue += value;
        }

        public void AddModifier(StatSO stat, object key, float value)
        {
            StatSO targetStat = GetStat(stat);
            targetStat?.AddModifier(key, value);
        }

        public void AddModifier(string statName, object key, float value)
        {
            StatSO targetStat = GetStat(statName);
            targetStat?.AddModifier(key, value);
        }

        public void RemoveModifier(StatSO stat, object key)
        {
            StatSO targetStat = GetStat(stat);
            targetStat?.RemoveModifier(key);
        }

        public void RemoveModifier(string statName, object key)
        {
            StatSO targetStat = GetStat(statName);
            targetStat?.RemoveModifier(key);
        }

        public void ClearAllStatModifiers()
        {
            foreach (StatSO stat in stats.Values)
                stat.ClearModifiers();
        }

        public float SubscribeStat(StatSO stat, StatSO.ValueChangeHandler handler, float defaultValue = 0f)
        {
            if (!TryGetStat(stat, out StatSO targetStat))
                return defaultValue;

            targetStat.OnValueChanged += handler;
            return targetStat.Value;
        }

        public void UnsubscribeStat(StatSO stat, StatSO.ValueChangeHandler handler)
        {
            if (TryGetStat(stat, out StatSO targetStat))
                targetStat.OnValueChanged -= handler;
        }
    }
}
