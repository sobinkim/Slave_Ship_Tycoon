using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB.Scripts;

public static class InstallSeaCreatureV3
{
 const string ScenePath="Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity";
 const string Art="Assets/SB/Art/Sprites/SeaCreatures/";
 public static string Main()
 {
  var scene=EditorSceneManager.GetActiveScene();
  if(EditorApplication.isPlaying||scene.path!=ScenePath)throw new InvalidOperationException("Open stopped SeaCreatureTestScene.");
  Backup(ScenePath);
  AssetDatabase.Refresh();
  ImportSprite(Art+"ReefGlider_V3.png",1400f);ImportSprite(Art+"ReefbackLeviathan_V3.png",1350f);
  Material eel=MaterialAsset("SeaCreatureEelSurface", "Fleet/Sea Creature Surface Body");
  Material heavy=MaterialAsset("SeaCreatureHeavySurface", "Fleet/Sea Creature Surface Body");
  eel.SetFloat("_TailBendMultiplier",1f);heavy.SetFloat("_TailBendMultiplier",0.25f);
  EditorUtility.SetDirty(eel);EditorUtility.SetDirty(heavy);
  Material shadow=MaterialAsset("CreatureContactShadow","Fleet/Sea Creature Water Contact");shadow.SetFloat("_SurfaceKind",0f);EditorUtility.SetDirty(shadow);
  Material ring=MaterialAsset("CreatureContactRing","Fleet/Sea Creature Water Contact");ring.SetFloat("_SurfaceKind",1f);EditorUtility.SetDirty(ring);
  Material foam=MaterialAsset("CreatureContactFoam","Fleet/Sea Creature Water Contact");foam.SetFloat("_SurfaceKind",2f);EditorUtility.SetDirty(foam);
  Convert("Assets/SB/Prefabs/SeaCreatures/ReefGlider.prefab",false,eel,shadow,ring,foam);
  Convert("Assets/SB/Prefabs/SeaCreatures/ReefbackLeviathan.prefab",true,heavy,shadow,ring,foam);
  var root=scene.GetRootGameObjects().Single(g=>g.name=="SeaCreatureSurvival");
  var director=root.GetComponent<SeaCreatureSurvivalDirector>();
  var motor=root.GetComponent<SeaVoyageMotor>();if(motor==null)motor=root.AddComponent<SeaVoyageMotor>();
  var spawn=UnityEngine.Object.FindFirstObjectByType<StageSpawnManager>();
  var spawnSO=new SerializedObject(spawn);var spawnPoint=(Transform)spawnSO.FindProperty("mainShipSpawnPoint").objectReferenceValue;
  Transform camera=Camera.main.transform;
  Transform cameraRig=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="VoyageCameraRig")?.transform;
  if(cameraRig==null){cameraRig=new GameObject("VoyageCameraRig").transform;cameraRig.position=camera.position;camera.SetParent(cameraRig,true);}
  Set(motor,"fleetSpawnRoot",spawnPoint.parent);Set(motor,"followingCamera",cameraRig);
  Transform background=scene.GetRootGameObjects().Single(g=>g.name=="BackgroundRoot").transform;
  Set(motor,"backgroundRoot",background);
  Set(motor,"entryRoot",root.transform.Find("EntryPoints"));Set(director,"voyageMotor",motor);
  Set(director,"alliedFireBlockRadius",0.42f);Set(director,"cruiseSpeed",1.25f);
  Set(director,"groupSpacing",4.8f);Set(director,"entryDepth",2.2f);
  Set(director,"wavesPerEncounter",3);Set(director,"encounterStartDistance",2f);Set(director,"waveDelaySeconds",0.6f);
  Set(director,"initialCreatures",5);Set(director,"maximumAlive",14);Set(director,"chargerChance",0.3f);
  foreach(Transform entry in root.transform.Find("EntryPoints"))entry.localPosition=new Vector3(3.65f,entry.localPosition.y,0f);
  foreach(var layer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParallaxLayer>(true))) {
   Set(layer,"scrollReference",background);
   if(layer.name=="SeaLayerRoot")Set(layer,"scrollSpeed",1.25f);
   if(layer.name=="WaveLayerRoot")Set(layer,"scrollSpeed",1.65f);
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=root;
  return "Saved world-advancing fleet/camera, fixed-world left-facing creatures, matching V3 art, water contact rig/materials, blocked allied fire and non-piercing creature melee.";
 }
 static void Convert(string path,bool heavy,Material bodyMaterial,Material shadow,Material ring,Material foam)
 {
  Backup(path);var root=PrefabUtility.LoadPrefabContents(path);
  try{
   var creature=root.GetComponent<SeaCreatureEnemy>();var visual=root.transform.Find("Visual");
   visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;visual.localScale=Vector3.one*(heavy?0.95f:0.7f);
   var body=visual.GetComponent<SpriteRenderer>();body.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+(heavy?"ReefbackLeviathan_V3.png":"ReefGlider_V3.png"));
   body.sharedMaterial=bodyMaterial;body.flipX=false;body.flipY=false;body.color=Color.white;
   var rig=root.GetComponent<SeaCreatureSurfacePresentation>();if(rig==null)rig=root.AddComponent<SeaCreatureSurfacePresentation>();
   Set(creature,"surfacePresentation",rig);Set(creature,"artFacesRight",false);
   Set(creature,"swimSpeed",heavy?0.45f:0.7f);Set(creature,"bodyRadius",heavy?0.4f:0.28f);
   Set(creature,"biteRange",heavy?0.8f:0.7f);Set(creature,"biteDurationSeconds",heavy?0.5f:0.42f);
   Set(creature,"chargeWindupSeconds",0.65f);Set(creature,"deathSeconds",0.65f);
   Set(rig,"visualRoot",visual);Set(rig,"bodyRenderer",body);Set(rig,"artFacesRight",false);
   float water=heavy?-0.18f:-0.135f;Set(rig,"waterlineLocalY",water);Set(rig,"sinkDistance",heavy?0.65f:0.45f);
   Set(rig,"biteLungeDistance",heavy?0.14f:0.18f);Set(rig,"bodyPitchDegrees",heavy?2f:4f);
   Set(rig,"idleBendStrength",heavy?0.002f:0.004f);Set(rig,"swimBendStrength",heavy?0.007f:0.018f);
   float width=heavy?1.05f:0.82f;int order=body.sortingOrder;
   var contact=Child(root.transform,"SurfaceContact");
   var dark=Surface(contact,"Shadow",shadow,new Vector3(0,water-0.025f,0),new Vector3(width,0.17f,1),new Color(0.04f,0.25f,0.29f,0.35f),body.sortingLayerID,order-3);
   var band=Surface(contact,"Waterline",ring,new Vector3(0,water,0),new Vector3(width*1.06f,0.13f,1),new Color(0.65f,0.9f,0.89f,0.58f),body.sortingLayerID,order+1);
   var bow=Surface(contact,"BowFoam",foam,new Vector3(-width*0.38f,water,0),new Vector3(0.18f,0.07f,1),new Color(0.78f,0.95f,0.91f,0.72f),body.sortingLayerID,order+2);
   var ripple=Surface(contact,"Ripple",ring,new Vector3(0,water,0),new Vector3(width*1.1f,0.2f,1),new Color(0.7f,0.94f,0.92f,0.65f),body.sortingLayerID,order+2);
   var trails=new SpriteRenderer[3];
   for(int i=0;i<trails.Length;i++)trails[i]=Surface(contact,"Foam"+i,foam,new Vector3(width*0.3f,water+(i-1)*0.035f,0),new Vector3(0.11f,0.035f,1),new Color(0.74f,0.93f,0.9f,0.6f),body.sortingLayerID,order+1);
   Set(rig,"contactShadow",dark);Set(rig,"waterlineRenderer",band);Set(rig,"bowFoam",bow);Set(rig,"rippleRenderer",ripple);
   var rigSO=new SerializedObject(rig);var arr=rigSO.FindProperty("trailingFoam");arr.arraySize=trails.Length;
   for(int i=0;i<trails.Length;i++)arr.GetArrayElementAtIndex(i).objectReferenceValue=trails[i];rigSO.ApplyModifiedProperties();EditorUtility.SetDirty(rig);
   var health=root.transform.Find("WorldUI/HullCondition");if(health!=null)health.localPosition=new Vector3(0,water-0.15f,0);
   Set(root.GetComponent<ShipHullFeedback>(),"barWidth",heavy?0.85f:0.65f);
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 static SpriteRenderer Surface(Transform parent,string name,Material material,Vector3 position,Vector3 scale,Color color,int layer,int order) {
  var t=Child(parent,name);t.localPosition=position;t.localScale=scale;var r=t.GetComponent<SpriteRenderer>();if(r==null)r=t.gameObject.AddComponent<SpriteRenderer>();
  r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"ChargeWarning.asset");r.sharedMaterial=material;r.color=color;r.sortingLayerID=layer;r.sortingOrder=order;return r;
 }
 static void ImportSprite(string path,float ppu) {
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
  importer.spritePixelsPerUnit=ppu;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
  importer.textureCompression=TextureImporterCompression.Uncompressed;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
 }
 static Material MaterialAsset(string name,string shaderName) {
  string path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find(shaderName);if(shader==null)throw new InvalidOperationException("Missing shader "+shaderName);
  if(m==null){m=new Material(shader){name=name};AssetDatabase.CreateAsset(m,path);}else m.shader=shader;return m;
 }
 static Transform Child(Transform parent,string name){var t=parent.Find(name);if(t!=null)return t;var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
 static void Set(UnityEngine.Object obj,string field,object value) {
  var so=new SerializedObject(obj);var p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(obj.name+"."+field);
  if(value is UnityEngine.Object reference)p.objectReferenceValue=reference;else if(value is float f)p.floatValue=f;else if(value is int i)p.intValue=i;else if(value is bool b)p.boolValue=b;else throw new InvalidOperationException(field);
  so.ApplyModifiedProperties();EditorUtility.SetDirty(obj);
 }
 static void Backup(string path){string folder="AgentScripts/Backups/SeaCreatureV3-20261015";Directory.CreateDirectory(folder);string to=Path.Combine(folder,Path.GetFileName(path)+".backup");if(!File.Exists(to))File.Copy(path,to);}
}
