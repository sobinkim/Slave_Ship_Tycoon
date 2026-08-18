using SB.Scripts.Currency;
using SB.Scripts.Fleet;

namespace SB.Scripts.Fleet
{
    public enum EscortGachaFailureReason
    {
        None,
        MissingCurrencyManager,
        MissingFleetManager,
        MissingGachaTable,
        InvalidCost,
        InvalidGachaTable,
        InvalidReward,
        InsufficientCurrency,
        GrantFailed
    }

    public enum EscortMergeFailureReason
    {
        None,
        InvalidSource,
        MissingCatalog,
        SourceNotOwned,
        NotEnoughUnequippedCopies,
        HighestGrade,
        NoNextGradeCandidate,
        InvalidResult
    }

    public enum EscortFormationFailureReason
    {
        None,
        InvalidSlot,
        InvalidEscort,
        EscortNotOwned,
        NotEnoughCopies
    }

    public readonly struct EscortOwnershipSnapshot
    {
        public readonly EscortShipData ShipData;
        public readonly int OwnedCount;
        public readonly int EquippedCount;
        public readonly int UnequippedCount;

        public EscortOwnershipSnapshot(
            EscortShipData shipData,
            int ownedCount,
            int equippedCount)
        {
            ShipData = shipData;
            OwnedCount = ownedCount;
            EquippedCount = equippedCount;
            UnequippedCount = ownedCount - equippedCount;
        }
    }

    public readonly struct EscortGachaResult
    {
        public readonly bool IsSuccess;
        public readonly EscortGachaFailureReason FailureReason;
        public readonly EscortShipData Reward;
        public readonly CurrencyType CurrencyType;
        public readonly int Cost;
        public readonly int RewardOwnedCount;

        public EscortGachaResult(
            bool isSuccess,
            EscortGachaFailureReason failureReason,
            EscortShipData reward,
            CurrencyType currencyType,
            int cost,
            int rewardOwnedCount)
        {
            IsSuccess = isSuccess;
            FailureReason = failureReason;
            Reward = reward;
            CurrencyType = currencyType;
            Cost = cost;
            RewardOwnedCount = rewardOwnedCount;
        }
    }

    public readonly struct EscortMergeResult
    {
        public readonly bool IsSuccess;
        public readonly EscortMergeFailureReason FailureReason;
        public readonly EscortShipData Source;
        public readonly EscortShipData Result;
        public readonly int ConsumedAmount;
        public readonly int ResultOwnedCount;

        public EscortMergeResult(
            bool isSuccess,
            EscortMergeFailureReason failureReason,
            EscortShipData source,
            EscortShipData result,
            int consumedAmount,
            int resultOwnedCount)
        {
            IsSuccess = isSuccess;
            FailureReason = failureReason;
            Source = source;
            Result = result;
            ConsumedAmount = consumedAmount;
            ResultOwnedCount = resultOwnedCount;
        }
    }
}

namespace SB.Core.EventBus
{
    public readonly struct EscortOwnershipChangedEvent : IEvent
    {
        public readonly EscortOwnershipSnapshot Ownership;

        public EscortOwnershipChangedEvent(EscortOwnershipSnapshot ownership)
        {
            Ownership = ownership;
        }
    }

    public readonly struct PlayerFleetFormationChangedEvent : IEvent
    {
        public readonly int SlotIndex;
        public readonly EscortShipData PreviousEscort;
        public readonly EscortShipData CurrentEscort;

        public PlayerFleetFormationChangedEvent(
            int slotIndex,
            EscortShipData previousEscort,
            EscortShipData currentEscort)
        {
            SlotIndex = slotIndex;
            PreviousEscort = previousEscort;
            CurrentEscort = currentEscort;
        }
    }

    public readonly struct EscortGachaResultEvent : IEvent
    {
        public readonly EscortGachaResult Result;

        public EscortGachaResultEvent(EscortGachaResult result)
        {
            Result = result;
        }
    }

    public readonly struct EscortMergeResultEvent : IEvent
    {
        public readonly EscortMergeResult Result;

        public EscortMergeResultEvent(EscortMergeResult result)
        {
            Result = result;
        }
    }
}
