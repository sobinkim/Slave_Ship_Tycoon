using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SB.Scripts;
using SB.Scripts.Bounty;
using UnityEditor;
using UnityEngine;

public static class VerifyBountyShipLighting
{
    static readonly int TintId = Shader.PropertyToID("_EnvironmentTint");
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static BountyPursuitController Pursuit => UnityEngine.Object.FindFirstObjectByType<BountyPursuitController>();
    static SpriteRenderer[] Hulls(Ship ship) => ship.GetComponentsInChildren<SpriteRenderer>(true)
        .Where(r => r.sharedMaterial != null && r.sharedMaterial.HasProperty(TintId)).ToArray();
    static Color Tint(SpriteRenderer renderer)
    {
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        return block.HasColor(TintId) ? block.GetColor(TintId) : renderer.sharedMaterial.GetColor(TintId);
    }
    static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
    static async Task WaitForChase()
    {
        double until = EditorApplication.timeSinceStartup + 12;
        while (Pursuit.State != PursuitState.Chasing && EditorApplication.timeSinceStartup < until) await Task.Delay(50);
        Check(Pursuit.State == PursuitState.Chasing, "Pursuit did not start");
    }
    public static async Task<string> Preview()
    {
        Check(Application.isPlaying, "Play Mode required");
        Application.runInBackground = true;
        Time.timeScale = 1;
        if (Pursuit.IsActive) Pursuit.ReturnToFarming();
        await Task.Delay(500);
        Check(Pursuit.Enter(0), "Entry failed");
        await WaitForChase();
        var env = UnityEngine.Object.FindFirstObjectByType<BountyNightEnvironment>();
        var expected = (Color)typeof(BountyNightEnvironment).GetField("shipNightTint", Private).GetValue(env);
        var players = new List<Ship>();
        UnityEngine.Object.FindFirstObjectByType<StageSpawnManager>().CollectPlayerShips(players);
        var enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        var all = players.Concat(enemies.Cast<Ship>()).ToArray();
        Check(players.Count > 0 && enemies.Length == 4, "Missing player or enemy roster");
        foreach (var ship in all)
            Check(Hulls(ship).Length > 0 && Hulls(ship).All(r => Tint(r) == expected), "Night tint absent: " + ship.name);
        await Task.Delay(600);
        Check(all.SelectMany(Hulls).All(r => Tint(r) == expected), "Animation overwrote night lighting");
        Check(all.SelectMany(Hulls).All(r => r.sharedMaterial.GetColor(TintId) == Color.white), "Shared material changed");
        Pursuit.Retreat();
        Check(all.SelectMany(Hulls).All(r => Tint(r) == expected), "Result lost night lighting");
        Pursuit.Retry();
        await WaitForChase();
        var retry = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).SelectMany(Hulls).ToArray();
        Check(retry.Length > 0 && retry.All(r => Tint(r) == expected), "Retry compounded or lost tint");
        Time.timeScale = 0;
        return "PASS: player main/escorts and all 4 pirates tinted; animation retained tint; shared materials unchanged; result retained tint; retry did not compound tint. Paused for screenshot.";
    }
    public static async Task<string> Finish()
    {
        var env = UnityEngine.Object.FindFirstObjectByType<BountyNightEnvironment>();
        var ships = UnityEngine.Object.FindObjectsByType<Ship>(FindObjectsSortMode.None);
        var hulls = ships.SelectMany(Hulls).ToArray();
        var sample = hulls[0];
        var block = new MaterialPropertyBlock();
        sample.GetPropertyBlock(block);
        int unrelated = Shader.PropertyToID("_LightingQaSentinel");
        block.SetFloat(unrelated, .375f);
        sample.SetPropertyBlock(block);
        env.RestoreShipLighting();
        Check(hulls.All(r => r == null || Tint(r) == Color.white), "Night tint did not restore");
        sample.GetPropertyBlock(block);
        Check(Mathf.Approximately(block.GetFloat(unrelated), .375f), "Restore removed unrelated property");
        Pursuit.ReturnToFarming();
        Check(hulls.All(r => r == null || Tint(r) == Color.white), "Night tint leaked after farming return");
        Time.timeScale = 1;
        await Task.Delay(300);
        Check(!env.IsActive && !Pursuit.IsActive, "Environment still active");
        Check(Pursuit.Enter(1), "Smoke convoy entry failed");
        await WaitForChase();
        var enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Check(enemies.Length == 5 && enemies.SelectMany(Hulls).All(r => Tint(r) != Color.white), "Smoke convoy not tinted");
        var lastHulls = UnityEngine.Object.FindObjectsByType<Ship>(FindObjectsSortMode.None).SelectMany(Hulls).ToArray();
        env.enabled = false;
        Check(lastHulls.All(r => r == null || Tint(r) == Color.white), "Disable did not restore lighting");
        env.enabled = true;
        Pursuit.ReturnToFarming();
        Application.runInBackground = false;
        return "PASS: farming restored every hull; unrelated property survived restore; smoke convoy all 5 tinted; disabling environment restored lighting.";
    }
}
