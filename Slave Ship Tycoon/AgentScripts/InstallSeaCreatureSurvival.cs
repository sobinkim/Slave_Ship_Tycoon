using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using SB.Core;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.Scripts.Currency;
using SB.Scripts.Projectiles;
using SB.UI.SeaCreatureTest;

public static class InstallSeaCreatureSurvival
{
    private const string ScenePath = "Assets/SB/Animations/Scenes/SeaCreatureTestScene.unity";
    private const string GliderPath = "Assets/SB/Prefabs/SeaCreatures/ReefGlider.prefab";
    private const string HeavyPath = "Assets/SB/Prefabs/SeaCreatures/ReefbackLeviathan.prefab";

    public static string FixWarningAssets()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        Sprite sprite = WarningSprite(); Material material = WarningMaterial();
        Material bodyMaterial = CreatureMaterial();
        foreach (string path in new[] { GliderPath, HeavyPath })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var warning = root.transform.Find("ChargeWarning").GetComponent<SpriteRenderer>();
                var body = root.transform.Find("Visual").GetComponent<SpriteRenderer>();
                body.sharedMaterial = bodyMaterial;
                warning.sprite = sprite; warning.sharedMaterial = material;
                warning.drawMode = SpriteDrawMode.Simple;
                warning.sortingLayerID = body.sortingLayerID; warning.sortingOrder = body.sortingOrder - 1;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return "Charge path uses the Ship sorting layer and Built-in sprite material; creature bodies no longer use the ship cloth/waterline shader.";
    }

    private static Sprite WarningSprite()
    {
        const string path = "Assets/SB/Art/Sprites/SeaCreatures/ChargeWarning.asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false) { name = "ChargeWarningWhite" };
        texture.SetPixels(Enumerable.Repeat(Color.white, 64).ToArray()); texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f, 0, SpriteMeshType.FullRect);
        sprite.name = "ChargeWarning";
        AssetDatabase.CreateAsset(sprite, path); AssetDatabase.AddObjectToAsset(texture, sprite);
        return sprite;
    }

    private static Material WarningMaterial()
    {
        const string path = "Assets/SB/Art/Sprites/SeaCreatures/ChargeWarning.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) throw new InvalidOperationException("Built-in sprite shader missing.");
        if (material == null)
        {
            material = new Material(shader) { name = "ChargeWarning" };
            AssetDatabase.CreateAsset(material, path);
        }
        else { material.shader = shader; EditorUtility.SetDirty(material); }
        return material;
    }

    private static Material CreatureMaterial()
    {
        const string path = "Assets/SB/Art/Sprites/SeaCreatures/SeaCreatureSprite.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Sprites/Default")) { name = "SeaCreatureSprite" };
        AssetDatabase.CreateAsset(material, path); return material;
    }

    public static string TunePrototype()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open the test scene first.");
        var director = UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
        Set(director, "initialCreatures", 6); Set(director, "maximumAlive", 16);
        Set(director, "cruiseSpeed", 1.5f); Set(director, "groupSpacing", 2.85f); Set(director, "waveLength", 22.5f);
        Set(director, "chargerChance", 0.2f);
        foreach (string path in new[] { GliderPath, HeavyPath })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var creature = root.GetComponent<SeaCreatureEnemy>(); bool heavy = path == HeavyPath;
                Set(creature, "maxHealth", heavy ? 55f : 18f);
                Set(creature, "biteDamage", heavy ? 4f : 2f);
                Set(creature, "chargeDamage", 6f);
                Set(creature, "chargeWindupSeconds", 0.85f);
                Set(creature, "bodyRadius", heavy ? 0.38f : 0.25f);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Prototype tuning saved: 6 initial, 16 maximum, 2 every 1.9 seconds, 15-second waves, HP 18/55, charge damage 6.";
    }

    public static string Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing the test encounter.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open SeaCreatureTestScene first.");
        if (scene.isDirty) EditorSceneManager.SaveScene(scene);
        Backup(ScenePath); Backup(GliderPath); Backup(HeavyPath);
        ConvertCreature(GliderPath, SeaCreatureKind.Swarmer);
        ConvertCreature(HeavyPath, SeaCreatureKind.Charger);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(GliderPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(HeavyPath, ImportAssetOptions.ForceUpdate);
        var glider = AssetDatabase.LoadAssetAtPath<GameObject>(GliderPath).GetComponent<SeaCreatureEnemy>();
        var heavy = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyPath).GetComponent<SeaCreatureEnemy>();
        if (glider == null || heavy == null) throw new InvalidOperationException("Creature prefab conversion failed.");

        var spawn = UnityEngine.Object.FindFirstObjectByType<StageSpawnManager>();
        var pool = UnityEngine.Object.FindFirstObjectByType<PoolingManager>();
        var currency = UnityEngine.Object.FindFirstObjectByType<CurrencyManager>();
        Set(spawn, "externalEnemySpawning", true);
        foreach (var c in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)))
        {
            if (c == null) continue;
            if (c is GameFlowManager || c is StageEncounterDirector || c is MainShipDefeatDirector ||
                c.GetType().Name == "StageResultPopupController" || c.GetType().Name == "StageInfoBarController")
            {
                c.enabled = false;
                EditorUtility.SetDirty(c);
            }
        }
        foreach (string name in new[] { "InGameToolkitUI", "FleetToolkitUI", "IroncladDefender", "SummonUI" })
        {
            var go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (go != null) go.SetActive(false);
        }
        var equipment = GameObject.Find("/Managers/TransportEquipmentManager");
        if (equipment != null) equipment.SetActive(false);

        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "SeaCreatureSurvival");
        if (root == null) root = new GameObject("SeaCreatureSurvival");
        var director = root.GetComponent<SeaCreatureSurvivalDirector>();
        if (director == null) director = root.AddComponent<SeaCreatureSurvivalDirector>();
        Transform activeRoot = Child(root.transform, "Creatures");
        Transform entries = Child(root.transform, "EntryPoints");
        var entryRefs = new Transform[5];
        for (int i = 0; i < entryRefs.Length; i++)
        {
            entryRefs[i] = Child(entries, "Entry" + (i + 1));
            entryRefs[i].position = new Vector3(4.9f, -0.05f + i * 0.7f, 0f);
        }
        Set(director, "fleetSpawner", spawn);
        Set(director, "poolingManager", pool);
        Set(director, "currencyManager", currency);
        Set(director, "swarmerPrefab", glider);
        Set(director, "chargerPrefab", heavy);
        Set(director, "creatureRoot", activeRoot);
        var dso = new SerializedObject(director);
        var array = dso.FindProperty("entryPoints"); array.arraySize = entryRefs.Length;
        for (int i = 0; i < entryRefs.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = entryRefs[i];
        dso.ApplyModifiedProperties();

        Transform hudObject = Child(root.transform, "SurvivalHUD");
        var document = hudObject.GetComponent<UIDocument>();
        if (document == null) document = hudObject.gameObject.AddComponent<UIDocument>();
        document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/SB/UI/Shared/Settings/InGamePanelSettings.asset");
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/SB/UI/SeaCreatureTest/SeaCreatureTest.uxml");
        document.sortingOrder = 100;
        var hud = hudObject.GetComponent<SeaCreatureTestHUD>();
        if (hud == null) hud = hudObject.gameObject.AddComponent<SeaCreatureTestHUD>();
        Set(hud, "document", document); Set(hud, "director", director);
        EditorUtility.SetDirty(document);

        var layout = AssetDatabase.LoadAssetAtPath<StageEnemyLayout>("Assets/SB/Data/SeaCreatureTest/SeaCreatureTestLayout.asset");
        var lso = new SerializedObject(layout); var slots = lso.FindProperty("enemySlots");
        for (int i = 0; i < slots.arraySize; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = null;
        slots.GetArrayElementAtIndex(0).objectReferenceValue = glider;
        slots.GetArrayElementAtIndex(1).objectReferenceValue = heavy;
        slots.GetArrayElementAtIndex(3).objectReferenceValue = glider;
        slots.GetArrayElementAtIndex(4).objectReferenceValue = heavy;
        lso.ApplyModifiedProperties(); EditorUtility.SetDirty(layout);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save test scene.");
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        return "Installed continuous creature encounter in " + ScenePath + "; 5 offscreen entries; fixed formation/GameFlow/final salvo disabled in test scene; auto-start HUD connected; prefabs=" + glider.name + "," + heavy.name;
    }

    private static void ConvertCreature(string path, SeaCreatureKind kind)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var creature = root.GetComponent<SeaCreatureEnemy>();
            bool isNew = creature == null;
            if (isNew)
            {
                var previous = root.GetComponent<Enemy>();
                creature = root.AddComponent<SeaCreatureEnemy>();
                var previousSO = new SerializedObject(previous); var replacementSO = new SerializedObject(creature);
                foreach (string field in new[] { "entityName", "onHitEvent", "onDeathEvent", "_myShipType", "_shipRole", "canTargetProtectedShips" })
                {
                    var from = previousSO.FindProperty(field);
                    if (from != null) replacementSO.CopyFromSerializedProperty(from);
                }
                replacementSO.ApplyModifiedProperties();
                foreach (var c in root.GetComponentsInChildren<Component>(true))
                {
                    if (c == null || c == previous) continue;
                    var so = new SerializedObject(c); var p = so.GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == previous)
                            p.objectReferenceValue = creature;
                    so.ApplyModifiedProperties();
                }
                UnityEngine.Object.DestroyImmediate(previous, true);
            }
            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c is Base_ShipAttackCompo || c is TargetSelector || c is EntityAnimator || c is EntityAnimatorTrigger ||
                    c is Animator || c is ShipWoodImpactFeedback || c is ShipProjectileEvasion || c is ShipMissFeedback ||
                    c is RaidTargetingIndicator)
                    UnityEngine.Object.DestroyImmediate(c, true);
            }
            bool heavy = kind == SeaCreatureKind.Charger;
            var visual = root.transform.Find("Visual");
            visual.localScale = Vector3.one * (heavy ? 0.95f : 0.7f);
            visual.localPosition = Vector3.zero;
            var body = visual.GetComponent<SpriteRenderer>(); body.flipX = false; body.color = Color.white;
            body.sharedMaterial = CreatureMaterial();
            var marker = root.GetComponent<RaidTargetMarker>();
            if (marker == null) marker = root.AddComponent<RaidTargetMarker>();
            var hook = root.transform.Find("WorldUI/RaidTargetHook");
            hook.gameObject.SetActive(true);
            Set(marker, "ship", creature); Set(marker, "hull", body); Set(marker, "hookIcon", hook.GetComponent<MeshRenderer>());
            var warning = Child(root.transform, "ChargeWarning");
            var warningRenderer = warning.GetComponent<SpriteRenderer>();
            if (warningRenderer == null) warningRenderer = warning.gameObject.AddComponent<SpriteRenderer>();
            warningRenderer.sprite = WarningSprite();
            warningRenderer.sharedMaterial = WarningMaterial();
            warningRenderer.drawMode = SpriteDrawMode.Simple;
            warningRenderer.size = new Vector2(1f, 0.2f);
            warningRenderer.sortingLayerID = body.sortingLayerID;
            warningRenderer.sortingOrder = body.sortingOrder - 1;
            warningRenderer.enabled = false;
            Set(creature, "kind", (int)kind);
            Set(creature, "visual", visual); Set(creature, "bodyRenderer", body); Set(creature, "chargeWarning", warningRenderer);
            Set(creature, "entityName", heavy ? "산호 갑각수" : "암초 뱀장어");
            Set(creature, "_shipRole", (int)(heavy ? ShipRole.Tanker : ShipRole.Speedster));
            if (isNew)
            {
                Set(creature, "maxHealth", heavy ? 70f : 20f);
                Set(creature, "swimSpeed", heavy ? 0.55f : 1.15f);
                Set(creature, "bodyRadius", heavy ? 0.34f : 0.22f);
                Set(creature, "biteDamage", heavy ? 6f : 3f);
                Set(creature, "biteIntervalSeconds", heavy ? 1.6f : 1.1f);
                Set(creature, "hitStunSeconds", heavy ? 0.02f : 0.07f);
                Set(creature, "hitKnockback", heavy ? 0.025f : 0.09f);
            }
            Transform hit = visual.Find("HitPoint"); hit.localPosition = Vector3.zero;
            Transform water = visual.Find("WaterImpactPoint"); water.localPosition = new Vector3(0f, -0.2f, 0f);
            Set(root.GetComponentInChildren<ShipProjectileAnchorCompo>(true), "trackMovementDuringFlight", true);
            root.transform.Find("Presentation").gameObject.SetActive(false);
            foreach (string name in new[] { "WorldUI/IncomingFireCount", "WorldUI/MissLabel", "Visual/SailRoleEmblem", "Visual/MuzzleVfx" })
            {
                var child = root.transform.Find(name); if (child != null) child.gameObject.SetActive(false);
            }
            root.transform.Find("WorldUI/HullCondition").localPosition = new Vector3(0f, heavy ? -0.5f : -0.35f, 0f);
            Set(root.GetComponent<ShipHullFeedback>(), "barWidth", heavy ? 0.65f : 0.45f);
            Set(root.GetComponent<ShipHullFeedback>(), "barHeight", 0.035f);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Set(UnityEngine.Object target, string field, object value)
    {
        var so = new SerializedObject(target); var property = so.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.name + "." + field + " was not found");
        if (value is UnityEngine.Object obj) property.objectReferenceValue = obj;
        else if (value is bool boolean) property.boolValue = boolean;
        else if (value is float number) property.floatValue = number;
        else if (value is int integer) property.intValue = integer;
        else if (value is string text) property.stringValue = text;
        else throw new InvalidOperationException("Unsupported serialized value");
        so.ApplyModifiedProperties(); EditorUtility.SetDirty(target);
    }

    private static Transform Child(Transform parent, string name)
    {
        var child = parent.Find(name); if (child != null) return child;
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
    }

    private static void Backup(string path)
    {
        string directory = "AgentScripts/Backups/SeaCreatureSurvival-20261015";
        Directory.CreateDirectory(directory);
        string destination = Path.Combine(directory, Path.GetFileName(path) + ".backup");
        if (!File.Exists(destination)) File.Copy(path, destination);
    }
}
