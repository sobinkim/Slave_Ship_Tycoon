// Run with Unity Pipeline eval_file while SampleScene is in Play Mode.
var folder = System.IO.Path.GetFullPath("Artifacts/MovementEffects");
System.IO.Directory.CreateDirectory(folder);
var reportPath = System.IO.Path.Combine(folder, "Verification.txt");
System.IO.File.WriteAllText(reportPath, "RUNNING\n");

async System.Threading.Tasks.Task Verify()
{
    var report = new System.Text.StringBuilder();
    var flow = UnityEngine.Object.FindFirstObjectByType<SB.Scripts.GameFlowManager>();
    var director = UnityEngine.Object.FindFirstObjectByType<SB.Scripts.SeaCreatureSurvivalDirector>();
    var spawner = UnityEngine.Object.FindFirstObjectByType<SB.Scripts.StageSpawnManager>();
    var ships = new System.Collections.Generic.List<SB.Scripts.Ship>();
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

    void Require(bool value, string message)
    {
        if (!value) throw new System.InvalidOperationException(message);
    }

    void StartRoute(int chapter)
    {
        typeof(SB.Scripts.GameFlowManager).GetField("currentChapter", flags).SetValue(flow, chapter);
        typeof(SB.Scripts.GameFlowManager).GetField("currentStage", flags).SetValue(flow, 1);
        flow.StartStage();
        spawner.CollectPlayerShips(ships);
    }

    void CheckWake(string label, bool moving)
    {
        Require(ships.Count > 0, label + ": no fleet");
        foreach (var ship in ships)
        {
            Require(!ship.IsDead, label + ": unexpected dead ship");
            var wake = ship.GetComponentsInChildren<SB.Scripts.ShipWakeEffect>(true).Single();
            var so = new UnityEditor.SerializedObject(wake);
            var animator = (UnityEngine.Animator)so.FindProperty("shipAnimator").objectReferenceValue;
            var renderer = (UnityEngine.SpriteRenderer)so.FindProperty("wakeRenderer").objectReferenceValue;
            Require(animator.GetBool("MOVE") == moving, label + ": MOVE mismatch " + ship.name);
            Require(renderer.enabled == moving, label + ": wake visibility mismatch " + ship.name);
            Require(!moving || renderer.color.a > 0.1f, label + ": transparent wake " + ship.name);
            var foam = so.FindProperty("trailingFoam");
            for (int i = 0; i < foam.arraySize; i++)
                Require(((UnityEngine.SpriteRenderer)foam.GetArrayElementAtIndex(i).objectReferenceValue).enabled == moving,
                    label + ": trailing foam visibility mismatch " + ship.name);
        }
        report.AppendLine(label + ": PASS, ships=" + ships.Count + ", MOVE/wake/foam=" + moving);
    }

    async System.Threading.Tasks.Task WaitFor(System.Func<bool> condition, int seconds = 12)
    {
        var deadline = System.DateTime.UtcNow.AddSeconds(seconds);
        while (!condition())
        {
            Require(UnityEngine.Application.isPlaying && System.DateTime.UtcNow < deadline, "Phase transition timed out");
            await System.Threading.Tasks.Task.Delay(25);
        }
    }

    try
    {
        Require(UnityEngine.Application.isPlaying, "Play Mode required");
        UnityEditor.EditorApplication.isPaused = false;
        StartRoute(1);
        var instanceIds = ships.Select(s => s.GetInstanceID()).ToArray();
        await System.Threading.Tasks.Task.Delay(450);
        Require(director.IsSailing, "Pirate route did not start sailing");
        CheckWake("Pirate sailing", true);
        UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "PirateSailing.png"));

        await WaitFor(() => director.Phase == SB.Scripts.SeaVoyagePhase.Combat);
        float combatDistance = director.DistanceTravelled;
        await System.Threading.Tasks.Task.Delay(550);
        CheckWake("Pirate combat fade out", false);
        Require(UnityEngine.Mathf.Approximately(combatDistance, director.DistanceTravelled), "Combat fleet kept travelling");
        UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "PirateCombat.png"));

        // Clear the three waves through normal health/death events to reach the next voyage promptly.
        int lastEncounter = director.EncounterCount;
        var deadline = System.DateTime.UtcNow.AddSeconds(12);
        while (director.EncounterCount == lastEncounter)
        {
            Require(director.IsRunning && System.DateTime.UtcNow < deadline, "Wave clear timed out");
            foreach (var creature in director.ActiveCreatures.ToArray())
            {
                var health = creature.GetCompo<SB.Core.EntityHealth>();
                health.ApplyDamage(health.MaxHealth, creature.transform.position);
            }
            await System.Threading.Tasks.Task.Delay(50);
        }
        await System.Threading.Tasks.Task.Delay(450);
        spawner.CollectPlayerShips(ships);
        Require(director.IsSailing, "Next voyage did not resume");
        Require(instanceIds.SequenceEqual(ships.Select(s => s.GetInstanceID())), "Unchanged fleet was replaced");
        CheckWake("Pirate next encounter, same fleet", true);

        StartRoute(2);
        await System.Threading.Tasks.Task.Delay(450);
        Require(!director.IsRunning, "Pirate director still running on transport route");
        CheckWake("Transport approach", true);
        UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "TransportSailing.png"));
        var encounter = UnityEngine.Object.FindFirstObjectByType<SB.Scripts.StageEncounterDirector>();
        await WaitFor(() => !(bool)typeof(SB.Scripts.StageEncounterDirector).GetField("_isPlaying", flags).GetValue(encounter));
        await System.Threading.Tasks.Task.Delay(550);
        CheckWake("Transport combat fade out", false);

        StartRoute(1);
        await System.Threading.Tasks.Task.Delay(450);
        CheckWake("Transport to pirate", true);
        director.StopEncounter();
        await System.Threading.Tasks.Task.Delay(550);
        CheckWake("Voyage stop fade out", false);
        report.AppendLine("RESULT: PASS");
    }
    catch (System.Exception exception)
    {
        report.AppendLine("RESULT: FAIL\n" + exception);
    }
    finally
    {
        System.IO.File.WriteAllText(reportPath, report.ToString());
        if (UnityEngine.Application.isPlaying)
        {
            StartRoute(1);
            UnityEditor.EditorApplication.isPaused = false;
        }
    }
}

_ = Verify();
return "Wake verification started. Report: " + reportPath;
