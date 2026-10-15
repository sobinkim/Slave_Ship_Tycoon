using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB.Core;
using SB.Scripts;
using SB.Scripts.Projectiles;

public static class InstallCreatureWaveOverlap
{
 const string Scene="Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity";
 public static string Main()
 {
  var scene=EditorSceneManager.GetActiveScene();
  if(EditorApplication.isPlaying||scene.path!=Scene)throw new InvalidOperationException("Open stopped SeaCreatureTestScene.");
  Backup(Scene);
  var director=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  Set(director,"nextWaveKillRatio",0.7f);
  var burst=Material("CreatureHitBurst",0f);var chips=Material("CreatureHitWaterChips",1f);
  foreach(string path in new[]{"Assets/SB/Prefabs/SeaCreatures/ReefGlider.prefab","Assets/SB/Prefabs/SeaCreatures/ReefbackLeviathan.prefab"}) {
   Backup(path);var root=PrefabUtility.LoadPrefabContents(path);
   try {
    var feedback=root.GetComponent<SeaCreatureHitFeedback>();if(feedback==null)feedback=root.AddComponent<SeaCreatureHitFeedback>();
    Transform effectRoot=Child(root.transform,"HitFeedback");
    var contact=Particles(Child(effectRoot,"ContactBurst"),burst);
    var water=Particles(Child(effectRoot,"WaterChips"),chips);
    Set(feedback,"health",root.GetComponentInChildren<EntityHealth>(true));
    Set(feedback,"projectileAnchors",root.GetComponentInChildren<ShipProjectileAnchorCompo>(true));
    Set(feedback,"bodyRenderer",root.transform.Find("Visual").GetComponent<SpriteRenderer>());
    Set(feedback,"contactBurst",contact);Set(feedback,"contactRenderer",contact.GetComponent<ParticleSystemRenderer>());
    Set(feedback,"waterChips",water);Set(feedback,"chipRenderer",water.GetComponent<ParticleSystemRenderer>());
    PrefabUtility.SaveAsPrefabAsset(root,path);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  return "Saved 70% current-wave overlap threshold; two creature prefabs have damage-only contact burst and water chips. Other test scene tuning preserved.";
 }
 static ParticleSystem Particles(Transform t,Material material) {
  var p=t.GetComponent<ParticleSystem>();if(p==null)p=t.gameObject.AddComponent<ParticleSystem>();
  p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=p.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
  var emission=p.emission;emission.enabled=false;var shape=p.shape;shape.enabled=false;
  var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
  renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.alignment=ParticleSystemRenderSpace.View;
  renderer.sortingLayerName="Ship";renderer.sortingOrder=4050;
  EditorUtility.SetDirty(p);EditorUtility.SetDirty(renderer);return p;
 }
 static Material Material(string name,float shape) {
  string path="Assets/SB/Art/Sprites/SeaCreatures/"+name+".mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(material==null){var shader=Shader.Find("Fleet/Sea Creature Hit");if(shader==null)throw new InvalidOperationException("Missing hit shader");material=new Material(shader){name=name};material.SetFloat("_Shape",shape);AssetDatabase.CreateAsset(material,path);}
  return material;
 }
 static Transform Child(Transform parent,string name){var t=parent.Find(name);if(t!=null)return t;var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
 static void Set(UnityEngine.Object target,string field,object value){var so=new SerializedObject(target);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(field);if(value is UnityEngine.Object obj)p.objectReferenceValue=obj;else if(value is float f)p.floatValue=f;so.ApplyModifiedProperties();EditorUtility.SetDirty(target);}
 static void Backup(string path){string folder="AgentScripts/Backups/SeaCreatureWaveOverlap-20261015";Directory.CreateDirectory(folder);string to=Path.Combine(folder,Path.GetFileName(path)+".backup");if(!File.Exists(to))File.Copy(path,to);}
}
