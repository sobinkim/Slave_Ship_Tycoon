using System;
using System.Collections.Generic;
using SB.Scripts.Projectiles;
using UnityEditor;
using UnityEngine;

namespace SB.Editor
{
    // Authored only on the explicit interception prefab. Runtime uses ordinary Particle Systems.
    public static class InterceptorExplosionAuthoring
    {
        private const string Folder = "Assets/SB/Art/Effects/Interception";
        private const float SizeScale = 1.5f;

        public static void Apply(GameObject root)
        {
            EnsureFolder(Folder);
            var shader = Shader.Find("Fleet/Interception Particles");
            if (shader == null) throw new InvalidOperationException("Import the interception particle shader first.");
            Material glow = Material("InterceptionGlow", shader, UnityEngine.Rendering.BlendMode.One, 1.35f);
            Material solid = Material("InterceptionColor", shader, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, 1f);
            Mesh halo = RadialMesh("GlowDisc", 32, 0, Color.white,
                new Color(1, 1, 1, .35f), new Color(1, 1, 1, .07f));
            Mesh flash = RadialMesh("ImpactFlash", 12, -1, new Color(1, 1, .98f, 1),
                new Color(1, .98f, .72f, 1), new Color(1, .8f, .24f, .8f));
            Mesh fire = RadialMesh("FireBloom", 40, .08f, new Color(1, 1, .86f, 1),
                new Color(1, .9f, .34f, 1), new Color(1, .43f, .06f, .85f));
            Mesh smoke = RadialMesh("SmokePuff", 40, .07f, Color.white,
                Color.white, new Color(1, 1, 1, .25f));
            Mesh spark = PolygonMesh("GoldenSpark", new[] {
                new Vector2(-.5f, 0), new Vector2(-.18f, .08f),
                new Vector2(.5f, 0), new Vector2(-.18f, -.08f)
            }, new Color(1, 1, .8f, 1), new Color(1, .84f, .3f, 1));
            Mesh shard = PolygonMesh("MetalShard", new[] {
                new Vector2(-.43f, -.12f), new Vector2(.23f, -.2f),
                new Vector2(.5f, .02f), new Vector2(.16f, .18f), new Vector2(-.3f, .15f)
            }, Color.white, new Color(.72f, .8f, .92f, 1));

            while (root.transform.childCount > 0)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            int layer = SortingLayer.NameToID("Ship");
            var systems = new List<ParticleSystem>();
            var renderers = new List<ParticleSystemRenderer>();

            ParticleSystem Add(string name, Mesh mesh, Material material, int count,
                float minSize, float maxSize, float minLife, float maxLife,
                float delay, float radialSpeed, Gradient color, AnimationCurve size)
            {
                var child = new GameObject(name);
                child.transform.SetParent(root.transform, false);
                var system = child.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.duration = .65f;
                main.loop = false;
                main.playOnAwake = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.maxParticles = 32;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(minSize * SizeScale, maxSize * SizeScale);
                main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
                main.startColor = Color.white;
                main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
                var emission = system.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(delay, (short)count) });
                var shape = system.shape;
                shape.enabled = radialSpeed > 0f;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = .018f;
                shape.radiusThickness = 1f;
                var velocity = system.velocityOverLifetime;
                velocity.enabled = radialSpeed > 0f;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.radial = new ParticleSystem.MinMaxCurve(radialSpeed * .65f, radialSpeed);
                velocity.speedModifier = new ParticleSystem.MinMaxCurve(1f,
                    new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, .3f)));
                var colorModule = system.colorOverLifetime;
                colorModule.enabled = true;
                colorModule.color = color;
                var sizeModule = system.sizeOverLifetime;
                sizeModule.enabled = true;
                sizeModule.size = new ParticleSystem.MinMaxCurve(1f, size);
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = mesh;
                renderer.sharedMaterial = material;
                renderer.sortingLayerID = layer;
                renderer.enableGPUInstancing = false;
                systems.Add(system);
                renderers.Add(renderer);
                return system;
            }

            var white = Color.white;
            var gold = new Color(1f, .88f, .35f);
            var orange = new Color(1f, .48f, .1f);
            Add("Glow", halo, glow, 1, .44f, .48f, .23f, .26f, 0, 0,
                Colors(gold, gold, gold, .55f, .35f),
                Curve(.65f, 1f, 1.12f));
            Add("Flash", flash, glow, 1, .27f, .32f, .22f, .25f, 0, 0,
                Colors(white, new Color(1, .95f, .63f), gold, 1f, .35f),
                Curve(1f, 1.15f, .75f));
            Add("HotCore", halo, glow, 1, .17f, .20f, .20f, .23f, 0, 0,
                Colors(white, white, gold, 1f, .4f),
                Curve(.9f, 1.2f, .65f));
            Add("Fire", fire, solid, 6, .13f, .19f, .22f, .33f, .012f, .43f,
                Colors(white, new Color(1, .94f, .68f), orange, 1f, .45f),
                Curve(.75f, 1.05f, 1.25f));
            ParticleSystem cloud = Add("Smoke", smoke, solid, 3, .14f, .20f, .32f, .42f, .045f, .20f,
                Colors(new Color(.64f, .61f, .58f), new Color(.7f, .67f, .63f),
                    new Color(.73f, .7f, .68f), .28f, .22f), Curve(.7f, 1.1f, 1.6f));
            var upward = cloud.velocityOverLifetime;
            upward.y = .15f;
            Add("Sparks", spark, glow, 7, .08f, .13f, .20f, .30f, .005f, 1.25f,
                Colors(new Color(1, 1, .88f), gold, orange, 1f, .42f),
                Curve(1f, .9f, .35f));
            ParticleSystem fragments = Add("ShellFragments", shard, solid, 5, .045f, .075f, .24f, .37f, .008f, .85f,
                Colors(new Color(.9f, .96f, 1), new Color(.83f, .89f, .97f),
                    new Color(.72f, .78f, .87f), 1f, .6f), Curve(1f, .95f, .7f));
            var fragmentMain = fragments.main;
            fragmentMain.gravityModifier = .12f;
            var rotation = fragments.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-8f, 8f);

            var fields = new SerializedObject(root.GetComponent<InterceptionBurst>());
            var particleArray = fields.FindProperty("particleSystems");
            var rendererArray = fields.FindProperty("particleRenderers");
            var offsets = fields.FindProperty("sortingOrderOffsets");
            int[] orders = { 0, 5, 6, 4, 1, 5, 3 };
            particleArray.arraySize = rendererArray.arraySize = offsets.arraySize = systems.Count;
            for (int i = 0; i < systems.Count; i++)
            {
                particleArray.GetArrayElementAtIndex(i).objectReferenceValue = systems[i];
                rendererArray.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                offsets.GetArrayElementAtIndex(i).intValue = orders[i];
            }
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AnimationCurve Curve(float start, float middle, float end) =>
            new AnimationCurve(new Keyframe(0, start), new Keyframe(.28f, middle), new Keyframe(1, end));

        private static Gradient Colors(Color start, Color middle, Color end, float alpha, float hold)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(start, 0), new GradientColorKey(middle, .45f), new GradientColorKey(end, 1) },
                new[] { new GradientAlphaKey(alpha, 0), new GradientAlphaKey(alpha, hold), new GradientAlphaKey(0, 1) });
            return gradient;
        }

        private static Mesh RadialMesh(string name, int segments, float lobes, Color center, Color middle, Color edge)
        {
            string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new List<Vector3> { Vector3.zero };
            // CPU mesh particles require normalized 8-bit vertex colors.
            var colors = new List<Color32> { center };
            var triangles = new List<int>();
            float[] bands = { .55f, .88f, 1f };
            Color[] shades = { middle, edge, new Color(edge.r, edge.g, edge.b, 0) };
            for (int ring = 0; ring < bands.Length; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float shape = lobes < 0 ? (i % 2 == 0 ? 1f : .32f) : 1f + lobes * Mathf.Sin(angle * 5f);
                    vertices.Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * .5f * bands[ring] * shape);
                    colors.Add(shades[ring]);
                }
            }
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.AddRange(new[] { 0, 1 + i, 1 + next });
                for (int ring = 0; ring < bands.Length - 1; ring++)
                {
                    int a = 1 + ring * segments + i;
                    int b = 1 + ring * segments + next;
                    int c = a + segments;
                    int d = b + segments;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh PolygonMesh(string name, Vector2[] outline, Color center, Color edge)
        {
            string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new Vector3[outline.Length + 1];
            var colors = new Color32[vertices.Length];
            var triangles = new int[outline.Length * 3];
            colors[0] = center;
            for (int i = 0; i < outline.Length; i++)
            {
                vertices[i + 1] = outline[i];
                colors[i + 1] = edge;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % outline.Length + 1;
            }
            var mesh = new Mesh { name = name, vertices = vertices, colors32 = colors, triangles = triangles };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material Material(string name, Shader shader, UnityEngine.Rendering.BlendMode destination, float brightness)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader) { name = name };
            material.SetFloat("_DestinationBlend", (float)destination);
            material.SetFloat("_Brightness", brightness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int separator = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, separator));
            AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
        }
    }
}
