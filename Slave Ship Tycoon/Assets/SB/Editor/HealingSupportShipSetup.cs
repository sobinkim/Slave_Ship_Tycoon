using System;
using System.Collections.Generic;
using System.IO;
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
    // Explicit one-time authoring; gameplay never searches for art or guesses prefab anchors.
    public static class HealingSupportShipSetup
    {
        private const string PlayerSpritePath = "Assets/SB/Art/Sprites/Ships/HealingSupportShip.png";
        private const string EnemySpritePath = "Assets/SB/Art/Sprites/Ships/Enemies/EnemyHealingSupportShip.png";
        private const string PlayerCapsulePath = "Assets/SB/Art/Sprites/Projectiles/HealingCapsule.png";
        private const string EnemyCapsulePath = "Assets/SB/Art/Sprites/Projectiles/EnemyHealingCapsule.png";
        private const string StatusIconPath = "Assets/SB/Art/Sprites/Effects/StatusIcons/HealingSupport.png";
        private const string EffectArtFolder = "Assets/SB/Art/Effects/HealingSupport";
        private const string PlayerPrefabPath = "Assets/SB/Prefabs/PlayerShips/HealingSupportShip.prefab";
        private const string EnemyPrefabPath = "Assets/SB/Prefabs/EnemyShips/EnemyHealingSupportShip.prefab";
        private const string EscortDataPath = "Assets/SB/Data/PlayerFleet/EscortShips/Escort_HealingSupportShip.asset";

        [MenuItem("Tools/Fleet/Set Up Healing Support Ships")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before authoring healing support ships.");

            Sprite playerSprite = ImportSprite(PlayerSpritePath, .86f);
            Sprite enemySprite = ImportSprite(EnemySpritePath, .86f);
            Sprite playerCapsule = ImportSprite(PlayerCapsulePath, .42f);
            Sprite enemyCapsule = ImportSprite(EnemyCapsulePath, .42f);
            Sprite statusIcon = EnsureStatusIcon();
            EnsureFolder(EffectArtFolder);
            Sprite cross = CreateEffectSprite("HealCross", false);

            Color playerColor = new Color(.20f, 1f, .55f, 1f);
            Color enemyColor = new Color(1f, .28f, .35f, 1f);
            HealingSupportEffect playerSupport = EnsureSupportEffect("HealingSupport", statusIcon, playerColor);
            HealingSupportEffect enemySupport = EnsureSupportEffect("EnemyHealingSupport", statusIcon, enemyColor);
            HealingImpactEffect playerImpact = EnsureImpactEffect("HealingImpact", cross, playerColor);
            HealingImpactEffect enemyImpact = EnsureImpactEffect("EnemyHealingImpact", cross, enemyColor);
            HealingProjectile playerProjectile = EnsureProjectile("HealingCapsule", playerCapsule, playerImpact, playerColor);
            HealingProjectile enemyProjectile = EnsureProjectile("EnemyHealingCapsule", enemyCapsule, enemyImpact, enemyColor);

            Ship playerShip = EnsureShip(false, playerSprite, playerProjectile, playerSupport);
            EnsureShip(true, enemySprite, enemyProjectile, enemySupport);
            EscortShipData escortData = EnsureEscortData(playerShip, playerSprite);
            AddToRecruitment(escortData);
            AssetDatabase.SaveAssets();
            Debug.Log("Healing support ships authored: player/enemy prefabs, capsule projectiles, animation overrides, visible heal effects, support icons, and recruitment entry. Existing loadouts and stages are preserved.");
        }

        private static Sprite ImportSprite(string path, float widthUnits)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Healing support artwork is required before setup.", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Author-time scale alignment uses the visible art width; transparent padding stays untouched.
            importer.spritePixelsPerUnit = GetOpaqueWidthPixels(path) / widthUnits;
            importer.spritePivot = new Vector2(.5f, .5f);
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static int GetOpaqueWidthPixels(string path)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(path)))
                    throw new InvalidOperationException("Cannot read healing support sprite: " + path);
                Color32[] pixels = source.GetPixels32();
                int left = source.width;
                int right = -1;
                for (int y = 0; y < source.height; y++)
                {
                    for (int x = 0; x < source.width; x++)
                    {
                        if (pixels[y * source.width + x].a < 16) continue;
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                    }
                }
                if (right < left)
                    throw new InvalidOperationException("Healing support sprite has no visible pixels: " + path);
                return right - left + 1;
            }
            finally { Object.DestroyImmediate(source); }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, separator));
            AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
        }

        private static SerializedProperty Property(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException($"{serialized.targetObject.GetType().Name} has no serialized field '{name}'.");
            return property;
        }

        private static void CopySharedFields(Component source, Component destination)
        {
            var sourceFields = new SerializedObject(source);
            var destinationFields = new SerializedObject(destination);
            SerializedProperty field = sourceFields.GetIterator();
            if (field.NextVisible(true))
            {
                do
                {
                    if (!field.name.StartsWith("m_", StringComparison.Ordinal) &&
                        destinationFields.FindProperty(field.propertyPath) != null)
                        destinationFields.CopyFromSerializedProperty(field);
                }
                while (field.NextVisible(false));
            }
            destinationFields.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite CreateEffectSprite(string name, bool isRing)
        {
            string path = EffectArtFolder + "/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float localX = (x + .5f) / size * 2f - 1f;
                    float localY = (y + .5f) / size * 2f - 1f;
                    float alpha;
                    if (isRing)
                    {
                        float radius = Mathf.Sqrt(localX * localX + localY * localY);
                        alpha = Mathf.Clamp01((.96f - radius) * size) * Mathf.Clamp01((radius - .78f) * size);
                    }
                    else
                    {
                        float vertical = Mathf.Min(.22f - Mathf.Abs(localX), .80f - Mathf.Abs(localY));
                        float horizontal = Mathf.Min(.80f - Mathf.Abs(localX), .22f - Mathf.Abs(localY));
                        alpha = Mathf.Clamp01(Mathf.Max(vertical, horizontal) * size);
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return ImportSprite(path, 1f);
        }

        private static Sprite EnsureStatusIcon()
        {
            if (File.Exists(StatusIconPath)) return ImportSprite(StatusIconPath, 1f);
            EnsureFolder("Assets/SB/Art/Sprites/Effects/StatusIcons");
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float localX = (x + .5f) / size * 2f - 1f;
                    float localY = (y + .5f) / size * 2f - 1f;
                    // Keep the cross within 75% of the canvas, matching the shield icon's visible height.
                    const float crossHalfLength = .75f;
                    const float crossHalfThickness = .22f;
                    float vertical = Mathf.Min(crossHalfThickness - Mathf.Abs(localX), crossHalfLength - Mathf.Abs(localY));
                    float horizontal = Mathf.Min(crossHalfLength - Mathf.Abs(localX), crossHalfThickness - Mathf.Abs(localY));
                    float crossAlpha = Mathf.Clamp01(Mathf.Max(vertical, horizontal) * size);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, crossAlpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(StatusIconPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return ImportSprite(StatusIconPath, 1f);
        }

        private static HealingSupportEffect EnsureSupportEffect(string name, Sprite icon, Color color)
        {
            const string folder = "Assets/SB/Data/FleetEffects";
            EnsureFolder(folder);
            string path = folder + "/" + name + ".asset";
            HealingSupportEffect effect = AssetDatabase.LoadAssetAtPath<HealingSupportEffect>(path);
            if (effect != null) return effect;
            effect = ScriptableObject.CreateInstance<HealingSupportEffect>();
            AssetDatabase.CreateAsset(effect, path);
            var serialized = new SerializedObject(effect);
            Property(serialized, "statusIcon").objectReferenceValue = icon;
            Property(serialized, "statusIconColor").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        private static HealingImpactEffect EnsureImpactEffect(string name, Sprite cross, Color color)
        {
            const string folder = "Assets/SB/Prefabs/Effects";
            EnsureFolder(folder);
            string path = folder + "/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<HealingImpactEffect>();
            var root = new GameObject(name);
            try
            {
                root.AddComponent<HealingImpactEffect>();
                ConfigureCrossParticles(root, cross, color);
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<HealingImpactEffect>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void ConfigureCrossParticles(GameObject root, Sprite cross, Color color)
        {
            int sortingLayerId = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SB/Prefabs/PlayerShips/IroncladDefender.prefab")
                .transform.Find("Visual").GetComponent<SpriteRenderer>().sortingLayerID;
            const string materialPath = "Assets/SB/Art/Materials/Effects/HealingCrossParticles.mat";
            EnsureFolder("Assets/SB/Art/Materials/Effects");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default")) { name = "HealingCrossParticles" };
                material.mainTexture = cross.texture;
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var child = new GameObject("HealingCrossParticles", typeof(ParticleSystem));
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(0f, .05f, 0f);
            ParticleSystem particles = child.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = .3f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(.11f, .18f);
            main.startRotation = 0f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 16;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] {
                new ParticleSystem.Burst(0f, 4),
                new ParticleSystem.Burst(.10f, 3),
                new ParticleSystem.Burst(.20f, 3)
            });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(.65f, .18f, 0f);
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-.12f, .12f);
            velocity.y = new ParticleSystem.MinMaxCurve(.45f, .85f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .12f), new GradientAlphaKey(1f, .55f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule lifetimeColor = particles.colorOverLifetime;
            lifetimeColor.enabled = true;
            lifetimeColor.color = new ParticleSystem.MinMaxGradient(fade);
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = 4500;
            var serialized = new SerializedObject(root.GetComponent<HealingImpactEffect>());
            Property(serialized, "crossParticles").objectReferenceValue = particles;
            Property(serialized, "crossParticleRenderer").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HealingProjectile EnsureProjectile(string name, Sprite sprite, HealingImpactEffect impact, Color color)
        {
            const string folder = "Assets/SB/Prefabs/Projectiles";
            EnsureFolder(folder);
            string path = folder + "/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<HealingProjectile>();
            GameObject root = PrefabUtility.LoadPrefabContents(folder + "/RaiderHarpoon.prefab");
            try
            {
                root.name = name;
                NormalProjectile previousProjectile = root.GetComponent<NormalProjectile>();
                HealingProjectile projectile = root.AddComponent<HealingProjectile>();
                CopySharedFields(previousProjectile, projectile);
                Object.DestroyImmediate(previousProjectile);
                foreach (ProjectilePathDisplay display in root.GetComponentsInChildren<ProjectilePathDisplay>(true))
                {
                    var displayFields = new SerializedObject(display);
                    Property(displayFields, "projectile").objectReferenceValue = projectile;
                    Property(displayFields, "pathColor").colorValue = new Color(color.r, color.g, color.b, .45f);
                    displayFields.ApplyModifiedPropertiesWithoutUndo();
                }
                root.GetComponent<SpriteRenderer>().sprite = sprite;
                var serialized = new SerializedObject(projectile);
                Property(serialized, "healingImpactEffectPrefab").objectReferenceValue = impact;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true))
                {
                    trail.startColor = new Color(color.r, color.g, color.b, .7f);
                    trail.endColor = new Color(color.r, color.g, color.b, 0f);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<HealingProjectile>();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static RuntimeAnimatorController EnsureAnimations(bool isEnemy, Sprite sprite)
        {
            string shipName = isEnemy ? "EnemyHealingSupportShip" : "HealingSupportShip";
            string folder = "Assets/SB/Animations/Ships/" + (isEnemy ? "Enemies/" : "") + shipName;
            EnsureFolder(folder);
            string controllerPath = folder + "/" + shipName + ".overrideController";
            AnimatorOverrideController existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
            if (existing != null) return existing;
            const string sourceFolder = "Assets/SB/Animations/Ships/IroncladDefender/";
            var source = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(sourceFolder + "IroncladDefender.controller");
            var overrides = new AnimatorOverrideController(source) { name = shipName };
            var replacements = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (string state in new[] { "Idle", "Move", "Attack", "Dead" })
            {
                AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourceFolder + "IroncladDefender_" + state + ".anim");
                // Enemy presentation has matching mirrored recoil and death poses.
                string motionPath = isEnemy
                    ? "Assets/SB/Animations/Ships/Enemies/EnemyIroncladDefender/EnemyIroncladDefender_" + state + ".anim"
                    : AssetDatabase.GetAssetPath(sourceClip);
                AnimationClip clip = Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>(motionPath));
                clip.name = shipName + "_" + state;
                foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite") continue;
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    for (int i = 0; i < keys.Length; i++) keys[i].value = sprite;
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                }
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                    if (binding.path == "SailRoleEmblem") AnimationUtility.SetEditorCurve(clip, binding, null);
                AssetDatabase.CreateAsset(clip, folder + "/" + clip.name + ".anim");
                replacements.Add(new KeyValuePair<AnimationClip, AnimationClip>(sourceClip, clip));
            }
            overrides.ApplyOverrides(replacements);
            AssetDatabase.CreateAsset(overrides, controllerPath);
            return overrides;
        }

        private static Ship EnsureShip(bool isEnemy, Sprite sprite, HealingProjectile projectile, HealingSupportEffect support)
        {
            string path = isEnemy ? EnemyPrefabPath : PlayerPrefabPath;
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<Ship>();
            string sourcePath = isEnemy ? "Assets/SB/Prefabs/EnemyShips/EnemyIroncladDefender.prefab" : "Assets/SB/Prefabs/PlayerShips/IroncladDefender.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(sourcePath);
            try
            {
                root.name = isEnemy ? "EnemyHealingSupportShip" : "HealingSupportShip";
                Ship ship = root.GetComponent<Ship>();
                var shipFields = new SerializedObject(ship);
                Property(shipFields, "entityName").stringValue = isEnemy ? "적 회복 지원함" : "회복 지원함";
                Property(shipFields, "_shipRole").intValue = (int)ShipRole.Support;
                shipFields.ApplyModifiedPropertiesWithoutUndo();

                Transform visual = root.transform.Find("Visual");
                visual.localScale = new Vector3(isEnemy ? -1.12f : 1.12f, 1.44f, 1f);
                SpriteRenderer hull = visual.GetComponent<SpriteRenderer>();
                hull.sprite = sprite;
                const string materialFolder = "Assets/SB/Art/Materials/Ships";
                string materialPath = materialFolder + (isEnemy ? "/Enemies/EnemyHealingSupportHull.mat" : "/HealingSupportHull.mat");
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(hull.sharedMaterial) { name = isEnemy ? "EnemyHealingSupportHull" : "HealingSupportHull" };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                hull.sharedMaterial = material;
                visual.GetComponent<Animator>().runtimeAnimatorController = EnsureAnimations(isEnemy, sprite);
                Transform sailEmblem = visual.Find("SailRoleEmblem");
                if (sailEmblem != null) Object.DestroyImmediate(sailEmblem.gameObject);

                // These are authored local anchors, exposed as Transforms for later art adjustments.
                Transform shootPoint = visual.Find("ShootPos");
                shootPoint.localPosition = new Vector3(.156f, .073f, 0f);
                Transform muzzle = visual.Find("MuzzleVfx");
                muzzle.localPosition = shootPoint.localPosition;
                muzzle.localEulerAngles = new Vector3(0f, 0f, 90f);

                FleetEffectProvider provider = root.GetComponent<FleetEffectProvider>();
                var providerFields = new SerializedObject(provider);
                Property(providerFields, "targetingMode").intValue = 1;
                Property(providerFields, "effects").arraySize = 0;
                providerFields.ApplyModifiedPropertiesWithoutUndo();
                Test_ShipAttackCompo previousAttack = root.GetComponentInChildren<Test_ShipAttackCompo>(true);
                HealingProjectileAttackCompo attack = previousAttack.gameObject.AddComponent<HealingProjectileAttackCompo>();
                CopySharedFields(previousAttack, attack);
                Object.DestroyImmediate(previousAttack);
                attack.gameObject.name = "HealingProjectileAttackCompo";
                var attackFields = new SerializedObject(attack);
                Property(attackFields, "projectilePrefab").objectReferenceValue = projectile;
                Property(attackFields, "travelMode").intValue = (int)ProjectileTravelMode.BallisticArc;
                Property(attackFields, "ballisticApexHeight").floatValue = .9f;
                Property(attackFields, "ballisticTimeToApex").floatValue = .38f;
                Property(attackFields, "ballisticDescentTimeMultiplier").floatValue = 1f;
                Property(attackFields, "ballisticTargetClearance").floatValue = .25f;
                Property(attackFields, "initialStraightSpeed").floatValue = 2f;
                Property(attackFields, "straightMaximumSpeed").floatValue = 12f;
                Property(attackFields, "healAmount").floatValue = 30f;
                Property(attackFields, "attackIntervalMultiplier").floatValue = 3f;
                Property(attackFields, "baselineAttackAnimationDurationSeconds").floatValue = .45833334f;
                Property(attackFields, "supportEffectProvider").objectReferenceValue = provider;
                Property(attackFields, "supportEffect").objectReferenceValue = support;
                attackFields.ApplyModifiedPropertiesWithoutUndo();

                var selectorFields = new SerializedObject(root.GetComponentInChildren<TargetSelector>(true));
                Property(selectorFields, "targetTeam").intValue = 1;
                Property(selectorFields, "selectionMode").intValue = (int)TargetSelectionMode.RandomInRule;
                Property(selectorFields, "requiresDamagedTarget").boolValue = true;
                Property(selectorFields, "excludesHealingSupportShips").boolValue = true;
                Property(selectorFields, "allowsRuleFallback").boolValue = false;
                SerializedProperty rules = Property(selectorFields, "targetRules");
                rules.arraySize = 3;
                TargetLine[] healingLines = { TargetLine.Front, TargetLine.Middle, TargetLine.Back };
                for (int i = 0; i < healingLines.Length; i++)
                {
                    rules.GetArrayElementAtIndex(i).FindPropertyRelative("Line").intValue = (int)healingLines[i];
                    rules.GetArrayElementAtIndex(i).FindPropertyRelative("Role").intValue = (int)ShipRole.None;
                }
                selectorFields.ApplyModifiedPropertiesWithoutUndo();
                ConfigureInitialStats(root.GetComponentInChildren<ShipStatCompo>(true));
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<Ship>();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureInitialStats(ShipStatCompo stats)
        {
            var serialized = new SerializedObject(stats);
            Object healthStat = Property(serialized, "_health_Stat").objectReferenceValue;
            Object damageStat = Property(serialized, "_attackDamage_Stat").objectReferenceValue;
            SerializedProperty overrides = Property(serialized, "statContainer.StatOverrides");
            for (int i = 0; i < overrides.arraySize; i++)
            {
                SerializedProperty entry = overrides.GetArrayElementAtIndex(i);
                Object stat = entry.FindPropertyRelative("stat").objectReferenceValue;
                if (stat != healthStat && stat != damageStat) continue;
                entry.FindPropertyRelative("useOverride").boolValue = true;
                entry.FindPropertyRelative("overrideBaseValue").floatValue = stat == healthStat ? 120f : 0f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EscortShipData EnsureEscortData(Ship prefab, Sprite icon)
        {
            EscortShipData data = AssetDatabase.LoadAssetAtPath<EscortShipData>(EscortDataPath);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<EscortShipData>();
            AssetDatabase.CreateAsset(data, EscortDataPath);
            var serialized = new SerializedObject(data);
            Property(serialized, "_stableId").stringValue = "escort_healing_support_ship";
            Property(serialized, "_displayName").stringValue = "회복 지원함";
            Property(serialized, "_icon").objectReferenceValue = icon;
            Property(serialized, "_grade").intValue = (int)EscortShipGrade.Uncommon;
            Property(serialized, "_shipPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void AddToRecruitment(EscortShipData healer)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EscortShipCatalog>("Assets/SB/Data/PlayerFleet/EscortShipCatalog.asset");
            var table = AssetDatabase.LoadAssetAtPath<EscortGachaTable>("Assets/SB/Data/PlayerFleet/EscortGachaTable.asset");
            if (catalog == null || table == null) throw new InvalidOperationException("Escort recruitment catalog and table are required.");
            if (!catalog.Contains(healer))
            {
                var serialized = new SerializedObject(catalog);
                SerializedProperty entries = Property(serialized, "_ships");
                int index = entries.arraySize++;
                entries.GetArrayElementAtIndex(index).objectReferenceValue = healer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            bool isRecruitable = false;
            for (int i = 0; i < table.EntryCount; i++)
                if (table.GetEntryAt(i).ShipData == healer) isRecruitable = true;
            if (!isRecruitable)
            {
                var serialized = new SerializedObject(table);
                SerializedProperty entries = Property(serialized, "_entries");
                int index = entries.arraySize++;
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_shipData").objectReferenceValue = healer;
                entry.FindPropertyRelative("_weight").intValue = 25;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            if (!catalog.TryValidate(out string catalogError)) throw new InvalidOperationException(catalogError);
            if (!table.TryValidate(out string tableError)) throw new InvalidOperationException(tableError);
        }
    }
}
