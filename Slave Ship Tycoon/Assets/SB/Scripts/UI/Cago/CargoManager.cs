using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts
{
    public enum ETransportItemType
    {
        // Existing numeric values are part of the persisted loadout key.
        Gold = 0,
        ShipElement = 1,
        Diamond = 2,
        Emerald = 3,
        Other = 4
    }

    public enum CargoLoadoutPersistence
    {
        SessionOnly,
        PlayerPrefs
    }

    public class CargoManager : MonoBehaviour
    {
        [Header("Cargo Loadout")]
        [SerializeField, Tooltip("다음 운송에 사용할 화물 종류와 수량. 실제 운송 화물과 별도로 유지됩니다.")]
        private CargoData[] _cargoData;
        [SerializeField, Min(0f), Tooltip("업그레이드 이벤트로 갱신되는 최대 적재 중량입니다.")]
        private float _statCargoCapacity;
        [SerializeField, Tooltip("현재 적재 설정의 총중량입니다. 수량이 변경되면 다시 계산됩니다.")]
        private float _currentCargoCapacity;

        [Header("Loadout Persistence")]
        [SerializeField, Tooltip("PlayerPrefs 선택 시 적재 설정을 게임을 종료한 뒤에도 유지합니다.")]
        private CargoLoadoutPersistence loadoutPersistence = CargoLoadoutPersistence.PlayerPrefs;
        [SerializeField, Tooltip("저장할 적재 설정의 키. 서로 다른 저장 슬롯에는 별도의 키를 지정하세요.")]
        private string loadoutSaveKey = "CargoLoadout.v1";

        private IReadOnlyList<CargoData> transportCargoData = Array.Empty<CargoData>();

        public CargoData[] CargoData => _cargoData;
        public IReadOnlyList<CargoData> TransportCargoData => transportCargoData;
        public bool IsLoadoutWithinCapacity => _currentCargoCapacity <= _statCargoCapacity;

        public bool HasValidLoadout
        {
            get
            {
                if (_cargoData == null || !IsLoadoutWithinCapacity)
                    return false;

                foreach (CargoData cargo in _cargoData)
                {
                    if (cargo == null || cargo.Item == null || cargo.Amount < 0 ||
                        cargo.Item.BaseWeight < 0f || float.IsNaN(cargo.Item.BaseWeight) ||
                        float.IsInfinity(cargo.Item.BaseWeight))
                        return false;
                }

                return true;
            }
        }

        public float GetCurrentCargoCapacity() => _currentCargoCapacity;

        private void Awake()
        {
            if (_cargoData == null)
            {
                Debug.LogError("CargoManager requires an explicitly assigned cargo loadout.", this);
                _cargoData = Array.Empty<CargoData>();
            }

            if (loadoutPersistence == CargoLoadoutPersistence.PlayerPrefs &&
                string.IsNullOrWhiteSpace(loadoutSaveKey))
                Debug.LogError("CargoManager requires a save key for persistent loadouts.", this);

            RestoreLoadout();
            GetTotalWeight();
        }

        private void OnEnable()
        {
            Bus<UpgradeEvent>.OnEvent += GetMainShipUpgradeData;
            Bus<SellChapterCompletedEvent>.OnEvent += HandleSellChapterCompleted;
        }

        private void OnDisable()
        {
            Bus<UpgradeEvent>.OnEvent -= GetMainShipUpgradeData;
            Bus<SellChapterCompletedEvent>.OnEvent -= HandleSellChapterCompleted;
        }

        private void Start()
        {
            Bus<RefreshMainShipStatsEvent>.Raise(new RefreshMainShipStatsEvent());
            GetTotalWeight();
            RaiseCargoChanged();
            RaiseTransportCargoChanged();
        }

        private void GetMainShipUpgradeData(UpgradeEvent evt)
        {
            if (evt._mainShipUpgradeData == null)
                return;

            _statCargoCapacity = evt._mainShipUpgradeData.cargoCapacity;
            RaiseCargoChanged();
        }

        public float GetStatCargoCapacity()
        {
            return _statCargoCapacity;
        }

        public bool LoadCargo(ETransportItemType targetType)
        {
            if (!CanLoad(targetType, out CargoData cargoData))
                return false;

            cargoData.Amount++;
            CompleteLoadoutChange();
            return true;
        }

        public bool UnloadCargo(ETransportItemType targetType)
        {
            CargoData cargoData = GetCargo(targetType);

            if (cargoData == null || cargoData.Amount <= 0)
                return false;

            cargoData.Amount--;
            CompleteLoadoutChange();
            return true;
        }

        public bool CanLoadCargo(ETransportItemType targetType)
        {
            return CanLoad(targetType, out CargoData _);
        }

        public bool RemoveCargo(ETransportItemType targetType)
        {
            CargoData cargoData = GetCargo(targetType);
            if (cargoData == null || cargoData.Amount == 0)
                return false;

            cargoData.Amount = 0;
            CompleteLoadoutChange();
            return true;
        }

        private bool CanLoad(ETransportItemType targetType, out CargoData cargoData)
        {
            cargoData = GetCargo(targetType);

            if (cargoData == null || cargoData.Item == null || cargoData.Amount < 0 ||
                cargoData.Amount == int.MaxValue || !HasValidLoadout)
                return false;

            return GetRemainingCapacity() >= cargoData.Item.BaseWeight;
        }

        private CargoData GetCargo(ETransportItemType targetType)
        {
            foreach (CargoData cargo in _cargoData)
            {
                if (cargo != null && cargo.Item != null && cargo.Item.Type == targetType)
                    return cargo;
            }

            return null;
        }

        public int GetCargoAmount(ETransportItemType targetType)
        {
            CargoData cargoData = GetCargo(targetType);

            return cargoData != null ? cargoData.Amount : 0;
        }

        public float GetTotalWeight()
        {
            _currentCargoCapacity = 0f;

            foreach (CargoData cargo in _cargoData)
            {
                if (cargo == null || cargo.Item == null)
                    continue;

                _currentCargoCapacity += cargo.Amount * cargo.Item.BaseWeight;
            }

            return _currentCargoCapacity;
        }

        public float GetRemainingCapacity()
        {
            return _statCargoCapacity - _currentCargoCapacity;
        }

        public void ClearCargo()
        {
            foreach (CargoData cargo in _cargoData)
            {
                if (cargo != null)
                    cargo.Amount = 0;
            }

            CompleteLoadoutChange();
        }

        public bool PrepareTransportCargo()
        {
            GetTotalWeight();
            if (!HasValidLoadout)
            {
                Debug.LogError("Cargo loadout is invalid or exceeds capacity. Adjust the loadout before transport.", this);
                return false;
            }

            CargoData[] manifest = new CargoData[_cargoData.Length];
            for (int index = 0; index < _cargoData.Length; index++)
            {
                CargoData cargo = _cargoData[index];
                manifest[index] = new CargoData { Item = cargo.Item, Amount = cargo.Amount };
            }

            // The editable plan is retained; transport and settlement use this separate snapshot.
            transportCargoData = Array.AsReadOnly(manifest);
            RaiseTransportCargoChanged();
            return true;
        }

        private void HandleSellChapterCompleted(SellChapterCompletedEvent evt)
        {
            transportCargoData = Array.Empty<CargoData>();
            RaiseTransportCargoChanged();
        }

        private void CompleteLoadoutChange()
        {
            GetTotalWeight();
            SaveLoadout();
            RaiseCargoChanged();
        }

        private void RestoreLoadout()
        {
            if (loadoutPersistence != CargoLoadoutPersistence.PlayerPrefs ||
                string.IsNullOrWhiteSpace(loadoutSaveKey))
                return;

            foreach (CargoData cargo in _cargoData)
            {
                if (cargo == null || cargo.Item == null)
                    continue;

                string cargoSaveKey = GetCargoSaveKey(cargo.Item.Type);
                if (PlayerPrefs.HasKey(cargoSaveKey))
                    cargo.Amount = PlayerPrefs.GetInt(cargoSaveKey);
            }
        }

        private void SaveLoadout()
        {
            if (loadoutPersistence != CargoLoadoutPersistence.PlayerPrefs ||
                string.IsNullOrWhiteSpace(loadoutSaveKey))
                return;

            foreach (CargoData cargo in _cargoData)
            {
                if (cargo != null && cargo.Item != null)
                    PlayerPrefs.SetInt(GetCargoSaveKey(cargo.Item.Type), cargo.Amount);
            }

            PlayerPrefs.Save();
        }

        private string GetCargoSaveKey(ETransportItemType cargoType)
        {
            return $"{loadoutSaveKey}.{(int)cargoType}";
        }

        private void RaiseCargoChanged()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.Raise(
                new ChangedCurrentCargoCapacityEvent(_cargoData));
        }

        private void RaiseTransportCargoChanged()
        {
            Bus<TransportCargoChangedEvent>.Raise(
                new TransportCargoChangedEvent(this, transportCargoData));
        }
    }
}
