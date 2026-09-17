using System;
using System.Linq;
using SB.Scripts;
using SB.Scripts.UI.Upgrade;
using SB.Scripts.UI.Fleet;
using SB.Scripts.Upgrade;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SB.Editor
{
    // Explicit authoring positions for the 1080 x 1920 portrait frame.
    public static class HarborUxSceneSetup
    {
        private static readonly Color Navy = new Color(.035f,.075f,.105f,1);
        private static readonly Color Panel = new Color(.055f,.105f,.14f,1);
        private static readonly Color Card = new Color(.085f,.155f,.19f,1);
        private static readonly Color Brass = new Color(.84f,.67f,.39f,1);
        private static readonly Color Ink = new Color(.88f,.9f,.83f,1);
        private static TMP_FontAsset font;

        private static void Place(Transform t,float x,float y,float w,float h)
        {
            var r=(RectTransform)t; Undo.RecordObject(r,"Harbor UX layout");
            r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
        private static void Ref(Object target,string field,Object value)
        {
            var so=new SerializedObject(target);
            var p=so.FindProperty(field);
            if(p==null) throw new InvalidOperationException(target.GetType().Name+"."+field);
            p.objectReferenceValue=value;so.ApplyModifiedProperties();
        }
        private static T Read<T>(Object target,string field) where T:Object
        { return new SerializedObject(target).FindProperty(field).objectReferenceValue as T; }
        private static GameObject Node(Transform parent,string name)
        {
            var t=parent.Find(name);
            if(t!=null)return t.gameObject;
            var go=new GameObject(name,typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go,"Harbor UX element");go.transform.SetParent(parent,false);return go;
        }
        private static TMP_Text Text(Transform parent,string name,string text,float x,float y,float w,float h,float size,Color? color=null)
        {
            var go=Node(parent,name);var t=go.GetComponent<TMP_Text>()??Undo.AddComponent<TextMeshProUGUI>(go);
            Undo.RecordObject(t,"Harbor UX text");Place(go.transform,x,y,w,h);
            t.font=font;t.fontSharedMaterial=font.material;t.fontSize=size;t.enableAutoSizing=false;
            t.text=text;t.color=color??Ink;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;
            return t;
        }
        private static Image Line(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var go=Node(parent,name);var image=go.GetComponent<Image>()??Undo.AddComponent<Image>(go);
            Place(go.transform,x,y,w,h);Undo.RecordObject(image,"Harbor trim");image.color=color;image.raycastTarget=false;return image;
        }
        private static Button Button(Transform parent,string name,string label,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var image=Line(parent,name,x,y,w,h,Card);image.raycastTarget=true;
            var b=image.GetComponent<Button>()??Undo.AddComponent<Button>(image.gameObject);
            b.targetGraphic=image;Undo.RecordObject(b,"Harbor button");
            b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);
            Text(b.transform,"Label",label,15,0,w-30,h,26,Brass).alignment=TextAlignmentOptions.Center;
            return b;
        }

        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SB/Fonts/MalgunGothic SDF.asset");
            var canvas=GameObject.Find("Canvas").transform;
            var frame=canvas.Find("GameFrame");
            var camera=UnityEngine.Camera.main;Undo.RecordObject(camera.transform,"Harbor battle framing");camera.transform.position=new Vector3(.2f,-.9f,-10f);
            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject,"Harbor UX refresh");

            Place(frame.Find("BottomUIBackground"),0,980,1080,940);
            frame.Find("BottomUIBackground").GetComponent<Image>().color=Navy;
            Place(frame.Find("BattleArea"),0,0,1080,980);
            Place(frame.Find("CommandStrip"),24,990,1032,145);
            frame.Find("CommandStrip").GetComponent<Image>().color=Panel;
            Line(frame.Find("CommandStrip"),"BrassRule",0,0,1032,2,Brass);
            foreach(var t in frame.Find("CommandStrip").GetComponentsInChildren<TMP_Text>(true))t.fontSize=24;
            foreach(var slotName in new[]{"SkillSlot_1","SkillSlot_2"})
            {
                var slot=frame.Find("CommandStrip/CommanderSkillBar/"+slotName);
                var icon=slot.Find("Icon").GetComponent<Image>();
                Place(icon.transform,12,8,64,56);icon.preserveAspect=true;
                Place(slot.Find("CooldownText"),6,66,82,24);
                slot.Find("CooldownText").GetComponent<TMP_Text>().fontSize=16;
                Place(slot.Find("Label"),92,8,285,78);
            }
            Place(frame.Find("currencyBar"),24,1145,1032,70);
            var currencyLayout=frame.Find("currencyBar").GetComponent<HorizontalLayoutGroup>();
            if(currencyLayout!=null)currencyLayout.enabled=false;
            string[] currencies={"GoldCurrencyElement","DiamondCurrencyElement","EmeraldCurrencyElement"};
            string[] names={"골드 · 함대 강화","다이아 · 호위선 모집","에메랄드 · 지휘 강화"};
            for(int i=0;i<currencies.Length;i++)
            {
                var root=frame.Find("currencyBar/"+currencies[i]);Place(root,i*344,0,334,70);
                foreach(var image in root.GetComponentsInChildren<Image>(true))
                    if(image.name.ToLower().Contains("icon")){Place(image.transform,0,28,30,30);image.preserveAspect=true;}
                var value=root.GetComponentInChildren<TMP_Text>(true);Place(value.transform,40,23,290,44);value.fontSize=30;
                Text(root,"CurrencyName",names[i],0,0,334,24,19,Brass);
            }
            frame.Find("currencyBar").GetComponent<Image>().color=Navy;
            Place(frame.Find("CurrencyLogRoot"),835,240,220,100);
            var logs=frame.Find("CurrencyLogRoot").GetComponent<SB.Scripts.UI.HUD.CurrencyGainLogController>();
            var logSo=new SerializedObject(logs);logSo.FindProperty("_maxVisibleCount").intValue=2;logSo.ApplyModifiedProperties();

            foreach(string name in new[]{"UpgradePanel","CagoPanel ","TransportEquipmentPanel"})
            {
                var p=frame.Find(name);Place(p,0,1225,1080,555);p.GetComponent<Image>().color=Navy;
                Line(p,"LedgerRule",24,1,1032,2,new Color(.2f,.32f,.34f));
            }
            var upgrade=frame.Find("UpgradePanel");
            Text(upgrade,"PanelTitle","함대 정비",24,10,400,38,27,Brass);
            Text(upgrade,"HoldHint","길게 눌러 연속 강화",610,10,440,38,20).alignment=TextAlignmentOptions.MidlineRight;
            var grid=upgrade.Find("MainShipUpgradeButtons");Place(grid,24,58,1032,490);
            var layout=grid.GetComponent<GridLayoutGroup>();layout.cellSize=new Vector2(508,112);layout.spacing=new Vector2(16,10);
            var manager=Object.FindFirstObjectByType<ShipUpgradeManager>(FindObjectsInactive.Include);
            string[] titles={"본선 체력","함대 공격력","함대 사격 속도","최대 적재량","운 · 시세 호황","지휘력 최대치","지휘력 회복"};
            foreach(var slot in grid.GetComponentsInChildren<MainShipUpgradeSlotView>(true))
            {
                var so=new SerializedObject(slot);int type=so.FindProperty("_upgradeType").enumValueIndex;
                Ref(slot,"upgradeManager",manager);Ref(slot,"feedbackSurface",slot.GetComponent<Image>());
                slot.GetComponent<Image>().color=Card;
                Text(slot.transform,"Name",titles[type],16,6,470,29,24);
                Ref(slot,"effectText",Text(slot.transform,"EffectPreview","강화 수치 확인",16,37,470,30,21,new Color(.53f,.82f,.76f)));
                Place(Read<TMP_Text>(slot,"_level").transform,16,78,110,27);Read<TMP_Text>(slot,"_level").fontSize=19;
                Place(Read<TMP_Text>(slot,"_cost").transform,132,78,356,27);Read<TMP_Text>(slot,"_cost").fontSize=21;
                Line(slot.transform,"Edge",0,10,3,92,Brass);
            }
            var nav=frame.Find("PannelButtons");Place(nav,0,1788,1080,132);nav.GetComponent<Image>().color=Panel;
            foreach(var p in nav.GetComponentsInChildren<SB.Scripts.Visual.PanelView>(true))
            {var so=new SerializedObject(p);so.FindProperty("_selectedColor").colorValue=new Color(.12f,.32f,.33f);so.ApplyModifiedProperties();}
            foreach(var b in nav.GetComponentsInChildren<Button>(true))
            {
                b.GetComponent<Image>().color=Card;Place(b.transform,((RectTransform)b.transform).anchoredPosition.x,10,201,110);
                foreach(var t in b.GetComponentsInChildren<TMP_Text>(true)){t.fontSize=25;if(t.text=="장비")t.text="운송 장비";}
            }
            nav.Find("RecruitButton").GetComponent<Image>().color=new Color(.22f,.2f,.14f);
            Line(nav,"TopRule",24,0,1032,2,Brass);

            var stage=frame.Find("BattleArea/StageInfoBar");Place(stage,24,74,1032,130);stage.GetComponent<Image>().color=new Color(.035f,.075f,.105f,.91f);
            var stageView=stage.GetComponent<StageInfoBarView>();
            if(stageView==null)stageView=Object.FindFirstObjectByType<StageInfoBarView>(FindObjectsInactive.Include);
            Ref(stageView,"routeText",Text(stage,"stageText","항해 준비",20,12,760,40,28,Brass));
            Ref(stageView,"voyageText",Text(stage,"VoyageDetail","함대를 정비하고 화물을 준비하세요",20,58,770,38,21));
            var boss=stage.Find("BossButton");Place(boss,810,16,200,80);
            boss.GetComponent<Image>().sprite=null;boss.GetComponent<Image>().color=Card;
            Text(boss,"Label","보스 도전",6,0,188,80,25,Brass).alignment=TextAlignmentOptions.Center;
            Place(stage.Find("currentCagoItemCheckArea"),20,98,760,30);
            var stageController=Object.FindFirstObjectByType<StageInfoBarController>(FindObjectsInactive.Include);
            Ref(stageController,"chapterDatabase",Read<ChapterDatabase>(Object.FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include),"chapterDatabase"));
            Line(stage,"Edge",0,0,4,130,Brass);
            foreach(var popup in Object.FindObjectsByType<StageResultPopupView>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var parent=Read<GameObject>(popup,"parents");
                if(parent==null || !parent.transform.IsChildOf(frame.Find("BattleArea")))continue;
                var rect=(RectTransform)parent.transform;
                rect.SetParent(frame.Find("BattleArea"),false);
                Place(rect,210,235,660,105);
                var surface=parent.GetComponent<Image>();
                if(surface==null)surface=Undo.AddComponent<Image>(parent);
                surface.color=new Color(.035f,.075f,.105f,.9f);surface.raycastTarget=false;
                var result=Read<TMP_Text>(popup,"resultText");
                var number=Read<TMP_Text>(popup,"currentStageText");
                Place(number.transform,20,8,620,27);number.fontSize=20;number.color=Brass;number.alignment=TextAlignmentOptions.Center;
                Place(result.transform,20,40,620,49);result.fontSize=29;result.color=Ink;result.alignment=TextAlignmentOptions.Center;
            }
            var fleet=canvas.Find("FleetPanel");fleet.GetComponent<Image>().color=Navy;
            var merge=fleet.GetComponentInChildren<FleetMergeScreen>(true);
            if(merge!=null)
            {
                merge.GetComponent<Image>().color=Navy;
                Place(merge.transform.Find("Close"),40,1780,320,100);
                foreach(var label in merge.transform.Find("Close").GetComponentsInChildren<TMP_Text>(true))
                { label.text="닫기"; Place(label.transform,0,0,320,100); label.fontSize=29; }
                Place(merge.transform.Find("Confirm"),380,1780,320,100);
                foreach(var label in merge.transform.Find("Confirm").GetComponentsInChildren<TMP_Text>(true))
                { label.text="선택 합성"; Place(label.transform,0,0,320,100); }
                Button(merge.transform,"MergeAllButton","일괄 합성",720,1780,320,100,merge.MergeAll);
                Text(merge.transform,"MergeAllHint","일괄 합성: 미편성 함선만 · 새로 얻은 함선도 연속 합성",40,1690,1000,45,22,Brass);
                Place(merge.transform.Find("Status"),40,420,1000,120);
            }

            ConfigureCargo(frame);
            ConfigureEquipment(frame);
            ConfigureSettlement(frame);
            ConfigureRecruitment(frame);

            foreach(var b in canvas.GetComponentsInChildren<Button>(true))
            {
                Undo.RecordObject(b,"Harbor control states");var c=b.colors;
                c.normalColor=Color.white;c.highlightedColor=new Color(.86f,1f,.94f);
                c.pressedColor=new Color(.65f,.8f,.75f);c.disabledColor=new Color(.46f,.5f,.5f,.7f);c.fadeDuration=.12f;b.colors=c;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            return "Harbor layout, explicit UI references and cargo prefab saved.";
        }
        private static void ConfigureCargo(Transform frame)
        {
            var panel=frame.Find("CagoPanel ");
            Text(panel,"PanelTitle","화물 적재 장부",24,10,500,40,27,Brass);
            Text(panel,"CargoHint","길게 눌러 연속 적재 · 출항 전 준비",24,56,800,30,20);
            Place(panel.Find("CurrentCargoCapacityBar"),24,92,1032,45);
            Place(panel.Find("CagoItemBar"),24,490,1032,55);
            var controller=panel.GetComponentInChildren<CagoController>(true);
            var buttonRoot=Read<Transform>(controller,"buttonRoot");Place(buttonRoot,24,150,1032,330);
            var grid=buttonRoot.GetComponent<GridLayoutGroup>();
            if(grid!=null){grid.cellSize=new Vector2(1032,145);grid.spacing=new Vector2(0,14);}
            var prefab=Read<CagoButtonView>(controller,"viewPrefab");
            string path=AssetDatabase.GetAssetPath(prefab);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view=root.GetComponent<CagoButtonView>();Place(root.transform,0,0,1032,145);
                root.GetComponent<Image>().color=Card;
                var icon=Read<Image>(view,"_icon");Place(icon.transform,16,32,80,70);icon.preserveAspect=true;
                var detail=Read<TMP_Text>(view,"_weightText");Place(detail.transform,112,10,555,125);detail.fontSize=25;detail.alignment=TextAlignmentOptions.MidlineLeft;
                var plus=Read<Button>(view,"_loadButton");var minus=Read<Button>(view,"_unLoadButton");
                // The existing Buttons parent is kept; its RectTransform is explicit.
                Place(plus.transform.parent,680,0,352,145);
                Place(plus.transform,0,28,155,88);Place(minus.transform,172,28,155,88);
                foreach(var b in new[]{plus,minus})
                {b.GetComponent<Image>().color=Panel;var t=b.GetComponentInChildren<TMP_Text>();t.text=b==plus?"싣기 +":"내리기 -";t.fontSize=24;}
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void ConfigureEquipment(Transform frame)
        {
            var p=frame.Find("TransportEquipmentPanel");
            Text(p,"Title","운송 장비",24,10,350,38,27,Brass);
            Place(p.Find("EquippedIcon"),24,62,75,70);
            Place(p.Find("EquippedName"),116,58,370,34);p.Find("EquippedName").GetComponent<TMP_Text>().fontSize=26;
            Place(p.Find("EquippedEffect"),116,93,370,42);p.Find("EquippedEffect").GetComponent<TMP_Text>().fontSize=23;
            Ref(p.GetComponent<TransportEquipmentPanelView>(),"applicationText",Text(p,"Application","",500,55,550,84,20));
            Place(p.Find("EquipmentItems"),24,164,1032,370);
            var grid=p.Find("EquipmentItems").GetComponent<GridLayoutGroup>();if(grid!=null){grid.cellSize=new Vector2(508,166);grid.spacing=new Vector2(16,14);}
            foreach(var item in p.Find("EquipmentItems").GetComponentsInChildren<TransportEquipmentItemButtonView>(true))
            {item.GetComponent<Image>().color=Card;foreach(var t in item.GetComponentsInChildren<TMP_Text>(true))t.fontSize=24;}
        }
        private static void ConfigureSettlement(Transform frame)
        {
            var root=frame.Find("SaleSettlementUI");var panel=root.Find("SaleSettlementPanel");
            Place(panel,90,310,900,610);panel.GetComponent<Image>().color=Panel;
            Text(panel,"LedgerHeading","항 만  정 산  장 부",32,22,836,38,22,Brass).alignment=TextAlignmentOptions.Center;
            Place(panel.Find("Title"),32,78,836,62);panel.Find("Title").GetComponent<TMP_Text>().fontSize=36;
            Place(panel.Find("CargoSummary"),32,145,836,86);panel.Find("CargoSummary").GetComponent<TMP_Text>().fontSize=23;
            Line(panel,"BrassRule",32,242,836,2,Brass);
            int i=0;foreach(var slot in panel.GetComponentsInChildren<SaleRewardSlotView>(true))
            {
                Place(slot.transform,32,266+i*87,836,78);
                var surface=slot.GetComponent<Image>();if(surface!=null)surface.color=Card;
                Place(slot.transform.Find("Icon"),8,15,42,42);
                Place(slot.transform.Find("CurrencyName"),72,0,270,78);slot.transform.Find("CurrencyName").GetComponent<TMP_Text>().fontSize=27;
                Place(slot.transform.Find("Amount"),345,0,475,78);var amount=slot.transform.Find("Amount").GetComponent<TMP_Text>();amount.fontSize=40;amount.color=Brass;amount.alignment=TextAlignmentOptions.MidlineRight;i++;
            }
            Text(panel,"Footer","정산 완료 · 함대를 정비해 다음 항로로",32,548,836,42,23).alignment=TextAlignmentOptions.Center;
            Button(frame.Find("UpgradePanel"),"LastSettlement","지난 정산",420,10,180,38,root.GetComponent<SaleSettlementView>().ReplayLastSettlement);
        }
        private static void ConfigureRecruitment(Transform frame)
        {
            var summon=GameObject.Find("SummonUI");var portrait=summon.transform.Find("PortraitFrame");var result=portrait.Find("ResultPanel");
            var grid=result.Find("ResultGrid");Place(grid,64,450,952,950);
            var layout=grid.GetComponent<GridLayoutGroup>();if(layout!=null)layout.enabled=false;
            foreach(var view in summon.GetComponents<ShipRecruitmentView>())
            {
                Ref(view,"panelManager",frame.Find("PannelButtons").GetComponent<SB.Scripts.Visual.PannelManager>());
                var so=new SerializedObject(view);so.FindProperty("singleCardPosition").vector2Value=new Vector2(476,-350);
                so.FindProperty("singleCardScale").floatValue=2.2f;so.FindProperty("revealSeconds").floatValue=.38f;
                var cards=so.FindProperty("cards");
                for(int i=0;i<cards.arraySize;i++)
                {
                    var entry=cards.GetArrayElementAtIndex(i);var rect=entry.FindPropertyRelative("root").objectReferenceValue as RectTransform;
                    if(rect==null)continue;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(200,240);
                    var position=new Vector2(110+(i%4)*244,-145-(i/4)*290);rect.anchoredPosition=position;entry.FindPropertyRelative("gridPosition").vector2Value=position;
                    var background=rect.Find("CardBackground") as RectTransform;
                    if(background!=null){background.anchorMin=Vector2.zero;background.anchorMax=Vector2.one;background.offsetMin=new Vector2(2,2);background.offsetMax=new Vector2(-2,-2);}
                    Place(rect.Find("ShipIcon"),24,48,152,112);
                    Place(rect.Find("Rarity"),12,10,176,30);
                    Place(rect.Find("Grade"),12,183,176,44);
                    rect.Find("Grade").GetComponent<TMP_Text>().fontSize=20;
                }
                so.ApplyModifiedProperties();
            }
            var single=portrait.Find("ShopPanel/ShipSummonCard/SingleSummonButton").GetComponent<Button>();
            var active=single.onClick.GetPersistentTarget(0) as ShipRecruitmentView;
            if(active==null)throw new InvalidOperationException("Recruitment button missing view");
            var formation=Button(result,"FormationButton","함대에서 편성",388,1660,304,105,active.OpenFormation);
            var repeat=Button(result,"RepeatMultiButton","11회 다시 모집",712,1660,304,105,active.RepeatMultiSummon);
            foreach(var view in summon.GetComponents<ShipRecruitmentView>())
            {
                Ref(view,"formationButton",formation.gameObject);
                Ref(view,"repeatMultiButton",repeat);
            }
            Place(result.Find("ConfirmButton"),64,1660,304,105);
            foreach(var t in result.Find("ConfirmButton").GetComponentsInChildren<TMP_Text>(true))t.text="확인";
            result.GetComponent<Image>().color=Navy;
            Text(result,"Title","새 호위선 입항",64,125,952,82,44,Brass).alignment=TextAlignmentOptions.Center;
            Text(result,"Subtitle","함대에 배치해 함께 출항하세요",64,330,952,55,25).alignment=TextAlignmentOptions.Center;
            Line(result,"LedgerTop",64,420,952,2,Brass);
            foreach(var t in portrait.Find("ShopPanel").GetComponentsInChildren<TMP_Text>(true))if(t.text=="레어")t.text="희귀";
        }
    }
}
