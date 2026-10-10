if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");
var replacements = new System.Collections.Generic.Dictionary<string, string[][]>
{
    ["Assets/SB/Scripts/Stage/StageSpawnManager.cs"] = new[]
    {
        new[] { "[SerializeField] private Transform[] escortShipSpawnPoints", "[SerializeField, Tooltip(\"1~6: 기존 중열/전열 3행. 7: 메인 함선 위 후열. 8: 메인 함선 아래 후열.\")] private Transform[] escortShipSpawnPoints" },
        new[] { "mainShip.SetSpawnSlot(new Vector2Int(-1, -1));", "mainShip.SetSpawnSlot(PlayerFleetLoadout.MainShipSlot);" },
        new[] { "escortShip.SetSpawnSlot(new Vector2Int(\n                    i % PlayerFleetLoadout.ColumnCount,\n                    i / PlayerFleetLoadout.ColumnCount));", "escortShip.SetSpawnSlot(PlayerFleetLoadout.GetEscortSlotCoordinates(i));" }
    },
    ["Assets/SB/Scripts/Ship/Battle/TargetSelector.cs"] = new[]
    {
        new[] { "TargetLine.Front => target.myShipData.SpawnSlot.x == PlayerFleetLoadout.ColumnCount - 1,\n                TargetLine.Back", "TargetLine.Front => target.myShipData.SpawnSlot.x == PlayerFleetLoadout.ColumnCount - 1,\n                TargetLine.Middle => target.myShipData.SpawnSlot.x == 1,\n                TargetLine.Back" }
    },
    ["Assets/SB/Scripts/UI/Fleet/FleetAttackPreview.cs"] = new[]
    {
        new[] { "? $\" · {slotIndex / PlayerFleetLoadout.ColumnCount + 1}행 {(slotIndex % PlayerFleetLoadout.ColumnCount == 1 ? \"전열\" : \"후열\")}\" : \"\";", "? \" · \" + PlayerFleetLoadout.GetFormationPositionLabel(slotIndex) : \"\";" },
        new[] { "아군 전열 · 저체력 우선", "아군 전열 → 중열 → 후열" },
        new[] { "아군 전열 중 현재 체력이 낮은 함선 우선 · 최대 체력 제외", "해당 열의 회복이 필요한 아군 중 무작위 선택 · 최대 체력 제외" }
    },
    ["Assets/SB/Editor/StageSpawnManagerEditor.cs"] = new[]
    {
        new[] { "Escort Spawn Points (Slots 1-6)", "Escort Spawn Points (8 slots: Back / Middle / Front)" },
        new[] { "int slotIndex = row * PlayerFleetLoadout.ColumnCount + column;\n                    SerializedProperty pointProperty", "int slotIndex = PlayerFleetLoadout.GetEscortSlotIndex(column, row);\n                    if (slotIndex < 0)\n                    {\n                        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));\n                        EditorGUILayout.LabelField(\"Main ship (fixed)\", EditorStyles.miniBoldLabel);\n                        EditorGUILayout.LabelField(\"Use Main Ship Spawn Point above\", EditorStyles.miniLabel);\n                        EditorGUILayout.EndVertical();\n                        continue;\n                    }\n                    SerializedProperty pointProperty" }
    }
};
var staged = new System.Collections.Generic.Dictionary<string, string>();
foreach (var file in replacements)
{
    string contents = System.IO.File.ReadAllText(file.Key).Replace("\r\n", "\n");
    foreach (var pair in file.Value)
    {
        if (!contents.Contains(pair[0])) throw new System.Exception("Expected code missing: " + file.Key + " " + pair[0]);
        contents = contents.Replace(pair[0], pair[1]);
    }
    staged.Add(file.Key, contents);
}
staged.Add("Assets/SB/Scripts/Stage/PlayerFleetLoadout.cs", System.IO.File.ReadAllText("QA/MainShipSideSlots/PlayerFleetLoadout.cs"));
System.IO.Directory.CreateDirectory("QA/MainShipSideSlots/Before");
string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
if (!System.IO.File.Exists("QA/MainShipSideSlots/Before/SampleScene.unity.txt")) System.IO.File.Copy(scenePath, "QA/MainShipSideSlots/Before/SampleScene.unity.txt");
foreach (var file in staged)
{
    string backup = "QA/MainShipSideSlots/Before/" + System.IO.Path.GetFileName(file.Key) + ".txt";
    if (!System.IO.File.Exists(backup)) System.IO.File.Copy(file.Key, backup);
    System.IO.File.WriteAllText(file.Key, file.Value, new System.Text.UTF8Encoding(false));
}
UnityEditor.AssetDatabase.Refresh();
return "Eight escort slots with preserved saved indices; three depth columns; matching preview and spawn inspector.";
