using SB.Core.EventBus;
using SB.Scripts.TransportEquipment;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class TransportEquipmentPanelController : MonoBehaviour
    {
        [SerializeField] private TransportEquipmentManager _equipmentManager;
        [SerializeField] private TransportEquipmentPanelView _view;

        private void OnEnable()
        {
            Bus<TransportEquipmentChangedEvent>.OnEvent += HandleEquipmentChanged;

            if (_view != null)
                _view.OnItemSelected += HandleItemSelected;

            Refresh();
        }

        private void OnDisable()
        {
            Bus<TransportEquipmentChangedEvent>.OnEvent -= HandleEquipmentChanged;

            if (_view != null)
                _view.OnItemSelected -= HandleItemSelected;
        }

        private void HandleItemSelected(TransportEquipmentItemData itemData)
        {
            _equipmentManager?.TryEquip(itemData);
        }

        private void HandleEquipmentChanged(TransportEquipmentChangedEvent evt)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_equipmentManager != null && _view != null)
                _view.Refresh(_equipmentManager.EquippedItem);
        }
    }
}
