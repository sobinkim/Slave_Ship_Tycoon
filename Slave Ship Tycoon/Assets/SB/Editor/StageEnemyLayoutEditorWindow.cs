using SB.Scripts;
using UnityEditor;
using UnityEngine;

namespace SB.Editor
{
    public sealed class StageEnemyLayoutEditorWindow : EditorWindow
    {
        private StageEnemyLayoutDatabase database;
        private StageEnemyLayout loadedLayout;
        private Enemy[] enemySlots = new Enemy[StageEnemyLayout.SlotCount];
        private int chapter = 1;
        private int stage = 1;
        private bool isDraftDirty;

        [MenuItem("SB/Stage Enemies/Layout Editor")]
        public static void Open()
        {
            StageEnemyLayoutEditorWindow window = GetWindow<StageEnemyLayoutEditorWindow>();
            window.titleContent = new GUIContent("Enemy Layout");
            window.minSize = new Vector2(440f, 430f);
            window.Show();
        }

        private void OnEnable()
        {
            database = StageEnemyAssetUtility.FindOrCreateDatabase();
            LoadStage(false);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Stage Enemy Layout", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            database = (StageEnemyLayoutDatabase)EditorGUILayout.ObjectField(
                "Database",
                database,
                typeof(StageEnemyLayoutDatabase),
                false);

            DrawStageSelector();
            EditorGUILayout.Space(8f);
            DrawGrid();
            EditorGUILayout.Space(10f);
            DrawActions();
            EditorGUILayout.Space(8f);
            DrawStatus();
        }

        private void DrawStageSelector()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            chapter = Mathf.Max(1, EditorGUILayout.IntField("Chapter", chapter));
            stage = Mathf.Max(1, EditorGUILayout.IntField("Stage", stage));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                isDraftDirty = true;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load"))
                LoadStage(true);

            if (GUILayout.Button("New / Clear"))
                ClearDraft(true);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            EditorGUILayout.LabelField("Formation (Slots 1-9)", EditorStyles.boldLabel);

            for (int row = 0; row < StageEnemyLayout.GridSize; row++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int column = 0; column < StageEnemyLayout.GridSize; column++)
                {
                    int slotIndex = row * StageEnemyLayout.GridSize + column;
                    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField($"Slot {slotIndex + 1}", EditorStyles.miniBoldLabel);

                    EditorGUI.BeginChangeCheck();
                    Enemy enemy = (Enemy)EditorGUILayout.ObjectField(
                        enemySlots[slotIndex],
                        typeof(Enemy),
                        false,
                        GUILayout.Height(48f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        enemySlots[slotIndex] = enemy;
                        isDraftDirty = true;
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawActions()
        {
            GUI.enabled = database != null;

            if (GUILayout.Button("Save Stage Layout", GUILayout.Height(30f)))
                SaveStage();

            GUI.enabled = true;

            if (GUILayout.Button("Create 3 Test Enemies"))
            {
                StageEnemyTestContentCreator.CreateTestContent();
                database = StageEnemyAssetUtility.FindOrCreateDatabase();
                LoadStage(false);
            }
        }

        private void DrawStatus()
        {
            if (database == null)
            {
                EditorGUILayout.HelpBox("Select or create a layout database.", MessageType.Error);
                return;
            }

            string assetName = loadedLayout != null ? loadedLayout.name : "New layout";
            string dirtyState = isDraftDirty ? " (Unsaved)" : string.Empty;
            EditorGUILayout.HelpBox(
                $"Chapter {chapter}, Stage {stage} - {assetName}{dirtyState}",
                isDraftDirty ? MessageType.Warning : MessageType.Info);
        }

        private void SaveStage()
        {
            int enemyCount = 0;

            for (int i = 0; i < enemySlots.Length; i++)
            {
                if (enemySlots[i] != null)
                    enemyCount++;
            }

            if (enemyCount == 0)
            {
                EditorUtility.DisplayDialog(
                    "Empty Layout",
                    "Place at least one Enemy prefab before saving.",
                    "OK");
                return;
            }

            loadedLayout = StageEnemyAssetUtility.SaveLayout(
                database,
                chapter,
                stage,
                enemySlots);

            isDraftDirty = false;
            Selection.activeObject = loadedLayout;
            EditorGUIUtility.PingObject(loadedLayout);
        }

        private void LoadStage(bool confirmDiscard)
        {
            if (confirmDiscard && isDraftDirty &&
                !EditorUtility.DisplayDialog(
                    "Discard Changes?",
                    "Unsaved formation changes will be lost.",
                    "Load",
                    "Cancel"))
            {
                return;
            }

            if (database != null && database.TryGetLayout(chapter, stage, out StageEnemyLayout layout))
            {
                loadedLayout = layout;
                enemySlots = StageEnemyAssetUtility.CopySlots(layout);
            }
            else
            {
                loadedLayout = null;
                enemySlots = new Enemy[StageEnemyLayout.SlotCount];
            }

            isDraftDirty = false;
            Repaint();
        }

        private void ClearDraft(bool confirmDiscard)
        {
            if (confirmDiscard && isDraftDirty &&
                !EditorUtility.DisplayDialog(
                    "Clear Formation?",
                    "Unsaved formation changes will be lost.",
                    "Clear",
                    "Cancel"))
            {
                return;
            }

            loadedLayout = null;
            enemySlots = new Enemy[StageEnemyLayout.SlotCount];
            isDraftDirty = true;
        }
    }
}
