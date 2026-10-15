using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB.Scripts;
public static class InstallOffscreenReinforcements
{
 public static string Main(){
  var scene=EditorSceneManager.GetActiveScene();
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
  if(scene.path!="Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity"){
   if(scene.isDirty)throw new InvalidOperationException("Current scene has unsaved changes.");
   scene=EditorSceneManager.OpenScene("Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity",OpenSceneMode.Single);
  }
  var director=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Test camera missing.");
  var so=new SerializedObject(director);
  so.FindProperty("battleCamera").objectReferenceValue=camera;
  so.FindProperty("reinforcementOffscreenPadding").floatValue=0.5f;
  so.FindProperty("reinforcementEntryDepth").floatValue=0.8f;
  so.FindProperty("reinforcementEntrySpeed").floatValue=2.2f;
  so.ApplyModifiedProperties();EditorUtility.SetDirty(director);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  return "Saved offscreen reinforcements: camera="+camera.name+" padding=0.5 depth=0.8 entrySpeed=2.2. Current tuning otherwise preserved.";
 }
}
