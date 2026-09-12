using System.Linq;
using SB.Scripts.UI.Fleet;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SB.Editor
{
    public static class FleetCardLayoutSetup
    {
        private static readonly Color Background = new Color(.015f,.045f,.06f,1);
        private static readonly Color Surface = new Color(.035f,.11f,.14f,1);
        private static readonly Color Accent = new Color(.04f,.7f,.68f,1);
        private static TMP_FontAsset font;
        private static void Rect(Transform t,float x,float y,float w,float h)
        {
            var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
        private static void Fill(Transform t)
        {
            var r=(RectTransform)t;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        }
        private static GameObject Node(Transform parent,string name)
        {
            var existing=parent.Find(name);if(existing!=null)return existing.gameObject;
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go;
        }
        private static UnityEngine.UI.Image Image(Transform parent,string name,Color color,float x,float y,float w,float h)
        {
            var go=Node(parent,name);var img=go.GetComponent<UnityEngine.UI.Image>()??go.AddComponent<UnityEngine.UI.Image>();img.color=color;img.raycastTarget=false;Rect(go.transform,x,y,w,h);return img;
        }
        private static TMP_Text Text(Transform parent,string name,string value,float x,float y,float w,float h,float size)
        {
            var go=Node(parent,name);var txt=go.GetComponent<TMP_Text>()??go.AddComponent<TextMeshProUGUI>();Rect(go.transform,x,y,w,h);txt.font=font;txt.fontSharedMaterial=font.material;txt.text=value;txt.fontSize=size;txt.enableAutoSizing=false;txt.color=new Color(.86f,.94f,.95f);txt.alignment=TextAlignmentOptions.Center;txt.raycastTarget=false;return txt;
        }
        private static UnityEngine.UI.Button Button(Transform parent,string name,string label,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var image=Image(parent,name,Surface,x,y,w,h);image.raycastTarget=true;
            var b=image.GetComponent<UnityEngine.UI.Button>()??image.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();if(action!=null)UnityEventTools.AddPersistentListener(b.onClick,action);
            var border=image.GetComponent<UnityEngine.UI.Outline>()??image.gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectColor=Accent;border.effectDistance=new Vector2(1,-1);
            Text(b.transform,"Label",label,0,0,w,h,28);return b;
        }
        private static void Ref(SerializedObject so,string name,Object value)=>so.FindProperty(name).objectReferenceValue=value;
        private static void Fade(GameObject go)
        {
            var group=go.GetComponent<CanvasGroup>()??go.AddComponent<CanvasGroup>();
            var fade=go.GetComponent<PanelFadeIn>()??go.AddComponent<PanelFadeIn>();var so=new SerializedObject(fade);Ref(so,"canvasGroup",group);so.ApplyModifiedProperties();
        }
        private static RectTransform Scroll(Transform parent,string name,float x,float y,float w,float h)
        {
            var viewport=Image(parent,name,Color.clear,x,y,w,h);viewport.raycastTarget=true;
            if(viewport.GetComponent<UnityEngine.UI.RectMask2D>()==null)viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var content=(RectTransform)Node(viewport.transform,"Content").transform;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;content.anchoredPosition=Vector2.zero;
            Grid(content);
            var scroll=viewport.GetComponent<UnityEngine.UI.ScrollRect>()??viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.content=content;scroll.viewport=(RectTransform)viewport.transform;scroll.horizontal=false;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            return content;
        }
        private static void Grid(RectTransform content)
        {
            var vertical=content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();if(vertical!=null)Object.DestroyImmediate(vertical);
            var grid=content.GetComponent<UnityEngine.UI.GridLayoutGroup>()??content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();grid.cellSize=new Vector2(176,210);grid.spacing=new Vector2(25,36);grid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=5;grid.childAlignment=TextAnchor.UpperLeft;grid.padding=new RectOffset(3,3,6,8);
            var fit=content.GetComponent<UnityEngine.UI.ContentSizeFitter>()??content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fit.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;fit.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying)return "Stop Play Mode";
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SB/Fonts/MalgunGothic SDF.asset");
            string path="Assets/SB/Prefabs/UI/EscortInventoryItem.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            var border=prefab.GetComponent<UnityEngine.UI.Image>();border.color=Accent;Rect(prefab.transform,0,0,176,210);
            Image(prefab.transform,"Surface",Surface,2,2,172,206).transform.SetAsFirstSibling();
            Rect(prefab.transform.Find("Icon"),17,37,142,104);prefab.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>().preserveAspect=true;
            Rect(prefab.transform.Find("Name"),8,143,160,30);var n=prefab.transform.Find("Name").GetComponent<TMP_Text>();n.fontSize=18;n.enableAutoSizing=true;n.fontSizeMin=14;n.fontSizeMax=18;n.alignment=TextAlignmentOptions.Center;
            Rect(prefab.transform.Find("Grade"),8,7,130,28);prefab.transform.Find("Grade").GetComponent<TMP_Text>().fontSize=20;
            Rect(prefab.transform.Find("Count"),6,174,164,30);var count=prefab.transform.Find("Count").GetComponent<TMP_Text>();count.enableAutoSizing=false;count.fontSize=15;count.alignment=TextAlignmentOptions.Center;
            var selection=prefab.transform.Find("SelectedEffect");Fill(selection);selection.GetComponent<UnityEngine.UI.Image>().color=new Color(.05f,.85f,.8f,.16f);selection.SetAsLastSibling();selection.gameObject.SetActive(false);
            var item=new SerializedObject(prefab.GetComponent<EscortInventoryItemView>());Ref(item,"gradeBorder",border);item.ApplyModifiedProperties();
            foreach(var t in prefab.GetComponentsInChildren<TMP_Text>(true)){t.font=font;t.fontSharedMaterial=font.material;t.raycastTarget=false;}
            PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);

            var root=GameObject.Find("Canvas").transform.Find("FleetPanel");root.GetComponent<UnityEngine.UI.Image>().color=Background;root.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;Fade(root.gameObject);
            foreach(string hidden in new[]{"SummonButton","PauseNotice","SelectedEscort"})root.Find(hidden).gameObject.SetActive(false);
            var view=new SerializedObject(root.GetComponent<PlayerFleetPanelView>());Ref(view,"_summonButton",null);Ref(view,"_summonCostText",null);Ref(view,"_mergeStatusText",null);view.ApplyModifiedProperties();
            Text(root,"Title","함대",40,75,1000,70,40);
            Text(root,"FormationHeading","항해 중인 함선",44,182,992,44,25).alignment=TextAlignmentOptions.Left;
            Rect(root.Find("FormationRoot"),44,244,992,390);var formation=root.Find("FormationRoot").GetComponent<UnityEngine.UI.GridLayoutGroup>();formation.cellSize=new Vector2(300,175);formation.spacing=new Vector2(32,26);formation.constraintCount=3;
            foreach(Transform slot in root.Find("FormationRoot"))
            {
                slot.GetComponent<UnityEngine.UI.Image>().color=Surface;
                Rect(slot.Find("Icon"),75,23,150,99);slot.Find("Icon").GetComponent<UnityEngine.UI.Image>().preserveAspect=true;
                Rect(slot.Find("Name"),12,129,276,34);slot.Find("Name").GetComponent<TMP_Text>().fontSize=22;
            }
            Text(root,"OwnedHeading","보유 함선",44,695,992,44,26).alignment=TextAlignmentOptions.Left;
            Text(root,"FleetHint","함선을 선택한 뒤 배치할 슬롯을 누르세요",44,745,992,40,22).color=new Color(.4f,.65f,.68f);
            Rect(root.Find("InventoryViewport"),40,818,1000,770);var content=(RectTransform)root.Find("InventoryViewport/InventoryRoot");Grid(content);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            Text(root,"Result","",44,1615,992,70,24);
            var footer=Image(root,"Footer",new Color(.02f,.17f,.20f),0,1730,1080,190);footer.transform.SetSiblingIndex(0);
            var mergeButton=root.Find("MergeButton");Rect(mergeButton,35,1780,350,100);mergeButton.GetComponent<UnityEngine.UI.Image>().color=Surface;
            foreach(var t in mergeButton.GetComponentsInChildren<TMP_Text>(true))t.gameObject.SetActive(false);
            Text(mergeButton,"CardMergeLabel","함선 합성",0,0,350,100,29);
            var close=root.Find("CloseFleet");Rect(close,466,1780,148,100);close.GetComponent<UnityEngine.UI.Image>().color=new Color(.06f,.38f,.48f);Text(close,"Label","×",0,0,148,100,60);

            var screen=Node(root,"FleetMergeScreen");Fill(screen.transform);var screenImage=screen.GetComponent<UnityEngine.UI.Image>()??screen.AddComponent<UnityEngine.UI.Image>();screenImage.color=Background;screenImage.raycastTarget=true;Fade(screen);
            var logic=screen.GetComponent<FleetMergeScreen>()??screen.AddComponent<FleetMergeScreen>();
            Text(screen.transform,"Title","함선 합성",40,80,1000,65,40);
            Image(screen.transform,"SourceFrame",Surface,286,205,180,195);
            Image(screen.transform,"ResultFrame",Surface,614,205,180,195);
            var source=Image(screen.transform,"SourceIcon",Color.white,301,220,150,125);source.preserveAspect=true;source.enabled=false;
            var result=Image(screen.transform,"ResultIcon",Color.white,629,220,150,125);result.preserveAspect=true;result.enabled=false;
            var sourceLabel=Text(screen.transform,"SourceLabel","재료 선택",286,355,180,36,23);
            var resultLabel=Text(screen.transform,"ResultLabel","상위 등급",614,355,180,36,23);
            Text(screen.transform,"Arrow","›",478,240,124,100,70).color=Accent;
            var status=Text(screen.transform,"Status","",40,435,1000,90,25);
            var serialized=new SerializedObject(logic);Ref(serialized,"fleetManager",Object.FindFirstObjectByType<SB.Scripts.Fleet.PlayerFleetManager>(FindObjectsInactive.Include));Ref(serialized,"cardPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EscortInventoryItemView>());
            Ref(serialized,"sourceIcon",source);Ref(serialized,"resultIcon",result);Ref(serialized,"sourceLabel",sourceLabel);Ref(serialized,"resultLabel",resultLabel);Ref(serialized,"statusLabel",status);
            var icons=serialized.FindProperty("materialIcons");var labels=serialized.FindProperty("materialLabels");icons.arraySize=labels.arraySize=3;
            UnityEngine.Events.UnityAction[] remove={logic.RemoveFirst,logic.RemoveSecond,logic.RemoveThird};
            for(int i=0;i<3;i++)
            {
                var b=Button(screen.transform,"Material_"+i,"",210+i*240,555,180,190,remove[i]);
                var icon=Image(b.transform,"Icon",Color.white,15,15,150,110);icon.preserveAspect=true;icon.enabled=false;
                var label=Text(b.transform,"Label","+",5,130,170,50,20);
                icons.GetArrayElementAtIndex(i).objectReferenceValue=icon;labels.GetArrayElementAtIndex(i).objectReferenceValue=label;
            }
            Image(screen.transform,"Divider",new Color(.12f,.3f,.33f),40,790,1000,2);
            Text(screen.transform,"InventoryHeading","합성 재료 선택",44,820,992,45,25).alignment=TextAlignmentOptions.Left;
            Ref(serialized,"inventoryRoot",Scroll(screen.transform,"Inventory",40,895,1000,740));
            Image(screen.transform,"Footer",new Color(.02f,.17f,.2f),0,1730,1080,190);
            Button(screen.transform,"Close","×",40,1780,120,100,logic.Close);
            var confirm=Button(screen.transform,"Confirm","합성",350,1780,380,100,logic.Confirm);confirm.GetComponent<UnityEngine.UI.Image>().color=new Color(.025f,.5f,.52f);Ref(serialized,"confirmButton",confirm);serialized.ApplyModifiedProperties();
            var controller=new SerializedObject(root.GetComponent<PlayerFleetPanelController>());Ref(controller,"mergeScreen",logic);controller.ApplyModifiedProperties();
            screen.SetActive(false);screen.transform.SetAsLastSibling();root.Find("MergePreview").gameObject.SetActive(false);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            return "Fleet and merge card layouts saved";
        }
    }
}
