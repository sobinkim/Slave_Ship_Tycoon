using System.Linq;
using SB.Scripts;
using SB.Scripts.Commander;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SB.Editor
{
    public static class CommanderPanelSetup
    {
        private static readonly Color Card=new Color(.085f,.155f,.19f);
        private static readonly Color Brass=new Color(.84f,.67f,.39f);
        private static TMP_FontAsset font;
        private static RectTransform Node(Transform p,string n)
        {
            var t=p.Find(n);if(t!=null)return (RectTransform)t;
            var go=new GameObject(n,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Commander UI");go.transform.SetParent(p,false);return (RectTransform)go.transform;
        }
        private static void Place(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        private static UnityEngine.UI.Image Surface(Transform p,string n,float x,float y,float w,float h,Color c)
        {var r=Node(p,n);Place(r,x,y,w,h);var a=r.GetComponent<UnityEngine.UI.Image>();if(a==null)a=Undo.AddComponent<UnityEngine.UI.Image>(r.gameObject);a.color=c;a.raycastTarget=false;return a;}
        private static TMP_Text Text(Transform p,string n,string value,float x,float y,float w,float h,float size)
        {var r=Node(p,n);Place(r,x,y,w,h);var a=r.GetComponent<TextMeshProUGUI>();if(a==null)a=Undo.AddComponent<TextMeshProUGUI>(r.gameObject);a.font=font;a.fontSize=size;a.text=value;a.color=new Color(.88f,.92f,.89f);a.alignment=TextAlignmentOptions.MidlineLeft;a.raycastTarget=false;return a;}
        private static UnityEngine.UI.Button Button(Transform p,string n,string label,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {var a=Surface(p,n,x,y,w,h,Card);a.raycastTarget=true;var b=a.GetComponent<UnityEngine.UI.Button>();if(b==null)b=Undo.AddComponent<UnityEngine.UI.Button>(a.gameObject);b.targetGraphic=a;b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();if(action!=null)UnityEventTools.AddPersistentListener(b.onClick,action);Text(b.transform,"Label",label,8,0,w-16,h,23).alignment=TextAlignmentOptions.Center;return b;}
        private static void Ref(Object o,string n,Object v){var so=new SerializedObject(o);so.FindProperty(n).objectReferenceValue=v;so.ApplyModifiedProperties();}
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SB/Fonts/MalgunGothic SDF.asset");
            var manager=Object.FindFirstObjectByType<CommanderSkillManager>(FindObjectsInactive.Include);
            var db=(CommanderSkillDatabase)new SerializedObject(manager).FindProperty("_skillDatabase").objectReferenceValue;
            var upgrade=Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.name=="UpgradePanel");
            Undo.RegisterFullObjectHierarchyUndo(upgrade.gameObject,"Commander tabs");
            var panel=upgrade.GetComponent<CommanderSettingsPanel>();if(panel==null)panel=Undo.AddComponent<CommanderSettingsPanel>(upgrade.gameObject);
            var maintenance=Node(upgrade,"MaintenanceContent");Place(maintenance,0,62,1080,493);
            foreach(var name in new[]{"MainShipUpgradeButtons","PanelTitle","HoldHint","LastSettlement"})
            {var t=upgrade.Find(name);if(t!=null)t.SetParent(maintenance,false);}
            maintenance.Find("PanelTitle").gameObject.SetActive(false);
            Place(maintenance.Find("LastSettlement"),410,0,180,30);
            Place(maintenance.Find("HoldHint"),650,0,400,30);
            var grid=maintenance.Find("MainShipUpgradeButtons");Place(grid,24,34,1032,452);
            var layout=grid.GetComponent<UnityEngine.UI.GridLayoutGroup>();layout.cellSize=new Vector2(508,105);layout.spacing=new Vector2(16,8);
            foreach(var t in grid.GetComponentsInChildren<RectTransform>(true).Where(x=>x.name=="Edge"))t.sizeDelta=new Vector2(3,85);
            var settings=Node(upgrade,"CommanderContent");Place(settings,24,70,1032,480);
            var tab1=Button(upgrade,"MaintenanceTab","함대 정비",24,0,300,55,panel.ShowMaintenance);
            var tab2=Button(upgrade,"CommanderTab","지휘 스킬",338,0,300,55,panel.ShowSettings);
            Ref(panel,"manager",manager);Ref(panel,"maintenance",maintenance.gameObject);Ref(panel,"settings",settings.gameObject);
            Ref(panel,"panelManager",Object.FindFirstObjectByType<SB.Scripts.Visual.PannelManager>(FindObjectsInactive.Include));
            Ref(panel,"maintenanceTab",tab1.GetComponent<UnityEngine.UI.Image>());Ref(panel,"settingsTab",tab2.GetComponent<UnityEngine.UI.Image>());
            var so=new SerializedObject(panel);so.FindProperty("slotLabels").arraySize=2;so.FindProperty("slotSurfaces").arraySize=2;
            for(int i=0;i<2;i++)
            {
                var b=Button(settings,"Slot"+i,"슬롯",i*520,0,370,56,i==0?panel.SelectFirst:panel.SelectSecond);
                so.FindProperty("slotLabels").GetArrayElementAtIndex(i).objectReferenceValue=b.GetComponentInChildren<TMP_Text>();
                so.FindProperty("slotSurfaces").GetArrayElementAtIndex(i).objectReferenceValue=b.GetComponent<UnityEngine.UI.Image>();
                Button(settings,"Remove"+i,"해제",i*520+382,0,128,56,i==0?panel.RemoveFirst:panel.RemoveSecond);
            }
            so.FindProperty("status").objectReferenceValue=Text(settings,"Status","",0,60,1032,40,21);
            var rows=so.FindProperty("rows");rows.arraySize=db.Skills.Count;
            for(int i=0;i<db.Skills.Count;i++)
            {
                var skill=db.Skills[i];var row=Surface(settings,"SkillRow"+i,0,110+i*173,1032,163,Card);
                var icon=Surface(row.transform,"Icon",12,23,110,110,Color.white);icon.sprite=skill.Icon;icon.preserveAspect=true;
                var desc=Text(row.transform,"Description","",142,10,676,140,23);
                var b=Button(row.transform,"Equip","장착",840,30,176,103,null);
                var r=rows.GetArrayElementAtIndex(i);r.FindPropertyRelative("skill").objectReferenceValue=skill;r.FindPropertyRelative("description").objectReferenceValue=desc;r.FindPropertyRelative("equipButton").objectReferenceValue=b;r.FindPropertyRelative("actionLabel").objectReferenceValue=b.GetComponentInChildren<TMP_Text>();
            }
            so.ApplyModifiedProperties();settings.gameObject.SetActive(false);maintenance.gameObject.SetActive(true);
            var slots=Object.FindObjectsByType<CommanderSkillSlotView>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(x=>x.name).ToArray();
            for(int i=0;i<slots.Length;i++)
            {
                var slot=slots[i];Undo.RegisterFullObjectHierarchyUndo(slot.gameObject,"Commander icon slot");Place(slot.transform,i*126,0,112,100);
                var s=new SerializedObject(slot);var icon=(UnityEngine.UI.Image)s.FindProperty("_skillIcon").objectReferenceValue;
                var detail=(TMP_Text)s.FindProperty("_detailText").objectReferenceValue;var number=(TMP_Text)s.FindProperty("_cooldownText").objectReferenceValue;
                Place(icon.transform,4,4,104,72);icon.preserveAspect=true;
                Place(detail.transform,0,76,112,24);detail.enableAutoSizing=false;detail.fontSize=18;detail.alignment=TextAlignmentOptions.Center;
                Place(number.transform,4,4,104,72);number.enableAutoSizing=false;number.fontSize=29;number.alignment=TextAlignmentOptions.Center;
                var fill=Surface(slot.transform,"ChargeFill",4,4,104,72,new Color(.2f,.65f,.8f,.5f));
                fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Vertical;fill.fillOrigin=0;fill.transform.SetSiblingIndex(icon.transform.GetSiblingIndex()+1);number.transform.SetAsLastSibling();
                foreach(var field in new[]{"_cooldownFill","_activeEffect"}){var obj=s.FindProperty(field).objectReferenceValue;if(obj is Component component)component.gameObject.SetActive(false);if(obj is GameObject go)go.SetActive(false);s.FindProperty(field).objectReferenceValue=null;}
                s.FindProperty("chargeFill").objectReferenceValue=fill;s.FindProperty("stateBorder").objectReferenceValue=slot.GetComponent<UnityEngine.UI.Image>();s.ApplyModifiedProperties();
                slot.GetComponent<UnityEngine.UI.Button>().transition=UnityEngine.UI.Selectable.Transition.None;
            }
            var bar=slots[0].transform.parent;
            Place(bar.Find("AutoUseButton"),270,0,112,100);
            Button(bar,"SettingsButton","스킬 설정",408,0,200,100,panel.OpenSettings);
            Text(bar,"StateLegend","청록: 사용 가능   ·   파랑: 충전 중\n황동: 발동 중   ·   숫자: 남은 시간",636,0,390,100,20);
            EditorSceneManager.MarkSceneDirty(upgrade.gameObject.scene);EditorSceneManager.SaveScene(upgrade.gameObject.scene);AssetDatabase.SaveAssets();
            return "Commander icon bar and maintenance/settings tabs saved.";
        }
    }
}
