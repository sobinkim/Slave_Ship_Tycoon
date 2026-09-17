using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace SB.Scripts
{
    public class CagoController : MonoBehaviour
    {
        [SerializeField] private CargoManager _cargoManager;
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private CurrentCagoSliderView currentCagoSliderView;
        [SerializeField] private CagoButtonView viewPrefab;

        private readonly Dictionary<CargoData, CagoButtonView> cargoViews = new Dictionary<CargoData, CagoButtonView>();

        private void Awake()
        {
            CreateButton();
        }

        private void OnEnable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += UpdateCurrentCagoText;
            Bus<ChangedCurrentCargoCapacityEvent>.Raise(new ChangedCurrentCargoCapacityEvent());
        }

        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= UpdateCurrentCagoText;
        }

        private void UpdateCurrentCagoText(ChangedCurrentCargoCapacityEvent evt)
        {
            foreach (var pair in cargoViews) pair.Value.RefreshCargo(pair.Key);
            currentCagoSliderView.SetCargoCapacityText(_cargoManager.GetStatCargoCapacity(), 
                _cargoManager.GetCurrentCargoCapacity());

            currentCagoSliderView.SetCurrentCagoText(_cargoManager.GetStatCargoCapacity(),
                _cargoManager.GetCurrentCargoCapacity());
        }

        private void CreateButton()
        {
            foreach (CargoData cargoData in _cargoManager.CargoData)
            {
                CagoButtonView buttonView = Instantiate(viewPrefab, buttonRoot);

                ETransportItemType type = cargoData.Item.Type;

                buttonView.SetIcon(cargoData.Item.Icon);
                buttonView.RefreshCargo(cargoData);
                cargoViews.Add(cargoData, buttonView);

                buttonView.OnloadButtonClicked +=
                    () => _cargoManager.LoadCargo(type);

                buttonView.OnunloadButtonClicked +=
                    () => _cargoManager.UnloadCargo(type);
            }
        }
    }
}
