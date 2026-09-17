using System.Linq;
using SB.Scripts.UI.Fleet;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SB.Editor
{
    public static class FleetMergeResultSetup
    {
        private static readonly Color Navy = new Color(.035f,.075f,.105f);
        private static readonly Color Brass = new Color(.84f,.67f,.39f);
        private static readonly Color Ink = new Color(.88f,.9f,.83f);
        private static TMP_FontAsset font;

        private static RectTransform Node(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return (RectTransform)child;
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Merge result window");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0,1);
            rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(x,-y);
            rect.sizeDelta = new Vector2(width,height);
        }

        private static UnityEngine.UI.Image Surface(Transform parent, string name,
            float x, float y, float width, float height, Color color)
        {
            var rect = Node(parent,name);
            Place(rect,x,y,width,height);
            var image = rect.GetComponent<UnityEngine.UI.Image>();
            if (image == null) image = Undo.AddComponent<UnityEngine.UI.Image>(rect.gameObject);
            image.color=color;
            image.raycastTarget=false;
            return image;
        }

        private static TMP_Text Label(Transform parent, string name, string text,
            float x,float y,float width,float height,float size,Color color)
        {
            var rect=Node(parent,name);
            Place(rect,x,y,width,height);
            var label=rect.GetComponent<TextMeshProUGUI>();
            if(label==null)label=Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            label.font=font;label.fontSize=size;label.text=text;label.color=color;
            label.alignment=TextAlignmentOptions.MidlineLeft;
            label.raycastTarget=false;
            return label;
        }

        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
            var merge=Object.FindObjectsByType<FleetMergeScreen>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
            Undo.RegisterFullObjectHierarchyUndo(merge.gameObject,"Merge result window");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SB/Fonts/MalgunGothic SDF.asset");
            var shade=Surface(merge.transform,"MergeResultPopup",0,0,1080,1920,new Color(.01f,.025f,.04f,.86f));
            shade.raycastTarget=true;
            var root=shade.rectTransform;
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;
            root.offsetMin=root.offsetMax=Vector2.zero;
            root.SetAsLastSibling();
            var view=root.GetComponent<FleetMergeResultView>();
            if(view==null)view=Undo.AddComponent<FleetMergeResultView>(root.gameObject);
            var group=root.GetComponent<CanvasGroup>();
            if(group==null)group=Undo.AddComponent<CanvasGroup>(root.gameObject);
            group.blocksRaycasts=true;group.interactable=true;
            var panel=Surface(root,"Panel",0,0,920,900,Navy).rectTransform;
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);
            panel.anchoredPosition=Vector2.zero;
            Surface(panel,"BrassRule",0,0,920,3,Brass);
            Label(panel,"Eyebrow","F L E E T   /   U P G R A D E",42,26,836,30,18,Brass);
            Label(panel,"Title","함선 합성 완료",42,65,836,58,40,Ink);
            var summary=Label(panel,"Summary","최종 획득 함선",42,125,836,35,24,Brass);
            var so=new SerializedObject(view);
            so.FindProperty("panel").objectReferenceValue=panel;
            so.FindProperty("visibility").objectReferenceValue=group;
            so.FindProperty("summaryText").objectReferenceValue=summary;
            var rows=so.FindProperty("rows");rows.arraySize=4;
            for(int i=0;i<4;i++)
            {
                var row=Surface(panel,"Reward_"+i,42,180+i*130,836,118,new Color(.085f,.155f,.19f));
                var icon=Surface(row.transform,"Icon",14,10,122,98,Color.white);
                icon.preserveAspect=true;
                var title=Label(row.transform,"Ship","",156,14,490,90,28,Ink);
                var amount=Label(row.transform,"Amount","",656,14,160,90,32,Brass);
                amount.alignment=TextAlignmentOptions.MidlineRight;
                var entry=rows.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("root").objectReferenceValue=row.gameObject;
                entry.FindPropertyRelative("icon").objectReferenceValue=icon;
                entry.FindPropertyRelative("nameText").objectReferenceValue=title;
                entry.FindPropertyRelative("amountText").objectReferenceValue=amount;
            }
            var hint=Label(panel,"Hint","획득 함선은 미편성 상태입니다. 함대에서 배치하세요.",42,0,836,60,22,Ink);
            var hintRect=hint.rectTransform;
            hintRect.anchorMin=hintRect.anchorMax=new Vector2(0,0);
            hintRect.pivot=new Vector2(0,0);
            hintRect.anchoredPosition=new Vector2(42,125);
            var buttonSurface=Surface(panel,"Confirm",42,0,836,90,new Color(.12f,.23f,.26f));
            buttonSurface.raycastTarget=true;
            var buttonRect=buttonSurface.rectTransform;
            buttonRect.anchorMin=buttonRect.anchorMax=new Vector2(0,0);
            buttonRect.pivot=new Vector2(0,0);
            buttonRect.anchoredPosition=new Vector2(42,32);
            var button=buttonSurface.GetComponent<UnityEngine.UI.Button>();
            if(button==null)button=Undo.AddComponent<UnityEngine.UI.Button>(buttonSurface.gameObject);
            button.targetGraphic=buttonSurface;button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick,view.Close);
            Label(button.transform,"Label","확인",0,0,836,90,30,Brass).alignment=TextAlignmentOptions.Center;
            so.ApplyModifiedProperties();
            var mergeSo=new SerializedObject(merge);
            mergeSo.FindProperty("resultView").objectReferenceValue=view;
            mergeSo.ApplyModifiedProperties();
            root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(merge.gameObject.scene);
            EditorSceneManager.SaveScene(merge.gameObject.scene);
            AssetDatabase.SaveAssets();
            return "Merge result popup and explicit references saved.";
        }
    }
}
