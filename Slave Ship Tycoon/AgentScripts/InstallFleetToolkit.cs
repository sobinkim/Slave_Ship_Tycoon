using System;
using System.IO;
using System.Linq;
using SB.Scripts.Currency;
using SB.Scripts.Fleet;
using SB.Scripts.UI.Fleet;
using SB.Scripts.Visual;
using SB.UI.Fleet;
using SB.UI.Recruitment;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public static class InstallFleetToolkit
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before installation.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/SB/Animations/Scenes/SampleScene.unity" || scene.isDirty)
            throw new InvalidOperationException("Expected the saved SampleScene.");
        if (GameObject.Find("FleetToolkitUI") != null)
            throw new InvalidOperationException("Toolkit UI already exists; edit its explicit references instead of reinstalling.");

        var legacyFleet = UnityEngine.Object.FindObjectsByType<PlayerFleetPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var legacyFields = new SerializedObject(legacyFleet);
        var fleetManager = (PlayerFleetManager)legacyFields.FindProperty("_playerFleetManager").objectReferenceValue;
        var gachaManager = (EscortGachaManager)legacyFields.FindProperty("_gachaManager").objectReferenceValue;
        var legacyRecruitment = (ShipRecruitmentView)legacyFields.FindProperty("recruitmentView").objectReferenceValue;
        var recruitmentFields = new SerializedObject(legacyRecruitment);
        var currencyManager = (CurrencyManager)recruitmentFields.FindProperty("currencyManager").objectReferenceValue;
        var panelManager = (PannelManager)recruitmentFields.FindProperty("panelManager").objectReferenceValue;
        var fleetButton = UnityEngine.Object.FindObjectsByType<PanelView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(v => new SerializedObject(v).FindProperty("_panel").objectReferenceValue == legacyFleet.gameObject);
        var recruitButton = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(b => b.name == "RecruitButton" && b.transform.IsChildOf(panelManager.transform));
        Sprite mainIcon = legacyFleet.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(i => i.sprite != null).sprite;

        string rootPath = Directory.GetParent(Application.dataPath).FullName;
        string backupPath = Path.Combine(rootPath, "AgentScripts", "Backups", "SampleScene.before-fleet-toolkit.unity.backup");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
        if (!File.Exists(backupPath)) File.Copy(Path.Combine(rootPath, scene.path), backupPath);

        EnsureFolder("Assets/SB/UI/Shared/Settings");
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/SB/Fonts/MalgunGothic.ttf");
        var fontAsset = FontAsset.CreateFontAsset(font, 64, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        fontAsset.name = "HarborKorean";
        AssetDatabase.CreateAsset(fontAsset, "Assets/SB/UI/Shared/Settings/HarborKorean.asset");
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        foreach (var atlas in fontAsset.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        var textSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
        textSettings.defaultFontAsset = fontAsset;
        AssetDatabase.CreateAsset(textSettings, "Assets/SB/UI/Shared/Settings/HarborTextSettings.asset");
        var settings = ScriptableObject.CreateInstance<PanelSettings>();
        settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        settings.referenceResolution = new Vector2Int(1080, 1920);
        settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        settings.match = 0f;
        settings.sortingOrder = 150;
        settings.textSettings = textSettings;
        settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/SB/UI/Shared/Styles/HarborRuntimeTheme.tss");
        AssetDatabase.CreateAsset(settings, "Assets/SB/UI/Shared/Settings/HarborPanelSettings.asset");

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Migrate fleet and recruitment to UI Toolkit");
        var toolkitRoot = new GameObject("FleetToolkitUI");
        Undo.RegisterCreatedObjectUndo(toolkitRoot, "Create Toolkit root");
        GameObject fleetObject = CreateDocument("FleetScreen", toolkitRoot.transform, settings, "Assets/SB/UI/Fleet/FleetScreen.uxml", 10);
        GameObject recruitmentObject = CreateDocument("RecruitmentScreen", toolkitRoot.transform, settings, "Assets/SB/UI/Recruitment/RecruitmentScreen.uxml", 0);
        GameObject mergeObject = CreateDocument("MergeScreen", fleetObject.transform, settings, "Assets/SB/UI/Fleet/FleetMergeScreen.uxml", 20);
        var fleet = Undo.AddComponent<FleetToolkitScreen>(fleetObject);
        var recruitment = Undo.AddComponent<RecruitmentToolkitScreen>(recruitmentObject);
        var merge = Undo.AddComponent<FleetMergeToolkitScreen>(mergeObject);
        var cardTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/SB/UI/Shared/ShipCard.uxml");
        Set(fleet, "document", fleetObject.GetComponent<UIDocument>());
        Set(fleet, "fleetManager", fleetManager);
        Set(fleet, "panelManager", panelManager);
        Set(fleet, "recruitmentScreen", recruitment);
        Set(fleet, "mergeScreen", merge);
        Set(fleet, "cardTemplate", cardTemplate);
        Set(fleet, "mainShipIcon", mainIcon);
        Set(recruitment, "document", recruitmentObject.GetComponent<UIDocument>());
        Set(recruitment, "gachaManager", gachaManager);
        Set(recruitment, "currencyManager", currencyManager);
        Set(recruitment, "panelManager", panelManager);
        Set(recruitment, "cardTemplate", cardTemplate);
        Set(recruitment, "recruitmentIcon", mainIcon);
        Copy(recruitmentFields, recruitment, "multiCount");
        Copy(recruitmentFields, recruitment, "revealSeconds");
        Copy(recruitmentFields, recruitment, "cardDelaySeconds");
        Copy(recruitmentFields, recruitment, "initialScale");
        Set(merge, "document", mergeObject.GetComponent<UIDocument>());
        Set(merge, "fleetManager", fleetManager);
        Set(merge, "fleetScreen", fleet);
        Set(merge, "cardTemplate", cardTemplate);

        Set(fleetButton, "_panel", fleetObject);
        Undo.RecordObject(recruitButton, "Connect recruitment button");
        for (int i = recruitButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            if (recruitButton.onClick.GetPersistentTarget(i) is ShipRecruitmentView)
                UnityEventTools.RemovePersistentListener(recruitButton.onClick, i);
        UnityEventTools.AddPersistentListener(recruitButton.onClick, recruitment.Open);
        EditorUtility.SetDirty(recruitButton);

        Undo.RecordObject(legacyFleet.gameObject, "Retire old fleet screen");
        legacyFleet.gameObject.SetActive(false);
        Undo.RecordObject(legacyRecruitment.gameObject, "Retire old recruitment screen");
        legacyRecruitment.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        return new { scene = scene.path, backup = backupPath, panelSettings = AssetDatabase.GetAssetPath(settings), fleet = fleetObject.name, recruitment = recruitmentObject.name, merge = mergeObject.name };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    private static GameObject CreateDocument(string name, Transform parent, PanelSettings settings, string source, int order)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var document = Undo.AddComponent<UIDocument>(go);
        document.panelSettings = settings;
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(source);
        document.sortingOrder = order;
        return go;
    }

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    private static void Copy(SerializedObject source, UnityEngine.Object target, string field)
    {
        var so = new SerializedObject(target);
        so.CopyFromSerializedProperty(source.FindProperty(field));
        so.ApplyModifiedProperties();
    }
}
