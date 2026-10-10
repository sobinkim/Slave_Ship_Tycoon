using System;
using System.Linq;
using SB.Scripts;
using SB.Scripts.Projectiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TransportMainShipFinalSalvoSetup
{
    private const string MainPrefabPath = "Assets/SB/Prefabs/PlayerShips/TransportMainShip.prefab";
    private const string EffectFolder = "Assets/SB/Art/Effects/TransportMainShip";

    public static string Main()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before changing transport assets.");
        if (!AssetDatabase.IsValidFolder(EffectFolder))
            AssetDatabase.CreateFolder("Assets/SB/Art/Effects", "TransportMainShip");

        GameObject explosionPrefab = CreateExplosion();
        GameObject muzzlePrefab = CreateMuzzle();
        var crate = PrefabUtility.LoadPrefabContents("Assets/SB/Prefabs/Cargo/TransportCargoCrate.prefab");
        try
        {
            crate.transform.localScale = new Vector3(.095f, .2f, 1f);
            PrefabUtility.SaveAsPrefabAsset(crate, "Assets/SB/Prefabs/Cargo/TransportCargoCrate.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(crate); }

        var ship = PrefabUtility.LoadPrefabContents(MainPrefabPath);
        try
        {
            Transform visual = ship.transform.Find("Visual");
            visual.localScale = new Vector3(2.15f, 3.1f, 1f);
            // Enlarge around the existing waterline. The Animator owns this Y;
            // update its existing clip curves instead of adding a correction script.
            float baselineY = (3.1f - 2.3f) * (988f * .65f - 64f) / 1920f;
            foreach (string name in new[] { "Idle", "Move", "Dead" })
            {
                string path = "Assets/SB/Animations/Ships/TransportMainShip/TransportMainShip_" + name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y");
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                float priorBaseline = curve.keys[0].value;
                var keys = curve.keys;
                for (int index = 0; index < keys.Length; index++)
                    keys[index].value += baselineY - priorBaseline;
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
                EditorUtility.SetDirty(clip);
            }
            visual.localPosition = new Vector3(0f, baselineY, 0f);
            var cargo = new SerializedObject(ship.GetComponent<ShipCargoVisualizer>());
            cargo.FindProperty("stackLocalOffset").vector3Value = new Vector3(0f, .066f, 0f);
            cargo.ApplyModifiedPropertiesWithoutUndo();

            var hullFeedback = new SerializedObject(ship.GetComponent<ShipHullFeedback>());
            hullFeedback.FindProperty("showHealthBar").boolValue = false;
            hullFeedback.ApplyModifiedPropertiesWithoutUndo();
            ship.transform.Find("WorldUI/HullCondition").gameObject.SetActive(false);

            MainShip owner = ship.GetComponent<MainShip>();
            MainShipDestruction destruction = ship.GetComponent<MainShipDestruction>();
            if (destruction == null) destruction = ship.AddComponent<MainShipDestruction>();
            Transform anchor = ship.transform.Find("Presentation/ExplosionAnchor");
            if (anchor == null)
            {
                anchor = new GameObject("ExplosionAnchor").transform;
                anchor.SetParent(ship.transform.Find("Presentation"), false);
            }
            anchor.localPosition = new Vector3(.02f, -.3f, 0f);
            var settings = new SerializedObject(destruction);
            settings.FindProperty("owner").objectReferenceValue = owner;
            settings.FindProperty("explosionAnchor").objectReferenceValue = anchor;
            settings.FindProperty("explosionEffectPrefab").objectReferenceValue = explosionPrefab;
            settings.FindProperty("hitFeedback").objectReferenceValue = ship.GetComponent<ShipWoodImpactFeedback>();
            var roots = settings.FindProperty("presentationRoots");
            roots.arraySize = 3;
            roots.GetArrayElementAtIndex(0).objectReferenceValue = visual.gameObject;
            roots.GetArrayElementAtIndex(1).objectReferenceValue = ship.transform.Find("Presentation").gameObject;
            roots.GetArrayElementAtIndex(2).objectReferenceValue = ship.transform.Find("WorldUI").gameObject;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var mainSettings = new SerializedObject(owner);
            mainSettings.FindProperty("destruction").objectReferenceValue = destruction;
            mainSettings.ApplyModifiedPropertiesWithoutUndo();
            visual.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SB/Art/Sprites/Ships/TransportShip.png");
            PrefabUtility.SaveAsPrefabAsset(ship, MainPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(ship); }

        BattleManager manager = Object.FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);
        MainShipDefeatDirector director = manager.GetComponent<MainShipDefeatDirector>();
        if (director == null) director = Undo.AddComponent<MainShipDefeatDirector>(manager.gameObject);
        var directorSettings = new SerializedObject(director);
        directorSettings.FindProperty("salvoProjectilePrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SB/Prefabs/Projectiles/NormalProjectile.prefab").GetComponent<NormalProjectile>();
        directorSettings.FindProperty("salvoMuzzleEffectPrefab").objectReferenceValue = muzzlePrefab;
        directorSettings.ApplyModifiedProperties();

        var spawn = new SerializedObject(Object.FindFirstObjectByType<StageSpawnManager>());
        var mainPoint = (Transform)spawn.FindProperty("mainShipSpawnPoint").objectReferenceValue;
        Undo.RecordObject(mainPoint, "Make transport visible");
        mainPoint.position = new Vector3(-2.01f, mainPoint.position.y, mainPoint.position.z);
        // Keep the escort formation spacing while giving the larger transport bow room.
        var points = spawn.FindProperty("escortShipSpawnPoints");
        for (int index = 0; index < points.arraySize; index++)
        {
            var point = (Transform)points.GetArrayElementAtIndex(index).objectReferenceValue;
            if (point == null) continue;
            Undo.RecordObject(point, "Give transport bow room");
            bool isBackColumn = index % PlayerFleetLoadout.ColumnCount == 0;
            Vector3 position = point.position;
            position.x = isBackColumn ? -.8f : .15f;
            point.position = position;
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorSceneManager.SaveScene(manager.gameObject.scene);
        return "Larger transport and taller crates saved; health bar hidden; final-salvo director and explosion references connected.";
    }

    private static GameObject CreateExplosion()
    {
        var root = new GameObject("TransportMainShipExplosion");
        try
        {
            var timer = root.AddComponent<TimedPoolEffect>();
            var settings = new SerializedObject(timer);
            settings.FindProperty("lifeTime").floatValue = 1.3f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Mesh flash = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SB/Art/Effects/IroncladDefender/MuzzleFlash.asset");
            Mesh smoke = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SB/Art/Effects/IroncladDefender/MuzzleSmoke.asset");
            Mesh debris = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SB/Art/Effects/IncomingFire/JaggedWoodPlank.asset");
            Particles(root.transform, "Flash", flash, 4, .18f, .28f, 5f, 8f, .3f, .8f,
                new Color(1f, .85f, .35f), 0f, .08f, 4203);
            Particles(root.transform, "Fire", smoke, 18, .3f, .55f, 4f, 8f, .7f, 1.6f,
                new Color(1f, .36f, .08f), -.08f, .17f, 4202);
            Particles(root.transform, "Smoke", smoke, 12, .65f, 1.05f, 6f, 11f, .2f, .6f,
                new Color(.23f, .14f, .18f, .85f), -.1f, .2f, 4201);
            Particles(root.transform, "WoodDebris", debris, 14, .5f, .9f, .8f, 1.6f, 1.1f, 2f,
                new Color(.78f, .36f, .13f), .3f, .12f, 4204);
            return PrefabUtility.SaveAsPrefabAsset(root, EffectFolder + "/TransportMainShipExplosion.prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static GameObject CreateMuzzle()
    {
        var root = new GameObject("FinalSalvoMuzzleFlash");
        try
        {
            var timer = root.AddComponent<TimedPoolEffect>();
            var settings = new SerializedObject(timer);
            settings.FindProperty("lifeTime").floatValue = .3f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Particles(root.transform, "Flash", AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SB/Art/Effects/IroncladDefender/MuzzleFlash.asset"),
                2, .1f, .17f, 1.5f, 2.5f, .1f, .3f, new Color(1f, .8f, .3f), 0f, .01f, 4200);
            return PrefabUtility.SaveAsPrefabAsset(root, EffectFolder + "/FinalSalvoMuzzleFlash.prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void Particles(Transform parent, string name, Mesh mesh, short count,
        float minLife, float maxLife, float minSize, float maxSize, float minSpeed,
        float maxSpeed, Color color, float gravity, float radius, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.duration = 1.2f;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startColor = color;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = gravity;
        main.maxParticles = 32;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        var lifetime = particles.colorOverLifetime;
        lifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, .45f), new GradientColorKey(Color.black, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .45f), new GradientAlphaKey(0f, 1f) });
        lifetime.color = gradient;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, .4f));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SB/Art/Effects/IncomingFire/WoodFragments.mat");
        renderer.sortingLayerID = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath).transform.Find("Visual").GetComponent<SpriteRenderer>().sortingLayerID;
        renderer.sortingOrder = order;
    }
}
