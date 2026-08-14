using SB.Core.EventBus;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.Scripts.Fleet
{
    [DisallowMultipleComponent]
    public sealed class EscortGachaManager : MonoBehaviour
    {
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private PlayerFleetManager _playerFleetManager;
        [SerializeField] private EscortGachaTable _gachaTable;
        [SerializeField] private CurrencyType _currencyType = CurrencyType.Diamond;
        [SerializeField, Min(1)] private int _summonCost = 1;

        public CurrencyType CurrencyType => _currencyType;
        public int SummonCost => _summonCost;

        public bool CanSummon(out EscortGachaFailureReason failureReason)
        {
            if (_currencyManager == null)
            {
                failureReason = EscortGachaFailureReason.MissingCurrencyManager;
                return false;
            }

            if (_playerFleetManager == null)
            {
                failureReason = EscortGachaFailureReason.MissingFleetManager;
                return false;
            }

            if (_gachaTable == null)
            {
                failureReason = EscortGachaFailureReason.MissingGachaTable;
                return false;
            }

            if (_summonCost <= 0)
            {
                failureReason = EscortGachaFailureReason.InvalidCost;
                return false;
            }

            if (_gachaTable.TryValidate(out _) == false)
            {
                failureReason = EscortGachaFailureReason.InvalidGachaTable;
                return false;
            }

            for (int i = 0; i < _gachaTable.EntryCount; i++)
            {
                EscortShipData reward = _gachaTable.GetEntryAt(i).ShipData;

                if (_playerFleetManager.CanGrantEscort(reward) == false)
                {
                    failureReason = EscortGachaFailureReason.InvalidReward;
                    return false;
                }
            }

            if (_currencyManager.GetCurrency(_currencyType) < _summonCost)
            {
                failureReason = EscortGachaFailureReason.InsufficientCurrency;
                return false;
            }

            failureReason = EscortGachaFailureReason.None;
            return true;
        }

        public bool TrySummon(out EscortGachaResult result)
        {
            if (CanSummon(out EscortGachaFailureReason failureReason) == false)
            {
                result = CreateFailedResult(failureReason, null);
                RaiseResult(result);
                return false;
            }

            if (_gachaTable.TryRoll(out EscortShipData reward) == false)
            {
                result = CreateFailedResult(EscortGachaFailureReason.InvalidGachaTable, null);
                RaiseResult(result);
                return false;
            }

            if (_playerFleetManager.CanGrantEscort(reward) == false)
            {
                result = CreateFailedResult(EscortGachaFailureReason.InvalidReward, reward);
                RaiseResult(result);
                return false;
            }

            if (_currencyManager.TrySpendCurrency(_currencyType, _summonCost) == false)
            {
                result = CreateFailedResult(EscortGachaFailureReason.InsufficientCurrency, reward);
                RaiseResult(result);
                return false;
            }

            if (_playerFleetManager.TryGrantEscort(reward) == false)
            {
                _currencyManager.AddCurrency(_currencyType, _summonCost);
                result = CreateFailedResult(EscortGachaFailureReason.GrantFailed, reward);
                RaiseResult(result);
                return false;
            }

            result = new EscortGachaResult(
                true,
                EscortGachaFailureReason.None,
                reward,
                _currencyType,
                _summonCost,
                _playerFleetManager.GetOwnedCount(reward));
            RaiseResult(result);
            return true;
        }

        private EscortGachaResult CreateFailedResult(
            EscortGachaFailureReason failureReason,
            EscortShipData reward)
        {
            return new EscortGachaResult(
                false,
                failureReason,
                reward,
                _currencyType,
                _summonCost,
                reward != null && _playerFleetManager != null
                    ? _playerFleetManager.GetOwnedCount(reward)
                    : 0);
        }

        private static void RaiseResult(EscortGachaResult result)
        {
            Bus<EscortGachaResultEvent>.Raise(new EscortGachaResultEvent(result));
        }
    }
}
