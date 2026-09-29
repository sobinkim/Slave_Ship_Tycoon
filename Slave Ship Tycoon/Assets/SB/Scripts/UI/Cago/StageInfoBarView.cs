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

        [SerializeField] private TMPro.TMP_Text routeText;
        [SerializeField] private TMPro.TMP_Text voyageText;
        [SerializeField] private StageProgressGraphic stageProgress;
        [Header("Route Card")]
        [SerializeField] private TMPro.TMP_Text routeNumberText;
        [SerializeField] private TMPro.TMP_Text progressText;
        [SerializeField] private GameObject transportCargoGroup;
        [SerializeField] private UnityEngine.UI.Button bossChallengeButton;
        [SerializeField] private TMPro.TMP_Text bossButtonText;
        [SerializeField] private TMPro.TMP_Text bossHintText;

        public void SetRouteNumber(int chapter)
        {
            if (routeNumberText != null)
                routeNumberText.text = $"항로 {chapter:00}";
        }

        public void SetProgress(int stage,int total,bool visible)
        {
            if (progressText != null)
            {
                progressText.gameObject.SetActive(visible);
                progressText.text = $"{stage} / {total} 구간";
            }
            if(stageProgress==null)return;
            stageProgress.gameObject.SetActive(visible);
            stageProgress.SetProgress(stage,total);
        }

        public void SetRoute(string route, string detail)
        {
            if (routeText != null) routeText.text = route;
            if (voyageText != null) voyageText.text = detail;
        }

        private readonly Dictionary<ETransportItemType, ItemElementView> currentCargoItems = new();

        public void SetStageInfoBarMode(StageInfoBarMode mode, bool isBossBattle = false)
        {
            if (bossButton != null)
                bossButton.SetActive(mode == StageInfoBarMode.Odd);
            if (transportCargoGroup != null)
                transportCargoGroup.SetActive(mode == StageInfoBarMode.Even);
            if (bossChallengeButton != null)
                bossChallengeButton.interactable = !isBossBattle;
            if (bossButtonText != null)
                bossButtonText.text = isBossBattle ? "전투 중" : "보스 도전";
            if (bossHintText != null)
                bossHintText.text = isBossBattle ? "봉쇄 함대를 격파하세요" : "승리 시 운송 항로 개방";
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
                newItem.SetMarketMultiplier(1);
                currentCargoItems[cargoData.Item.Type] = newItem;
            }
        }

        public void SetMarketMultiplier(ETransportItemType cargoType, int multiplier)
        {
            if (currentCargoItems.TryGetValue(cargoType, out ItemElementView itemView))
                itemView.SetMarketMultiplier(multiplier);
        }

        public void ClearCargoItems()
        {
            foreach (ItemElementView item in currentCargoItems.Values)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            currentCargoItems.Clear();
        }
    }
}
