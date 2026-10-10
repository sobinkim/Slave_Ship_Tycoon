if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode before prefab authoring.");
foreach (string path in new[]
{
    "Assets/SB/Prefabs/PlayerShips/HealingSupportShip.prefab",
    "Assets/SB/Prefabs/EnemyShips/EnemyHealingSupportShip.prefab"
})
{
    string backupName = System.IO.Path.GetFileName(path) + ".txt";
    string backupPath = "QA/MainShipSideSlots/Before/" + backupName;
    if (!System.IO.File.Exists(backupPath)) System.IO.File.Copy(path, backupPath);
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        var selector = root.GetComponentInChildren<SB.Scripts.TargetSelector>(true);
        if (selector == null) throw new System.Exception("TargetSelector missing on " + path);
        var serialized = new UnityEditor.SerializedObject(selector);
        var exclude = serialized.FindProperty("excludesHealingSupportShips");
        if (exclude == null) throw new System.Exception("Healing support target filter missing.");
        exclude.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
}
UnityEditor.AssetDatabase.SaveAssets();
return "Player and enemy healing support prefabs exclude all healing support ships from same-team target candidates.";
