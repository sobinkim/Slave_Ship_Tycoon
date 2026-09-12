using System.Collections.Generic;
using SB.Scripts.Fleet;
using TMPro;
using UnityEngine;

namespace SB.Scripts.UI.Fleet
{
    public sealed class FleetMergeScreen : MonoBehaviour
    {
        [SerializeField] private PlayerFleetManager fleetManager;
        [SerializeField] private EscortInventoryItemView cardPrefab;
        [SerializeField] private Transform inventoryRoot;
        [SerializeField] private UnityEngine.UI.Image sourceIcon;
        [SerializeField] private UnityEngine.UI.Image resultIcon;
        [SerializeField] private TMP_Text sourceLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private UnityEngine.UI.Image[] materialIcons;
        [SerializeField] private TMP_Text[] materialLabels;
        [SerializeField] private UnityEngine.UI.Button confirmButton;
        private readonly List<EscortShipData> selected = new List<EscortShipData>();
        private readonly Dictionary<EscortShipData, EscortInventoryItemView> cards = new Dictionary<EscortShipData, EscortInventoryItemView>();
        private bool isCommitting;

        public void Open() { selected.Clear(); gameObject.SetActive(true); Refresh(); }
        public void Close() { selected.Clear(); gameObject.SetActive(false); }
        private void OnDisable()
        {
            selected.Clear();
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
        private void OnDestroy() { foreach(var card in cards.Values) if(card != null) card.OnSelected -= SelectMaterial; }

        private void SelectMaterial(EscortShipData ship)
        {
            if (selected.Count >= PlayerFleetManager.MergeMaterialCount || ship.Grade == EscortShipGrade.Legendary) return;
            if (selected.Count > 0 && selected[0].Grade != ship.Grade) return;
            if (fleetManager.GetUnequippedCount(ship) <= CountSelected(ship)) return;
            selected.Add(ship);
            Refresh();
        }

        public void RemoveFirst() => RemoveMaterial(0);
        public void RemoveSecond() => RemoveMaterial(1);
        public void RemoveThird() => RemoveMaterial(2);
        private void RemoveMaterial(int index) { if(index < selected.Count) selected.RemoveAt(index); Refresh(); }
        private int CountSelected(EscortShipData ship) { int count=0; foreach(var entry in selected) if(entry==ship) count++; return count; }

        private void Refresh()
        {
            foreach (var snapshot in fleetManager.GetOwnershipSnapshot())
            {
                if (!cards.TryGetValue(snapshot.ShipData, out var card))
                {
                    card = Instantiate(cardPrefab, inventoryRoot);
                    card.OnSelected += SelectMaterial;
                    cards.Add(snapshot.ShipData, card);
                }
                card.Refresh(snapshot, CountSelected(snapshot.ShipData)>0);
                bool available = selected.Count < PlayerFleetManager.MergeMaterialCount && snapshot.ShipData.Grade != EscortShipGrade.Legendary &&
                    (selected.Count == 0 || snapshot.ShipData.Grade == selected[0].Grade) && snapshot.UnequippedCount > CountSelected(snapshot.ShipData);
                card.SetMaterialState(available, CountSelected(snapshot.ShipData));
            }
            EscortShipData source = selected.Count > 0 ? selected[0] : null;
            fleetManager.TryGetMergePreview(source, out var result);
            sourceIcon.sprite=source!=null?source.Icon:null;sourceIcon.enabled=sourceIcon.sprite!=null;
            resultIcon.sprite=result!=null?result.Icon:null;resultIcon.enabled=resultIcon.sprite!=null;
            sourceLabel.text=source!=null?EscortInventoryItemView.GradeLabel(source.Grade):"재료 선택";
            resultLabel.text=result!=null?EscortInventoryItemView.GradeLabel(result.Grade):"상위 등급";
            statusLabel.text=$"같은 등급의 미편성 함선 3척 필요  ({selected.Count}/3)\n선택한 재료를 누르면 해제됩니다";
            for(int i=0;i<materialIcons.Length;i++)
            {
                materialIcons[i].sprite=i<selected.Count?selected[i].Icon:null;
                materialIcons[i].enabled=materialIcons[i].sprite!=null;
                materialLabels[i].text=i<selected.Count?selected[i].DisplayName:"+";
            }
            confirmButton.interactable=selected.Count==PlayerFleetManager.MergeMaterialCount && !isCommitting;
        }

        public void Confirm()
        {
            if (isCommitting || selected.Count != PlayerFleetManager.MergeMaterialCount) return;
            isCommitting=true;
            bool success=fleetManager.TryMergeSelected(selected,out var reward);
            selected.Clear();
            isCommitting=false;
            Refresh();
            statusLabel.text=success?$"{reward.DisplayName} 획득!\n다음 재료를 선택해 계속 합성할 수 있어요.":"재료가 변경됐어요. 다시 선택해주세요.";
            if(success){resultIcon.sprite=reward.Icon;resultIcon.enabled=reward.Icon!=null;resultLabel.text=EscortInventoryItemView.GradeLabel(reward.Grade);}
        }
    }
}
