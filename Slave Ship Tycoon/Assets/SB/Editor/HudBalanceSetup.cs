using System.Linq;
using SB.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Editor
{
    public static class HudBalanceSetup
    {
        private static void Place(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        private static void Label(TMP_Text t,float x,float y,float w,float h,float font)
        {Place(t.transform,x,y,w,h);t.enableAutoSizing=false;t.fontSize=font;}
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
            var panel=Object.FindFirstObjectByType<CommanderSettingsPanel>(FindObjectsInactive.Include);
            var frame=panel.transform.parent;Undo.RegisterFullObjectHierarchyUndo(frame.gameObject,"HUD balance");
            Place(frame.Find("CommandStrip"),24,990,1032,165);
            var bar=Object.FindFirstObjectByType<CommanderSkillBarView>(FindObjectsInactive.Include).transform;Place(bar,0,30,1032,132);
            var horizontal=bar.GetComponent<ScrollRect>();Place(horizontal.viewport,0,0,1032,132);horizontal.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,132);
            horizontal.content.GetComponent<HorizontalLayoutGroup>().padding=new RectOffset(2,2,2,2);
            foreach(var slot in bar.GetComponentsInChildren<CommanderSkillSlotView>(true))
            {
                var size=slot.GetComponent<LayoutElement>();size.minWidth=size.preferredWidth=size.minHeight=size.preferredHeight=128;
                var body=slot.transform.Find("CardBody");Place(body,3,3,122,122);
                var so=new SerializedObject(slot);
                Place(((UnityEngine.UI.Image)so.FindProperty("_skillIcon").objectReferenceValue).transform,0,0,122,122);
                Place(((UnityEngine.UI.Image)so.FindProperty("chargeFill").objectReferenceValue).transform,0,0,122,122);
                Place(body.Find("NameBacking"),0,96,122,26);
                Label((TMP_Text)so.FindProperty("_detailText").objectReferenceValue,0,96,122,26,21);
                Label((TMP_Text)so.FindProperty("_cooldownText").objectReferenceValue,0,0,122,96,32);
            }
            var currency=frame.Find("currencyBar");Place(currency,24,1165,1032,110);
            foreach(Transform root in currency)
            {
                if(root.GetComponentInChildren<TMP_Text>(true)==null)continue;
                var rect=(RectTransform)root;Place(root,rect.anchoredPosition.x,0,334,110);
                foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if(t.name=="CurrencyName")Label(t,0,4,334,30,23);
                    else Label(t,52,38,282,64,40);
                }
                foreach(var icon in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    if(icon.name.ToLower().Contains("icon"))Place(icon.transform,0,49,42,42);
            }
            foreach(var name in new[]{"UpgradePanel","CagoPanel ","TransportEquipmentPanel"})Place(frame.Find(name),0,1285,1080,495);
            foreach(var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {
                Place(scroll.transform,24,68,1032,427);Place(scroll.viewport,0,0,1032,427);
                var track=scroll.transform.Find("ScrollTrack");if(track!=null)Place(track,1022,0,10,427);
            }
            var maintenance=panel.transform.Find("MaintenanceContent");var content=maintenance.GetComponent<ScrollRect>().content;
            var header=content.Find("Header");
            foreach(var name in new[]{"LastSettlement","HoldHint"})
            {var child=header.Find(name);if(child!=null)child.SetParent(maintenance,false);}
            Place(maintenance.Find("LastSettlement"),648,-68,176,55);
            Label(maintenance.Find("HoldHint").GetComponent<TMP_Text>(),834,-68,198,55,18);
            maintenance.Find("HoldHint").GetComponent<TMP_Text>().text="길게 눌러 연속 강화";
            header.gameObject.SetActive(false);
            var grid=content.Find("MainShipUpgradeButtons").GetComponent<GridLayoutGroup>();grid.cellSize=new Vector2(1032,134);grid.spacing=new Vector2(0,10);
            var gridSize=grid.GetComponent<LayoutElement>();gridSize.minHeight=gridSize.preferredHeight=grid.transform.childCount*144-10;
            foreach(Transform row in grid.transform)
            {
                Label(row.Find("Name").GetComponent<TMP_Text>(),24,8,630,34,28);
                Label(row.Find("EffectPreview").GetComponent<TMP_Text>(),24,45,630,35,26);
                var so=new SerializedObject(row.GetComponent<SB.Scripts.UI.Upgrade.MainShipUpgradeSlotView>());
                Label((TMP_Text)so.FindProperty("_level").objectReferenceValue,24,90,270,30,20);
                Label((TMP_Text)so.FindProperty("_cost").objectReferenceValue,660,27,348,80,28);
                Place(row.Find("Edge"),0,10,4,114);
            }
            foreach(var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);scroll.content.anchoredPosition=Vector2.zero;scroll.verticalNormalizedPosition=1;}
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);AssetDatabase.SaveAssets();
            return "128px skill cards, 110px currency bar, three 134px upgrade rows fit in 427px viewport.";
        }
    }
}
