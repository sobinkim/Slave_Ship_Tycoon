using System;
using System.Collections.Generic;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.Scripts.Fleet;
using SB.Scripts.Fleet.Effects;
using SB.Scripts.Projectiles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SB.Editor
{
    // Explicit authoring invoked once through the Editor. Existing assets are never overwritten.
    public static class InterceptorShipSetup
    {
        public const string PrefabPath = "Assets/SB/Prefabs/PlayerShips/InterceptorShip.prefab";
        public const string DataPath = "Assets/SB/Data/PlayerFleet/EscortShips/Escort_InterceptorShip.asset";
        private const string AnimationFolder = "Assets/SB/Animations/Ships/InterceptorShip";
        private const string SourceAnimationFolder = "Assets/SB/Animations/Ships/IroncladDefender/";

        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SB/Art/Sprites/Ships/InterceptorShip.png");
            if (sprite == null) throw new InvalidOperationException("Import the approved interceptor sprite first.");
            var projectile = CreateProjectile();
            var controller = CreateAnimations(sprite);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = PrefabUtility.LoadPrefabContents("Assets/SB/Prefabs/PlayerShips/IroncladDefender.prefab");
                try
                {
                    root.name = "InterceptorShip";
                    var ship = root.GetComponent<Ship>();
                    ship.EntityName = "요격 호위함";
                    var shipFields = new SerializedObject(ship);
                    shipFields.FindProperty("_shipRole").intValue = (int)ShipRole.Support;
                    shipFields.FindProperty("canTargetProtectedShips").boolValue = false;
                    shipFields.ApplyModifiedPropertiesWithoutUndo();
                    var provider = root.GetComponent<FleetEffectProvider>();
                    if (provider != null) Object.DestroyImmediate(provider);
                    var selector = root.GetComponentInChildren<TargetSelector>(true);
                    if (selector != null) Object.DestroyImmediate(selector);
                    var visual = root.transform.Find("Visual");
                    visual.localScale = new Vector3(1.12f, 1.44f, 1f);
                    var hull = visual.GetComponent<SpriteRenderer>();
                    hull.sprite = sprite;
                    var material = new Material(hull.sharedMaterial) { name = "InterceptorHull" };
                    material.SetVector("_ClothCanvas", new Vector4(sprite.rect.width / sprite.pixelsPerUnit,
                        sprite.rect.height / sprite.pixelsPerUnit, .5f, .5f));
                    material.SetVector("_FlagBounds", new Vector4(.09f, .08f, .34f, .25f));
                    material.SetVector("_FlagPoleTip", new Vector4(.33f, .10f, 0f, 0f));
                    material.SetVector("_SailTop", new Vector4(.18f, .355f, .51f, .425f));
                    material.SetVector("_SailBottom", new Vector4(.20f, .69f, .52f, .65f));
                    material.SetFloat("_Waterline", -.29f);
                    material.SetFloat("_FlutterPhase", 2.3f);
                    AssetDatabase.CreateAsset(material, "Assets/SB/Art/Materials/Ships/InterceptorHull.mat");
                    hull.sharedMaterial = material;
                    visual.GetComponent<Animator>().runtimeAnimatorController = controller;
                    var firePoint = visual.Find("ShootPos");
                    firePoint.localPosition = new Vector3(.378f, -.012f, 0f);
                    var muzzle = visual.Find("MuzzleVfx");
                    muzzle.localPosition = firePoint.localPosition;
                    muzzle.localEulerAngles = new Vector3(0f, 0f, 16f);
                    visual.Find("HitPoint").localPosition = new Vector3(0f, -.17f, 0f);
                    visual.Find("WaterImpactPoint").localPosition = new Vector3(0f, -.29f, 0f);
                    var oldAttack = root.GetComponentInChildren<Base_ShipAttackCompo>(true);
                    var attack = oldAttack.gameObject.AddComponent<InterceptorAttackCompo>();
                    Object.DestroyImmediate(oldAttack);
                    attack.gameObject.name = "InterceptorAttackCompo";
                    Set(attack, "projectilePrefab", projectile);
                    Set(attack, "firePoint", firePoint);
                    SetFloat(attack, "attackCooldown", 1.4f);
                    var stats = root.GetComponentInChildren<ShipStatCompo>(true);
                    var statFields = new SerializedObject(stats);
                    var health = statFields.FindProperty("_health_Stat").objectReferenceValue;
                    var damage = statFields.FindProperty("_attackDamage_Stat").objectReferenceValue;
                    var entries = statFields.FindProperty("statContainer.StatOverrides");
                    for (int i = 0; i < entries.arraySize; i++)
                    {
                        var entry = entries.GetArrayElementAtIndex(i);
                        var stat = entry.FindPropertyRelative("stat").objectReferenceValue;
                        if (stat != health && stat != damage) continue;
                        entry.FindPropertyRelative("useOverride").boolValue = true;
                        entry.FindPropertyRelative("overrideBaseValue").floatValue = stat == health ? 120f : 0f;
                    }
                    statFields.ApplyModifiedPropertiesWithoutUndo();
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var data = AssetDatabase.LoadAssetAtPath<EscortShipData>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EscortShipData>();
                AssetDatabase.CreateAsset(data, DataPath);
                var fields = new SerializedObject(data);
                fields.FindProperty("_stableId").stringValue = "escort_interceptor_ship";
                fields.FindProperty("_displayName").stringValue = "요격 호위함";
                fields.FindProperty("_grade").intValue = (int)EscortShipGrade.Uncommon;
                fields.FindProperty("_icon").objectReferenceValue = sprite;
                fields.FindProperty("_shipPrefab").objectReferenceValue = prefab.GetComponent<Ship>();
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            Register(data);
            AssetDatabase.SaveAssets();
            return "Interceptor prefab, animations, material, projectile, burst and Uncommon recruitment entry authored.";
        }

        private static RuntimeAnimatorController CreateAnimations(Sprite sprite)
        {
            if (!AssetDatabase.IsValidFolder(AnimationFolder))
                AssetDatabase.CreateFolder("Assets/SB/Animations/Ships", "InterceptorShip");
            string path = AnimationFolder + "/InterceptorShip.overrideController";
            var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            if (existing != null) return existing;
            var controller = new AnimatorOverrideController(
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SourceAnimationFolder + "IroncladDefender.controller"));
            var replacements = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var state in new[] { "Idle", "Move", "Attack", "Dead" })
            {
                var original = AssetDatabase.LoadAssetAtPath<AnimationClip>(SourceAnimationFolder + "IroncladDefender_" + state + ".anim");
                var clip = Object.Instantiate(original);
                clip.name = "InterceptorShip_" + state;
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite") continue;
                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    for (int i = 0; i < keys.Length; i++) keys[i].value = sprite;
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                }
                if (state == "Attack")
                {
                    const float windup = 1f / 6f;
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        var keys = new SortedDictionary<float, Keyframe>();
                        foreach (var originalKey in curve.keys)
                        {
                            var key = originalKey;
                            key.time = Mathf.Max(0f, key.time - windup);
                            keys[key.time] = key;
                        }
                        curve.keys = new List<Keyframe>(keys.Values).ToArray();
                        AnimationUtility.SetEditorCurve(clip, binding, curve);
                    }
                    var events = new List<AnimationEvent>();
                    foreach (var evt in AnimationUtility.GetAnimationEvents(clip))
                    {
                        if (evt.functionName == "AttackStart") continue;
                        evt.time = Mathf.Max(0f, evt.time - windup);
                        events.Add(evt);
                    }
                    AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                }
                AssetDatabase.CreateAsset(clip, AnimationFolder + "/" + clip.name + ".anim");
                replacements.Add(new KeyValuePair<AnimationClip, AnimationClip>(original, clip));
            }
            controller.ApplyOverrides(replacements);
            AssetDatabase.CreateAsset(controller, path);
            return controller;
        }

        private static InterceptorProjectile CreateProjectile()
        {
            const string path = "Assets/SB/Prefabs/Projectiles/InterceptorProjectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<InterceptorProjectile>();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SB/Prefabs/Projectiles/NormalProjectile.prefab");
            var sourceTrail = source.GetComponentInChildren<TrailRenderer>(true);
            var burstRoot = new GameObject("InterceptionBurst");
            InterceptionBurst burst;
            try
            {
                burstRoot.AddComponent<InterceptionBurst>();
                AuthorExplosionParticles(burstRoot);
                burst = PrefabUtility.SaveAsPrefabAsset(burstRoot, "Assets/SB/Prefabs/Effects/InterceptionBurst.prefab").GetComponent<InterceptionBurst>();
            }
            finally { Object.DestroyImmediate(burstRoot); }
            var root = new GameObject("InterceptorProjectile");
            try
            {
                root.transform.localScale = new Vector3(.4f, .12f, 1f);
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = source.GetComponent<SpriteRenderer>().sprite;
                renderer.sharedMaterial = source.GetComponent<SpriteRenderer>().sharedMaterial;
                renderer.color = new Color(1f, .94f, .65f, 1f);
                renderer.sortingLayerID = source.GetComponent<SpriteRenderer>().sortingLayerID;
                var trail = root.AddComponent<TrailRenderer>();
                trail.sharedMaterial = sourceTrail.sharedMaterial;
                trail.time = .09f;
                trail.widthMultiplier = .035f;
                trail.minVertexDistance = .01f;
                trail.startColor = new Color(1f, .88f, .36f, .9f);
                trail.endColor = new Color(1f, .78f, .18f, 0f);
                trail.sortingLayerID = renderer.sortingLayerID;
                var projectile = root.AddComponent<InterceptorProjectile>();
                Set(projectile, "projectileRenderer", renderer);
                Set(projectile, "trail", trail);
                Set(projectile, "impactEffect", burst);
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<InterceptorProjectile>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        public static void AuthorExplosionParticles(GameObject root) =>
            InterceptorExplosionAuthoring.Apply(root);

        private static void Register(EscortShipData data)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EscortShipCatalog>("Assets/SB/Data/PlayerFleet/EscortShipCatalog.asset");
            var table = AssetDatabase.LoadAssetAtPath<EscortGachaTable>("Assets/SB/Data/PlayerFleet/EscortGachaTable.asset");
            if (!catalog.Contains(data))
            {
                var fields = new SerializedObject(catalog);
                var entries = fields.FindProperty("_ships");
                int index = entries.arraySize++;
                entries.GetArrayElementAtIndex(index).objectReferenceValue = data;
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            bool registered = false;
            for (int i = 0; i < table.EntryCount; i++) if (table.GetEntryAt(i).ShipData == data) registered = true;
            if (!registered)
            {
                var fields = new SerializedObject(table);
                var entries = fields.FindProperty("_entries");
                int index = entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_shipData").objectReferenceValue = data;
                entry.FindPropertyRelative("_weight").intValue = 25;
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            if (!catalog.TryValidate(out string catalogError)) throw new InvalidOperationException(catalogError);
            if (!table.TryValidate(out string tableError)) throw new InvalidOperationException(tableError);
        }

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
