using SB.Scripts;
using UnityEditor;
using UnityEngine;

namespace SB.Editor
{
    public static class StageEnemyTestContentCreator
    {
        [MenuItem("SB/Stage Enemies/Create Test Content")]
        public static void CreateTestContent()
        {
            ChapterDatabase database = StageEnemyAssetUtility.FindOrCreateChapterDatabase();
            Enemy enemyA = StageEnemyAssetUtility.CreateTestEnemyPrefab("TestEnemy_A");
            Enemy enemyB = StageEnemyAssetUtility.CreateTestEnemyPrefab("TestEnemy_B");
            Enemy enemyC = StageEnemyAssetUtility.CreateTestEnemyPrefab("TestEnemy_C");

            if (!database.TryGetStage(1, 1, out _))
            {
                Enemy[] slots = new Enemy[StageEnemyLayout.SlotCount];
                slots[1] = enemyA;
                slots[4] = enemyB;
                slots[7] = enemyC;
                StageEnemyAssetUtility.SaveLayout(database, 1, 1, slots);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);

            Debug.Log(
                "Created three empty Enemy prefabs and a Chapter 1, Stage 1 test layout.",
                database);
        }
    }
}
