using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SB.Core;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.Scripts.Bounty;
using SB.Scripts.Currency;
using SB.Scripts.Reward;
using SB.Scripts.Visual;
using SB.UI.Bounty;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public static class InstallBountyPursuit
{
    const string Folder = "Assets/SB/Content/Bounty";
    const string Art = "Assets/SB/Art/Sprites/Ships/Pursuit/";
    public static string Main()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing.");
        if (EditorSceneManager.GetActiveScene().GetRootGameObjects().Any(x => x.name == "BountyPursuit"))
            throw new InvalidOperationException("BountyPursuit is already installed. Edit its existing assets and references.");
        Directory.CreateDirectory(Folder + "/Prefabs");
        Directory.CreateDirectory(Folder + "/Animations");
        Directory.CreateDirectory(Folder + "/Materials");
        Directory.CreateDirectory(Folder + "/Data");
        AssetDatabase.Refresh();
        var main = BuildShip("MainPirate", "EnemyGunship", "메인 해적선", 1.42f, 1200, 4, false);
        var defender = BuildShip("PursuitDefender", "EnemyIroncladDefender", "후미 방어함", 1.02f, 350, 3, false);
        var interceptor = BuildShip("PursuitInterceptor", "InterceptorShip", "해적 요격함", .94f, 180, 0, true);
        var raider = BuildShip("PursuitRaider", "EnemyHarpoonRaider", "해적 습격함", .96f, 180, 8, false);
        var smoke = BuildShip("PursuitSmoke", "EnemySmokeSupportShip", "해적 연막함", .96f, 140, 0, false);
        var basicLayout = Layout("BasicConvoy", main, defender, interceptor, raider, null);
        var smokeLayout = Layout("SmokeConvoy", main, defender, interceptor, raider, smoke);
        var basic = Definition("BasicPursuit", "붉은 돛 해적단", basicLayout, 25);
        var screened = Definition("SmokePursuit", "연막 호위 함대", smokeLayout, 35);

        var scene = EditorSceneManager.GetActiveScene();
        var spawner = SceneObject<StageSpawnManager>();
        var flow = SceneObject<GameFlowManager>();
        var battle = SceneObject<BattleManager>();
        var currencies = SceneObject<CurrencyManager>();
        var panel = SceneObject<PannelManager>();
        var hud = SceneObject<SB.UI.InGame.InGameToolkitScreen>();
        var existing = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "BountyPursuit");
        if (existing != null) throw new InvalidOperationException("BountyPursuit already exists; update its explicit references instead of duplicating it.");
        var root = new GameObject("BountyPursuit");
        Undo.RegisterCreatedObjectUndo(root, "Install bounty pursuit");
        var env = root.AddComponent<BountyNightEnvironment>();
        var controller = root.AddComponent<BountyPursuitController>();
        var camera = Camera.main;
        var pose = new GameObject("PursuitCameraPose").transform;
        pose.SetParent(root.transform, false);
        pose.position = new Vector3(.86f, -.65f, -10f);
        var originalBackground = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true))
            .First(t => t.name == "BackgroundRoot").gameObject;
        var night = Object.Instantiate(originalBackground, root.transform);
        night.name = "NightBackground";
        foreach (var manager in night.GetComponentsInChildren<ParallaxLayerManager>(true)) Object.DestroyImmediate(manager);
        Material sea = NightMaterial("NightSea", Shader.Find("Fleet/Cartoon Sea"));
        sea.SetColor("_FarColor", new Color(.10f, .23f, .31f));
        sea.SetColor("_NearColor", new Color(.025f, .10f, .17f));
        sea.SetColor("_RippleColor", new Color(.33f, .56f, .64f));
        sea.SetColor("_FoamColor", new Color(.55f, .74f, .78f));
        sea.SetFloat("_RippleOpacity", .27f);
        sea.SetFloat("_FoamOpacity", .32f);
        Material sky = NightMaterial("NightSky", Shader.Find("Fleet/Atmospheric Background"));
        sky.SetColor("_ShadowColor", new Color(.055f, .095f, .19f));
        sky.SetColor("_LightColor", new Color(.18f, .26f, .38f));
        sky.SetColor("_HazeColor", new Color(.12f, .22f, .30f));
        Material islands = NightMaterial("NightIslands", Shader.Find("Fleet/Atmospheric Background"));
        islands.SetColor("_ShadowColor", new Color(.035f, .075f, .12f));
        islands.SetColor("_LightColor", new Color(.10f, .17f, .24f));
        islands.SetColor("_HazeColor", new Color(.12f, .22f, .30f));
        foreach (var layer in night.GetComponentsInChildren<ParallaxLayer>(true))
        {
            var so = new SerializedObject(layer);
            var sprites = so.FindProperty("sprites");
            float width = so.FindProperty("spriteWidth").floatValue;
            for (int i = 0; i < sprites.arraySize; i++)
            {
                var t = sprites.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (t == null) continue;
                var position = t.position;
                position.x = pose.position.x + (i - 1) * width;
                t.position = position;
                var renderer = t.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;
                renderer.sharedMaterial = layer.name.Contains("Sea") ? sea : layer.name.Contains("Sky") ? sky : islands;
            }
            Set(layer, "leftResetX", pose.position.x - width * 1.5f);
            Set(layer, "scrollReference", null);
            Set(layer, "scrollSpeed", layer.name.Contains("Sea") ? 1.4f : layer.name.Contains("Sky") ? .12f : .3f);
        }
        // A small existing moon sprite belongs to the stage art, not to the shared daytime layers.
        var moonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SB/Art/Sprites/Background/Parallax/MistMoon.png");
        if (moonSprite != null)
        {
            var moon = new GameObject("PursuitMoon");
            moon.transform.SetParent(night.transform, false);
            moon.transform.position = new Vector3(-.7f, 4.5f, 0);
            var renderer = moon.AddComponent<SpriteRenderer>();
            renderer.sprite = moonSprite;
            renderer.sortingOrder = -95;
            float size = .62f / Mathf.Max(.01f, moonSprite.bounds.size.x);
            moon.transform.localScale = Vector3.one * size;
            renderer.color = new Color(.73f, .83f, .9f, .85f);
        }
        night.SetActive(false);
        Set(env, "battleCamera", camera);
        Set(env, "pursuitCameraPose", pose);
        Set(env, "farmingBackground", originalBackground);
        Set(env, "pursuitBackground", night);
        SetArray(env, "pursuitLayers", night.GetComponentsInChildren<ParallaxLayer>(true));
        Set(controller, "gameFlow", flow);
        Set(controller, "spawner", spawner);
        Set(controller, "battleManager", battle);
        Set(controller, "environment", env);
        Set(controller, "currencyManager", currencies);
        SetArray(controller, "pursuits", new Object[] { basic, screened });
        var screen = new GameObject("BountyScreen");
        screen.transform.SetParent(hud.transform.parent, false);
        var document = screen.AddComponent<UIDocument>();
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/SB/UI/Bounty/BountyPursuit.uxml");
        document.panelSettings = hud.GetComponent<UIDocument>().panelSettings;
        document.sortingOrder = hud.GetComponent<UIDocument>().sortingOrder + 5;
        var ui = screen.AddComponent<BountyPursuitToolkitScreen>();
        Set(ui, "document", document);
        Set(ui, "entryDocument", hud.GetComponent<UIDocument>());
        Set(ui, "pursuit", controller);
        Set(ui, "panelManager", panel);
        Set(ui, "mainPiratePortrait", AssetDatabase.LoadAssetAtPath<Sprite>(Art + "MainPirate.png"));
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Installed bounty pursuit: 5 ships, 2 layouts, fixed 60 seconds, night shader materials and existing HUD bountyButton binding.";
    }

    public static string RepairPresentation()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var hud = SceneObject<SB.UI.InGame.InGameToolkitScreen>();
        var old = hud.GetComponent<BountyPursuitToolkitScreen>();
        if (old != null) Object.DestroyImmediate(old);
        var screen = new GameObject("BountyScreen");
        screen.transform.SetParent(hud.transform.parent, false);
        var document = screen.AddComponent<UIDocument>();
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/SB/UI/Bounty/BountyPursuit.uxml");
        document.panelSettings = hud.GetComponent<UIDocument>().panelSettings;
        document.sortingOrder = hud.GetComponent<UIDocument>().sortingOrder + 5;
        var ui = screen.AddComponent<BountyPursuitToolkitScreen>();
        Set(ui, "document", document);
        Set(ui, "entryDocument", hud.GetComponent<UIDocument>());
        Set(ui, "pursuit", SceneObject<BountyPursuitController>());
        Set(ui, "panelManager", SceneObject<PannelManager>());
        Set(ui, "mainPiratePortrait", AssetDatabase.LoadAssetAtPath<Sprite>(Art + "MainPirate.png"));
        var prefab = PrefabUtility.LoadPrefabContents(Folder + "/Prefabs/MainPirate.prefab");
        try
        {
            var label = prefab.GetComponentsInChildren<TextMeshPro>(true).First(x => x.name == "MainPirateLabel");
            ConfigureLabel(label);
            PrefabUtility.SaveAsPrefabAsset(prefab, Folder + "/Prefabs/MainPirate.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        EditorSceneManager.SaveScene(hud.gameObject.scene);
        return "Moved pursuit HUD into an independent UIDocument; bound existing bounty button and existing Korean TMP font.";
    }

    static void ConfigureLabel(TextMeshPro label)
    {
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SB/Fonts/MalgunGothic SDF.asset");
        string path = Folder + "/Materials/MainPirateLabel.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(label.font.material);
            material.SetFloat("_OutlineWidth", .18f);
            material.SetColor("_OutlineColor", new Color(0.08f, .05f, .09f, 1));
            AssetDatabase.CreateAsset(material, path);
        }
        label.fontSharedMaterial = material;
        label.text = "메인 해적선";
        label.fontSize = 1.9f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1, .87f, .55f);
        label.rectTransform.sizeDelta = new Vector2(2, .35f);
        label.GetComponent<MeshRenderer>().sortingOrder = 80;
    }

    public static string FinalizeVisuals()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        foreach (string name in new[] { "MainPirate", "PursuitDefender", "PursuitInterceptor", "PursuitRaider", "PursuitSmoke" })
        {
            string path = Folder + "/Prefabs/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var visual = root.GetComponentInChildren<Animator>(true).GetComponent<SpriteRenderer>();
                foreach (var label in root.GetComponentsInChildren<TextMeshPro>(true).Where(x => x.name == "MainPirateLabel"))
                {
                    label.GetComponent<MeshRenderer>().sortingLayerID = visual.sortingLayerID;
                    label.GetComponent<MeshRenderer>().sortingOrder = visual.sortingOrder + 30;
                }
                foreach (var wake in root.GetComponentsInChildren<ShipWakeEffect>(true))
                    foreach (var sprite in wake.GetComponentsInChildren<SpriteRenderer>(true)) sprite.flipX = false;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string guid in AssetDatabase.FindAssets("PursuitInterceptor t:AnimationClip", new[] { Folder + "/Animations" }))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.path != "" || (binding.propertyName != "m_LocalPosition.x" && binding.propertyName != "localEulerAnglesRaw.z")) continue;
                var sourceController = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SB/Prefabs/PlayerShips/InterceptorShip.prefab")
                    .GetComponentInChildren<Animator>().runtimeAnimatorController;
                var sourceClip = sourceController.animationClips.FirstOrDefault(c => c.name.Split('_').Last() == clip.name.Split('_').Last());
                if (sourceClip == null) continue;
                var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                if (curve == null) continue;
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                { keys[i].value = -keys[i].value; keys[i].inTangent = -keys[i].inTangent; keys[i].outTangent = -keys[i].outTangent; }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }
        var env = SceneObject<BountyNightEnvironment>();
        var duplicate = env.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "PursuitMoon");
        if (duplicate != null) Object.DestroyImmediate(duplicate.gameObject);
        var moon = env.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(x => x.name == "MistMoon");
        if (moon != null)
        {
            moon.transform.position = new Vector3(-.7f, 4.5f, 0);
            moon.transform.localScale = Vector3.one * (.62f / moon.sprite.bounds.size.x);
            moon.color = new Color(.73f, .83f, .9f, .85f);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(env.gameObject.scene);
        EditorSceneManager.SaveScene(env.gameObject.scene);
        return "Saved main pirate label on ship sorting layer, rightward wakes, mirrored interceptor recoil and existing moon placement.";
    }

    static T SceneObject<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .First(x => x.gameObject.scene.IsValid());

    static Material NightMaterial(string name, Shader shader)
    {
        var source = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(x => x.gameObject.scene.IsValid() && x.sharedMaterial != null && x.sharedMaterial.shader == shader)?.sharedMaterial;
        var material = source != null ? new Material(source) : new Material(shader);
        material.name = name;
        AssetDatabase.CreateAsset(material, Folder + "/Materials/" + name + ".mat");
        return material;
    }

    static Sprite ImportSprite(string name)
    {
        string path = Art + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spritePixelsPerUnit = 1536f;
        settings.spriteAlignment = 9;
        settings.spritePivot = new Vector2(.5f, .5f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Enemy BuildShip(string name, string sourceName, string display, float size, float hp, float damage, bool convert)
    {
        string source = "Assets/SB/Prefabs/" + (convert ? "PlayerShips/" : "EnemyShips/") + sourceName + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(source);
        try
        {
            root.name = name;
            Ship ship = root.GetComponent<Ship>();
            if (convert)
            {
                var old = ship;
                var enemy = root.AddComponent<Enemy>();
                var oldSo = new SerializedObject(old);
                var newSo = new SerializedObject(enemy);
                foreach (string field in new[] { "entityName", "onHitEvent", "onDeathEvent", "_myShipType", "_shipRole", "canTargetProtectedShips" })
                    newSo.CopyFromSerializedProperty(oldSo.FindProperty(field));
                newSo.ApplyModifiedPropertiesWithoutUndo();
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component == old) continue;
                    var so = new SerializedObject(component);
                    var it = so.GetIterator();
                    while (it.Next(true))
                        if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == old)
                            it.objectReferenceValue = enemy;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Object.DestroyImmediate(old);
                ship = enemy;
            }
            Set(ship, "_myShipType", (int)ShipType.Enemy);
            Set(ship, "entityName", display);
            foreach (var reward in root.GetComponentsInChildren<EnemyRewardDropCompo>(true)) Object.DestroyImmediate(reward);
            Sprite sprite = ImportSprite(name);
            var animator = root.GetComponentInChildren<Animator>(true);
            var visual = animator.GetComponent<SpriteRenderer>();
            bool originallyMirrored = visual.transform.localScale.x < 0;
            var material = new Material(visual.sharedMaterial);
            material.name = name + "Hull";
            material.SetFloat("_SpriteAlphaCutoff", .45f);
            material.SetVector("_ClothCanvas", new Vector4(1, 2f / 3f, .5f, .5f));
            material.SetVector("_ClothTransform", new Vector4(1, 1, 0, 0));
            material.SetVector("_ClothUvScale", new Vector4(1, 1, 0, 0));
            material.SetFloat("_Waterline", -.31f);
            material.SetVector("_FlagBounds", new Vector4(.55f, .02f, .86f, .23f));
            material.SetVector("_FlagPoleTip", new Vector4(.58f, .84f, 0, 0));
            material.SetVector("_SailTop", new Vector4(.43f, .30f, .77f, .32f));
            material.SetVector("_SailBottom", new Vector4(.40f, .64f, .84f, .64f));
            material.SetFloat("_FlutterPhase", name == "MainPirate" ? .1f : name.Length * .31f);
            AssetDatabase.CreateAsset(material, Folder + "/Materials/" + material.name + ".mat");
            visual.sprite = sprite;
            visual.sharedMaterial = material;
            visual.flipX = false;
            if (originallyMirrored)
                foreach (Transform child in visual.transform)
                {
                    var p = child.localPosition; p.x = -p.x; child.localPosition = p;
                    var scale = child.localScale; scale.x = -scale.x; child.localScale = scale;
                    var rotation = child.localEulerAngles; rotation.z = -rotation.z; child.localEulerAngles = rotation;
                }
            else
            {
                // The source interceptor faced right. Its weapon attachments now fire aft.
                foreach (Transform child in visual.transform)
                    if (child.name.Contains("Shoot") || child.name.Contains("Muzzle"))
                    { var p = child.localPosition; p.x = -Mathf.Abs(p.x); child.localPosition = p; }
            }
            visual.transform.localScale = new Vector3(size, size, 1);
            foreach (var emblem in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "SailRoleEmblem"))
                emblem.gameObject.SetActive(false);
            foreach (var wake in root.GetComponentsInChildren<ShipWakeEffect>(true))
            {
                var position = wake.transform.localPosition;
                position.x = -Mathf.Abs(position.x);
                wake.transform.localPosition = position;
                var scale = wake.transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                wake.transform.localScale = scale;
            }
            var overrides = new AnimatorOverrideController(animator.runtimeAnimatorController);
            overrides.name = name;
            var clips = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrides.GetOverrides(clips);
            for (int i = 0; i < clips.Count; i++)
            {
                AnimationClip effective = clips[i].Value != null ? clips[i].Value : clips[i].Key;
                var copy = Object.Instantiate(effective);
                copy.name = name + "_" + effective.name.Split('_').Last();
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(copy))
                {
                    if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite" || binding.path != "") continue;
                    var keys = AnimationUtility.GetObjectReferenceCurve(copy, binding);
                    for (int k = 0; k < keys.Length; k++) keys[k].value = sprite;
                    AnimationUtility.SetObjectReferenceCurve(copy, binding, keys);
                }
                foreach (var binding in AnimationUtility.GetCurveBindings(copy))
                    if (binding.path.StartsWith("SailRoleEmblem")) AnimationUtility.SetEditorCurve(copy, binding, null);
                AssetDatabase.CreateAsset(copy, Folder + "/Animations/" + copy.name + ".anim");
                clips[i] = new KeyValuePair<AnimationClip, AnimationClip>(clips[i].Key, copy);
            }
            overrides.ApplyOverrides(clips);
            AssetDatabase.CreateAsset(overrides, Folder + "/Animations/" + name + ".overrideController");
            animator.runtimeAnimatorController = overrides;
            var stats = root.GetComponentInChildren<ShipStatCompo>(true);
            OverrideStat(stats, "_health_Stat", hp);
            OverrideStat(stats, "_attackDamage_Stat", damage);
            OverrideStat(stats, "healthRecoveryPerSecondStat", 0);
            if (convert)
            {
                Set(ship, "_shipRole", (int)ShipRole.Support);
                Set(root.GetComponentInChildren<InterceptorAttackCompo>(true), "protectsMainShip", false);
            }
            if (name == "MainPirate")
            {
                var marker = new GameObject("MainPirateLabel");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition = new Vector3(0, .69f, 0);
                var label = marker.AddComponent<TextMeshPro>();
                ConfigureLabel(label);
                label.GetComponent<MeshRenderer>().sortingLayerID = visual.sortingLayerID;
                label.GetComponent<MeshRenderer>().sortingOrder = visual.sortingOrder + 30;
            }
            string path = Folder + "/Prefabs/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Enemy>();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void OverrideStat(ShipStatCompo stats, string statField, float value)
    {
        var so = new SerializedObject(stats);
        var stat = so.FindProperty(statField).objectReferenceValue;
        if (stat == null) return;
        var entries = so.FindProperty("statContainer.StatOverrides");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("stat").objectReferenceValue != stat) continue;
            entry.FindPropertyRelative("useOverride").boolValue = true;
            entry.FindPropertyRelative("overrideBaseValue").floatValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static StageEnemyLayout Layout(string name, Enemy main, Enemy defender, Enemy interceptor, Enemy raider, Enemy smoke)
    {
        var layout = ScriptableObject.CreateInstance<StageEnemyLayout>();
        var so = new SerializedObject(layout);
        var slots = so.FindProperty("enemySlots");
        slots.arraySize = 12;
        slots.GetArrayElementAtIndex(2).objectReferenceValue = raider;
        slots.GetArrayElementAtIndex(3).objectReferenceValue = defender;
        slots.GetArrayElementAtIndex(4).objectReferenceValue = main;
        slots.GetArrayElementAtIndex(5).objectReferenceValue = interceptor;
        slots.GetArrayElementAtIndex(7).objectReferenceValue = smoke;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(layout, Folder + "/Data/" + name + ".asset");
        return layout;
    }

    static BountyPursuitDefinition Definition(string name, string title, StageEnemyLayout layout, int reward)
    {
        var definition = ScriptableObject.CreateInstance<BountyPursuitDefinition>();
        Set(definition, "displayName", title);
        Set(definition, "enemyLayout", layout);
        Set(definition, "diamondReward", reward);
        AssetDatabase.CreateAsset(definition, Folder + "/Data/" + name + ".asset");
        return definition;
    }

    static void Set(Object target, string name, object value)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(name);
        if (property == null) throw new ArgumentException(target.name + "." + name);
        if (value == null || value is Object) property.objectReferenceValue = value as Object;
        else if (value is bool b) property.boolValue = b;
        else if (value is int i) property.intValue = i;
        else if (value is float f) property.floatValue = f;
        else if (value is string s) property.stringValue = s;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    static void SetArray(Object target, string name, Object[] values)
    {
        var so = new SerializedObject(target);
        var array = so.FindProperty(name);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
