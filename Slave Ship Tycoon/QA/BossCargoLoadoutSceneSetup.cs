using System;
using System.Collections.Generic;
using SB.Scripts;
using SB.Scripts.TransportEquipment;
using SB.SO.TransportItemSOs;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BossCargoLoadoutSceneSetup
{
    private static TMP_FontAsset font;
    private static readonly Color Background = new Color(.018f,.055f,.065f,1f);
    private static readonly Color Surface = new Color(.028f,.075f,.09f,1f);
    private static readonly Color Cyan = new Color(.02f,.77f,.83f,1f);
    private static readonly Color TextColor = new Color(.93f,.96f,.97f,1f);
    private static readonly Color Muted = new Color(.52f,.7f,.75f,1f);
    private static readonly Color Edge = new Color(.12f,.25f,.29f,1f);
    private static readonly Color[] CargoColors = { new Color(1f,.59f,.06f,1f), new Color(.8f,.65f,.4f,1f) };
    private static readonly List<CargoLoadoutCardView> cards = new List<CargoLoadoutCardView>();
    private static UnityEngine.UI.Image[] currencyIcons;
    private static CargoManager cargo;
    private static BossCargoLoadoutPanel controller;

    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring cargo UI.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SB/Fonts/MalgunGothic SDF.asset");
        Transform frame = GameObject.Find("Canvas").transform.Find("GameFrame");
        cargo = GameObject.Find("Managers").transform.Find("Cago/CargoManager").GetComponent<CargoManager>();
        var flow = GameObject.Find("Managers").transform.Find("GameFlowManager").GetComponent<GameFlowManager>();
        var equipment = GameObject.Find("Managers").transform.Find("TransportEquipmentManager").GetComponent<TransportEquipmentManager>();
        var items = new[] {
            AssetDatabase.LoadAssetAtPath<TransportItemSO>("Assets/SB/SO/TransportItemSOs/GoldBox.asset"),
            AssetDatabase.LoadAssetAtPath<TransportItemSO>("Assets/SB/SO/TransportItemSOs/ShipElementBox.asset")
        };
        ConfigureRewards(items[0], new[] {20480200,1120,90});
        ConfigureRewards(items[1], new[] {24930100,1400,160});
        var materialIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SB/Art/Sprites/Cargo/TransportCargoCrate_V2.png");
        if (materialIcon != null) { Undo.RecordObject(items[1],"Cargo material icon"); items[1].Icon=materialIcon; EditorUtility.SetDirty(items[1]); }

        var header = frame.Find("currencyBar");
        string[] currencyNames = {"GoldCurrencyElement","DiamondCurrencyElement","EmeraldCurrencyElement"};
        currencyIcons = new UnityEngine.UI.Image[3];
        for (int i=0;i<3;i++) currencyIcons[i]=header.Find(currencyNames[i]+"/icon").GetComponent<UnityEngine.UI.Image>();

        GameObject root = Node(frame,"BossCargoLoadoutUI");
        Stretch(root.transform);
        controller = root.GetComponent<BossCargoLoadoutPanel>() ?? Undo.AddComponent<BossCargoLoadoutPanel>(root);
        Ref(controller,"cargoManager",cargo); Ref(controller,"gameFlowManager",flow); Ref(controller,"transportEquipmentManager",equipment);
        Ref(flow,"cargoManager",cargo);
        GameObject panel = Box(root.transform,"Panel",0,0,1080,1920,Background).gameObject;
        Stretch(panel.transform);
        panel.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
        Ref(controller,"panelRoot",panel);
        root.transform.SetAsLastSibling();

        var hud = panel.transform.Find("CurrencyHeader");
        if (hud==null)
        {
            var copy=Object.Instantiate(header.gameObject,panel.transform,false);
            Undo.RegisterCreatedObjectUndo(copy,"Cargo currency header"); copy.name="CurrencyHeader";hud=copy.transform;
        }
        Place(hud,0,0,1080,94);
        var headerLayout=hud.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();if(headerLayout!=null)headerLayout.enabled=false;
        hud.GetComponent<UnityEngine.UI.Image>().color=Background;
        for(int i=0;i<3;i++)
        {
            var element=hud.Find(currencyNames[i]);Place(element,i*360,0,360,94);
            Place(element.Find("icon"),24,40,34,34);
            var value=element.Find("CurrencyValue").GetComponent<TMP_Text>();Place(value.transform,72,33,274,46);value.fontSize=30;
            var name=element.Find("CurrencyName").GetComponent<TMP_Text>();Place(name.transform,24,6,326,28);name.fontSize=20;
            var marker=element.Find("CurrencyMarker");if(marker!=null)marker.gameObject.SetActive(false);
            Box(hud,"Divider"+i,i*360,0,1,94,Edge);
        }
        Box(panel.transform,"HeaderRule",0,94,1080,2,Edge);
        Label(panel.transform,"Title","적재",40,128,1000,65,48,TextAlignmentOptions.Center);
        Label(panel.transform,"Subtitle","기존 적재 설정을 바탕으로 수량과 화물을 조정하세요",40,198,1000,34,23,TextAlignmentOptions.Center,Muted);
        var loadout=Box(panel.transform,"Loadout",32,250,1016,1000,Surface,true).transform;
        Label(loadout,"CapacityLabel","현재 적재량",24,22,470,46,30);
        Ref(controller,"capacityText",Label(loadout,"Capacity","0 / 300 t",512,22,480,46,31,TextAlignmentOptions.MidlineRight));
        Box(loadout,"CapacityTrack",24,82,968,48,Background,true);
        var fill=Box(loadout,"CapacityFill",30,88,956,36,Cyan);
        fill.sprite=LoadCapacityFillSprite();
        fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.fillAmount=0;
        Ref(controller,"capacityFill",fill);
        var preview=Box(loadout,"RewardPreview",24,154,968,165,Background,true).transform;
        Label(preview,"Heading","예상 획득 재화",26,12,550,40,28);
        string[] labels={"골드","다이아","에메랄드"};
        string[] rewardFields={"goldRewardText","diamondRewardText","emeraldRewardText"};
        for(int i=0;i<3;i++)
        {
            Icon(preview,"Icon"+i,currencyIcons[i].sprite,currencyIcons[i].color,30+i*316,68,42,42);
            var rewardLabel=Label(preview,"Reward"+i,"0",84+i*316,55,234,42,28);
            rewardLabel.enableAutoSizing=true;rewardLabel.fontSizeMin=14;rewardLabel.fontSizeMax=28;
            Ref(controller,rewardFields[i],rewardLabel);
            Label(preview,"Name"+i,labels[i],84+i*316,96,230,28,22,TextAlignmentOptions.MidlineLeft,Muted);
            if(i>0)Box(preview,"Divider"+i,i*316,65,1,68,Edge);
        }
        Label(loadout,"EstimateHint","기본 시세 x1 · 장비 효과 포함 · 운송 중 시세에 따라 변동",24,327,968,30,20,TextAlignmentOptions.MidlineRight,Muted);
        Label(loadout,"SelectedHeading","선택한 화물",24,373,520,35,27);
        Label(loadout,"SelectedHint","− 수량 감소   × 종류 전체 제거",470,373,522,35,20,TextAlignmentOptions.MidlineRight,Muted);
        var grid=Node(loadout,"SelectedCargoSlots").transform;Place(grid,24,428,968,620);
        var gridLayout=grid.GetComponent<UnityEngine.UI.GridLayoutGroup>()??Undo.AddComponent<UnityEngine.UI.GridLayoutGroup>(grid.gameObject);
        gridLayout.cellSize=new Vector2(150,258);gridLayout.spacing=new Vector2(12,22);gridLayout.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;gridLayout.constraintCount=6;
        cards.Clear();
        for(int i=0;i<2;i++)CreateCard(grid,"SelectedCargo"+i,items[i],i,CargoLoadoutCardMode.SelectedCargo,150,258);
        for(int i=2;i<12;i++)
        {
            var empty=Button(grid,"EmptySlot"+i,"+",0,0,150,258,controller.ShowCargoPicker,Background,Edge,48);
            empty.GetComponentInChildren<TMP_Text>().color=Muted;
        }
        Label(loadout,"StackHint","같은 종류의 화물은 수량을 합쳐 표시합니다",24,970,968,24,19,TextAlignmentOptions.MidlineRight,Muted);

        var available=Box(panel.transform,"AvailableCargo",32,1260,1016,395,Surface,true).transform;
        Label(available,"Heading","적재 가능한 화물",24,12,530,44,29);
        Label(available,"Hint","최대 적재량까지 자유롭게 선택",480,12,512,44,21,TextAlignmentOptions.MidlineRight,Cyan);
        for(int i=0;i<2;i++)
        {
            var card=CreateCard(available,"AvailableCargo"+i,items[i],i,CargoLoadoutCardMode.AvailableCargo,472,308);
            Place(card.transform,24+i*496,66,472,308);
        }
        Ref(controller,"statusText",Label(panel.transform,"Status","보스 승리 후 운송 해역 진입 시 본선에 적재됩니다.",40,1672,1000,38,22,TextAlignmentOptions.Center,Cyan));
        var footer=Box(panel.transform,"Footer",0,1720,1080,200,new Color(.012f,.105f,.125f,1f)).transform;
        Button(footer,"CloseButton","닫기",40,34,310,112,controller.Close,Background,Edge,34);
        var confirm=Button(footer,"ConfirmButton","적재완료/보스 도전",376,34,664,112,controller.ConfirmBossChallenge,new Color(0,.44f,.49f,1f),Cyan,33);
        Ref(controller,"confirmButton",confirm);
        Label(footer,"RememberHint","설정은 다음 도전에도 유지됩니다",40,157,1000,30,20,TextAlignmentOptions.Center,Muted);

        var picker=Box(panel.transform,"CargoPicker",0,0,1080,1920,new Color(0,0,0,.8f)).gameObject;Stretch(picker.transform);picker.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
        Ref(controller,"cargoPickerRoot",picker);
        var pickerPanel=Box(picker.transform,"ChoicePanel",90,610,900,640,Surface,true).transform;
        Label(pickerPanel,"Heading","적재할 화물을 선택하세요",30,22,840,55,32,TextAlignmentOptions.Center);
        for(int i=0;i<2;i++)
        {
            var card=CreateCard(pickerPanel,"ChoiceCargo"+i,items[i],i,CargoLoadoutCardMode.AvailableCargo,408,400);
            Place(card.transform,30+i*432,100,408,400);
            UnityEventTools.AddPersistentListener(Read<UnityEngine.UI.Button>(card,"loadButton").onClick,controller.CloseCargoPicker);
        }
        Button(pickerPanel,"CloseButton","닫기",292,536,316,72,controller.CloseCargoPicker,Background,Edge,28);
        ArrayRefs(controller,"cargoCards",cards.ToArray());
        picker.SetActive(false);panel.SetActive(false);

        var boss=frame.Find("BattleArea/StageInfoBar/BossButton").GetComponent<UnityEngine.UI.Button>();
        Undo.RecordObject(boss,"Boss preparation action");boss.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(boss.onClick,controller.Open);
        ConfigureNavigation(frame);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return "Boss cargo preparation UI, references, three-currency rewards and three-button navigation saved.";
    }

    private static CargoLoadoutCardView CreateCard(Transform parent,string name,TransportItemSO item,int typeIndex,CargoLoadoutCardMode mode,float w,float h)
    {
        var surface=Box(parent,name,0,0,w,h,Background,true,CargoColors[typeIndex]);
        var view=surface.GetComponent<CargoLoadoutCardView>()??Undo.AddComponent<CargoLoadoutCardView>(surface.gameObject);
        Ref(view,"cargoManager",cargo);Ref(view,"cargoItem",item);
        var so=new SerializedObject(view);so.FindProperty("cargoDisplayName").stringValue=typeIndex==0?"금괴 화물":"선박 자재";so.FindProperty("displayMode").enumValueIndex=(int)mode;so.ApplyModifiedProperties();
        var content=Node(surface.transform,"Content");Stretch(content.transform);Ref(view,"contentRoot",content);
        bool selected=mode==CargoLoadoutCardMode.SelectedCargo;
        float titleSize=selected?20:27;
        Ref(view,"nameText",Label(content.transform,"Name",typeIndex==0?"금괴 화물":"선박 자재",8,8,selected?w-54:w-16,32,selected?18:titleSize,TextAlignmentOptions.Center));
        Ref(view,"icon",Icon(content.transform,"CargoIcon",item.Icon,Color.white,selected?12:((w-156)/2),44,selected?126:156,70));
        Ref(view,"amountText",Label(content.transform,"Amount","x0",selected?38:10,selected?111:119,selected?74:w-20,30,selected?24:21,TextAlignmentOptions.Center));
        Ref(view,"weightText",Label(content.transform,"Weight","0 t",10,selected?165:153,w-20,26,selected?20:22,TextAlignmentOptions.Center,Muted));
        string[] fields={"goldText","diamondText","emeraldText"};
        for(int i=0;i<3;i++)
        {
            float y=selected?197+i*18:184+i*21;
            Icon(content.transform,"RewardIcon"+i,currencyIcons[i].sprite,currencyIcons[i].color,selected?8:24,y,selected?16:22,selected?16:22);
            var rewardLabel=Label(content.transform,"Reward"+i,"0",selected?30:56,y-2,w-(selected?36:72),selected?20:27,selected?16:23,TextAlignmentOptions.MidlineLeft,i==0?new Color(1,.78f,.26f,1):i==1?Cyan:new Color(.16f,1f,.46f,1));
            if(selected){rewardLabel.enableAutoSizing=true;rewardLabel.fontSizeMin=10;rewardLabel.fontSizeMax=16;}
            Ref(view,fields[i],rewardLabel);
        }
        if(selected)
        {
            Ref(view,"unloadButton",Button(content.transform,"DecreaseButton","−",8,112,30,30,view.SubtractCargo,Surface,Edge,24));
            Ref(view,"loadButton",Button(content.transform,"IncreaseButton","+",112,112,30,30,view.AddCargo,Surface,Edge,24));
            Ref(view,"removeButton",Button(content.transform,"RemoveButton","×",112,8,30,30,view.RemoveCargo,Surface,Edge,22));
            var empty=Button(surface.transform,"Empty","+",0,0,w,h,view.AddCargo,Background,Edge,44).gameObject;
            Label(empty.transform,"CargoName",typeIndex==0?"금괴 화물":"선박 자재",8,h-42,w-16,30,19,TextAlignmentOptions.Center,Muted);
            Ref(view,"emptyRoot",empty);content.SetActive(false);
            Ref(view,"emptyLoadButton",empty.GetComponent<UnityEngine.UI.Button>());
        }
        else Ref(view,"loadButton",Button(content.transform,"LoadButton","적재 +",16,h-54,w-32,42,view.AddCargo,new Color(.025f,.24f,.27f,1),CargoColors[typeIndex],26));
        cards.Add(view);return view;
    }

    private static void ConfigureNavigation(Transform frame)
    {
        var nav=frame.Find("PannelButtons");
        var oldCargo=nav.Find("CagoPanelButton");var oldEquipment=nav.Find("TransportEquipmentPanelButton");
        Undo.RecordObject(oldCargo.gameObject,"Remove cargo navigation");oldCargo.gameObject.SetActive(false);
        Undo.RecordObject(oldEquipment.gameObject,"Remove equipment navigation");oldEquipment.gameObject.SetActive(false);
        frame.Find("CagoPanel ").gameObject.SetActive(false);frame.Find("TransportEquipmentPanel").gameObject.SetActive(false);
        var manager=nav.GetComponent<SB.Scripts.Visual.PannelManager>();var serialized=new SerializedObject(manager);var entries=serialized.FindProperty("_panelInfo");
        for(int i=entries.arraySize-1;i>=0;i--) {int t=entries.GetArrayElementAtIndex(i).FindPropertyRelative("type").enumValueIndex;if(t==1||t==3)entries.DeleteArrayElementAtIndex(i);}
        serialized.ApplyModifiedProperties();
        var layout=nav.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        Undo.RecordObject(layout,"Three navigation buttons");layout.enabled=true;layout.padding=new RectOffset(16,16,10,10);layout.spacing=14;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=true;
        string[] names={"UpgradePanelButton","FleetPanelButton","RecruitButton"};
        for(int i=0;i<names.Length;i++)
        {
            var button=nav.Find(names[i]);button.SetSiblingIndex(i);
            var element=button.GetComponent<UnityEngine.UI.LayoutElement>()??Undo.AddComponent<UnityEngine.UI.LayoutElement>(button.gameObject);
            element.minWidth=0;element.preferredWidth=320;element.flexibleWidth=1;element.preferredHeight=110;
        }
        var trim=nav.Find("TopRule");if(trim!=null)(trim.GetComponent<UnityEngine.UI.LayoutElement>()??Undo.AddComponent<UnityEngine.UI.LayoutElement>(trim.gameObject)).ignoreLayout=true;
    }

    private static Sprite LoadCapacityFillSprite()
    {
        const string folder="Assets/SB/Art/Sprites/UI";
        const string path=folder+"/CargoCapacityFill.png";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/SB/Art/Sprites","UI");
        if(!System.IO.File.Exists(path))
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=false;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void ConfigureRewards(TransportItemSO item,int[] amounts)
    {
        Undo.RecordObject(item,"Cargo three-currency rewards");
        var serialized=new SerializedObject(item);var rewards=serialized.FindProperty("Rewards");rewards.arraySize=3;
        for(int i=0;i<3;i++){var e=rewards.GetArrayElementAtIndex(i);e.FindPropertyRelative("CurrencyType").enumValueIndex=i;e.FindPropertyRelative("CurrencyAmount").intValue=amounts[i];}
        serialized.ApplyModifiedProperties();EditorUtility.SetDirty(item);
    }
    private static GameObject Node(Transform parent,string name)
    {
        var existing=parent.Find(name);if(existing!=null)return existing.gameObject;
        var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Cargo UI element");go.layer=5;go.transform.SetParent(parent,false);return go;
    }
    private static void Place(Transform t,float x,float y,float w,float h)
    {
        var r=(RectTransform)t;Undo.RecordObject(r,"Cargo UI layout");r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
    }
    private static void Stretch(Transform t)
    {
        var r=(RectTransform)t;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;
    }
    private static UnityEngine.UI.Image Box(Transform parent,string name,float x,float y,float w,float h,Color color,bool border=false,Color? borderColor=null)
    {
        var go=Node(parent,name);Place(go.transform,x,y,w,h);var image=go.GetComponent<UnityEngine.UI.Image>()??Undo.AddComponent<UnityEngine.UI.Image>(go);image.color=color;image.raycastTarget=false;
        if(border){var outline=go.GetComponent<UnityEngine.UI.Outline>()??Undo.AddComponent<UnityEngine.UI.Outline>(go);outline.effectColor=borderColor??Edge;outline.effectDistance=new Vector2(1.5f,-1.5f);}
        return image;
    }
    private static UnityEngine.UI.Image Icon(Transform parent,string name,Sprite sprite,Color color,float x,float y,float w,float h)
    {var icon=Box(parent,name,x,y,w,h,color);icon.sprite=sprite;icon.preserveAspect=true;return icon;}
    private static TMP_Text Label(Transform parent,string name,string text,float x,float y,float w,float h,float size,TextAlignmentOptions alignment=TextAlignmentOptions.MidlineLeft,Color? color=null)
    {
        var go=Node(parent,name);Place(go.transform,x,y,w,h);var label=go.GetComponent<TMP_Text>()??Undo.AddComponent<TextMeshProUGUI>(go);label.font=font;label.fontSharedMaterial=font.material;label.fontSize=size;label.enableAutoSizing=false;label.text=text;label.alignment=alignment;label.color=color??TextColor;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Overflow;return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent,string name,string text,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,Color color,Color border,float size)
    {
        var surface=Box(parent,name,x,y,w,h,color,true,border);surface.raycastTarget=true;var button=surface.GetComponent<UnityEngine.UI.Button>()??Undo.AddComponent<UnityEngine.UI.Button>(surface.gameObject);
        button.targetGraphic=surface;button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.85f,1,1);colors.pressedColor=new Color(.6f,.85f,.86f);colors.disabledColor=new Color(.4f,.5f,.5f,.7f);button.colors=colors;
        Label(surface.transform,"Label",text,2,0,w-4,h,size,TextAlignmentOptions.Center);return button;
    }
    private static void Ref(Object target,string field,Object value)
    {var so=new SerializedObject(target);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(target.GetType().Name+"."+field);p.objectReferenceValue=value;so.ApplyModifiedProperties();}
    private static T Read<T>(Object target,string field) where T:Object
    {return new SerializedObject(target).FindProperty(field).objectReferenceValue as T;}
    private static void ArrayRefs(Object target,string field,Object[] values)
    {var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedProperties();}
}
