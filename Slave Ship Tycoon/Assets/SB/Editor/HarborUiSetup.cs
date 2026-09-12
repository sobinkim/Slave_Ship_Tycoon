using System.Linq;
using SB.Scripts.UI.Fleet;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SB.Editor
{
    public static class HarborUiSetup
    {
        private static readonly Color Navy = new Color(.035f,.065f,.105f,1);
        private static readonly Color Surface = new Color(.075f,.12f,.18f,1);
        private static readonly Color Teal = new Color(.08f,.52f,.54f,1);
        private static TMP_FontAsset font;
        private static Transform Find(Transform root, string path) => root.Find(path);
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0,1);
            rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(x,-y);
            rect.sizeDelta = new Vector2(width,height);
        }
        private static TMP_Text Label(Transform root, string name, string value, float x, float y, float w, float h, float size)
        {
            Transform existing = root.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
            go.transform.SetParent(root,false);
            Place((RectTransform)go.transform,x,y,w,h);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.font = font; text.fontSharedMaterial = font.material; text.text = value;
            text.fontSize = size; text.color = new Color(.91f,.95f,.98f);
            text.alignment = TextAlignmentOptions.Midline; text.raycastTarget = false;
            return text;
        }
        private static void Wire(UnityEngine.UI.Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick,action);
        }
        private static void Ref(SerializedObject so, string key, Object value) => so.FindProperty(key).objectReferenceValue = value;

        public static string Apply()
        {
            if (EditorApplication.isPlaying) return "Stop Play Mode first";
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SB/Fonts/MalgunGothic SDF.asset");
            Transform canvas = GameObject.Find("Canvas").transform;
            Transform frame = canvas.Find("GameFrame");
            Transform summon = GameObject.Find("SummonUI").transform;
            Transform portrait = summon.Find("PortraitFrame");
            Transform shop = portrait.Find("ShopPanel");
            Transform results = portrait.Find("ResultPanel");
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                Undo.RecordObject(text,"Restore readable UI text");
                if (text.font == font) { text.fontSharedMaterial = font.material; text.enabled = true; }
                text.raycastTarget = false;
            }
            foreach (UnityEngine.UI.Image image in frame.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                string name = image.name;
                if (name == "ScreenFadePanel" || name.Contains("Effect") || name.Contains("Cooldown") || name.ToLower().Contains("icon") || name == "Fill") continue;
                if (image.sprite != null && image.sprite.name != "UISprite" && image.sprite.name != "Background") continue;
                Undo.RecordObject(image,"Harbor UI surface");
                if (image.GetComponent<UnityEngine.UI.Button>() != null) image.color = Surface;
                else if (name.Contains("Panel") || name.Contains("Background") || name == "currencyBar" || name == "CommandStrip" || name == "topUIArea") image.color = Navy;
            }
            foreach (UnityEngine.UI.Button button in frame.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var colors = button.colors; colors.highlightedColor = new Color(.65f,1,1); colors.pressedColor = new Color(.35f,.72f,.78f); colors.disabledColor = new Color(.65f,.7f,.75f,.65f); button.colors = colors;
            }
            foreach (var panel in frame.GetComponentsInChildren<SB.Scripts.Visual.PanelView>(true))
            {
                var settings = new SerializedObject(panel); settings.FindProperty("_selectedColor").colorValue = Teal; settings.ApplyModifiedProperties();
            }
            foreach (UnityEngine.UI.Image image in shop.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.name.StartsWith("Rarity") || image.transform.parent.name.StartsWith("Rarity") || image.name == "ShipIcon") continue;
                image.color = image.name.Contains("Trim") || image.name.Contains("Underline") ? Teal : image.GetComponent<UnityEngine.UI.Button>() != null ? Teal : Surface;
                if (image.name == "Inset" || image.name == "CardSurface") image.color = Navy;
            }
            shop.GetComponent<UnityEngine.UI.Image>().color = Navy;
            results.GetComponent<UnityEngine.UI.Image>().color = Navy;
            Label(shop,"Title","호위선 모집",40,55,800,75,48);
            Label(shop,"Subtitle","함대를 확장하고, 더 먼 항로로",40,135,850,50,26);
            Label(shop,"Eyebrow","H A R B O R   /   F L E E T",40,25,850,30,18);
            var balance = Label(shop,"RecruitBalance","",40,200,920,45,28);
            var status = Label(shop,"RecruitStatus","",40,1010,990,70,25);
            foreach (string path in new[]{"CategoryTab","PreviewResultsButton","ShipSummonCard/AdSummonButton","ShipSummonCard/LevelProgress","ShipSummonCard/ProgressValue","ShipSummonCard/Level"})
                if (shop.Find(path)!=null) shop.Find(path).gameObject.SetActive(false);
            foreach (TMP_Text t in shop.GetComponentsInChildren<TMP_Text>(true))
                if (t.text.Contains("미리보기") || t.text.Contains("소환 레벨") || t.text == "0 / 100") t.gameObject.SetActive(false);
            results.Find("AutoSummonToggle").gameObject.SetActive(false);
            results.Find("Summon33Button").gameObject.SetActive(false);
            results.Find("Summon11Button").gameObject.SetActive(false);
            foreach (Transform ray in results.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("GlowRay"))) ray.gameObject.SetActive(false);
            Label(results,"Title","새로운 함대의 동료",48,120,984,90,46);
            var count = Label(results,"Count","",48,225,984,70,25);
            var confirm = results.Find("ConfirmButton").GetComponent<UnityEngine.UI.Button>();
            Place((RectTransform)confirm.transform,290,1680,500,110);
            confirm.GetComponentInChildren<TMP_Text>().text = "확인";
            Transform skip = results.Find("SkipReveal");
            if(skip==null) { var go=new GameObject("SkipReveal",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));skip=go.transform;skip.SetParent(results,false); }
            Place((RectTransform)skip,720,55,300,70); skip.GetComponent<UnityEngine.UI.Image>().color=Surface;
            Label(skip,"Label","건너뛰기",0,0,300,70,25);
            Transform flash=results.Find("RevealFlash");
            if(flash==null){ var go=new GameObject("RevealFlash",typeof(RectTransform),typeof(UnityEngine.UI.Image));flash=go.transform;flash.SetParent(results,false); }
            Place((RectTransform)flash,0,0,1080,1920);flash.GetComponent<UnityEngine.UI.Image>().color=Color.clear;flash.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var view=summon.GetComponent<ShipRecruitmentView>();if(view==null)view=Undo.AddComponent<ShipRecruitmentView>(summon.gameObject);
            var so=new SerializedObject(view);
            Ref(so,"shopPanel",shop.gameObject);Ref(so,"resultPanel",results.gameObject);Ref(so,"statusText",status);Ref(so,"balanceText",balance);
            Ref(so,"singleCostText",shop.Find("ShipSummonCard/SingleCost").GetComponent<TMP_Text>());
            Ref(so,"multiCostText",shop.Find("ShipSummonCard/MultiCost").GetComponent<TMP_Text>());
            Ref(so,"resultCountText",count);Ref(so,"confirmButton",confirm.gameObject);Ref(so,"skipButton",skip.gameObject);Ref(so,"revealFlash",flash.GetComponent<UnityEngine.UI.Image>());
            var single=shop.Find("ShipSummonCard/SingleSummonButton").GetComponent<UnityEngine.UI.Button>();
            var multi=shop.Find("ShipSummonCard/MultiSummonButton").GetComponent<UnityEngine.UI.Button>();
            Ref(so,"singleButton",single);Ref(so,"multiButton",multi);
            var cardArray=so.FindProperty("cards");cardArray.arraySize=11;
            for(int i=0;i<11;i++)
            {
                Transform card=results.Find("ResultGrid/ResultItem_"+(i+1));
                var group=card.GetComponent<CanvasGroup>();if(group==null)group=card.gameObject.AddComponent<CanvasGroup>();
                var entry=cardArray.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("root").objectReferenceValue=card;
                entry.FindPropertyRelative("visibility").objectReferenceValue=group;
                entry.FindPropertyRelative("icon").objectReferenceValue=card.Find("ShipIcon").GetComponent<UnityEngine.UI.Image>();
                entry.FindPropertyRelative("surface").objectReferenceValue=card.Find("CardBackground").GetComponent<UnityEngine.UI.Image>();
                entry.FindPropertyRelative("title").objectReferenceValue=card.Find("Grade").GetComponent<TMP_Text>();
                entry.FindPropertyRelative("grade").objectReferenceValue=card.Find("Rarity").GetComponent<TMP_Text>();
                var name=card.Find("Grade").GetComponent<TMP_Text>();name.fontSize=20;name.enableAutoSizing=true;name.fontSizeMin=14;name.fontSizeMax=20;
            }
            so.ApplyModifiedProperties();
            Wire(single,view.SummonSingle);Wire(multi,view.SummonMulti);Wire(confirm,view.Confirm);Wire(skip.GetComponent<UnityEngine.UI.Button>(),view.SkipReveal);
            Wire(shop.Find("CloseButton").GetComponent<UnityEngine.UI.Button>(),view.Close);
            shop.gameObject.SetActive(false);results.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(summon.gameObject,"Assets/SB/Prefabs/UI/Summon/SummonUI.prefab");
            view=GameObject.Find("SummonUI").GetComponent<ShipRecruitmentView>();
            so=new SerializedObject(view);Ref(so,"gachaManager",Object.FindFirstObjectByType<SB.Scripts.Fleet.EscortGachaManager>(FindObjectsInactive.Include));Ref(so,"currencyManager",Object.FindFirstObjectByType<SB.Scripts.Currency.CurrencyManager>(FindObjectsInactive.Include));so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            Transform nav=frame.Find("PannelButtons");
            Transform recruit=nav.Find("RecruitButton");
            if(recruit==null){var go=new GameObject("RecruitButton",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));recruit=go.transform;recruit.SetParent(nav,false);}
            recruit.GetComponent<UnityEngine.UI.Image>().color=Teal;
            string[] navigation={"UpgradePanelButton","FleetPanelButton","RecruitButton","CagoPanelButton","TransportEquipmentPanelButton"};
            var layout=nav.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();if(layout!=null)layout.enabled=false;
            for(int i=0;i<navigation.Length;i++){Transform button=nav.Find(navigation[i]);Place((RectTransform)button,12+i*213,12,201,120);}
            Label(recruit,"Label","모집",0,0,201,120,30);Wire(recruit.GetComponent<UnityEngine.UI.Button>(),view.Open);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/SB/Prefabs/UI"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);if(path.Contains("/Summon/"))continue;
                var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
                foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))if(t.font==font){t.fontSharedMaterial=font.material;t.enabled=true;changed=true;}
                if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            return "Harbor UI connected and saved";
        }
    }
}
