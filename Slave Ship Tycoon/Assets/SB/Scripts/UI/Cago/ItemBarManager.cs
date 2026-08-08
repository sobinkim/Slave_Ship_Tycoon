using System;
using SB.Core.EventBus;
using UnityEngine;
using UnityEngine.Serialization;

namespace SB.Scripts
{
    public class ItemBarManager : MonoBehaviour
    {
        [SerializeField] private CargoManager _cargoManager;
        [SerializeField] private ItemBarView _itemBarView;

        private void OnEnable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent += RefreshItemElementsView;
        }

        private void OnDisable()
        {
            Bus<ChangedCurrentCargoCapacityEvent>.OnEvent -= RefreshItemElementsView;
        }

        private void RefreshItemElementsView(ChangedCurrentCargoCapacityEvent evt)
        {
            foreach (CargoData _cargoData in _cargoManager.CargoData)
            {
                if (_itemBarView.SearchCurrentElements(_cargoData.Item.Type))
                {
                    if (_cargoData.Amount > 0)
                    {
                        _itemBarView.UpdateItemElement(_cargoData);
                    }
                    else
                    {
                        _itemBarView.RemoveItemElement(_cargoData);
                    }
                }
                else
                {
                    if (_cargoData.Amount <= 0)
                      continue;

                    _itemBarView.CreateItemElement(_cargoData);
                }
            }
        }
    }
}