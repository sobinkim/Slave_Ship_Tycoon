using SB.Core.EventBus;
using SB.Scripts.Currency;
using SB.Scripts.Fleet;
using UnityEngine;

namespace SB.Scripts.UI.Fleet
{
    public sealed class PlayerFleetPanelController : MonoBehaviour
    {
        [SerializeField] private PlayerFleetManager _playerFleetManager;
        [SerializeField] private EscortGachaManager _gachaManager;
        [SerializeField] private PlayerFleetPanelView _view;

        private EscortShipData _selectedEscort;

        private void OnEnable()
        {
            Bus<EscortOwnershipChangedEvent>.OnEvent += HandleOwnershipChanged;
            Bus<PlayerFleetFormationChangedEvent>.OnEvent += HandleFormationChanged;
            Bus<EscortGachaResultEvent>.OnEvent += HandleGachaResult;
            Bus<EscortMergeResultEvent>.OnEvent += HandleMergeResult;
            Bus<CurrencyChangedEvent>.OnEvent += HandleCurrencyChanged;

            if (_view == null)
                return;

            _view.OnEscortSelected += HandleEscortSelected;
            _view.OnFormationSlotClicked += HandleFormationSlotClicked;
            _view.OnFormationRemoveClicked += HandleFormationRemoveClicked;
            _view.OnSummonClicked += HandleSummonClicked;
            _view.OnMergeClicked += HandleMergeClicked;

            if (_gachaManager != null)
                _view.SetSummonCost(_gachaManager.CurrencyType, _gachaManager.SummonCost);

            RefreshAll();
        }

        private void OnDisable()
        {
            Bus<EscortOwnershipChangedEvent>.OnEvent -= HandleOwnershipChanged;
            Bus<PlayerFleetFormationChangedEvent>.OnEvent -= HandleFormationChanged;
            Bus<EscortGachaResultEvent>.OnEvent -= HandleGachaResult;
            Bus<EscortMergeResultEvent>.OnEvent -= HandleMergeResult;
            Bus<CurrencyChangedEvent>.OnEvent -= HandleCurrencyChanged;

            if (_view == null)
                return;

            _view.OnEscortSelected -= HandleEscortSelected;
            _view.OnFormationSlotClicked -= HandleFormationSlotClicked;
            _view.OnFormationRemoveClicked -= HandleFormationRemoveClicked;
            _view.OnSummonClicked -= HandleSummonClicked;
            _view.OnMergeClicked -= HandleMergeClicked;
        }

        private void HandleEscortSelected(EscortShipData shipData)
        {
            _selectedEscort = shipData;
            RefreshAll();
        }

        private void HandleFormationSlotClicked(int slotIndex)
        {
            if (_playerFleetManager == null || _view == null)
                return;

            if (_selectedEscort == null)
            {
                _view.SetResult("Select an owned escort first.");
                return;
            }

            if (_playerFleetManager.TryPlaceEscort(
                    slotIndex,
                    _selectedEscort,
                    out EscortFormationFailureReason failureReason) == false)
            {
                _view.SetResult(GetFormationFailureMessage(failureReason));
                return;
            }

            _view.SetResult($"Placed {_selectedEscort.DisplayName} in slot {slotIndex + 1}.");
            RefreshAll();
        }

        private void HandleFormationRemoveClicked(int slotIndex)
        {
            if (_playerFleetManager == null || _view == null)
                return;

            if (_playerFleetManager.TryRemoveEscort(slotIndex))
                _view.SetResult($"Cleared formation slot {slotIndex + 1}.");

            RefreshAll();
        }

        private void HandleSummonClicked()
        {
            if (_gachaManager == null)
            {
                _view?.SetResult("Gacha manager is not assigned.");
                return;
            }

            _gachaManager.TrySummon(out _);
        }

        private void HandleMergeClicked()
        {
            if (_playerFleetManager == null || _selectedEscort == null)
            {
                _view?.SetResult("Select an owned escort first.");
                return;
            }

            _playerFleetManager.TryMerge(_selectedEscort);
        }

        private void HandleOwnershipChanged(EscortOwnershipChangedEvent evt)
        {
            RefreshAll();
        }

        private void HandleFormationChanged(PlayerFleetFormationChangedEvent evt)
        {
            RefreshAll();
        }

        private void HandleGachaResult(EscortGachaResultEvent evt)
        {
            if (_view == null)
                return;

            EscortGachaResult result = evt.Result;
            _view.SetResult(result.IsSuccess
                ? $"Summoned {result.Reward.DisplayName}. Owned: {result.RewardOwnedCount}"
                : $"Summon failed: {result.FailureReason}");
            RefreshActions();
        }

        private void HandleMergeResult(EscortMergeResultEvent evt)
        {
            if (_view == null)
                return;

            EscortMergeResult result = evt.Result;
            _view.SetResult(result.IsSuccess
                ? $"Merged {result.ConsumedAmount} {result.Source.Grade} escorts into {result.Result.DisplayName}."
                : $"Merge failed: {result.FailureReason}");
            RefreshAll();
        }

        private void HandleCurrencyChanged(CurrencyChangedEvent evt)
        {
            if (_gachaManager != null && evt.CurrencyType == _gachaManager.CurrencyType)
                RefreshActions();
        }

        private void RefreshAll()
        {
            if (_playerFleetManager == null || _view == null)
                return;

            if (_selectedEscort != null && _playerFleetManager.GetOwnedCount(_selectedEscort) <= 0)
                _selectedEscort = null;

            _view.RefreshInventory(
                _playerFleetManager.GetOwnershipSnapshot(),
                _selectedEscort);
            _view.RefreshFormation(_playerFleetManager.GetFormationSnapshot());
            _view.SetSelectedEscort(_selectedEscort);
            RefreshActions();
        }

        private void RefreshActions()
        {
            if (_view == null)
                return;

            bool canSummon = _gachaManager != null && _gachaManager.CanSummon(out _);
            bool canMerge = _playerFleetManager != null &&
                            _selectedEscort != null &&
                            _playerFleetManager.CanMerge(_selectedEscort, out _, out _);

            _view.SetSummonInteractable(canSummon);
            _view.SetMergeInteractable(canMerge);
        }

        private static string GetFormationFailureMessage(EscortFormationFailureReason failureReason)
        {
            switch (failureReason)
            {
                case EscortFormationFailureReason.InvalidSlot:
                    return "Invalid formation slot.";
                case EscortFormationFailureReason.InvalidEscort:
                    return "Invalid escort data.";
                case EscortFormationFailureReason.EscortNotOwned:
                    return "This escort is not owned.";
                case EscortFormationFailureReason.NotEnoughCopies:
                    return "No unequipped copy is available.";
                default:
                    return "Formation update failed.";
            }
        }
    }
}
