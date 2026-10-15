using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SB.Core;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using UnityEngine;

public static class InspectPirateFiring
{
    public static async Task<string> ObserveRoutes()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play required for runtime diagnosis.");
        var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowManager>();
        var director = UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
        var result = new StringBuilder();
        foreach (int chapter in new[] { 1, 2 })
        {
            var shots = new HashSet<string>(UnityEngine.Object.FindObjectsByType<SB.Scripts.Projectiles.BaseProjectile>(FindObjectsSortMode.None)
                .Select(projectile => projectile.GetInstanceID() + ":" + projectile.FlightGeneration));
            typeof(GameFlowManager).GetField("currentChapter", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flow, chapter);
            typeof(GameFlowManager).GetField("currentStage", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flow, 1);
            flow.StartStage();
            int creatureShots = 0, shipShots = 0;
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (DateTime.UtcNow < deadline)
            {
                foreach (var projectile in UnityEngine.Object.FindObjectsByType<SB.Scripts.Projectiles.BaseProjectile>(FindObjectsSortMode.None))
                {
                    Ship target = projectile.IntendedTarget;
                    if (target == null || target is not Enemy) continue;
                    string key = projectile.GetInstanceID() + ":" + projectile.FlightGeneration;
                    if (!shots.Add(key)) continue;
                    if (target is SeaCreatureEnemy) creatureShots++; else shipShots++;
                }
                await Task.Delay(20);
            }
            result.AppendLine("Chapter=" + chapter + " creatureVoyage=" + director.IsRunning + " creatureTargetShots=" + creatureShots + " shipTargetShots=" + shipShots + " kills=" + director.KillCount);
        }
        string report = result.ToString();
        File.WriteAllText("Artifacts/SeaCreatures/PirateTransportFiringObservation.txt", report);
        return report;
    }

    public static string MoveCapture()
    {
        string source = "Assets/Artifacts/SeaCreatures/PirateFiringCurrent.png";
        if (File.Exists(source))
        {
            File.Copy(source, "Artifacts/SeaCreatures/PirateFiringCurrent.png", true);
            UnityEditor.AssetDatabase.DeleteAsset(source);
        }
        string captureFolder = "Assets/Artifacts/SeaCreatures";
        if (Directory.Exists(captureFolder) && !Directory.EnumerateFileSystemEntries(captureFolder).Any())
            UnityEditor.AssetDatabase.DeleteAsset(captureFolder);
        return "Capture moved to Artifacts/SeaCreatures/PirateFiringCurrent.png";
    }
    public static string BlockedAlternatives()
    {
        var director = UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
        var ships = new List<Ship>();
        UnityEngine.Object.FindFirstObjectByType<StageSpawnManager>().CollectPlayerShips(ships);
        var b = new StringBuilder();
        foreach (var ship in ships)
        {
            var attack = ship.GetCompo<Base_ShipAttackCompo>();
            var selector = ship.GetCompo<TargetSelector>();
            if (attack == null || selector == null || !attack.IsFireLaneBlocked) continue;
            var alternatives = director.ActiveCreatures.Where(c => selector.IsValidTarget(c) && !attack.TryGetBlockingShip(c, out _)).ToArray();
            b.AppendLine(ship.name + " target=" + attack.CurrentTarget.name + " blocker=" + attack.CurrentBlockingShip.name + " clear alternatives=" + alternatives.Length);
        }
        string result = b.ToString();
        File.WriteAllText("Artifacts/SeaCreatures/PirateBlockedAlternatives.txt", result);
        return result;
    }
    public static string Catalog()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<SB.Scripts.Fleet.PlayerFleetManager>();
        var b = new StringBuilder();
        b.AppendLine("Fleet ready=" + manager.IsReady + " enabled=" + manager.enabled);
        foreach (var data in manager.GetFormationSnapshot()) b.AppendLine("Formation=" + (data == null ? "empty" : data.name));
        for (int i = 0; i < manager.Catalog.Count; i++)
        {
            var data = manager.Catalog.GetAt(i);
            b.AppendLine(data.name + " " + UnityEditor.EditorJsonUtility.ToJson(data));
        }
        return b.ToString();
    }
    private static object Field(object obj, string name)
    {
        if (obj == null) return null;
        for (var type = obj.GetType(); type != null; type = type.BaseType)
        {
            var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f.GetValue(obj);
        }
        return null;
    }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play required for runtime diagnosis.");
        var director = UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
        var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowManager>();
        var spawner = UnityEngine.Object.FindFirstObjectByType<StageSpawnManager>();
        if (!director.IsRunning) flow.StartStage();
        var report = new StringBuilder();
        var ships = new List<Ship>();
        for (int sample = 0; sample < 14; sample++)
        {
            report.AppendLine("SAMPLE " + sample + " frame=" + Time.frameCount + " scale=" + Time.timeScale + " phase=" + director.Phase + " alive=" + director.ActiveCount + " kills=" + director.KillCount);
            spawner.CollectPlayerShips(ships);
            foreach (var ship in ships)
            {
                var attack = ship.GetCompo<Base_ShipAttackCompo>();
                var selector = ship.GetCompo<TargetSelector>();
                var anim = ship.GetCompo<EntityAnimator>();
                var stateMachine = Field(ship, "_stateMachine");
                report.AppendLine(ship.name + " pos=" + ship.transform.position + " dead=" + ship.IsDead + " enabled=" + ship.isActiveAndEnabled + " type=" + ship.myShipData.ShipType +
                    " state=" + Field(stateMachine, "currentState") + "/" + Field(stateMachine, "_currentState") + " weapon=" + (attack == null ? "none" : attack.GetType().Name) +
                    " running=" + Field(attack, "_isBattleRunning") + " canAttack=" + (attack != null && attack.CanAttack) + " target=" + (attack != null && attack.CurrentTarget != null ? attack.CurrentTarget.name : "none") +
                    " blocked=" + (attack != null && attack.CurrentBlockingShip != null ? attack.CurrentBlockingShip.name : "none") + " selectorTarget=" + (selector != null ? selector.GetTarget()?.name : "none") +
                    " nearest=" + selector?.SelectionMode + " requireEngaged=" + selector?.RequiresEngagedSeaCreatures + " animator=" + (anim != null && anim.RuntimeAnimator != null ? anim.RuntimeAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash.ToString() : "none"));
            }
            foreach (var c in director.ActiveCreatures.Take(5))
                report.AppendLine("CREATURE " + c.name + " pos=" + c.transform.position + " state=" + c.State + " engaged=" + c.IsEngaged + " protected=" + c.IsDirectTargetProtected + " hp=" + c.GetCompo<EntityHealth>().CurrentHealth);
            await Task.Delay(500);
        }
        Directory.CreateDirectory("Artifacts/SeaCreatures");
        File.WriteAllText("Artifacts/SeaCreatures/PirateFiringDiagnosis.txt", report.ToString());
        return report.ToString();
    }
}
