using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB.Scripts;

public static class ApplyWhiteCreatureEmission
{
    private static readonly string[] CreaturePaths = {
        "Assets/SB/Prefabs/SeaCreatures/ReefGlider.prefab",
        "Assets/SB/Prefabs/SeaCreatures/ReefbackLeviathan.prefab"
    };
    private static readonly string[] ScenePaths = {
        "Assets/SB/Animations/Scenes/SampleScene.unity",
        "Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity"
    };

    public static string Inspect()
    {
        var b = new StringBuilder();
        foreach (string path in CreaturePaths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var hit = new SerializedObject(root.GetComponent<SeaCreatureHitFeedback>());
            var surface = new SerializedObject(root.GetComponent<SeaCreatureSurfacePresentation>());
            b.AppendLine(path + " hit=" + hit.FindProperty("emissionColor").colorValue + " charge=" + surface.FindProperty("chargeEmissionColor").colorValue);
        }
        var scene = SceneManager.GetActiveScene();
        var background = scene.GetRootGameObjects().Single(go => go.name == "BackgroundRoot");
        foreach (Transform child in background.transform)
            b.AppendLine("Background child=" + child.name + "; sprite renderers=" + child.GetComponentsInChildren<SpriteRenderer>(true).Length);
        return b.ToString();
    }

    private static void SetWhite(UnityEngine.Object target, string field)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).colorValue = Color.white;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before saving scene assets.");
        string folder = "AgentScripts/Backups/WhiteEmissionNoWave-20261016";
        Directory.CreateDirectory(folder);
        AssetDatabase.ImportAsset("Assets/SB/Art/Shaders/SeaCreatureSurfaceBody.shader", ImportAssetOptions.ForceUpdate);
        var b = new StringBuilder();
        foreach (string path in CreaturePaths)
        {
            string backup = Path.Combine(folder, Path.GetFileName(path));
            if (!File.Exists(backup)) File.Copy(path, backup);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SetWhite(root.GetComponent<SeaCreatureHitFeedback>(), "emissionColor");
                SetWhite(root.GetComponent<SeaCreatureSurfacePresentation>(), "chargeEmissionColor");
                PrefabUtility.SaveAsPrefabAsset(root, path);
                b.AppendLine("White hit+charge: " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string path in new[] {
            "Assets/SB/Art/Sprites/SeaCreatures/SeaCreatureEelSurface.mat",
            "Assets/SB/Art/Sprites/SeaCreatures/SeaCreatureHeavySurface.mat" })
        {
            string backup = Path.Combine(folder, Path.GetFileName(path));
            if (!File.Exists(backup)) File.Copy(path, backup);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            material.SetColor("_HitEmissionColor", Color.white);
            material.SetColor("_ChargeEmissionColor", Color.white);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        var previousActive = SceneManager.GetActiveScene();
        foreach (string path in ScenePaths)
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                string backup = Path.Combine(folder, Path.GetFileName(path));
                if (!File.Exists(backup) && !EditorSceneManager.SaveScene(scene, backup, true))
                    throw new InvalidOperationException("Could not preserve " + path);
                var background = scene.GetRootGameObjects().Single(go => go.name == "BackgroundRoot");
                var waves = background.transform.Find("WaveLayerRoot");
                if (waves == null) { b.AppendLine("Wave layer already absent: " + path); continue; }
                // Only the explicit background wave subtree is removed; its sprite/material assets are shared.
                var removedObjects = waves.GetComponentsInChildren<Component>(true).Select(component => (UnityEngine.Object)component)
                    .Concat(waves.GetComponentsInChildren<Transform>(true).Select(t => (UnityEngine.Object)t.gameObject)).ToArray();
                foreach (var component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)))
                {
                    if (component == null || component.transform == waves || component.transform.IsChildOf(waves)) continue;
                    var so = new SerializedObject(component);
                    var property = so.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null && removedObjects.Contains(property.objectReferenceValue))
                            throw new InvalidOperationException("External wave reference: " + component.GetType().Name + "." + property.propertyPath);
                }
                int waveSprites = waves.GetComponentsInChildren<SpriteRenderer>(true).Length;
                Undo.DestroyObjectImmediate(waves.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + path);
                b.AppendLine("Removed background wave sprites=" + waveSprites + ": " + path);
            }
            finally
            {
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }
        if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
        Directory.CreateDirectory("Artifacts/SeaCreatures");
        File.WriteAllText("Artifacts/SeaCreatures/WhiteEmissionNoWave.txt", b.ToString());
        return b.ToString();
    }
}
