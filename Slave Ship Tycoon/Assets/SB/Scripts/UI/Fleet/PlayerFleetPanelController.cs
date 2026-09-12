using System.Collections.Generic;
using System.Text;
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
        [SerializeField] private ShipRecruitmentView recruitmentView;
        [SerializeField] private FleetMergeScreen mergeScreen;

        private EscortShipData _selectedEscort;
        private int _selectedSlotIndex = -1;
        private EscortShipData _mergeSource;
        private EscortShipData _mergeResult;
        private List<KeyValuePair<EscortShipData, int>> _mergeMaterials;

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
            _view.OnMergeConfirmed += HandleMergeConfirmed;
            _view.OnMergeCancelled += CancelMerge;

            if (_gachaManager != null)
                _view.SetSummonCost(_gachaManager.CurrencyType, _gachaManager.SummonCost);

            RefreshAll();
            _view.SetResult(string.Empty);
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
            _view.OnMergeConfirmed -= HandleMergeConfirmed;
            _view.OnMergeCancelled -= CancelMerge;
            CancelMerge();
        }

        private void HandleEscortSelected(EscortShipData shipData)
        {
            _selectedEscort = _selectedEscort == shipData && _selectedSlotIndex < 0 ? null : shipData;

            if (_selectedSlotIndex >= 0 && _selectedEscort != null)
            {
                PlaceSelectedEscort(_selectedSlotIndex);
                return;
            }

            _view.SetResult(string.Empty);
            RefreshAll();
        }

        private void HandleFormationSlotClicked(int slotIndex)
        {
            if (_playerFleetManager == null || _view == null)
                return;

            if (_selectedEscort == null || _playerFleetManager.GetFormationAt(slotIndex) == _selectedEscort)
            {
                _selectedEscort = null;
                _selectedSlotIndex = _selectedSlotIndex == slotIndex ? -1 : slotIndex;
                _view.SetResult(_selectedSlotIndex >= 0 ? $"{slotIndex + 1}번 슬롯 선택" : string.Empty);
                RefreshAll();
                return;
            }

            PlaceSelectedEscort(slotIndex);
        }

        private void PlaceSelectedEscort(int slotIndex)
        {
            if (_playerFleetManager.TryPlaceEscort(
                    slotIndex,
                    _selectedEscort,
                    out EscortFormationFailureReason failureReason) == false)
            {
                _view.SetResult(GetFormationFailureMessage(failureReason));
                RefreshAll();
                return;
            }

            _selectedSlotIndex = -1;
            _view.SetResult($"{_selectedEscort.DisplayName} - {slotIndex + 1}번 슬롯\n다음 전투부터 적용");
            _selectedEscort = null;
            RefreshAll();
        }

        private void HandleFormationRemoveClicked(int slotIndex)
        {
            if (_playerFleetManager == null || _view == null)
                return;

            if (_playerFleetManager.TryRemoveEscort(slotIndex))
                _view.SetResult($"{slotIndex + 1}번 슬롯 해제\n다음 전투부터 적용");

            _selectedSlotIndex = -1;
            RefreshAll();
        }

        private void HandleSummonClicked()
        {
            if (recruitmentView != null)
            {
                recruitmentView.Open();
                return;
            }
            if (_gachaManager == null)
            {
                _view?.SetResult("소환 설정이 없습니다.");
                return;
            }

            _gachaManager.TrySummon(out _);
        }

        private void HandleMergeClicked()
        {
            if (mergeScreen != null) { mergeScreen.Open(); return; }
            if (_playerFleetManager == null || _selectedEscort == null)
            {
                _view?.SetResult("보유한 호위선을 먼저 선택하세요.");
                return;
            }

            if (_playerFleetManager.CanMerge(_selectedEscort, out _, out EscortShipData result) == false)
                return;

            _mergeSource = _selectedEscort;
            _mergeResult = result;
            _mergeMaterials = _playerFleetManager.GetMergeMaterials(_mergeSource);
            StringBuilder preview = new StringBuilder("재료\n");

            for (int i = 0; i < _mergeMaterials.Count; i++)
                preview.AppendLine($"{_mergeMaterials[i].Key.DisplayName} x{_mergeMaterials[i].Value}");

            preview.Append($"\n결과\n{result.DisplayName} x1");
            _view.ShowMergePreview(preview.ToString());
        }

        private void HandleMergeConfirmed()
        {
            if (_mergeSource == null || _mergeMaterials == null || _playerFleetManager == null)
                return;

            List<KeyValuePair<EscortShipData, int>> materials = _playerFleetManager.GetMergeMaterials(_mergeSource);
            bool unchanged = materials.Count == _mergeMaterials.Count &&
                             _playerFleetManager.CanMerge(_mergeSource, out _, out EscortShipData result) &&
                             result == _mergeResult;

            for (int i = 0; unchanged && i < materials.Count; i++)
                unchanged = materials[i].Key == _mergeMaterials[i].Key && materials[i].Value == _mergeMaterials[i].Value;

            EscortShipData source = _mergeSource;
            CancelMerge();

            if (unchanged == false)
            {
                _view.SetResult("재료가 변경됐습니다. 합성을 다시 확인하세요.");
                RefreshAll();
                return;
            }

            _playerFleetManager.TryMerge(source);
        }

        private void CancelMerge()
        {
            _mergeSource = null;
            _mergeResult = null;
            _mergeMaterials = null;
            _view?.HideMergePreview();
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
                ? $"{result.Reward.DisplayName} +1  /  보유 {result.RewardOwnedCount:N0}"
                : GetSummonFailureMessage(result.FailureReason));

            if (result.IsSuccess)
            {
                _selectedEscort = result.Reward;
                _selectedSlotIndex = -1;
            }

            RefreshAll();

            if (result.IsSuccess)
                _view.FocusEscort(result.Reward);
        }

        private void HandleMergeResult(EscortMergeResultEvent evt)
        {
            if (_view == null)
                return;

            EscortMergeResult result = evt.Result;
            _view.SetResult(result.IsSuccess
                ? $"{result.Result.DisplayName} +1"
                : GetMergeFailureMessage(result.FailureReason));

            if (result.IsSuccess)
                _selectedEscort = result.Result;

            RefreshAll();

            if (result.IsSuccess)
                _view.FocusEscort(result.Result);
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
            _view.SetFormationSelection(_selectedSlotIndex, _selectedEscort,
                _selectedEscort != null && _playerFleetManager.GetUnequippedCount(_selectedEscort) > 0);
            RefreshActions();
        }

        private void RefreshActions()
        {
            if (_view == null)
                return;

            EscortGachaFailureReason summonFailure = EscortGachaFailureReason.MissingGachaTable;
            bool canSummon = _gachaManager != null && _gachaManager.CanSummon(out summonFailure);
            EscortMergeFailureReason mergeFailure = EscortMergeFailureReason.InvalidSource;
            EscortShipData mergeResult = null;
            bool canMerge = _playerFleetManager != null &&
                            _selectedEscort != null &&
                            _playerFleetManager.CanMerge(_selectedEscort, out mergeFailure, out mergeResult);

            _view.SetSummonInteractable(recruitmentView != null || canSummon);
            _view.SetMergeInteractable(mergeScreen != null || canMerge);

            if (_gachaManager != null)
                _view.SetSummonStatus(_gachaManager.CurrencyType, _gachaManager.SummonCost,
                    canSummon ? string.Empty : GetSummonFailureMessage(summonFailure));

            _view.SetMergeStatus(_selectedEscort == null ? "선택한 호위선 없음" : canMerge
                ? $"재료 {PlayerFleetManager.MergeMaterialCount}개 > {mergeResult.DisplayName}"
                : GetMergeFailureMessage(mergeFailure));
        }

        private static string GetSummonFailureMessage(EscortGachaFailureReason reason)
        {
            return reason == EscortGachaFailureReason.InsufficientCurrency
                ? "재화가 부족합니다"
                : "소환할 수 없습니다";
        }

        private static string GetMergeFailureMessage(EscortMergeFailureReason reason)
        {
            switch (reason)
            {
                case EscortMergeFailureReason.HighestGrade:
                    return "최대 등급입니다";
                case EscortMergeFailureReason.NotEnoughUnequippedCopies:
                    return $"같은 등급의 미편성 호위선 {PlayerFleetManager.MergeMaterialCount}개가 필요합니다";
                case EscortMergeFailureReason.SourceNotOwned:
                    return "보유하지 않은 호위선입니다";
                default:
                    return "합성할 수 없습니다";
            }
        }

        private static string GetFormationFailureMessage(EscortFormationFailureReason failureReason)
        {
            switch (failureReason)
            {
                case EscortFormationFailureReason.InvalidSlot:
                    return "잘못된 편성 슬롯입니다.";
                case EscortFormationFailureReason.InvalidEscort:
                    return "잘못된 호위선 데이터입니다.";
                case EscortFormationFailureReason.EscortNotOwned:
                    return "보유하지 않은 호위선입니다.";
                case EscortFormationFailureReason.NotEnoughCopies:
                    return "편성되지 않은 호위선이 없습니다.";
                default:
                    return "편성 변경에 실패했습니다.";
            }
        }
    }
}
