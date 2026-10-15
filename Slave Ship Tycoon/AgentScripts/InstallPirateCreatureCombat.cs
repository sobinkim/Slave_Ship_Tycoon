using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB.Scripts;
using SB.Scripts.Currency;

public static class InstallPirateCreatureCombat
{
    private static T ComponentIn<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

    private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(UnityEngine.Object target, string field, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string UiFingerprint(Scene scene)
    {
        var names = new[] { "Canvas", "FleetToolkitUI", "InGameToolkitUI", "SummonUI", "EventSystem" };
        var b = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects().Where(root => names.Contains(root.name)).OrderBy(root => root.name))
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                b.Append(t.name).Append(t.gameObject.activeSelf);
                foreach (var c in t.GetComponents<Component>())
                    b.Append(c == null ? "Missing" : c.GetType().FullName + EditorJsonUtility.ToJson(c));
            }
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()))).Replace("-", "");
    }

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before scene installation.");
        var source = SceneManager.GetSceneByPath("Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity");
        var scene = SceneManager.GetSceneByPath("Assets/SB/Animations/Scenes/SampleScene.unity");
        if (!source.isLoaded || !scene.isLoaded) throw new InvalidOperationException("Both source and target scenes must be loaded.");
        if (scene.GetRootGameObjects().Any(root => root.name == "PirateSeaCreatureCombat"))
            throw new InvalidOperationException("Pirate combat is already installed; inspect before updating.");

        string backupFolder = "AgentScripts/Backups/PirateCreatureIntegration-20261015";
        Directory.CreateDirectory(backupFolder);
        if (!EditorSceneManager.SaveScene(scene, backupFolder + "/SampleScene_Before.unity", true))
            throw new InvalidOperationException("Could not preserve the production scene before installation.");
        if (!EditorSceneManager.SaveScene(source, backupFolder + "/SeaCreatureTestScene_Unsaved.unity", true))
            throw new InvalidOperationException("Could not preserve the source scene's unsaved edits.");

        string uiBefore = UiFingerprint(scene);
        SceneManager.SetActiveScene(scene);
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install pirate creature combat");
        var sourceRoot = source.GetRootGameObjects().Single(root => root.name == "SeaCreatureSurvival");
        var root = UnityEngine.Object.Instantiate(sourceRoot);
        root.name = "PirateSeaCreatureCombat";
        SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Add pirate creature combat");
        var testHud = root.transform.Find("SurvivalHUD");
        if (testHud != null) Undo.DestroyObjectImmediate(testHud.gameObject);

        var director = root.GetComponent<SeaCreatureSurvivalDirector>();
        var motor = root.GetComponent<SeaVoyageMotor>();
        var spawner = ComponentIn<StageSpawnManager>(scene);
        var pooling = ComponentIn<PoolingManager>(scene);
        var currency = ComponentIn<CurrencyManager>(scene);
        var camera = ComponentIn<Camera>(scene);
        var background = scene.GetRootGameObjects().Single(go => go.name == "BackgroundRoot").transform;
        var spawnerProperties = new SerializedObject(spawner);
        var mainSpawn = (Transform)spawnerProperties.FindProperty("mainShipSpawnPoint").objectReferenceValue;

        var cameraRig = new GameObject("PirateVoyageCameraRig");
        SceneManager.MoveGameObjectToScene(cameraRig, scene);
        Undo.RegisterCreatedObjectUndo(cameraRig, "Add voyage camera rig");
        cameraRig.transform.position = camera.transform.position;
        Undo.SetTransformParent(camera.transform, cameraRig.transform, "Parent voyage camera");

        Undo.RecordObjects(new UnityEngine.Object[] { director, motor, spawner }, "Connect pirate creature combat");
        Assign(director, "fleetSpawner", spawner);
        Assign(director, "poolingManager", pooling);
        Assign(director, "currencyManager", currency);
        Assign(director, "battleCamera", camera);
        Assign(director, "creatureRoot", root.transform.Find("Creatures"));
        Assign(director, "voyageMotor", motor);
        Assign(director, "voyagePresentation", root.GetComponentInChildren<SeaVoyagePresentation>(true));
        SetBool(director, "startOnPlay", false);
        SetBool(director, "allowRestartShortcut", false);
        Assign(motor, "fleetSpawnRoot", mainSpawn.parent);
        Assign(motor, "followingCamera", cameraRig.transform);
        Assign(motor, "backgroundRoot", background);
        Assign(motor, "entryRoot", root.transform.Find("EntryPoints"));
        Assign(spawner, "pirateSeaCreatureDirector", director);

        var layers = background.GetComponentsInChildren<ParallaxLayer>(true);
        foreach (var layer in layers)
        {
            Undo.RecordObject(layer, "Bind moving sea background");
            Assign(layer, "scrollReference", background);
        }
        string uiAfter = UiFingerprint(scene);
        if (uiBefore != uiAfter) throw new InvalidOperationException("Existing UI changed during installation; inspect before saving.");

        var sceneReferences = new StringBuilder();
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) throw new InvalidOperationException("Missing script in pirate combat root.");
            var so = new SerializedObject(component);
            var property = so.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                var reference = property.objectReferenceValue as Component;
                if (reference != null && !EditorUtility.IsPersistent(reference) && reference.gameObject.scene != scene)
                    throw new InvalidOperationException("Cross-scene reference: " + component.GetType().Name + "." + property.propertyPath);
            }
        }
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save SampleScene.");
        // The native copy above preserves previously unsaved test-scene edits.
        EditorSceneManager.CloseScene(source, true);
        Selection.activeGameObject = root;
        string result = "Saved=" + scene.path + "\nUI unchanged=" + (uiBefore == uiAfter) + " fingerprint=" + uiAfter +
            "\nDirector=" + root.name + "\nCamera rig=" + cameraRig.name + "\nParallax references=" + layers.Length +
            "\nTest HUD copied=False\nStage auto-start=False; R shortcut=False\nBackup=" + backupFolder;
        Directory.CreateDirectory("Artifacts/SeaCreatures");
        File.WriteAllText("Artifacts/SeaCreatures/PirateIntegrationConnections.txt", result);
        return result;
    }
}
