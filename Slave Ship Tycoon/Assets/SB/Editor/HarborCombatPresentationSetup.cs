using System;
using System.IO;
using System.Linq;
using SB.Core;
using SB.Scripts;
using UnityEditor;
using UnityEngine;

namespace SB.Editor
{
    public static class HarborCombatPresentationSetup
    {
        private static readonly string[] ShipPaths =
        {
            "Assets/SB/Prefabs/EnemyShips/Test/TestEnemy_A.prefab",
            "Assets/SB/Prefabs/EnemyShips/Test/TestEnemy_B.prefab",
            "Assets/SB/Prefabs/EnemyShips/Test/TestEnemy_C.prefab",
            "Assets/SB/Prefabs/EnemyShips/Test/TestBoss.prefab",
            "Assets/SB/Prefabs/PlayerShips/Test/TestMainShip.prefab",
            "Assets/SB/Prefabs/PlayerShips/Test/TestEscort_A.prefab",
            "Assets/SB/Prefabs/PlayerShips/Test/TestEscort_B.prefab",
            "Assets/SB/Prefabs/PlayerShips/Test/TestEscort_C.prefab"
        };
        private static void Backup(string path)
        {
            string directory = Path.GetFullPath("QA/visual-prefab-backups");
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(directory, Path.GetFileName(path) + ".txt");
            if (!File.Exists(destination)) File.Copy(Path.GetFullPath(path), destination);
        }
        private static void Ref(SerializedObject so,string key,UnityEngine.Object value)
        {so.FindProperty(key).objectReferenceValue=value;}
        private static SpriteRenderer Bar(Transform parent,string name,Sprite sprite,Color color,int order,float width,float height)
        {
            var child=parent.Find(name);
            if(child==null){var go=new GameObject(name);go.transform.SetParent(parent,false);child=go.transform;}
            var renderer=child.GetComponent<SpriteRenderer>();
            if(renderer==null)renderer=child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite=sprite;renderer.drawMode=SpriteDrawMode.Sliced;
            renderer.size=new Vector2(width,height);renderer.color=color;renderer.sortingOrder=order;
            return renderer;
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            var sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            foreach(string path in ShipPaths)
            {
                Backup(path);
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var health=root.GetComponentInChildren<EntityHealth>(true);
                    var hull=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name.Trim()=="Visual");
                    bool enemy=path.Contains("EnemyShips");
                    bool main=path.EndsWith("TestMainShip.prefab");
                    if(enemy) hull.color=new Color(1f,.64f,.47f,1f);
                    var feedback=root.GetComponent<ShipHullFeedback>();
                    if(feedback==null)feedback=root.AddComponent<ShipHullFeedback>();
                    var barRoot=root.transform.Find("HullCondition");
                    if(barRoot==null){var go=new GameObject("HullCondition");go.transform.SetParent(root.transform,false);barRoot=go.transform;}
                    barRoot.localPosition=new Vector3(0f,main?.78f:.52f,0f);
                    float width=main?1f:.68f;
                    Bar(barRoot,"Track",sprite,new Color(.025f,.05f,.055f,.85f),4000,width+.04f,.08f);
                    var fill=Bar(barRoot,"Fill",sprite,new Color(.22f,.78f,.62f),4001,width,.045f);
                    foreach(var renderer in barRoot.GetComponentsInChildren<SpriteRenderer>(true))renderer.sortingLayerID=hull.sortingLayerID;
                    var so=new SerializedObject(feedback);
                    Ref(so,"health",health);Ref(so,"hull",hull);Ref(so,"healthFill",fill);Ref(so,"healthBar",barRoot.gameObject);
                    so.FindProperty("barWidth").floatValue=width;
                    so.FindProperty("alwaysShowHealth").boolValue=main;
                    so.ApplyModifiedProperties();
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            const string projectilePath="Assets/SB/Prefabs/Projectiles/NormalProjectile.prefab";
            Backup(projectilePath);
            var projectile=PrefabUtility.LoadPrefabContents(projectilePath);
            try
            {
                projectile.transform.localScale=Vector3.one*.72f;
                foreach(var trail in projectile.GetComponentsInChildren<TrailRenderer>(true))
                {
                    trail.time=.14f;trail.widthMultiplier=.045f;
                    var gradient=new Gradient();
                    gradient.SetKeys(
                        new[]{new GradientColorKey(new Color(.86f,.78f,.61f),0f),new GradientColorKey(new Color(.58f,.67f,.69f),1f)},
                        new[]{new GradientAlphaKey(.5f,0f),new GradientAlphaKey(0f,1f)});
                    trail.colorGradient=gradient;
                }
                PrefabUtility.SaveAsPrefabAsset(projectile,projectilePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(projectile);}
            AssetDatabase.SaveAssets();
            return "Visual-only feedback added to 8 ship prefabs; projectile smoke trail shortened. Original prefabs backed up under QA.";
        }
    }
}