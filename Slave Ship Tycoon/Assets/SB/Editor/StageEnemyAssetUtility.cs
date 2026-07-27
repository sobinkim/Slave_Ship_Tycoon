using System.IO;
using SB.Scripts;
using UnityEditor;
using UnityEngine;

namespace SB.Editor
{
    internal static class StageEnemyAssetUtility
    {
        internal const string RootFolder = "Assets/SB/Data/StageEnemies";
        internal const string LayoutFolder = RootFolder + "/Layouts";
        internal const string DefaultDatabasePath = RootFolder + "/StageEnemyLayoutDatabase.asset";
        internal const string TestPrefabFolder = "Assets/SB/Prefabs/Enemies/Test";

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
            StageEnemyLayoutDatabase database,
            int chapter,
            int stage,
            Enemy[] enemySlots)
        {
            StageEnemyLayout layout;

            if (!database.TryGetLayout(chapter, stage, out layout))
            {
                string chapterFolder = $"{LayoutFolder}/Chapter_{chapter:00}";
                EnsureFolder(chapterFolder);

                string path = $"{chapterFolder}/Stage_{chapter:00}_{stage:00}.asset";
                layout = AssetDatabase.LoadAssetAtPath<StageEnemyLayout>(path);

                if (layout == null)
                {
                    layout = ScriptableObject.CreateInstance<StageEnemyLayout>();
                    AssetDatabase.CreateAsset(layout, path);
                }
            }

            Undo.RecordObject(layout, "Save Stage Enemy Layout");
            SerializedObject layoutObject = new SerializedObject(layout);
            layoutObject.FindProperty("chapter").intValue = chapter;
            layoutObject.FindProperty("stage").intValue = stage;

            SerializedProperty slotsProperty = layoutObject.FindProperty("enemySlots");
            slotsProperty.arraySize = StageEnemyLayout.SlotCount;

            for (int i = 0; i < StageEnemyLayout.SlotCount; i++)
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = enemySlots[i];

            layoutObject.ApplyModifiedProperties();
            RegisterLayout(database, layout);
            EditorUtility.SetDirty(layout);
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
