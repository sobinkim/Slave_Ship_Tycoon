using SB.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Editor
{
    public static class CommanderCardShapeSetup
    {
        private static T Add<T>(GameObject go) where T:Component
        {var c=go.GetComponent<T>();return c!=null?c:Undo.AddComponent<T>(go);}
        private static void Place(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        private static Sprite Shape(string name,bool rounded)
        {
            const string folder="Assets/SB/Art/Sprites/UI";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/SB/Art/Sprites","UI");
            string path=folder+"/"+name+".asset";
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite saved)return saved;
            const int size=128;const float radius=12f;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x+.5f-size/2f)-(size/2f-radius),0);
                float dy=Mathf.Max(Mathf.Abs(y+.5f-size/2f)-(size/2f-radius),0);
                float alpha=rounded?Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f):1f;
                pixels[y*size+x]=new Color(1,1,1,alpha);
            }
            texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,path);
            var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name;
            AssetDatabase.AddObjectToAsset(sprite,texture);AssetDatabase.SaveAssets();return sprite;
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
            var rounded=Shape("CommanderRoundedPanel",true);var square=Shape("CommanderSolidFill",false);
            var panel=Object.FindFirstObjectByType<CommanderSettingsPanel>(FindObjectsInactive.Include);
            var line=panel.transform.Find("LedgerRule");Undo.RecordObject(line,"Tab divider below buttons");Place(line,24,60,1032,2);line.SetAsLastSibling();
            foreach(var slot in Object.FindObjectsByType<CommanderSkillSlotView>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                Undo.RegisterFullObjectHierarchyUndo(slot.gameObject,"Rounded skill card");
                var border=slot.GetComponent<UnityEngine.UI.Image>();border.sprite=rounded;border.type=UnityEngine.UI.Image.Type.Simple;
                var body=slot.transform.Find("CardBody");Place(body,4,4,166,166);
                var background=body.GetComponent<UnityEngine.UI.Image>();background.sprite=rounded;background.type=UnityEngine.UI.Image.Type.Simple;
                Add<Mask>(body.gameObject).showMaskGraphic=true;
                var so=new SerializedObject(slot);
                var icon=(UnityEngine.UI.Image)so.FindProperty("_skillIcon").objectReferenceValue;
                var fill=(UnityEngine.UI.Image)so.FindProperty("chargeFill").objectReferenceValue;
                var label=(TMP_Text)so.FindProperty("_detailText").objectReferenceValue;
                var number=(TMP_Text)so.FindProperty("_cooldownText").objectReferenceValue;
                icon.transform.SetParent(body,false);Place(icon.transform,0,0,166,166);icon.preserveAspect=false;
                fill.transform.SetParent(body,false);Place(fill.transform,0,0,166,166);fill.sprite=square;fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Vertical;fill.fillOrigin=0;fill.raycastTarget=false;
                var footer=body.Find("NameBacking");if(footer==null){var go=new GameObject("NameBacking",typeof(RectTransform),typeof(UnityEngine.UI.Image));Undo.RegisterCreatedObjectUndo(go,"Skill footer");go.transform.SetParent(body,false);footer=go.transform;}
                Place(footer,0,134,166,32);var footerImage=footer.GetComponent<UnityEngine.UI.Image>();footerImage.color=new Color(.015f,.025f,.035f,.82f);footerImage.raycastTarget=false;
                label.transform.SetParent(body,false);Place(label.transform,0,134,166,32);
                number.transform.SetParent(body,false);Place(number.transform,0,0,166,134);number.transform.SetAsLastSibling();
            }
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);AssetDatabase.SaveAssets();
            return "Divider separated; icon and rectangular charge clipped by one rounded square mask.";
        }
    }
}
