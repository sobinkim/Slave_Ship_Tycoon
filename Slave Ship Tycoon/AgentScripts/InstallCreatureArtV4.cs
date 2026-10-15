using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SB.Scripts;

public static class InstallCreatureArtV4
{
 const string Art="Assets/SB/Art/Sprites/SeaCreatures/";
 public static string Main()
 {
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before editing creature prefabs.");
  AssetDatabase.Refresh();
  var report=new System.Text.StringBuilder();
  foreach(string name in new[]{"ReefGlider","ReefbackLeviathan"}) {
   string path="Assets/SB/Prefabs/SeaCreatures/"+name+".prefab";
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   var oldBody=asset.transform.Find("Visual").GetComponent<SpriteRenderer>();
   float localWidth=oldBody.sprite.bounds.size.x;
   Sprite sprite=ImportSprite(Art+name+"_V4.png",localWidth);
   Backup(path);
   var root=PrefabUtility.LoadPrefabContents(path);
   try {
    var body=root.transform.Find("Visual").GetComponent<SpriteRenderer>();body.sprite=sprite;
    var feedback=root.GetComponent<SeaCreatureHitFeedback>();
    var so=new SerializedObject(feedback);
    so.FindProperty("emissionColor").colorValue=new Color(1f,0.86f,0.56f,1f);
    so.FindProperty("emissionIntensity").floatValue=1.8f;
    so.FindProperty("emissionAttackSeconds").floatValue=0.02f;
    so.FindProperty("emissionReleaseSeconds").floatValue=0.14f;
    so.ApplyModifiedProperties();EditorUtility.SetDirty(feedback);
    PrefabUtility.SaveAsPrefabAsset(root,path);
    report.AppendLine(name+" → "+AssetDatabase.GetAssetPath(sprite)+" width="+sprite.bounds.size.x.ToString("0.000")+" emission=1.8/.02/.14");
   }finally {PrefabUtility.UnloadPrefabContents(root);}
  }
  AssetDatabase.SaveAssets();
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/SB/Art/Shaders/SeaCreatureSurfaceBody.shader");
  AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(shader),ImportAssetOptions.ForceUpdate);
  report.AppendLine("Shader supported="+shader.isSupported+" messages="+string.Join(" | ",ShaderUtil.GetShaderMessages(shader).Select(m=>m.severity+":"+m.message)));
  return report.ToString();
 }
 static Sprite ImportSprite(string path,float localWidth)
 {
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);
  // Whole-image import only. No frame/pivot/outline edits: this Editor lacks the required frame-edit capability API.
  importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
  importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
  var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
  var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
  try {
   texture.LoadImage(File.ReadAllBytes(path));
   importer.spritePixelsPerUnit=texture.width/localWidth;
  }finally{UnityEngine.Object.DestroyImmediate(texture);}
  importer.SaveAndReimport();
  return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 static void Backup(string path){string dir="AgentScripts/Backups/CreatureArtV4-20261015";Directory.CreateDirectory(dir);string copy=Path.Combine(dir,Path.GetFileName(path)+".backup");if(!File.Exists(copy))File.Copy(path,copy);}
}
