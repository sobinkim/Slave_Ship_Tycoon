using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB.Scripts;

public static class ConfigureEncounterFlow
{
    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode before configuring scene references.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/SB/Animations/Scenes/SampleScene.unity") throw new System.InvalidOperationException("Unexpected active scene.");
        var parallax = Object.FindFirstObjectByType<ParallaxLayerManager>();
        var director = Object.FindFirstObjectByType<StageEncounterDirector>();
        var creatures = Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
        var spawner = Object.FindFirstObjectByType<StageSpawnManager>();
        var flow = Object.FindFirstObjectByType<GameFlowManager>();
        if (parallax == null || parallax.name != "BackgroundRoot") throw new System.InvalidOperationException("Expected BackgroundRoot parallax manager.");
        var data = new SerializedObject(director);
        data.FindProperty("parallaxLayers").objectReferenceValue = parallax;
        data.FindProperty("transportEntrySeconds").floatValue = 1.1f;
        data.FindProperty("bossEntrySeconds").floatValue = 1.7f;
        data.FindProperty("arrivalBlendSeconds").floatValue = 0.4f;
        data.ApplyModifiedProperties();
        data = new SerializedObject(creatures);
        data.FindProperty("parallaxLayers").objectReferenceValue = parallax;
        data.FindProperty("sailingBlendSeconds").floatValue = 0.4f;
        data.FindProperty("groupSpacing").floatValue = 1.4f;
        data.FindProperty("waveDelaySeconds").floatValue = 0.08f;
        data.ApplyModifiedProperties();
        data = new SerializedObject(flow);
        data.FindProperty("stageSpawner").objectReferenceValue = spawner;
        data.ApplyModifiedProperties();
        var boss = AssetDatabase.LoadAssetAtPath<StageEnemyLayout>("Assets/SB/Data/StageEnemies/Layouts/Chapter_01/Chapter1_Boss.asset");
        if (boss.GetEnemyAt(3) == null || boss.GetEnemyAt(3).name != "EnemyCrusherBoss") throw new System.InvalidOperationException("Boss slot changed.");
        data = new SerializedObject(boss);
        data.FindProperty("encounterFocusSlot").intValue = 3;
        data.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(boss);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new { scene = scene.path, parallax = parallax.name, transportSeconds = 1.1f, bossSeconds = 1.7f, blendSeconds = 0.4f, creatureSpacing = 1.4f, creatureDepartureDelay = 0.08f, focus = boss.GetEnemyAt(3).name };
    }
}
