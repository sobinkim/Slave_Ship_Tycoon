// Run as a statement block with Unity Pipeline eval_file.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
var replacements = new System.Collections.Generic.Dictionary<string, string[][]>
{
    ["Assets/SB/Scripts/Ship/Battle/TargetSelector.cs"] = new[]
    {
        new[] { "        LowestCurrentHealth\n", "        LowestCurrentHealth,\n        RandomInRule\n" },
        new[] { "각 규칙 안에서 현재 체력을 기준으로 선택합니다. 체력이 같으면 가까운 함선을 선택합니다.", "규칙은 배열 순서대로 적용합니다. RandomInRule은 해당 규칙의 유효 후보 안에서 균등 무작위로 선택합니다." },
        new[] { "            if (selectionMode == TargetSelectionMode.HighestCurrentHealth)", "            if (selectionMode == TargetSelectionMode.RandomInRule)\n                return targetList[Random.Range(0, targetList.Count)];\n\n            if (selectionMode == TargetSelectionMode.HighestCurrentHealth)" },
        new[] { "            if (target.myShipData.ShipType == ShipType.Enemy)\n            {\n                return line switch", "            // The player main ship has no escort slot and is the last support line.\n            if (targetTeam == TargetTeam.SameTeam && target.myShipData.ShipType == ShipType.MainShip)\n                return line == TargetLine.Back;\n\n            if (target.myShipData.ShipType == ShipType.Enemy)\n            {\n                return line switch" }
    },
    ["Assets/SB/Scripts/Ship/Battle/Projectile/HealingProjectile.cs"] = new[]
    {
        new[] { "            int frontColumn = isEnemyTarget\n                ? 0\n                : PlayerFleetLoadout.ColumnCount - 1;\n            if (!isSameTeam || target.myShipData.SpawnSlot.x != frontColumn)", "            // Line priority belongs to TargetSelector; a committed capsule can heal any allied line.\n            if (!isSameTeam)" }
    },
    ["Assets/SB/Scripts/Ship/Battle/AttackCompo/HealingProjectileAttackCompo.cs"] = new[]
    {
        new[] { "        protected override void OnAttackEnter()\n        {\n            // Reconsider health priority for every shot instead of retaining the first living ally.\n            TryAcquireTarget();\n            HandleTargetChanged(CurrentTarget);\n        }\n\n", "" },
        new[] { "        protected override void Fire()\n        {\n            TryAcquireTarget();", "        protected override void Fire()\n        {\n            // Pick once at the firing event so the support icon follows the committed recipient.\n            TryAcquireTarget();" }
    },
    ["Assets/SB/Editor/HealingSupportShipSetup.cs"] = new[]
    {
        new[] { "shootPoint.localPosition = isEnemy ? new Vector3(.355f, -.035f, 0f) : new Vector3(.385f, -.055f, 0f);", "shootPoint.localPosition = new Vector3(.156f, .073f, 0f);" },
        new[] { "muzzle.localEulerAngles = new Vector3(0f, 0f, 12f);", "muzzle.localEulerAngles = new Vector3(0f, 0f, 90f);" },
        new[] { "Property(attackFields, \"travelMode\").intValue = (int)ProjectileTravelMode.AcceleratingStraight;", "Property(attackFields, \"travelMode\").intValue = (int)ProjectileTravelMode.BallisticArc;\n                Property(attackFields, \"ballisticApexHeight\").floatValue = .9f;\n                Property(attackFields, \"ballisticTimeToApex\").floatValue = .38f;\n                Property(attackFields, \"ballisticDescentTimeMultiplier\").floatValue = 1f;\n                Property(attackFields, \"ballisticTargetClearance\").floatValue = .25f;" },
        new[] { "Property(selectorFields, \"selectionMode\").intValue = 2;", "Property(selectorFields, \"selectionMode\").intValue = (int)TargetSelectionMode.RandomInRule;" },
        new[] { "                rules.arraySize = 1;\n                rules.GetArrayElementAtIndex(0).FindPropertyRelative(\"Line\").intValue = (int)TargetLine.Front;\n                rules.GetArrayElementAtIndex(0).FindPropertyRelative(\"Role\").intValue = (int)ShipRole.None;", "                rules.arraySize = 3;\n                TargetLine[] healingLines = { TargetLine.Front, TargetLine.Middle, TargetLine.Back };\n                for (int i = 0; i < healingLines.Length; i++)\n                {\n                    rules.GetArrayElementAtIndex(i).FindPropertyRelative(\"Line\").intValue = (int)healingLines[i];\n                    rules.GetArrayElementAtIndex(i).FindPropertyRelative(\"Role\").intValue = (int)ShipRole.None;\n                }" }
    }
};
var staged = new System.Collections.Generic.Dictionary<string, string>();
foreach (var file in replacements)
{
    string contents = System.IO.File.ReadAllText(file.Key).Replace("\r\n", "\n");
    foreach (var replacement in file.Value)
    {
        if (!contents.Contains(replacement[0])) throw new System.InvalidOperationException("Expected code not found in " + file.Key + ": " + replacement[0]);
        contents = contents.Replace(replacement[0], replacement[1]);
    }
    staged.Add(file.Key, contents);
}
System.IO.Directory.CreateDirectory("QA/HealingMortar/Before");
foreach (var file in staged)
{
    string backup = "QA/HealingMortar/Before/" + System.IO.Path.GetFileName(file.Key) + ".txt";
    if (!System.IO.File.Exists(backup)) System.IO.File.Copy(file.Key, backup);
    System.IO.File.WriteAllText(file.Key, file.Value, new System.Text.UTF8Encoding(false));
}
UnityEditor.AssetDatabase.Refresh();
return "Updated random support targeting, all-line healing, and mortar authoring defaults.";
