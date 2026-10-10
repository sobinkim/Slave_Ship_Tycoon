// Statement block for Unity Pipeline eval_file. Existing sprite metadata and GUIDs are preserved.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
string referencePath = "C:/Users/sobin/AppData/Local/Temp/codex-clipboard-406d66d9-6669-4026-87ac-c1b665080059.png";
var reference = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, false);
if (!UnityEngine.ImageConversion.LoadImage(reference, System.IO.File.ReadAllBytes(referencePath))) throw new System.Exception("Cannot load approved reference.");
var spritePaths = new[] { "Assets/SB/Art/Sprites/Ships/HealingSupportShip.png", "Assets/SB/Art/Sprites/Ships/Enemies/EnemyHealingSupportShip.png" };
var prefabPaths = new[] { "Assets/SB/Prefabs/PlayerShips/HealingSupportShip.prefab", "Assets/SB/Prefabs/EnemyShips/EnemyHealingSupportShip.prefab" };
var authoredMuzzlePixels = new[] { new UnityEngine.Vector2(691f, 463f), new UnityEngine.Vector2(1369f, 463f) };
var report = new System.Collections.Generic.List<object>();
System.IO.Directory.CreateDirectory("QA/HealingMortar/Before");
for (int side = 0; side < 2; side++)
{
    string spritePath = spritePaths[side];
    string backup = "QA/HealingMortar/Before/" + System.IO.Path.GetFileName(spritePath);
    if (!System.IO.File.Exists(backup)) System.IO.File.Copy(spritePath, backup);
    var previous = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, false);
    UnityEngine.ImageConversion.LoadImage(previous, System.IO.File.ReadAllBytes(backup));
    var previousPixels = previous.GetPixels32();
    int previousLeft = previous.width, previousRight = -1, previousBottom = previous.height, previousTop = -1;
    for (int y = 0; y < previous.height; y++)
    for (int x = 0; x < previous.width; x++)
    {
        if (previousPixels[y * previous.width + x].a < 16) continue;
        previousLeft = System.Math.Min(previousLeft, x); previousRight = System.Math.Max(previousRight, x);
        previousBottom = System.Math.Min(previousBottom, y); previousTop = System.Math.Max(previousTop, y);
    }
    int split = reference.width / 2;
    int regionLeft = side == 0 ? 0 : split;
    int regionRight = side == 0 ? split : reference.width;
    var sourcePixels = reference.GetPixels32();
    int sourceLeft = regionRight, sourceRight = -1, sourceBottom = reference.height, sourceTop = -1;
    for (int y = 0; y < reference.height; y++)
    for (int x = regionLeft; x < regionRight; x++)
    {
        if (sourcePixels[y * reference.width + x].a < 16) continue;
        sourceLeft = System.Math.Min(sourceLeft, x); sourceRight = System.Math.Max(sourceRight, x);
        sourceBottom = System.Math.Min(sourceBottom, y); sourceTop = System.Math.Max(sourceTop, y);
    }
    if (sourceRight < sourceLeft || previousRight < previousLeft) throw new System.Exception("Ship artwork must contain transparent-background art.");
    float scale = (previousRight - previousLeft + 1f) / (sourceRight - sourceLeft + 1f);
    float sourceCenterX = (sourceLeft + sourceRight) * .5f;
    float sourceCenterY = (sourceBottom + sourceTop) * .5f;
    float targetCenterX = (previousLeft + previousRight) * .5f;
    // Extend transparent canvas when the upright mortar makes the new silhouette taller.
    // Preserve the old visible width and pixels-per-unit without clipping the flag.
    int outputHeight = System.Math.Max(previous.height, UnityEngine.Mathf.CeilToInt((sourceTop - sourceBottom + 1) * scale) + 40);
    float targetCenterY = (previousBottom + previousTop) * .5f + (outputHeight - previous.height) * .5f;
    var texture = new UnityEngine.Texture2D(previous.width, outputHeight, UnityEngine.TextureFormat.RGBA32, false);
    var output = new UnityEngine.Color[texture.width * texture.height];
    for (int y = 0; y < texture.height; y++)
    for (int x = 0; x < texture.width; x++)
    {
        float sourceX = sourceCenterX + (x - targetCenterX) / scale * (side == 1 ? -1f : 1f);
        float sourceY = sourceCenterY + (y - targetCenterY) / scale;
        if (sourceX < regionLeft || sourceX >= regionRight || sourceY < 0 || sourceY >= reference.height) continue;
        output[y * texture.width + x] = reference.GetPixelBilinear((sourceX + .5f) / reference.width, (sourceY + .5f) / reference.height);
    }
    texture.SetPixels(output); texture.Apply();
    System.IO.File.WriteAllBytes(spritePath, UnityEngine.ImageConversion.EncodeToPNG(texture));
    UnityEditor.AssetDatabase.ImportAsset(spritePath, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(spritePath);
    var muzzlePixel = authoredMuzzlePixels[side];
    float muzzleX = targetCenterX + (muzzlePixel.x - sourceCenterX) * scale * (side == 1 ? -1f : 1f);
    float muzzleY = targetCenterY + (muzzlePixel.y - sourceCenterY) * scale;
    var muzzleLocalPosition = new UnityEngine.Vector3((muzzleX - sprite.pivot.x) / sprite.pixelsPerUnit, (muzzleY - sprite.pivot.y) / sprite.pixelsPerUnit, 0f);
    string prefabBackup = "QA/HealingMortar/Before/" + System.IO.Path.GetFileName(prefabPaths[side]) + ".txt";
    if (!System.IO.File.Exists(prefabBackup)) System.IO.File.Copy(prefabPaths[side], prefabBackup);
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPaths[side]);
    try
    {
        var visual = root.transform.Find("Visual");
        visual.GetComponent<UnityEngine.SpriteRenderer>().sprite = sprite;
        visual.Find("ShootPos").localPosition = muzzleLocalPosition;
        visual.Find("MuzzleVfx").localPosition = muzzleLocalPosition;
        visual.Find("MuzzleVfx").localEulerAngles = new UnityEngine.Vector3(0f, 0f, 90f);
        var attack = new UnityEditor.SerializedObject(root.GetComponentInChildren<SB.Scripts.AttackCompo.HealingProjectileAttackCompo>(true));
        attack.FindProperty("travelMode").intValue = (int)SB.Scripts.AttackCompo.ProjectileTravelMode.BallisticArc;
        attack.FindProperty("ballisticApexHeight").floatValue = .9f;
        attack.FindProperty("ballisticTimeToApex").floatValue = .38f;
        attack.FindProperty("ballisticDescentTimeMultiplier").floatValue = 1f;
        attack.FindProperty("ballisticTargetClearance").floatValue = .25f;
        attack.ApplyModifiedPropertiesWithoutUndo();
        var selector = new UnityEditor.SerializedObject(root.GetComponentInChildren<SB.Scripts.TargetSelector>(true));
        selector.FindProperty("selectionMode").intValue = (int)SB.Scripts.TargetSelectionMode.RandomInRule;
        var rules = selector.FindProperty("targetRules"); rules.arraySize = 3;
        var lines = new[] { SB.Scripts.TargetLine.Front, SB.Scripts.TargetLine.Middle, SB.Scripts.TargetLine.Back };
        for (int i = 0; i < lines.Length; i++)
        {
            rules.GetArrayElementAtIndex(i).FindPropertyRelative("Line").intValue = (int)lines[i];
            rules.GetArrayElementAtIndex(i).FindPropertyRelative("Role").intValue = (int)SB.Scripts.ShipRole.None;
        }
        selector.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPaths[side]);
        report.Add(new { spritePath, width = texture.width, height = texture.height, muzzle = muzzleLocalPosition.ToString("F4"), mode = "BallisticArc", selection = "RandomInRule" });
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
    string shipName = side == 0 ? "HealingSupportShip" : "EnemyHealingSupportShip";
    string clipPath = "Assets/SB/Animations/Ships/" + (side == 0 ? "" : "Enemies/") + shipName + "/" + shipName + "_Attack.anim";
    string clipBackup = "QA/HealingMortar/Before/" + shipName + "_Attack.anim.txt";
    if (!System.IO.File.Exists(clipBackup)) System.IO.File.Copy(clipPath, clipBackup);
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPath);
    var horizontalBinding = UnityEditor.EditorCurveBinding.FloatCurve("", typeof(UnityEngine.Transform), "m_LocalPosition.x");
    var verticalBinding = UnityEditor.EditorCurveBinding.FloatCurve("", typeof(UnityEngine.Transform), "m_LocalPosition.y");
    var recoil = UnityEditor.AnimationUtility.GetEditorCurve(clip, horizontalBinding);
    if (recoil != null && recoil.keys.Length > 2)
    {
        var keys = recoil.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].value *= side == 0 ? 1f : -1f;
            keys[i].inTangent *= side == 0 ? 1f : -1f;
            keys[i].outTangent *= side == 0 ? 1f : -1f;
        }
        recoil.keys = keys;
        UnityEditor.AnimationUtility.SetEditorCurve(clip, horizontalBinding, UnityEngine.AnimationCurve.Constant(0f, clip.length, 0f));
        UnityEditor.AnimationUtility.SetEditorCurve(clip, verticalBinding, recoil);
        UnityEditor.EditorUtility.SetDirty(clip);
    }
    UnityEngine.Object.DestroyImmediate(previous); UnityEngine.Object.DestroyImmediate(texture);
}
UnityEngine.Object.DestroyImmediate(reference);
UnityEditor.AssetDatabase.SaveAssets();
string json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("QA/HealingMortar/AppliedAssets.json", json);
return json;
