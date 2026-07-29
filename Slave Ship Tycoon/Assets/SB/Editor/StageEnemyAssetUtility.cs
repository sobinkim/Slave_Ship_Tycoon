using SB.Scripts;
using UnityEditor;
using UnityEngine;

namespace SB.Editor
{
    internal static class StageEnemyAssetUtility
    {
        internal const string RootFolder = "Assets/SB/Data/StageEnemies";
        internal const string LayoutFolder = RootFolder + "/Layouts";
        internal const string ChapterFolder = RootFolder + "/Chapters";
        internal const string DefaultDatabasePath = RootFolder + "/StageEnemyLayoutDatabase.asset";
        internal const string DefaultChapterDatabasePath = RootFolder + "/ChapterDatabase.asset";
        internal const string TestPrefabFolder = "Assets/SB/Prefabs/EnemyShips/Test";

        internal static ChapterDatabase FindOrCreateChapterDatabase()
        {
            ChapterDatabase database =
                AssetDatabase.LoadAssetAtPath<ChapterDatabase>(DefaultChapterDatabasePath);

            if (database != null)
                return database;

            string[] databaseGuids = AssetDatabase.FindAssets("t:ChapterDatabase");

            if (databaseGuids.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(databaseGuids[0]);
                return AssetDatabase.LoadAssetAtPath<ChapterDatabase>(existingPath);
            }

            EnsureFolder(RootFolder);
            database = ScriptableObject.CreateInstance<ChapterDatabase>();
            AssetDatabase.CreateAsset(database, DefaultChapterDatabasePath);
            AssetDatabase.SaveAssets();
            return database;
        }

        internal static StageEnemyLayoutDatabase FindOrCreateDatabase()
        {
            StageEnemyLayoutDatabase database =
                AssetDatabase.LoadAssetAtPath<StageEnemyLayoutDatabase>(DefaultDatabasePath);

            if (database != null)
                return database;

            string[] databaseGuids = AssetDatabase.FindAssets("t:StageEnemyLayoutDatabase");

            if (databaseGuids.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(databaseGuids[0]);
                return AssetDatabase.LoadAssetAtPath<StageEnemyLayoutDatabase>(existingPath);
            }

            EnsureFolder(RootFolder);
            database = ScriptableObject.CreateInstance<StageEnemyLayoutDatabase>();
            AssetDatabase.CreateAsset(database, DefaultDatabasePath);
            AssetDatabase.SaveAssets();
            return database;
        }

        internal static StageEnemyLayout SaveLayout(
            ChapterDatabase database,
            int chapter,
            int stage,
            Enemy[] enemySlots)
        {
            if (database == null)
                return null;

            StageEnemyLayout layout = SaveLayoutAsset(chapter, stage, enemySlots);
            ChapterContainer container = FindOrCreateChapterContainer(chapter);

            RegisterChapter(database, container);
            RegisterStage(container, stage, layout);
            AssetDatabase.SaveAssets();
            return layout;
        }

        internal static StageEnemyLayout SaveLayout(
            StageEnemyLayoutDatabase database,
            int chapter,
            int stage,
            Enemy[] enemySlots)
        {
            if (database == null)
                return null;

            StageEnemyLayout layout = SaveLayoutAsset(chapter, stage, enemySlots);
            RegisterLayout(database, layout);
            AssetDatabase.SaveAssets();
            return layout;
        }

        internal static Enemy[] CopySlots(StageEnemyLayout layout)
        {
            Enemy[] slots = new Enemy[StageEnemyLayout.SlotCount];

            if (layout == null)
                return slots;

            for (int i = 0; i < slots.Length; i++)
                slots[i] = layout.GetEnemyAt(i);

            return slots;
        }

        internal static Enemy CreateTestEnemyPrefab(string prefabName)
        {
            EnsureFolder(TestPrefabFolder);
            string path = $"{TestPrefabFolder}/{prefabName}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (existingPrefab != null)
                return existingPrefab.GetComponent<Enemy>();

            GameObject enemyObject = new GameObject(prefabName);
            Enemy enemy = enemyObject.AddComponent<Enemy>();
            enemy.EntityName = prefabName;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemyObject, path);
            Object.DestroyImmediate(enemyObject);
            return prefab.GetComponent<Enemy>();
        }

        internal static void EnsureFolder(string folderPath)
        {
            string normalizedPath = folderPath.Replace('\\', '/');
            string[] parts = normalizedPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);

