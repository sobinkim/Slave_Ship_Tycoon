using System.Collections.Generic;
using SB.Scripts;
using UnityEditor;
using UnityEngine;

namespace SB.Editor
{
    [CustomEditor(typeof(StageSpawnManager))]
    public sealed class StageSpawnManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty escortShipSpawnPointsProperty;
        private SerializedProperty chapterDatabaseProperty;
        private SerializedProperty spawnPointsProperty;

        private void OnEnable()
        {
            escortShipSpawnPointsProperty =
                serializedObject.FindProperty("escortShipSpawnPoints");
            chapterDatabaseProperty = serializedObject.FindProperty("chapterDatabase");
            spawnPointsProperty = serializedObject.FindProperty("spawnPoints");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "escortShipSpawnPoints",
                "chapterDatabase",
                "spawnPoints");

            DrawEscortSpawnPointGrid();
            EditorGUILayout.Space(8f);
            EditorGUILayout.PropertyField(chapterDatabaseProperty);
            EditorGUILayout.Space(6f);
            DrawEnemySpawnPointGrid();
            DrawEnemyValidation();
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Assign Default Database"))
            {
                chapterDatabaseProperty.objectReferenceValue =
                    StageEnemyAssetUtility.FindOrCreateChapterDatabase();
            }

            if (GUILayout.Button("Open Enemy Layout Editor"))
                StageEnemyLayoutEditorWindow.Open();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEscortSpawnPointGrid()
        {
            if (escortShipSpawnPointsProperty.arraySize != PlayerFleetLoadout.SlotCount)
                escortShipSpawnPointsProperty.arraySize = PlayerFleetLoadout.SlotCount;

            EditorGUILayout.LabelField(
                "Escort Spawn Points (Slots 1-6)",
                EditorStyles.boldLabel);

            for (int row = 0; row < PlayerFleetLoadout.RowCount; row++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int column = 0; column < PlayerFleetLoadout.ColumnCount; column++)
                {
                    int slotIndex = row * PlayerFleetLoadout.ColumnCount + column;
                    SerializedProperty pointProperty =
                        escortShipSpawnPointsProperty.GetArrayElementAtIndex(slotIndex);

                    DrawSpawnPointSlot(pointProperty, slotIndex);
                }

                EditorGUILayout.EndHorizontal();
            }

            DrawSpawnPointValidation(
                escortShipSpawnPointsProperty,
                "escort spawn point");
        }

        private void DrawEnemySpawnPointGrid()
        {
            if (spawnPointsProperty.arraySize != StageEnemyLayout.SlotCount)
                spawnPointsProperty.arraySize = StageEnemyLayout.SlotCount;

            EditorGUILayout.LabelField("Spawn Points (Slots 1-9)", EditorStyles.boldLabel);

            for (int row = 0; row < StageEnemyLayout.GridSize; row++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int column = 0; column < StageEnemyLayout.GridSize; column++)
                {
                    int slotIndex = row * StageEnemyLayout.GridSize + column;
                    SerializedProperty pointProperty =
                        spawnPointsProperty.GetArrayElementAtIndex(slotIndex);

                    DrawSpawnPointSlot(pointProperty, slotIndex);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawSpawnPointSlot(
            SerializedProperty pointProperty,
            int slotIndex)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(
                $"Slot {slotIndex + 1}",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(pointProperty, GUIContent.none);
            EditorGUILayout.EndVertical();
        }

        private void DrawEnemyValidation()
        {
            DrawSpawnPointValidation(spawnPointsProperty, "enemy spawn point");
        }

        private static void DrawSpawnPointValidation(
            SerializedProperty pointsProperty,
            string pointName)
        {
            int missingCount = 0;
            HashSet<Object> assignedPoints = new HashSet<Object>();
            bool hasDuplicate = false;

            for (int i = 0; i < pointsProperty.arraySize; i++)
            {
                Object point = pointsProperty
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue;

                if (point == null)
                {
                    missingCount++;
                    continue;
                }

                if (!assignedPoints.Add(point))
                    hasDuplicate = true;
            }

            if (missingCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{missingCount} {pointName}(s) are not assigned.",
                    MessageType.Warning);
            }

            if (hasDuplicate)
            {
                EditorGUILayout.HelpBox(
                    $"The same Transform is assigned to multiple {pointName} slots.",
                    MessageType.Error);
            }
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();

            DrawSceneLabels(escortShipSpawnPointsProperty, "Escort");
            DrawSceneLabels(spawnPointsProperty, "Enemy");
        }

        private static void DrawSceneLabels(
            SerializedProperty pointsProperty,
            string prefix)
        {
            for (int i = 0; i < pointsProperty.arraySize; i++)
            {
                Transform point = pointsProperty
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as Transform;

                if (point == null)
                    continue;

                Handles.Label(point.position, $"{prefix} Slot {i + 1}");
            }
        }
    }
}
