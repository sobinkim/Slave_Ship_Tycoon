using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using SB.Scripts;
using SB.Scripts.Bounty;

public static class VerifyEncounterDirection
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    public static async Task<string> Main()
    {
        Check(Application.isPlaying, "Play Mode required");
        Application.runInBackground = true;
        Time.timeScale = 1;
        await Task.Delay(500);
        var director = UnityEngine.Object.FindFirstObjectByType<StageEncounterDirector>();
        var root = (Transform)typeof(StageEncounterDirector).GetField("enemyFleetRoot", Private).GetValue(director);
        var destination = (Transform)typeof(StageEncounterDirector).GetField("enemyFleetRootDestination", Private).GetValue(director);
        var pursuit = UnityEngine.Object.FindFirstObjectByType<BountyPursuitController>();
        Check(pursuit.Enter(0), "Pursuit entry failed");
        float pursuitStart = root.position.x;
        await Task.Delay(200);
        Check(root.position.x < pursuitStart, "Pursuit entrance did not move left");
        pursuit.ReturnToFarming();
        await Task.Delay(350);
        Check(Mathf.Approximately(root.position.x, 3.884f), "Pursuit return changed fleet start");
        var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowManager>();
        flow.StartBossBattle();
        bool throughFlow = (bool)typeof(GameFlowManager).GetField("currentBattleIsBoss", Private).GetValue(flow);
        if (!throughFlow)
            UnityEngine.Object.FindFirstObjectByType<StageSpawnManager>().SpawnStageBattle(1, 0, true);
        Check((bool)typeof(StageEncounterDirector).GetField("_isPlaying", Private).GetValue(director), "Boss encounter did not start");
        float start = root.position.x;
        Check(start > destination.position.x, "Boss starts to left of destination");
        double end = EditorApplication.timeSinceStartup + 10;
        float previous = start;
        int samples = 0;
        while ((bool)typeof(StageEncounterDirector).GetField("_isPlaying", Private).GetValue(director) && EditorApplication.timeSinceStartup < end)
        {
            await Task.Delay(50);
            Check(root.position.x <= previous + .0001f, "Boss moved right during entrance");
            previous = root.position.x;
            samples++;
        }
        Check(Mathf.Approximately(root.position.x, destination.position.x), "Boss failed to reach destination");
        Check(Mathf.Approximately(root.position.y, .91f), "Original fleet height changed");
        Application.runInBackground = false;
        return $"PASS: pursuit entrance moves left; return restores X=3.884; boss entrance X={start:F3} -> {root.position.x:F3}, {samples} monotonic-left samples, Y=.91 preserved. Boss GameFlow path={throughFlow}.";
    }
}