                currentPath = nextPath;
            }
        }

        private static StageEnemyLayout SaveLayoutAsset(
            int chapter,
            int stage,
            Enemy[] enemySlots)
        {
            string chapterFolder = $"{LayoutFolder}/Chapter_{chapter:00}";
            EnsureFolder(chapterFolder);

            string path = $"{chapterFolder}/Stage_{chapter:00}_{stage:00}.asset";
            StageEnemyLayout layout = AssetDatabase.LoadAssetAtPath<StageEnemyLayout>(path);

            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<StageEnemyLayout>();
                AssetDatabase.CreateAsset(layout, path);
            }

            Undo.RecordObject(layout, "Save Stage Enemy Layout");
            SerializedObject layoutObject = new SerializedObject(layout);
            layoutObject.FindProperty("chapter").intValue = chapter;
            layoutObject.FindProperty("stage").intValue = stage;

            SerializedProperty slotsProperty = layoutObject.FindProperty("enemySlots");
            slotsProperty.arraySize = StageEnemyLayout.SlotCount;

            for (int i = 0; i < StageEnemyLayout.SlotCount; i++)
            {
                Enemy enemy = enemySlots != null && i < enemySlots.Length
                    ? enemySlots[i]
                    : null;

                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = enemy;
            }

            layoutObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(layout);
            return layout;
        }

        private static ChapterContainer FindOrCreateChapterContainer(int chapter)
        {
            EnsureFolder(ChapterFolder);

            string path = $"{ChapterFolder}/Chapter_{chapter:00}.asset";
            ChapterContainer container = AssetDatabase.LoadAssetAtPath<ChapterContainer>(path);
            bool isNewContainer = container == null;

            if (isNewContainer)
            {
                container = ScriptableObject.CreateInstance<ChapterContainer>();
                AssetDatabase.CreateAsset(container, path);
            }

            Undo.RecordObject(container, "Save Chapter Container");
            SerializedObject containerObject = new SerializedObject(container);
            containerObject.FindProperty("chapter").intValue = chapter;

            if (isNewContainer)
            {
                bool isSellRoute = chapter % 2 == 0;
                containerObject.FindProperty("routeType").enumValueIndex = isSellRoute ? 1 : 0;
                containerObject.FindProperty("enemyFaction").enumValueIndex = isSellRoute ? 1 : 0;
            }

            containerObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(container);
            return container;
        }

        private static void RegisterChapter(
            ChapterDatabase database,
            ChapterContainer container)
        {
            SerializedObject databaseObject = new SerializedObject(database);
            SerializedProperty chaptersProperty = databaseObject.FindProperty("chapters");

            for (int i = 0; i < chaptersProperty.arraySize; i++)
            {
                SerializedProperty chapterProperty = chaptersProperty.GetArrayElementAtIndex(i);
                ChapterContainer candidate = chapterProperty.objectReferenceValue as ChapterContainer;

                if (candidate == null)
                    continue;

                if (candidate.Chapter == container.Chapter)
                {
                    chapterProperty.objectReferenceValue = container;
                    databaseObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(database);
                    return;
                }
            }

            Undo.RecordObject(database, "Register Chapter Container");
            int newIndex = chaptersProperty.arraySize;
            chaptersProperty.InsertArrayElementAtIndex(newIndex);
            chaptersProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = container;
            databaseObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
        }

        private static void RegisterStage(
            ChapterContainer container,
            int stage,
            StageEnemyLayout layout)
        {
            SerializedObject containerObject = new SerializedObject(container);
            SerializedProperty stagesProperty = containerObject.FindProperty("stages");

            if (stagesProperty.arraySize < stage)
                stagesProperty.arraySize = stage;

            Undo.RecordObject(container, "Register Stage Enemy Layout");
            stagesProperty.GetArrayElementAtIndex(stage - 1).objectReferenceValue = layout;
            containerObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(container);
        }

        private static void RegisterLayout(
            StageEnemyLayoutDatabase database,
            StageEnemyLayout layout)
        {
            SerializedObject databaseObject = new SerializedObject(database);
            SerializedProperty layoutsProperty = databaseObject.FindProperty("layouts");

            for (int i = 0; i < layoutsProperty.arraySize; i++)
            {
                if (layoutsProperty.GetArrayElementAtIndex(i).objectReferenceValue == layout)
                    return;
            }

            Undo.RecordObject(database, "Register Stage Enemy Layout");
            int newIndex = layoutsProperty.arraySize;
            layoutsProperty.InsertArrayElementAtIndex(newIndex);
            layoutsProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = layout;
            databaseObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
        }
    }
}
