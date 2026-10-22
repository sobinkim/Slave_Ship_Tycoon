using System;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using SB.Scripts;

public static class ConfigureCreatureChargeCue
{
    private const string HeavyPath = "Assets/SB/Prefabs/SeaCreatures/ReefbackLeviathan.prefab";
    public static string Main()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before editing the prefab.");
        const string shaderPath = "Assets/SB/Art/Shaders/SeaCreatureSurfaceBody.shader";
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        var messages = ShaderUtil.GetShaderMessages(shader);
        if (messages.Length > 0) throw new InvalidOperationException(string.Join(" | ", messages.Select(message => message.message)));
        string backupFolder = "AgentScripts/Backups/CreatureChargeCue-20261016";
        Directory.CreateDirectory(backupFolder);
        string backupPath = backupFolder + "/ReefbackLeviathan_Before.prefab";
        if (!File.Exists(backupPath)) File.Copy(HeavyPath, backupPath);
        var root = PrefabUtility.LoadPrefabContents(HeavyPath);
        try
        {
            var oldWarning = root.GetComponentsInChildren<SpriteRenderer>(true).SingleOrDefault(renderer => renderer.name == "ChargeWarning");
            bool removed = oldWarning != null;
            if (oldWarning != null) UnityEngine.Object.DestroyImmediate(oldWarning.gameObject);
            var surface = root.GetComponent<SeaCreatureSurfacePresentation>();
            var so = new SerializedObject(surface);
            so.FindProperty("windupCompression").vector2Value = new Vector2(0.085f, 0.025f);
            so.FindProperty("windupSinkDepth").floatValue = 0.025f;
            so.FindProperty("windupNoseDipDegrees").floatValue = 2.5f;
            so.FindProperty("chargeEmissionColor").colorValue = Color.white;
            so.FindProperty("chargeEmissionIntensity").floatValue = 0.4f;
            so.FindProperty("chargeFlashStartProgress").floatValue = 0.7f;
            so.FindProperty("chargeGatherDistance").floatValue = 0.45f;
            so.FindProperty("chargeGatherSpread").floatValue = 0.22f;
            so.FindProperty("chargeGatherOpacity").floatValue = 0.65f;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HeavyPath);
            return "Heavy prefab saved; old decal removed=" + removed + "; surface references retained; shader supported=" + shader.isSupported + "; shader messages=" + messages.Length;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    public static string Inspect()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyPath);
        var b = new StringBuilder();
        foreach (var c in root.GetComponents<MonoBehaviour>())
            if (c is SeaCreatureEnemy || c is SeaCreatureSurfacePresentation)
                b.AppendLine(c.GetType().Name + " " + EditorJsonUtility.ToJson(c));
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            b.AppendLine(renderer.name + " sprite=" + renderer.sprite?.name + " material=" + renderer.sharedMaterial?.name + " pos=" + renderer.transform.localPosition + " scale=" + renderer.transform.localScale + " color=" + renderer.color + " order=" + renderer.sortingOrder + " enabled=" + renderer.enabled);
        return b.ToString();
    }
}
