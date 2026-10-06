using System;
using System.Linq;
using SB.Core;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.SO.TransportItemSOs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class TransportMainShipSetup
{
    private const string BasePrefabPath = "Assets/SB/Prefabs/PlayerShips/Test/TestMainShip.prefab";
    private const string PrefabPath = "Assets/SB/Prefabs/PlayerShips/TransportMainShip.prefab";
    private const string ShipTexturePath = "Assets/SB/Art/Sprites/Ships/TransportShip.png";
    private const string CrateTexturePath = "Assets/SB/Art/Sprites/Cargo/TransportCargoCrate.png";
    private const string CratePrefabPath = "Assets/SB/Prefabs/Cargo/TransportCargoCrate.prefab";
    private const string AnimationFolder = "Assets/SB/Animations/Ships/TransportMainShip";
    private const float ShipPixelsPerUnit = 1920f;

    public static string Main()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before building the transport ship.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            throw new InvalidOperationException("TransportMainShip already exists; edit its Inspector values directly.");

        EnsureFolder("Assets/SB/Prefabs/Cargo");
        EnsureFolder(AnimationFolder);
        ConfigureTexture(ShipTexturePath, ShipPixelsPerUnit, new Vector2(.5f, .65f));
        ConfigureTexture(CrateTexturePath, 2000f, new Vector2(.5f, 194f / 1080f));
        Sprite shipSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShipTexturePath);
        Sprite crateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrateTexturePath);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        Material spriteMaterial = source.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial;
        int sortingLayer = source.transform.Find("Visual").GetComponent<SpriteRenderer>().sortingLayerID;
        SpriteRenderer cratePrefab = CreateCratePrefab(crateSprite, spriteMaterial, sortingLayer);
        AnimatorController controller = CreateController(shipSprite);
        Sprite maskSprite = CreateRailMask(shipSprite.texture);

        GameObject shipRoot = PrefabUtility.LoadPrefabContents(BasePrefabPath);
        try
        {
            shipRoot.name = "TransportMainShip";
            MainShip mainShip = shipRoot.GetComponent<MainShip>();
            mainShip.EntityName = "TransportMainShip";
            Transform visual = shipRoot.transform.Find("Visual");
            visual.GetComponent<SpriteRenderer>().sprite = shipSprite;
            visual.GetComponent<Animator>().runtimeAnimatorController = controller;
            visual.localScale = new Vector3(1.9f, 2.3f, 1f);

            foreach (Base_ShipAttackCompo attack in shipRoot.GetComponentsInChildren<Base_ShipAttackCompo>(true))
                Object.DestroyImmediate(attack);
            foreach (TargetSelector selector in shipRoot.GetComponentsInChildren<TargetSelector>(true))
                Object.DestroyImmediate(selector);
            RemoveEmptyNode(shipRoot.transform.Find("AboutBattle/ArcProjectileAttackCompo"));
            RemoveEmptyNode(shipRoot.transform.Find("AboutBattle/TargetSelector"));
            Object.DestroyImmediate(visual.Find("ShootPos").gameObject);

            visual.Find("HitPoint").localPosition = new Vector3(.04f, -.235f, 0f);
            visual.Find("WaterImpactPoint").localPosition = new Vector3(.04f, -.31f, 0f);

            var sortingGroup = visual.gameObject.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerID = sortingLayer;
            sortingGroup.sortingOrder = 0;
            Transform railMask = Node(visual, "CargoFrontRailMask");
            railMask.localPosition = new Vector3(0f, (285f * .5f - 988f * .65f) / ShipPixelsPerUnit, 0f);
            var mask = railMask.gameObject.AddComponent<SpriteMask>();
            mask.sprite = maskSprite;
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = sortingLayer;
            mask.backSortingLayerID = sortingLayer;
            mask.frontSortingOrder = 1;
            mask.backSortingOrder = 0;

            Transform cargoRoot = Node(visual, "CargoDeck");
            float[] columnPixelX = { 650f, 780f, 910f, 1040f, 1170f };
            Transform[] columns = new Transform[columnPixelX.Length];
            for (int index = 0; index < columns.Length; index++)
            {
                columns[index] = Node(cargoRoot, $"Column_{index + 1:00}");
                columns[index].localPosition = new Vector3(
                    (columnPixelX[index] - 1591f * .5f) / ShipPixelsPerUnit,
                    (260f - 988f * .65f) / ShipPixelsPerUnit, 0f);
            }

            ShipCargoVisualizer cargoVisualizer = shipRoot.AddComponent<ShipCargoVisualizer>();
            var cargoSettings = new SerializedObject(cargoVisualizer);
            cargoSettings.FindProperty("cratePrefab").objectReferenceValue = cratePrefab;
            SerializedProperty basePoints = cargoSettings.FindProperty("columnBasePoints");
            basePoints.arraySize = columns.Length;
            for (int index = 0; index < columns.Length; index++)
                basePoints.GetArrayElementAtIndex(index).objectReferenceValue = columns[index];
            cargoSettings.FindProperty("stackLocalOffset").vector3Value = new Vector3(0f, .044f, 0f);
            cargoSettings.ApplyModifiedPropertiesWithoutUndo();
            var mainSettings = new SerializedObject(mainShip);
            mainSettings.FindProperty("cargoVisualizer").objectReferenceValue = cargoVisualizer;
            mainSettings.ApplyModifiedPropertiesWithoutUndo();
            // Loading the test prefab can sample its old Animator before the
            // controller changes. Persist the transport sprite after that swap.
            visual.GetComponent<SpriteRenderer>().sprite = shipSprite;
            PrefabUtility.SaveAsPrefabAsset(shipRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(shipRoot);
        }

        MainShip prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<MainShip>();
        CargoManager cargoManager = Object.FindFirstObjectByType<CargoManager>(FindObjectsInactive.Include);
        if (cargoManager == null)
            throw new InvalidOperationException("The current scene needs its existing CargoManager.");
        int boundSpawners = 0;
        foreach (StageSpawnManager spawner in Object.FindObjectsByType<StageSpawnManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(spawner, "Bind transport cargo");
            var settings = new SerializedObject(spawner);
            settings.FindProperty("cargoManager").objectReferenceValue = cargoManager;
            var loadout = (PlayerFleetLoadout)settings.FindProperty("playerFleetLoadout").objectReferenceValue;
            if (loadout == null)
                throw new InvalidOperationException("The stage spawner needs a fleet loadout.");
            var fleetSettings = new SerializedObject(loadout);
            fleetSettings.FindProperty("mainShipPrefab").objectReferenceValue = prefab;
            fleetSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(loadout);
            settings.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            boundSpawners++;
        }

        foreach (PoolingManager pool in Object.FindObjectsByType<PoolingManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var settings = new SerializedObject(pool);
            SerializedProperty preload = settings.FindProperty("preloadPrefabs");
            for (int index = 0; index < preload.arraySize; index++)
            {
                SerializedProperty reference = preload.GetArrayElementAtIndex(index).FindPropertyRelative("Prefab");
                if (reference.objectReferenceValue == source)
                    reference.objectReferenceValue = prefab.gameObject;
            }
            settings.ApplyModifiedProperties();
        }

        // Both existing cargo items referenced a deleted test-ship texture.
        // Use the actual cargo sprite without changing weights or sale rewards.
        foreach (string path in new[] { "Assets/SB/SO/TransportItemSOs/GoldBox.asset", "Assets/SB/SO/TransportItemSOs/ShipElementBox.asset" })
        {
            var cargoItem = AssetDatabase.LoadAssetAtPath<TransportItemSO>(path);
            if (cargoItem.Icon != null)
                continue;
            Undo.RecordObject(cargoItem, "Restore cargo icon");
            cargoItem.Icon = crateSprite;
            EditorUtility.SetDirty(cargoItem);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return $"Created {PrefabPath}; bound {boundSpawners} stage spawner(s); five explicit cargo columns; transport has no attack component.";
    }

    private static void ConfigureTexture(string path, float pixelsPerUnit, Vector2 pivot)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spritePixelsPerUnit = pixelsPerUnit;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.alphaIsTransparency = true;
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static SpriteRenderer CreateCratePrefab(Sprite sprite, Material material, int sortingLayer)
    {
        var crate = new GameObject("TransportCargoCrate");
        try
        {
            var renderer = crate.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = 1;
            renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            crate.transform.localScale = new Vector3(.095f, .135f, 1f);
            return PrefabUtility.SaveAsPrefabAsset(crate, CratePrefabPath).GetComponent<SpriteRenderer>();
        }
        finally
        {
            Object.DestroyImmediate(crate);
        }
    }

    private static Sprite CreateRailMask(Texture2D texture)
    {
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, 285f),
            new Vector2(.5f, .5f), ShipPixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = "TransportFrontRailMask";
        AssetDatabase.CreateAsset(sprite, "Assets/SB/Art/Sprites/Ships/TransportFrontRailMask.asset");
        return sprite;
    }

    private static AnimatorController CreateController(Sprite sprite)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(AnimationFolder + "/TransportMainShip.controller");
        controller.AddParameter("MOVE", AnimatorControllerParameterType.Bool);
        controller.AddParameter("ATTACK", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine states = controller.layers[0].stateMachine;
        AnimatorState idle = states.AddState("Idle");
        AnimatorState move = states.AddState("Move");
        AnimatorState dead = states.AddState("Dead");
        idle.motion = CreateClip("Idle", sprite, 2f, .0025f, .15f, false);
        move.motion = CreateClip("Move", sprite, 1.25f, .005f, .3f, false);
        dead.motion = CreateClip("Dead", sprite, .65f, -.06f, -8f, true);
        states.defaultState = idle;
        Transition(idle.AddTransition(move), "MOVE", true);
        Transition(move.AddTransition(idle), "MOVE", false);
        AnimatorStateTransition die = states.AddAnyStateTransition(dead);
        Transition(die, "Dead", true);
        die.canTransitionToSelf = false;
        Transition(dead.AddTransition(idle), "Dead", false);
        return controller;
    }

    private static void Transition(AnimatorStateTransition transition, string parameter, bool value)
    {
        transition.hasExitTime = false;
        transition.duration = .1f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static AnimationClip CreateClip(string name, Sprite sprite, float durationSeconds,
        float bobLocalUnits, float rollDegrees, bool isDead)
    {
        var clip = new AnimationClip { name = "TransportMainShip_" + name, frameRate = 30f };
        var position = isDead
            ? AnimationCurve.EaseInOut(0f, 0f, durationSeconds, bobLocalUnits)
            : new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(durationSeconds * .5f, bobLocalUnits), new Keyframe(durationSeconds, 0f));
        var rotation = isDead
            ? AnimationCurve.EaseInOut(0f, 0f, durationSeconds, rollDegrees)
            : new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(durationSeconds * .5f, rollDegrees), new Keyframe(durationSeconds, 0f));
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", position);
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", rotation);
        var spriteBinding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, new[]
        {
            new ObjectReferenceKeyframe { time = 0f, value = sprite },
            new ObjectReferenceKeyframe { time = durationSeconds, value = sprite }
        });
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = !isDead;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, AnimationFolder + "/" + clip.name + ".anim");
        return clip;
    }

    private static Transform Node(Transform parent, string name)
    {
        var node = new GameObject(name).transform;
        node.SetParent(parent, false);
        return node;
    }

    private static void RemoveEmptyNode(Transform node)
    {
        if (node != null && node.childCount == 0 && node.GetComponents<Component>().Length == 1)
            Object.DestroyImmediate(node.gameObject);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
}
