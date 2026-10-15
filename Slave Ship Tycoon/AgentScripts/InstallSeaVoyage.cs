using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB.Scripts;

public static class InstallSeaVoyage
{
 public static string Main()
 {
  var scene=EditorSceneManager.GetActiveScene();
  if(EditorApplication.isPlaying || scene.path!="Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity")
   throw new InvalidOperationException("Open the stopped SeaCreatureTestScene.");
  var root=scene.GetRootGameObjects().Single(g=>g.name=="SeaCreatureSurvival");
  var director=root.GetComponent<SeaCreatureSurvivalDirector>();
  var visual=Child(root.transform,"VoyageMotion");
  var presentation=visual.GetComponent<SeaVoyagePresentation>();
  if(presentation==null)presentation=visual.gameObject.AddComponent<SeaVoyagePresentation>();
  var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/SB/Art/Sprites/SeaCreatures/ChargeWarning.mat");
  var wakes=new LineRenderer[12];var water=new LineRenderer[24];
  for(int i=0;i<wakes.Length;i++)wakes[i]=Line(Child(visual,"Wake"+i),material,3,0.045f,new Color(0.78f,0.97f,1f,0.6f));
  for(int i=0;i<water.Length;i++)water[i]=Line(Child(visual,"Water"+i),material,2,0.022f,new Color(0.75f,0.96f,1f,0.32f));
  Set(presentation,"director",director); Array(presentation,"wakeLines",wakes);Array(presentation,"waterLines",water);
  Set(director,"voyagePresentation",presentation);
  SetFloat(director,"cruiseSpeed",1.5f);SetFloat(director,"groupSpacing",2.85f);SetFloat(director,"waveLength",22.5f);
  foreach(string path in new[]{"Assets/SB/Prefabs/SeaCreatures/ReefGlider.prefab","Assets/SB/Prefabs/SeaCreatures/ReefbackLeviathan.prefab"}) {
   var prefab=PrefabUtility.LoadPrefabContents(path);
   try {var creature=prefab.GetComponent<SeaCreatureEnemy>();SetFloat(creature,"noticeRange",1.6f);SetFloat(creature,"cruisingSwimSpeed",path.Contains("Glider")?0.2f:0.05f);PrefabUtility.SaveAsPrefabAsset(prefab,path);}
   finally{PrefabUtility.UnloadPrefabContents(prefab);}
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=root;
  return "Voyage connected: 12 stern wake lines, 24 water streaks; sea/cruise speed 1.5; encounters every 2.85 distance; creatures face forward until fleet overtakes within 1.6.";
 }
 static Transform Child(Transform parent,string name) {
  var child=parent.Find(name);if(child!=null)return child;var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
 }
 static LineRenderer Line(Transform child,Material material,int count,float width,Color color) {
  var line=child.GetComponent<LineRenderer>();if(line==null)line=child.gameObject.AddComponent<LineRenderer>();
  line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=count;line.startWidth=width;line.endWidth=0f;
  line.startColor=color;line.endColor=new Color(color.r,color.g,color.b,0f);line.sortingLayerName="Ship";line.sortingOrder=-10;
  line.numCapVertices=3;line.numCornerVertices=3;line.enabled=false;EditorUtility.SetDirty(line);return line;
 }
 static void Set(UnityEngine.Object obj,string name,UnityEngine.Object value) {
  var so=new SerializedObject(obj);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedProperties();EditorUtility.SetDirty(obj);
 }
 static void SetFloat(UnityEngine.Object obj,string name,float value) {
  var so=new SerializedObject(obj);so.FindProperty(name).floatValue=value;so.ApplyModifiedProperties();EditorUtility.SetDirty(obj);
 }
 static void Array(UnityEngine.Object obj,string name,LineRenderer[] values) {
  var so=new SerializedObject(obj);var p=so.FindProperty(name);p.arraySize=values.Length;
  for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedProperties();EditorUtility.SetDirty(obj);
 }
}
