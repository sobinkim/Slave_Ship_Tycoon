using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SB.Core;
using SB.Scripts;
using SB.Scripts.Bounty;
using SB.Scripts.Currency;
using SB.Scripts.Visual;
using SB.UI.Bounty;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class VerifyBountyPursuit
{
    static readonly List<string> results = new List<string>();
    static BountyPursuitController pursuit;
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        Application.runInBackground = true;
        pursuit = UnityEngine.Object.FindFirstObjectByType<BountyPursuitController>();
        Time.timeScale = 1;
        await Task.Delay(500);
        var hud = UnityEngine.Object.FindFirstObjectByType<SB.UI.InGame.InGameToolkitScreen>().GetComponent<UIDocument>();
        var ui = UnityEngine.Object.FindFirstObjectByType<BountyPursuitToolkitScreen>().GetComponent<UIDocument>();
        var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowManager>();
        var env = UnityEngine.Object.FindFirstObjectByType<BountyNightEnvironment>();
        var bank = UnityEngine.Object.FindFirstObjectByType<CurrencyManager>();
        var panels = UnityEngine.Object.FindFirstObjectByType<PannelManager>();
        if (pursuit.IsActive) { pursuit.ReturnToFarming(); await Task.Delay(200); }
        Submit(hud, "bountyButton");
        results.Add("Entry state: " + pursuit.State + ", time=" + Time.time);
        await WaitFor(PursuitState.Chasing);
        Check(pursuit.TargetHealthRatio > 0 && env.IsActive && flow.IsPursuitActive, "Existing bounty button starts target battle and night environment");
        Time.timeScale = 0;
        await Task.Delay(100);
        float time = pursuit.ElapsedSeconds;
        await Task.Delay(250);
        Check(Mathf.Approximately(time, pursuit.ElapsedSeconds), "Escape clock pauses with gameplay");
        Time.timeScale = 1;
        var target = Target();
        Check(target.IsDirectTargetProtected, "Defender protects the adjacent main pirate");
        long diamonds = bank.GetCurrency(CurrencyType.Diamond);
        target.GetCompo<EntityHealth>().ApplyDamage(1000000, target.transform.position);
        Check(pursuit.State == PursuitState.Result && pursuit.Result == PursuitResult.Success, "Destroying only the main pirate succeeds");
        Check(UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Any(e => !e.IsDead && e.name.StartsWith("Pursuit")), "Success leaves surviving escorts");
        long paid = bank.GetCurrency(CurrencyType.Diamond) - diamonds;
        Check(paid == pursuit.Definition.DiamondReward, "Bounty reward paid once");
        bank.TrySpendCurrency(CurrencyType.Diamond, paid);
        await Task.Delay(100);
        Submit(ui, "pursuitRetry");
        await WaitFor(PursuitState.Chasing);
        typeof(BountyPursuitController).GetProperty("ElapsedSeconds").SetValue(pursuit, pursuit.Definition.EscapeSeconds - .01f);
        await WaitFor(PursuitState.Result);
        Check(pursuit.Result == PursuitResult.Escaped, "Destination arrival fails while target survives");
        Check(bank.GetCurrency(CurrencyType.Diamond) == diamonds, "Failure grants no bounty reward");
        Submit(ui, "pursuitNext");
        await WaitFor(PursuitState.Chasing);
        var enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e => !e.IsDead && (e.name.StartsWith("Pursuit") || e.name.StartsWith("MainPirate"))).ToArray();
        Check(enemies.Length == 5 && enemies.Any(e => e.name.StartsWith("PursuitSmoke")), "Smoke convoy contains five enemy ships");
        Check(enemies.Any(e => e.GetComponentInChildren<SB.Scripts.AttackCompo.InterceptorAttackCompo>() != null && e.myShipData.ShipType == ShipType.Enemy), "Existing interception logic runs on enemy team");
        Submit(ui, "pursuitRetreat");
        await WaitFor(PursuitState.Result);
        Submit(ui, "pursuitFormation");
        Check(panels.CurrentPanel == PanelType.Fleet, "Result opens existing formation screen");
        panels.CloseFleet();
        await Task.Delay(100);
        Time.timeScale = 1;
        Submit(ui, "pursuitQuickRetry");
        await WaitFor(PursuitState.Chasing);
        var ships = new List<Ship>();
        UnityEngine.Object.FindFirstObjectByType<StageSpawnManager>().CollectPlayerShips(ships);
        foreach (Ship escort in ships.Where(s => !(s is MainShip)))
            escort.GetCompo<EntityHealth>().ApplyDamage(1000000, escort.transform.position);
        await WaitFor(PursuitState.Result);
        Check(pursuit.Result == PursuitResult.MainShipLost, "Player main ship loss fails the pursuit");
        Submit(ui, "pursuitReturn");
        await Task.Delay(250);
        Check(!pursuit.IsActive && !flow.IsPursuitActive && !env.IsActive, "Return restores farming flow and daytime environment");
        Check(hud.rootVisualElement.Q("currencyBar").childCount == 3, "Original currency HUD remains populated");
        Application.runInBackground = false;
        return string.Join("\n", results);
    }

    static Enemy Target() => (Enemy)typeof(BountyPursuitController).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pursuit);
    static void Submit(UIDocument document, string name)
    {
        var button = document.rootVisualElement.Q<Button>(name);
        if (button == null || !button.enabledInHierarchy) throw new InvalidOperationException("Unavailable button: " + name);
        button.Focus();
        using (var evt = NavigationSubmitEvent.GetPooled()) button.SendEvent(evt);
    }
    static async Task WaitFor(PursuitState state)
    {
        double end = EditorApplication.timeSinceStartup + 12;
        while (pursuit != null && pursuit.State != state && EditorApplication.timeSinceStartup < end) await Task.Delay(50);
        Check(pursuit != null && pursuit.State == state, "Reached " + state);
    }
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("FAILED: " + message + "\n" + string.Join("\n", results));
        results.Add("PASS: " + message);
    }
}
