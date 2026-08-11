using SB.Core.EventBus;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts
{
    public enum ETransportItemType
    {
        Gold,
        ShipElement
    }

    public class CargoManager : MonoBehaviour
    {
        [SerializeField] private CargoData[] _cargoData;
        [SerializeField] private float _statCargoCapacity;
        [SerializeField] private float _currentCargoCapacity;
        public CargoData[] CargoData => _cargoData;

        public float GetCurrentCargoCapacity() => _currentCargoCapacity;

        private void OnEnable()
        {
            Bus<UpgradeEvent>.OnEvent += GetMainShipUpgradeData;
        }

        private void OnDisable()
        {
            Bus<UpgradeEvent>.OnEvent -= GetMainShipUpgradeData;
        }

        private void GetMainShipUpgradeData(UpgradeEvent evt)
        {
            _statCargoCapacity = evt._mainShipUpgradeData.cargoCapacity;
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
            _currentCargoCapacity += cargoData.Item.BaseWeight;

            Bus<ChangedCurrentCargoCapacityEvent>.Raise(new ChangedCurrentCargoCapacityEvent(_cargoData));
            return true;
        }

        public bool UnloadCargo(ETransportItemType targetType)
        {
            CargoData cargoData = GetCargo(targetType);

            if (cargoData == null || cargoData.Amount <= 0)
                return false;

            cargoData.Amount--;
            _currentCargoCapacity -= cargoData.Item.BaseWeight;

            Bus<ChangedCurrentCargoCapacityEvent>.Raise(new ChangedCurrentCargoCapacityEvent(_cargoData));
            return true;
        }

        private bool CanLoad(ETransportItemType targetType, out CargoData cargoData)
        {
            cargoData = GetCargo(targetType);

            if (cargoData == null)
                return false;

            return GetRemainingCapacity() >= cargoData.Item.BaseWeight;
        }

        private CargoData GetCargo(ETransportItemType targetType)
        {
            foreach (CargoData cargo in _cargoData)
            {
                if (cargo.Item.Type == targetType)
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
                cargo.Amount = 0;
            }

            _currentCargoCapacity = 0f;

            Bus<ChangedCurrentCargoCapacityEvent>.Raise(new ChangedCurrentCargoCapacityEvent(_cargoData));
        }
    }
}
