using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts
{
    public class ItemBarView : MonoBehaviour
    {
        [SerializeField] private Transform _itemElementRoot;
        [SerializeField] private ItemElementView _itemElementPrefab;

        private Dictionary<ETransportItemType, ItemElementView> _elements = new Dictionary<ETransportItemType, ItemElementView>();

        public bool SearchCurrentElements(ETransportItemType itemType)
        {
            return _elements.ContainsKey(itemType);
        }

        public void CreateItemElement(CargoData cargoData)
        {
            ItemElementView newItemElement = Instantiate(_itemElementPrefab, _itemElementRoot);
            newItemElement.SettingItemElementView(cargoData.Amount, cargoData.Item.Icon);
            _elements.Add(cargoData.Item.Type, newItemElement);
        }

        public void RemoveItemElement(CargoData cargoData)
        {
            ItemElementView targetElement = _elements[cargoData.Item.Type];
            Destroy(targetElement.gameObject);
            _elements.Remove(cargoData.Item.Type);
        }

        public void UpdateItemElement(CargoData cargoData)
        {
            ItemElementView targetElement = _elements[cargoData.Item.Type];
            targetElement.UpdateItemAmountText(cargoData.Amount);
        }
    }
}