using System.Linq;
using SB.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Editor
{
    public static class LargeControlSetup
    {
        private static T Add<T>(GameObject go) where T:Component
        {var c=go.GetComponent<T>();return c!=null?c:Undo.AddComponent<T>(go);}
        private static RectTransform Node(Transform parent,string name)
        {var t=parent.Find(name);if(t!=null)return (RectTransform)t;var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Large controls");go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
        private static void Place(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        private static void Text(Transform t,float x,float y,float w,float h,float size)
        {Place(t,x,y,w,h);var label=t.GetComponent<TMP_Text>();label.enableAutoSizing=false;label.fontSize=size;}
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
            var panel=Object.FindFirstObjectByType<CommanderSettingsPanel>(FindObjectsInactive.Include);
            var frame=panel.transform.parent;Undo.RegisterFullObjectHierarchyUndo(frame.gameObject,"Large scroll controls");
            var strip=frame.Find("CommandStrip");Place(strip,24,990,1032,225);
            Place(frame.Find("currencyBar"),24,1225,1032,70);
            foreach(var name in new[]{"UpgradePanel","CagoPanel ","TransportEquipmentPanel"})
                Place(frame.Find(name),0,1305,1080,475);
            // Shorter visible area is handled by scrolling; row heights stay readable.
            foreach(var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {
                Place(scroll.transform,24,68,1032,400);Place(scroll.viewport,0,0,1032,400);
                var track=Node(scroll.transform,"ScrollTrack");Place(track,1022,0,10,400);
                var trackImage=Add<UnityEngine.UI.Image>(track.gameObject);trackImage.color=new Color(.08f,.13f,.16f,.8f);
                var handle=Node(track,"Handle");handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
                var handleImage=Add<UnityEngine.UI.Image>(handle.gameObject);handleImage.color=new Color(.42f,.63f,.6f,.85f);
                var scrollbar=Add<Scrollbar>(track.gameObject);scrollbar.handleRect=handle;scrollbar.targetGraphic=handleImage;scrollbar.direction=Scrollbar.Direction.BottomToTop;
                scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            }
            var header=panel.transform.Find("MaintenanceContent/Viewport/Content/Header");
            Text(header.Find("HoldHint"),560,0,450,36,20);
            var grid=panel.GetComponentsInChildren<GridLayoutGroup>(true).First(x=>x.name=="MainShipUpgradeButtons");
            grid.cellSize=new Vector2(1032,180);grid.spacing=new Vector2(0,14);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=1;
            var gridSize=Add<LayoutElement>(grid.gameObject);gridSize.minHeight=gridSize.preferredHeight=grid.transform.childCount*194-14;
            foreach(Transform row in grid.transform)
            {
                Text(row.Find("Name"),24,14,640,44,32);
                Text(row.Find("EffectPreview"),24,64,640,44,30);
                var slot=row.GetComponent<SB.Scripts.UI.Upgrade.MainShipUpgradeSlotView>();var so=new SerializedObject(slot);
                var level=(TMP_Text)so.FindProperty("_level").objectReferenceValue;var cost=(TMP_Text)so.FindProperty("_cost").objectReferenceValue;
                Text(level.transform,24,124,270,38,24);Text(cost.transform,660,40,348,98,30);cost.alignment=TextAlignmentOptions.MidlineRight;
                Place(row.Find("Edge"),0,14,4,152);
            }
            foreach(var row in panel.GetComponentsInChildren<RectTransform>(true).Where(x=>x.name.StartsWith("SkillRow")))
            {
                var size=Add<LayoutElement>(row.gameObject);size.minHeight=size.preferredHeight=240;
                Place(row.Find("Icon"),18,45,145,145);Text(row.Find("Description"),182,20,574,200,27);
                Place(row.Find("Equip"),780,48,230,144);Text(row.Find("Equip/Label"),10,0,210,144,27);
            }
            var bar=Object.FindFirstObjectByType<CommanderSkillBarView>(FindObjectsInactive.Include).transform;Place(bar,0,38,1032,182);
            var viewport=Node(bar,"Viewport");Place(viewport,0,0,1032,182);
            Add<UnityEngine.UI.Image>(viewport.gameObject).color=Color.white;Add<Mask>(viewport.gameObject).showMaskGraphic=false;
            var content=Node(viewport,"Content");Place(content,0,0,1032,182);
            var h=Add<HorizontalLayoutGroup>(content.gameObject);h.spacing=14;h.padding=new RectOffset(4,4,4,4);h.childControlWidth=true;h.childControlHeight=true;h.childForceExpandWidth=false;h.childForceExpandHeight=false;
            var fitter=Add<ContentSizeFitter>(content.gameObject);fitter.horizontalFit=ContentSizeFitter.FitMode.PreferredSize;fitter.verticalFit=ContentSizeFitter.FitMode.Unconstrained;
            var horizontal=Add<ScrollRect>(bar.gameObject);horizontal.viewport=viewport;horizontal.content=content;horizontal.horizontal=true;horizontal.vertical=false;horizontal.movementType=ScrollRect.MovementType.Clamped;horizontal.scrollSensitivity=35;
            foreach(var slot in bar.GetComponentsInChildren<CommanderSkillSlotView>(true).OrderBy(x=>x.name))
            {
                slot.transform.SetParent(content,false);var size=Add<LayoutElement>(slot.gameObject);size.minWidth=size.preferredWidth=174;size.minHeight=size.preferredHeight=174;
                var so=new SerializedObject(slot);var icon=(UnityEngine.UI.Image)so.FindProperty("_skillIcon").objectReferenceValue;
                Place(icon.transform,7,7,160,126);
                var body=Node(slot.transform,"CardBody");Place(body,4,4,166,166);var bodyImage=Add<UnityEngine.UI.Image>(body.gameObject);bodyImage.color=new Color(.025f,.045f,.06f);bodyImage.raycastTarget=false;body.SetAsFirstSibling();
                var fill=(UnityEngine.UI.Image)so.FindProperty("chargeFill").objectReferenceValue;Place(fill.transform,7,7,160,126);
                var label=(TMP_Text)so.FindProperty("_detailText").objectReferenceValue;Text(label.transform,4,136,166,34,25);
                var number=(TMP_Text)so.FindProperty("_cooldownText").objectReferenceValue;Text(number.transform,7,7,160,126,40);
            }
            Place(strip.Find("Slider"),232,8,578,17);Place(strip.Find("CommandLabel"),24,2,200,30);Place(strip.Find("MaxCagoCapacityText"),824,2,196,30);
            Place(strip.Find("AutoUseButton"),882,-66,150,62);
            foreach(var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);scroll.content.anchoredPosition=Vector2.zero;scroll.verticalNormalizedPosition=1;}
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);AssetDatabase.SaveAssets();
            return "180px upgrade rows, 240px skill rows, 174px horizontal skill cards saved.";
        }
    }
}
