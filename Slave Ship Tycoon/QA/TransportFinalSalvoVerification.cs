using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SB.Core;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.Scripts.Projectiles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TransportFinalSalvoVerification
{
    public static async Task<string> Main()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("This verification runs in Play Mode.");
        Time.timeScale = 1f;
        var checks = new List<string>();
        GameFlowManager flow = Object.FindFirstObjectByType<GameFlowManager>();
        StageSpawnManager spawn = Object.FindFirstObjectByType<StageSpawnManager>();
        MainShipDefeatDirector director = Object.FindFirstObjectByType<MainShipDefeatDirector>();
        flow.StartStage();
        await WaitUntil(() => director.Phase == MainShipDefeatPhase.Combat, 8f);

        var players = new List<Ship>();
        spawn.CollectPlayerShips(players);
        MainShip main = players.OfType<MainShip>().Single();
        var enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(e => !e.IsDead).ToArray();
        Require(enemies.Length > 0, "Need living enemies in the actual stage.");
        Require(!main.transform.Find("WorldUI/HullCondition").gameObject.activeInHierarchy, "Transport HP bar visible.");
        EntityHealth mainHealth = main.GetCompo<EntityHealth>();
        float priorHP = mainHealth.CurrentHealth;
        mainHealth.ApplyDamage(1000000f, main.transform.position);
        Require(mainHealth.CurrentHealth == priorHP && !main.IsDead, "Ordinary damage defeated the transport.");
        Require(!main.CanAttack && !main.EnterAttack(), "Transport can attack.");
        checks.Add("No health bar, no attacks, ordinary damage does not defeat the transport.");

        var luck = new SerializedObject(main.GetCompo<ShipCombatStatCompo>());
        luck.FindProperty("baseLuck").floatValue = 10000f;
        luck.ApplyModifiedPropertiesWithoutUndo();
        var cargo = Object.FindFirstObjectByType<CargoManager>();
        cargo.ClearCargo();
        for (int index = 0; index < 15; index++) cargo.LoadCargo(ETransportItemType.Gold);

        var queued = PoolingManager.Instance.Get(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/SB/Prefabs/Projectiles/NormalProjectile.prefab").GetComponent<NormalProjectile>(),
            new Vector3(10f, 5f, 0f), Quaternion.identity);
        bool wasQueuedShotCancelled = false;
        Action<BaseProjectile, ProjectileResolution> queuedHandler = null;
        queuedHandler = (projectile, resolution) =>
        {
            wasQueuedShotCancelled = resolution == ProjectileResolution.Cancelled;
            projectile.Resolved -= queuedHandler;
        };
        queued.Resolved += queuedHandler;
        queued.Launch(new ProjectileLaunchData(main, queued.transform.position, main.transform.position,
            main.transform.position, new ProjectileFlightData
            { FlightSpeed = 1f, MinimumFlightDuration = 10f, ArcHeight = .4f, LifeTime = 20f }, 1f));

        foreach (Ship escort in players.Where(s => s is not MainShip))
        {
            escort.SetInvincible(false);
            escort.GetCompo<EntityHealth>().ApplyDamage(1000000f, escort.transform.position);
            Require(escort.IsDead, "Escort did not die during the controlled verification.");
        }
        await WaitUntil(() => director.Phase == MainShipDefeatPhase.PreparingSalvo, 2f);
        Require(wasQueuedShotCancelled, "Previous in-flight shot was not cancelled.");
        Require(enemies.Where(e => e.isActiveAndEnabled && !e.IsDead)
            .All(e => !e.GetCompo<Base_ShipAttackCompo>().CanAttack), "An ordinary enemy attack remains active.");
        checks.Add("Escort defeat stops ordinary attacks and cancels previous projectiles.");

        await WaitUntil(() => director.Phase == MainShipDefeatPhase.SalvoInFlight, 2f);
        VerifyShots(director, main, checks);

        var noEscorts = new BattleSpawnData(main, Array.Empty<Ship>(), new StageEnemySpawnData(enemies));
        Bus<StageBattleInitializedEvent>.Raise(new StageBattleInitializedEvent(noEscorts));
        Require(director.PendingShotCount == 0 && director.Phase == MainShipDefeatPhase.WaitingForBattle,
            "Interrupted sequence retained pending callbacks.");
        await Task.Delay(30);
        Require(!main.Destruction.HasExploded && !main.IsDead, "Cancelled salvo caused an explosion.");
        checks.Add("Stage reinitialization cancels pending shots without triggering defeat.");

        Bus<BattleStartEvent>.Raise(new BattleStartEvent(noEscorts));
        await WaitUntil(() => director.Phase == MainShipDefeatPhase.SalvoInFlight, 2f);
        VerifyShots(director, main, checks);
        checks.Add("A battle starting with no escorts triggers one fresh final salvo.");

        int failureEvents = 0;
        Action<StageBattleEndedEvent> endHandler = null;
        endHandler = evt =>
        {
            Bus<StageBattleEndedEvent>.OnEvent -= endHandler;
            if (!evt.IsClear) failureEvents++;
            Time.timeScale = 0f;
            System.IO.File.WriteAllText("QA/TransportFinalSalvoVerification.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    checks,
                    failureEvents,
                    allHitsBeforeExplosion = director.SalvoImpactCount == director.SalvoShotCount,
                    mainShipDead = main.IsDead,
                    pendingCallbacks = director.PendingShotCount,
                    phaseAfterDefeat = director.Phase.ToString()
                }, Newtonsoft.Json.Formatting.Indented));
        };
        Bus<StageBattleEndedEvent>.OnEvent += endHandler;
        await WaitUntil(() => director.Phase == MainShipDefeatPhase.Exploding, 3f);
        Require(director.SalvoImpactCount == director.SalvoShotCount && director.SalvoShotCount > 0,
            "Explosion began before all salvo hits.");
        Require(main.Destruction.HasExploded && !main.IsDead,
            "The explosion did not precede the final defeat event.");
        Require(mainHealth.CurrentHealth == priorHP, "Final salvo incorrectly used HP damage.");
        checks.Add("All guaranteed shots hit before explosion; HP stays unchanged; defeat follows the presentation hold.");
        await WaitUntil(() => failureEvents == 1, 2f);
        Require(main.IsDead && director.PendingShotCount == 0,
            "Final defeat did not clean up pending projectiles.");
        checks.Add("Exactly one normal defeat event fired after the explosion hold, with zero pending callbacks.");
        return Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            checks, shots = director.SalvoShotCount, impacts = director.SalvoImpactCount,
            phase = director.Phase.ToString(), failureEvents, verificationComplete = true
        });
    }

    private static void VerifyShots(MainShipDefeatDirector director, MainShip main, List<string> checks)
    {
        FieldInfo launch = typeof(BaseProjectile).GetField("launchData", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo duration = typeof(BaseProjectile).GetField("flightDuration", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo elapsed = typeof(BaseProjectile).GetField("flightElapsed", BindingFlags.Instance | BindingFlags.NonPublic);
        var shots = Object.FindObjectsByType<BaseProjectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(p => ((ProjectileLaunchData)launch.GetValue(p)).HitMode == ProjectileHitMode.Guaranteed).ToArray();
        Require(shots.Length == director.SalvoShotCount && shots.Length == director.PendingShotCount,
            "Final salvo shot count differs from pending shots.");
        Require(shots.All(p => ((ProjectileLaunchData)launch.GetValue(p)).Target == main && !p.IsLuckMiss),
            "Final salvo has a wrong target or luck miss.");
        float[] times = shots.Select(p => (float)elapsed.GetValue(p)).ToArray();
        Require(times.Max() - times.Min() < .0001f, "Final salvo shots did not launch together.");
        Require(shots.All(p => Mathf.Abs((float)duration.GetValue(p) - .7f) < .0001f),
            "Final salvo arrival times differ.");
        checks.Add($"{shots.Length} shots launched together, target the transport, ignore luck, and share the 0.7 second arrival time.");
    }

    private static async Task WaitUntil(Func<bool> condition, float timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                throw new InvalidOperationException("Verification timed out waiting for the next phase.");
            await Task.Delay(10);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
