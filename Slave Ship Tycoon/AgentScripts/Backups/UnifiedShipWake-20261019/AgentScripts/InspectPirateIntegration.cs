using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB.Scripts;

public static class InspectPirateIntegration
{
    public static string ProductionConnections()
    {
        var scene = SceneManager.GetSceneByName("SampleScene");
        var root = scene.GetRootGameObjects().Single(g => g.name == "PirateSeaCreatureCombat");
        var director = root.GetComponent<SeaCreatureSurvivalDirector>();
        var so = new SerializedObject(director);
        string[] required = { "fleetSpawner", "poolingManager", "currencyManager", "swarmerPrefab", "chargerPrefab", "creatureRoot", "voyagePresentation", "voyageMotor", "battleCamera" };
        var b = new StringBuilder();
        foreach (string field in required)
        {
            var reference = so.FindProperty(field).objectReferenceValue;
            if (reference == null) throw new System.InvalidOperationException("Unassigned " + field);
            b.AppendLine(field + "=" + reference.name);
        }
        var entries = so.FindProperty("entryPoints");
        for (int i = 0; i < entries.arraySize; i++)
            if (entries.GetArrayElementAtIndex(i).objectReferenceValue == null) throw new System.InvalidOperationException("Unassigned entry " + i);
        b.AppendLine("Entry points=" + entries.arraySize);
        b.AppendLine("Initial=" + so.FindProperty("initialCreatures").intValue + "; waves=" + so.FindProperty("wavesPerEncounter").intValue + "; increment=" + so.FindProperty("groupSize").intValue + "; killRatio=" + so.FindProperty("nextWaveKillRatio").floatValue);
        b.AppendLine("StartOnPlay=" + so.FindProperty("startOnPlay").boolValue + "; restartShortcut=" + so.FindProperty("allowRestartShortcut").boolValue);
        var motor = new SerializedObject(root.GetComponent<SeaVoyageMotor>());
        b.AppendLine("Camera movement parent=" + motor.FindProperty("followingCamera").objectReferenceValue.name);
        b.AppendLine("Test HUD count=" + root.GetComponentsInChildren<SB.UI.SeaCreatureTest.SeaCreatureTestHUD>(true).Length);
        b.AppendLine("Scene dirty=" + scene.isDirty);
        return b.ToString();
    }
    public static string CreatureSetup()
    {
        var scene = SceneManager.GetSceneByName("SeaCreatureTestScene");
        var root = scene.GetRootGameObjects().Single(g => g.name == "SeaCreatureSurvival");
        var b = new StringBuilder();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            b.AppendLine(PathOf(t) + " pos=" + t.position + " components=" + string.Join(",", t.GetComponents<Component>().Select(c => c == null ? "Missing" : c.GetType().Name)));
        foreach (var c in root.GetComponents<MonoBehaviour>()) b.AppendLine(c.GetType().Name + " " + EditorJsonUtility.ToJson(c));
        return b.ToString();
    }
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    public static string Main()
    {
        var b = new StringBuilder();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            b.AppendLine(scene.path + " dirty=" + scene.isDirty);
            foreach (var root in scene.GetRootGameObjects())
            {
                b.AppendLine("ROOT " + root.name + " " + root.transform.position);
                foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (c == null) continue;
                    string type = c.GetType().Name;
                    if (!(type.Contains("Stage") || type.Contains("Voyage") || type.Contains("Parallax") || type == "GameFlowManager" || type == "PoolingManager" || type == "CameraShake" || type == "CurrencyManager" || type == "MainShipDefeatDirector")) continue;
                    b.AppendLine("  " + PathOf(c.transform) + " " + type + " enabled=" + c.enabled);
                    var so = new SerializedObject(c);
                    var p = so.GetIterator();
                    while (p.NextVisible(true))
                    {
                        if (p.propertyType == SerializedPropertyType.ObjectReference)
                            b.AppendLine("    " + p.propertyPath + "=" + (p.objectReferenceValue == null ? "null" : p.objectReferenceValue is Component component ? PathOf(component.transform) : AssetDatabase.GetAssetPath(p.objectReferenceValue) + " " + p.objectReferenceValue.name));
                    }
                }
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) b.AppendLine("CAMERA " + PathOf(camera.transform) + " pos=" + camera.transform.position + " ortho=" + camera.orthographicSize);
            }
        }
        return b.ToString();
    }
}
