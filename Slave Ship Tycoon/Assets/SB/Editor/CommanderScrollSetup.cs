using System.Linq;
using SB.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Editor
{
    public static class CommanderScrollSetup
    {
        private static RectTransform Node(Transform parent,string name)
        {
            var child=parent.Find(name);if(child!=null)return (RectTransform)child;
            var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Tab scroll");go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        private static T Add<T>(GameObject go) where T:Component
        {var component=go.GetComponent<T>();return component!=null?component:Undo.AddComponent<T>(go);}
        private static void Place(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        private static void Height(Transform t,float height)
        {var e=Add<LayoutElement>(t.gameObject);e.minHeight=e.preferredHeight=height;e.flexibleHeight=0;}
        private static RectTransform Scroll(Transform root,float width,float height)
        {
            var viewport=Node(root,"Viewport");Place(viewport,0,0,width,height);
            var image=Add<UnityEngine.UI.Image>(viewport.gameObject);image.color=Color.white;image.raycastTarget=true;
            Add<Mask>(viewport.gameObject).showMaskGraphic=false;
            var content=Node(viewport,"Content");Place(content,0,0,width,height);
            var layout=Add<VerticalLayoutGroup>(content.gameObject);layout.spacing=12;layout.padding=new RectOffset(0,0,0,16);layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            var fitter=Add<ContentSizeFitter>(content.gameObject);fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=Add<ScrollRect>(root.gameObject);scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;scroll.verticalNormalizedPosition=1;
            return content;
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
            var panel=Object.FindFirstObjectByType<CommanderSettingsPanel>(FindObjectsInactive.Include);
            var upgrade=panel.transform;Undo.RegisterFullObjectHierarchyUndo(upgrade.gameObject,"Scrollable tall buttons");
            var maintenance=upgrade.Find("MaintenanceContent");Place(maintenance,24,68,1032,480);
            var content=Scroll(maintenance,1032,480);var header=Node(content,"Header");Height(header,40);
            foreach(var name in new[]{"LastSettlement","HoldHint","PanelTitle"})
            {var t=maintenance.Find(name);if(t!=null)t.SetParent(header,false);}
            var grid=maintenance.Find("MainShipUpgradeButtons");if(grid==null)grid=content.Find("MainShipUpgradeButtons");grid.SetParent(content,false);
            var layout=grid.GetComponent<GridLayoutGroup>();layout.cellSize=new Vector2(508,112);layout.spacing=new Vector2(16,10);layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=2;
            Height(grid,Mathf.Ceil(grid.childCount/2f)*122-10);
            var settings=upgrade.Find("CommanderContent");Place(settings,24,68,1032,480);
            var skillContent=Scroll(settings,1032,480);var slots=Node(skillContent,"SlotSelection");Height(slots,165);
            foreach(var name in new[]{"Slot0","Slot1","Remove0","Remove1","Status"})
            {var t=settings.Find(name);if(t!=null)t.SetParent(slots,false);}
            for(int i=0;i<2;i++)
            {
                Place(slots.Find("Slot"+i),i*520,0,370,105);Place(slots.Find("Remove"+i),i*520+382,0,128,105);
                foreach(var t in new[]{slots.Find("Slot"+i),slots.Find("Remove"+i)})
                {var label=t.GetComponentInChildren<TMPro.TMP_Text>(true);Place(label.transform,8,0,((RectTransform)t).sizeDelta.x-16,105);}
            }
            Place(slots.Find("Status"),0,113,1032,44);
            var rows=settings.GetComponentsInChildren<RectTransform>(true).Where(x=>x.name.StartsWith("SkillRow")).OrderBy(x=>x.name).ToArray();
            foreach(var row in rows){row.SetParent(skillContent,false);Height(row,190);var b=row.Find("Equip");Place(b,840,40,176,112);Place(b.GetComponentInChildren<TMPro.TMP_Text>(true).transform,8,0,160,112);}
            var bar=Object.FindFirstObjectByType<CommanderSkillBarView>(FindObjectsInactive.Include).transform;
            foreach(var name in new[]{"SettingsButton","StateLegend"}){var t=bar.Find(name);if(t!=null)Undo.DestroyObjectImmediate(t.gameObject);}
            var auto=bar.Find("AutoUseButton");if(auto==null)auto=bar.parent.Find("AutoUseButton");
            Undo.RecordObject(auto,"Auto above skills");auto.SetParent(bar.parent,false);Place(auto,882,-66,150,62);
            var autoLabel=auto.GetComponentInChildren<TMPro.TMP_Text>(true);autoLabel.enableAutoSizing=false;autoLabel.fontSize=22;
            EditorSceneManager.MarkSceneDirty(upgrade.gameObject.scene);EditorSceneManager.SaveScene(upgrade.gameObject.scene);AssetDatabase.SaveAssets();
            return "Tabs scroll vertically; heights restored; legend/settings removed; auto placed above icons.";
        }
    }
}
