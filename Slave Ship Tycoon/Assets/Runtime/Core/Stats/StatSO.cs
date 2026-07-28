using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Core
{
    [CreateAssetMenu(fileName = "Stat", menuName = "SB/Core/Stat")]
    public class StatSO : ScriptableObject
    {
        public delegate void ValueChangeHandler(StatSO stat, float currentValue, float previousValue);
        public event ValueChangeHandler OnValueChanged;

        [SerializeField] private string statName;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private float baseValue;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = 999999f;
        [SerializeField] private float incrementStep = 1f;
        [SerializeField] private bool isPercent;

        private readonly Dictionary<object, float> modifiersByKey = new();
        private float modifiedValue;

        public string StatName => statName;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? statName : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float IncrementStep => incrementStep;
        public bool IsPercent => isPercent;
        public float Value => Mathf.Clamp(baseValue + modifiedValue, minValue, maxValue);
        public bool IsMax => Mathf.Approximately(Value, maxValue);
        public bool IsMin => Mathf.Approximately(Value, minValue);
        public bool CanIncrementStep => BaseValue + incrementStep <= maxValue;

        public float BaseValue
        {
            get => baseValue;
            set
            {
                float previousValue = Value;
                baseValue = Mathf.Clamp(value, minValue, maxValue);
                InvokeValueChangedIfNeeded(previousValue);
            }
        }

        public float MinValue
        {
            get => minValue;
            set
            {
                float previousValue = Value;
                minValue = value;
                baseValue = Mathf.Clamp(baseValue, minValue, maxValue);
                InvokeValueChangedIfNeeded(previousValue);
            }
        }

        public float MaxValue
        {
            get => maxValue;
            set
            {
                float previousValue = Value;
                maxValue = value;
                baseValue = Mathf.Clamp(baseValue, minValue, maxValue);
                InvokeValueChangedIfNeeded(previousValue);
            }
        }

        public void AddModifier(object key, float value)
        {
            if (key == null || modifiersByKey.ContainsKey(key))
                return;

            float previousValue = Value;
            modifiersByKey.Add(key, value);
            modifiedValue += value;
            InvokeValueChangedIfNeeded(previousValue);
        }

        public void RemoveModifier(object key)
        {
            if (key == null || !modifiersByKey.TryGetValue(key, out float value))
                return;

            float previousValue = Value;
            modifiersByKey.Remove(key);
            modifiedValue -= value;
            InvokeValueChangedIfNeeded(previousValue);
        }

        public void ClearModifiers()
        {
            if (modifiersByKey.Count == 0 && Mathf.Approximately(modifiedValue, 0f))
                return;

            float previousValue = Value;
            modifiersByKey.Clear();
            modifiedValue = 0f;
            InvokeValueChangedIfNeeded(previousValue);
        }

        public StatSO Clone()
        {
            StatSO clone = Instantiate(this);
            clone.modifiersByKey.Clear();
            clone.modifiedValue = 0f;
            clone.OnValueChanged = null;
            return clone;
        }

        private void OnValidate()
        {
            if (maxValue < minValue)
                maxValue = minValue;

            baseValue = Mathf.Clamp(baseValue, minValue, maxValue);
            incrementStep = Mathf.Max(0f, incrementStep);
        }

        private void InvokeValueChangedIfNeeded(float previousValue)
        {
            float currentValue = Value;
            if (!Mathf.Approximately(currentValue, previousValue))
                OnValueChanged?.Invoke(this, currentValue, previousValue);
        }
    }
}
