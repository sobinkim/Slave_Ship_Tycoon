using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SB.Scripts
{
    public enum StageInfoBarMode
    {
        Even,
        Odd
    }

    public class StageInfoBarView : MonoBehaviour
    {
        [FormerlySerializedAs("BossButton")]
        [SerializeField] private GameObject bossButton;
        [FormerlySerializedAs("CagoItemRoot")]
        [SerializeField] private Transform cargoItemRoot;
        [FormerlySerializedAs("CagoItemPrefab")]
        [SerializeField] private ItemElementView cargoItemPrefab;

        private readonly List<GameObject> currentCargoItems = new();

        public void SetStageInfoBarMode(StageInfoBarMode mode)
        {
            if (bossButton != null)
                bossButton.SetActive(mode == StageInfoBarMode.Odd);
        }

        public void AddCargoItems(CargoData[] cargoDatas)
        {
            if (cargoDatas == null)
                return;

            foreach (CargoData cargoData in cargoDatas)
            {
                if (cargoData == null || cargoData.Item == null)
                    continue;

                if (cargoData.Amount <= 0)
                    continue;

                ItemElementView newItem = Instantiate(cargoItemPrefab, cargoItemRoot);
                newItem.SettingItemElementView(cargoData.Amount, cargoData.Item.Icon);
                currentCargoItems.Add(newItem.gameObject);
            }
        }

        public void ClearCargoItems()
        {
            foreach (GameObject item in currentCargoItems)
            {
                if (item != null)
                    Destroy(item);
            }

            currentCargoItems.Clear();
        }
    }
}
