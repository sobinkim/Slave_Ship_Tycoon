// Run in Edit Mode through eval_file; all temporary objects and Random state are restored.
var objects = new System.Collections.Generic.List<UnityEngine.GameObject>();
var checks = new System.Collections.Generic.List<string>();
var randomState = UnityEngine.Random.state;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
System.Action<bool, string> check = (passed, name) => { if (!passed) throw new System.Exception(name); checks.Add(name); };
System.Func<SB.Scripts.ShipType, int, int, float, SB.Scripts.Ship> makeShip = (team, column, row, healthRatio) =>
{
    var root = new UnityEngine.GameObject("Healing QA " + team + " " + column + ":" + row); objects.Add(root);
    var health = root.AddComponent<SB.Core.EntityHealth>();
    SB.Scripts.Ship ship = team == SB.Scripts.ShipType.Enemy ? (SB.Scripts.Ship)root.AddComponent<SB.Scripts.Enemy>() :
        team == SB.Scripts.ShipType.MainShip ? (SB.Scripts.Ship)root.AddComponent<SB.Scripts.MainShip>() : root.AddComponent<SB.Scripts.Ship>();
    ship.myShipData = new SB.Scripts.MyShipData { ShipType = team, SpawnSlot = new UnityEngine.Vector2Int(column, row) };
    typeof(SB.Core.Entity).GetMethod("CacheComponents", flags).Invoke(ship, null);
    health.Initialize(ship); health.ResetHealth(healthRatio);
    return ship;
};
try
{
    var playerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SB/Prefabs/PlayerShips/HealingSupportShip.prefab");
    var enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SB/Prefabs/EnemyShips/EnemyHealingSupportShip.prefab");
    foreach (var prefab in new[] { playerPrefab, enemyPrefab })
    {
        var selector = prefab.GetComponentInChildren<SB.Scripts.TargetSelector>(true);
        check(selector.SelectionMode == SB.Scripts.TargetSelectionMode.RandomInRule, prefab.name + ": random selection");
        check(selector.TargetRules.Count == 3 && selector.TargetRules[0].Line == SB.Scripts.TargetLine.Front && selector.TargetRules[1].Line == SB.Scripts.TargetLine.Middle && selector.TargetRules[2].Line == SB.Scripts.TargetLine.Back, prefab.name + ": front-middle-back order");
        var attack = prefab.GetComponentInChildren<SB.Scripts.AttackCompo.HealingProjectileAttackCompo>(true);
        var fields = new UnityEditor.SerializedObject(attack);
        check(fields.FindProperty("travelMode").intValue == (int)SB.Scripts.AttackCompo.ProjectileTravelMode.BallisticArc, prefab.name + ": ballistic arc configured");
        check(attack.AttackIntervalMultiplier == 3f, prefab.name + ": triple firing interval preserved");
        check(fields.FindProperty("firePoints").GetArrayElementAtIndex(0).objectReferenceValue == prefab.transform.Find("Visual/ShootPos"), prefab.name + ": explicit mortar fire point");
        check(fields.FindProperty("supportEffectProvider").objectReferenceValue != null && fields.FindProperty("supportEffect").objectReferenceValue != null, prefab.name + ": support status references");
        var projectile = (SB.Scripts.Projectiles.HealingProjectile)fields.FindProperty("projectilePrefab").objectReferenceValue;
        var projectileFields = new UnityEditor.SerializedObject(projectile);
        var effect = (SB.Scripts.Projectiles.HealingImpactEffect)projectileFields.FindProperty("healingImpactEffectPrefab").objectReferenceValue;
        check(effect != null && effect.GetComponentInChildren<UnityEngine.ParticleSystem>(true) != null && effect.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true).Length == 0, prefab.name + ": cross particle healing effect");
    }
    var source = makeShip(SB.Scripts.ShipType.Enemy, 2, 1, .5f);
    var frontA = makeShip(SB.Scripts.ShipType.Enemy, 0, 0, .8f);
    var frontB = makeShip(SB.Scripts.ShipType.Enemy, 0, 1, .1f);
    var middle = makeShip(SB.Scripts.ShipType.Enemy, 1, 0, .01f);
    var back = makeShip(SB.Scripts.ShipType.Enemy, 2, 0, .01f);
    var selection = source.gameObject.AddComponent<SB.Scripts.TargetSelector>();
    UnityEditor.EditorJsonUtility.FromJsonOverwrite(UnityEditor.EditorJsonUtility.ToJson(enemyPrefab.GetComponentInChildren<SB.Scripts.TargetSelector>(true)), selection);
    selection.Initialize(source);
    selection.SetBattleSpawnData(new SB.Scripts.BattleSpawnData(null, null, new SB.Scripts.StageEnemySpawnData(new[] { (SB.Scripts.Enemy)source, (SB.Scripts.Enemy)frontA, (SB.Scripts.Enemy)frontB, (SB.Scripts.Enemy)middle, (SB.Scripts.Enemy)back })));
    UnityEngine.Random.InitState(14319);
    int countA = 0, countB = 0;
    for (int i = 0; i < 128; i++)
    {
        var target = selection.GetTarget();
        if (target == frontA) countA++; else if (target == frontB) countB++; else throw new System.Exception("Skipped valid front line");
    }
    check(countA > 0 && countB > 0, "Both front ships selected despite different health: " + countA + "/" + countB);
    frontA.GetCompo<SB.Core.EntityHealth>().ResetHealth(); frontB.GetCompo<SB.Core.EntityHealth>().ResetHealth();
    check(selection.GetTarget() == middle, "Full front line falls through to damaged middle");
    middle.IsDead = true;
    check(selection.GetTarget() == back, "Unavailable middle falls through to back");
    back.GetCompo<SB.Core.EntityHealth>().ResetHealth();
    check(selection.GetTarget() == null, "Full-health allies and self are excluded");
    middle.IsDead = false;
    var capsuleObject = new UnityEngine.GameObject("QA healing capsule"); objects.Add(capsuleObject);
    var capsule = capsuleObject.AddComponent<SB.Scripts.Projectiles.HealingProjectile>(); capsule.ConfigureSource(source);
    var impact = typeof(SB.Scripts.Projectiles.HealingProjectile).GetMethod("OnTargetImpact", flags);
    foreach (var target in new[] { middle, back })
    {
        var health = target.GetCompo<SB.Core.EntityHealth>(); health.ResetHealth(.1f);
        float before = health.CurrentHealth;
        impact.Invoke(capsule, new object[] { target, 30f, UnityEngine.Vector3.zero });
        check(UnityEngine.Mathf.Approximately(health.CurrentHealth, before + 30f), "Capsule heals allied column " + target.myShipData.SpawnSlot.x);
    }
    var opponent = makeShip(SB.Scripts.ShipType.EscortShip, 1, 0, .1f);
    float opponentBefore = opponent.GetCompo<SB.Core.EntityHealth>().CurrentHealth;
    impact.Invoke(capsule, new object[] { opponent, 30f, UnityEngine.Vector3.zero });
    check(opponent.GetCompo<SB.Core.EntityHealth>().CurrentHealth == opponentBefore, "Capsule cannot heal opposing team");
    var playerSource = makeShip(SB.Scripts.ShipType.EscortShip, 0, 1, .5f);
    var playerFront = makeShip(SB.Scripts.ShipType.EscortShip, 1, 0, .5f);
    var playerRear = makeShip(SB.Scripts.ShipType.EscortShip, 0, 0, .5f);
    var mainShip = (SB.Scripts.MainShip)makeShip(SB.Scripts.ShipType.MainShip, -1, -1, .5f);
    var playerSelector = playerSource.gameObject.AddComponent<SB.Scripts.TargetSelector>();
    UnityEditor.EditorJsonUtility.FromJsonOverwrite(UnityEditor.EditorJsonUtility.ToJson(playerPrefab.GetComponentInChildren<SB.Scripts.TargetSelector>(true)), playerSelector);
    playerSelector.Initialize(playerSource);
    playerSelector.SetBattleSpawnData(new SB.Scripts.BattleSpawnData(mainShip, new[] { playerSource, playerFront, playerRear }, new SB.Scripts.StageEnemySpawnData(null)));
    check(playerSelector.GetTarget() == playerFront, "Player front line takes priority");
    playerFront.IsDead = true; playerRear.IsDead = true;
    check(playerSelector.GetTarget() == mainShip, "Main ship remains a valid rear support recipient");
    var launch = new SB.Scripts.Projectiles.ProjectileLaunchData(back, UnityEngine.Vector3.zero, new UnityEngine.Vector3(1f, -.2f, 0f), UnityEngine.Vector3.down, SB.Scripts.Projectiles.ProjectileFlightData.Default, 30f, SB.Scripts.Projectiles.ProjectileHitMode.Guaranteed);
    capsule.Launch(launch, SB.Scripts.AttackCompo.ProjectileTravelMode.BallisticArc, 0f, 0f, .9f, .38f, 1f, .35f, .25f);
    var positionMethod = typeof(SB.Scripts.Projectiles.BaseProjectile).GetMethod("EvaluateBallisticPosition", flags);
    var durationField = typeof(SB.Scripts.Projectiles.BaseProjectile).GetField("flightDuration", flags);
    float duration = (float)durationField.GetValue(capsule);
    var start = (UnityEngine.Vector3)positionMethod.Invoke(capsule, new object[] { 0f });
    var early = (UnityEngine.Vector3)positionMethod.Invoke(capsule, new object[] { .1f });
    var apex = (UnityEngine.Vector3)positionMethod.Invoke(capsule, new object[] { .38f / duration });
    var end = (UnityEngine.Vector3)positionMethod.Invoke(capsule, new object[] { 1f });
    check(start == UnityEngine.Vector3.zero && early.y > start.y && UnityEngine.Mathf.Approximately(apex.y, .9f) && (end - launch.TargetImpactPosition).sqrMagnitude < .00001f, "Mortar trajectory rises, reaches apex, and lands on target");
    var healerData = UnityEditor.AssetDatabase.LoadAssetAtPath<SB.Scripts.Fleet.EscortShipData>("Assets/SB/Data/PlayerFleet/EscortShips/Escort_HealingSupportShip.asset");
    var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<SB.Scripts.Fleet.EscortShipCatalog>("Assets/SB/Data/PlayerFleet/EscortShipCatalog.asset");
    check(healerData != null && catalog.Contains(healerData), "Recruitment catalog still includes healing support ship");
    string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = true, checks }, Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText("QA/HealingMortar/Verification.json", json);
    return json;
}
finally
{
    UnityEngine.Random.state = randomState;
    for (int i = objects.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(objects[i]);
}
