using System;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.Scripts.TransportEquipment
{
    public enum TransportSettlementModifierType
    {
        FlatReward,
        RewardMultiplier
    }

    [Serializable]
    public sealed class TransportSettlementModifier
    {
        [SerializeField] private CurrencyType _currencyType;
        [SerializeField] private TransportSettlementModifierType _modifierType;
        [SerializeField, Min(0)] private long _flatAmount;
        [SerializeField, Min(0.01f)] private float _multiplier = 1f;

        public CurrencyType CurrencyType => _currencyType;
        public TransportSettlementModifierType ModifierType => _modifierType;
        public long FlatAmount => _flatAmount;
        public float Multiplier => _multiplier;

        internal bool TryCreateSnapshotValue(out TransportSettlementModifierValue value)
        {
            value = default;

            switch (_modifierType)
            {
                case TransportSettlementModifierType.FlatReward:
                    if (_flatAmount < 0)
                        return false;

                    value = new TransportSettlementModifierValue(
                        _currencyType,
                        _modifierType,
                        _flatAmount,
                        1f);
                    return true;

                case TransportSettlementModifierType.RewardMultiplier:
                    if (_multiplier <= 0f || float.IsNaN(_multiplier) || float.IsInfinity(_multiplier))
                        return false;

                    value = new TransportSettlementModifierValue(
                        _currencyType,
                        _modifierType,
                        0,
                        _multiplier);
                    return true;

                default:
                    return false;
            }
        }

        internal void ValidateSerializedValues()
        {
            _flatAmount = Math.Max(0, _flatAmount);

            if (_multiplier <= 0f || float.IsNaN(_multiplier) || float.IsInfinity(_multiplier))
                _multiplier = 1f;
        }
    }

    public readonly struct TransportSettlementModifierValue
    {
        public readonly CurrencyType CurrencyType;
        public readonly TransportSettlementModifierType ModifierType;
        public readonly long FlatAmount;
        public readonly float Multiplier;

        internal TransportSettlementModifierValue(
            CurrencyType currencyType,
            TransportSettlementModifierType modifierType,
            long flatAmount,
            float multiplier)
        {
            CurrencyType = currencyType;
            ModifierType = modifierType;
            FlatAmount = flatAmount;
            Multiplier = multiplier;
        }
    }

    internal readonly struct TransportSettlementModifierKey : IEquatable<TransportSettlementModifierKey>
    {
        private readonly CurrencyType _currencyType;
        private readonly TransportSettlementModifierType _modifierType;

        public TransportSettlementModifierKey(
            CurrencyType currencyType,
            TransportSettlementModifierType modifierType)
        {
            _currencyType = currencyType;
            _modifierType = modifierType;
        }

        public bool Equals(TransportSettlementModifierKey other)
        {
            return _currencyType == other._currencyType && _modifierType == other._modifierType;
        }

        public override bool Equals(object obj)
        {
            return obj is TransportSettlementModifierKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)_currencyType * 397) ^ (int)_modifierType;
        }
    }
}
