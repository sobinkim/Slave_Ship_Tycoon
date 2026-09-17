using System.Collections.Generic;
using SB.Scripts;
using SB.Scripts.Fleet.Effects;
using UnityEditor;
using UnityEngine;

namespace SB.Editor
{
    // One-time authoring command; runtime never guesses anchors or edits prefab sizes.
    public static class FleetShieldSetup
    {
        private const string EffectFolder = "Assets/SB/Data/FleetEffects";
        private const string VisualFolder = "Assets/SB/Art/Effects/FleetShield";
        private const string PrefabFolder = "Assets/SB/Prefabs/Effects";
        private static readonly Vector2[] Outline = {
            new Vector2(0,.53f),new Vector2(.20f,.37f),new Vector2(.29f,.33f),
            new Vector2(.26f,-.08f),new Vector2(.17f,-.34f),new Vector2(0,-.53f),
            new Vector2(-.17f,-.34f),new Vector2(-.26f,-.08f),new Vector2(-.29f,.33f),new Vector2(-.20f,.37f)
        };
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/'); Folder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
        private static void Reference(SerializedObject so, string field, Object value)
        { so.FindProperty(field).objectReferenceValue = value; }
        private static T Component<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
        private static Mesh BuildMesh(int sector = -1)
        {
            var vertices = new List<Vector3>(); var colors = new List<Color>();
            var uvs = new List<Vector2>(); var indices = new List<int>();
            void Triangle(Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
            {
                int start = vertices.Count;
                foreach (Vector2 p in new[] { a, b, c })
                {
                    // A fixed oblique silhouette faces right; the enemy anchor mirrors it.
                    vertices.Add(new Vector3(p.x * .78f, p.y + p.x * .16f, 0));
                    uvs.Add(p + new Vector2(.5f,.55f));
                }
                colors.Add(ca); colors.Add(cb); colors.Add(cc);
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
            }
            Color core = new Color(.12f,1,0,.20f), edge = new Color(1,0,0,.94f), clear = new Color(.7f,0,0,0);
            for (int i = 0; i < Outline.Length; i++)
            {
                if (sector >= 0 && sector != i) continue;
                Vector2 a = Outline[i], b = Outline[(i + 1) % Outline.Length];
                Vector2 ia = a * .94f, ib = b * .94f, oa = a * 1.14f, ob = b * 1.14f;
                Triangle(Vector2.zero, ia, ib, core, core, core);
                Triangle(ia, a, b, edge, edge, edge); Triangle(ia, b, ib, edge, edge, edge);
                Triangle(a, oa, ob, edge, clear, clear); Triangle(a, ob, b, edge, clear, edge);
            }
            var mesh = new Mesh { name = sector < 0 ? "Shield" : "ShieldFragment" + sector };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0, uvs); mesh.SetTriangles(indices,0);
            mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh MeshAsset(string path, int sector = -1)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            Mesh mesh = BuildMesh(sector); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        [MenuItem("Tools/Fleet/Set Up Shield Effects")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before authoring shield assets.");
            Folder(EffectFolder); Folder(VisualFolder); Folder(PrefabFolder);
            var effect = AssetDatabase.LoadAssetAtPath<ImmediateRearProtection>(EffectFolder + "/ImmediateRearProtection.asset");
            if (effect == null)
            { effect = ScriptableObject.CreateInstance<ImmediateRearProtection>(); AssetDatabase.CreateAsset(effect,EffectFolder + "/ImmediateRearProtection.asset"); }
            var material = AssetDatabase.LoadAssetAtPath<Material>(VisualFolder + "/Shield.mat");
            if (material == null)
            { material = new Material(Shader.Find("SB/Fleet Shield")); AssetDatabase.CreateAsset(material,VisualFolder + "/Shield.mat"); }
            Mesh shieldMesh = MeshAsset(VisualFolder + "/Shield.asset");
            string burstPath = PrefabFolder + "/ShieldBreak.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(burstPath) == null)
            {
                var burstRoot = new GameObject("ShieldBreak");
                try
                {
                    var burst = burstRoot.AddComponent<ShieldBreakEffect>();
                    var so = new SerializedObject(burst); var parts = so.FindProperty("fragments"); parts.arraySize = Outline.Length;
                    for (int i = 0; i < Outline.Length; i++)
                    {
                        var part = new GameObject("Fragment_" + i, typeof(MeshFilter), typeof(MeshRenderer)); part.transform.SetParent(burstRoot.transform,false);
                        part.GetComponent<MeshFilter>().sharedMesh = MeshAsset(VisualFolder + "/Fragment_" + i + ".asset",i);
                        var renderer = part.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                        var entry = parts.GetArrayElementAtIndex(i);
                        entry.FindPropertyRelative("Renderer").objectReferenceValue = renderer;
                        Vector2 direction = (Outline[i] + Outline[(i+1)%Outline.Length]).normalized;
                        entry.FindPropertyRelative("LocalVelocity").vector3Value = new Vector3(direction.x * 1.5f,direction.y * 1.4f,0);
                        entry.FindPropertyRelative("SpinDegreesPerSecond").floatValue = i % 2 == 0 ? 120 : -100;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(burstRoot,burstPath);
                }
                finally { Object.DestroyImmediate(burstRoot); }
            }
            var burstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(burstPath).GetComponent<ShieldBreakEffect>();
            string[] paths = {
                "Assets/SB/Prefabs/PlayerShips/IroncladDefender.prefab",
                "Assets/SB/Prefabs/PlayerShips/Test/TestEscort_A.prefab",
                "Assets/SB/Prefabs/PlayerShips/Test/TestEscort_B.prefab",
                "Assets/SB/Prefabs/PlayerShips/Test/TestEscort_C.prefab",
                "Assets/SB/Prefabs/PlayerShips/Test/TestMainShip.prefab",
                "Assets/SB/Prefabs/EnemyShips/Test/TestEnemy_A.prefab",
                "Assets/SB/Prefabs/EnemyShips/Test/TestEnemy_B.prefab",
                "Assets/SB/Prefabs/EnemyShips/Test/TestEnemy_C.prefab",
                "Assets/SB/Prefabs/EnemyShips/Test/TestBoss.prefab"
            };
            foreach (string path in paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Ship ship = root.GetComponent<Ship>();
                    var receiver = Component<FleetEffectReceiver>(root);
                    bool isEnemy = path.Contains("EnemyShips"), isMain = path.Contains("TestMainShip");
                    Transform presentation = root.transform.Find("Presentation");
                    if (presentation == null)
                    {
                        presentation = new GameObject("Presentation").transform;
                        presentation.SetParent(root.transform, false);
                    }
                    Transform anchor = presentation.Find("ShieldAnchor") ?? root.transform.Find("ShieldAnchor");
                    if (anchor == null)
                    {
                        anchor = new GameObject("ShieldAnchor").transform; anchor.SetParent(presentation,false);
                        anchor.localPosition = new Vector3(isEnemy ? -.36f : isMain ? .72f : .40f, .02f, 0);
                        anchor.localScale = new Vector3(isEnemy ? -.65f : .65f,.72f,1);
                    }
                    else if (anchor.parent != presentation)
                    {
                        anchor.SetParent(presentation, true);
                    }
                    var meshFilter = Component<MeshFilter>(anchor.gameObject);
                    meshFilter.sharedMesh = shieldMesh;
                    var renderer = Component<MeshRenderer>(anchor.gameObject);
                    renderer.sharedMaterial = material; renderer.sortingOrder = 20; renderer.enabled = false;
                    // Match the actual hull sorting layer (including the existing "Visual " typo).
                    Transform visual = root.transform.Find("Visual") ?? root.transform.Find("Visual ");
                    if (visual != null && visual.TryGetComponent(out SpriteRenderer hull)) renderer.sortingLayerID = hull.sortingLayerID;
                    var shield = Component<ShipShieldVisual>(anchor.gameObject);
                    var vs = new SerializedObject(shield); Reference(vs,"shieldRenderer",renderer); Reference(vs,"breakPrefab",burstPrefab); vs.ApplyModifiedPropertiesWithoutUndo();
                    var rs = new SerializedObject(receiver); Reference(rs,"owner",ship);
                    var presentations = rs.FindProperty("presentations");
                    bool configured = false;
                    for (int i=0;i<presentations.arraySize;i++)
                        if (presentations.GetArrayElementAtIndex(i).FindPropertyRelative("Effect").objectReferenceValue == effect) configured = true;
                    if (!configured)
                    {
                        int index=presentations.arraySize; presentations.arraySize++;
                        var p=presentations.GetArrayElementAtIndex(index);p.FindPropertyRelative("Effect").objectReferenceValue=effect;p.FindPropertyRelative("Visual").objectReferenceValue=shield;
                    }
                    rs.ApplyModifiedPropertiesWithoutUndo();
                    if (path.Contains("IroncladDefender"))
                    {
                        var provider=Component<FleetEffectProvider>(root);
                        var ps=new SerializedObject(provider); Reference(ps,"owner",ship); Reference(ps,"sourceReceiver",receiver);
                        var effects=ps.FindProperty("effects");effects.arraySize=1;effects.GetArrayElementAtIndex(0).objectReferenceValue=effect;ps.ApplyModifiedPropertiesWithoutUndo();
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Fleet shield: 9 receivers configured; IroncladDefender supplies immediate rear protection.");
        }
    }
}
