using SB.Scripts.UI.Upgrade;
using SB.Scripts.Upgrade;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Editor
{
    public static class MainShipUpgradeUiBuilder
    {
        private const string ScenePath = "Assets/SB/Animations/Scenes/SampleScene.unity";
        private const string GeneratedRootName = "MainShipUpgradeButtons";

        [MenuItem("Tools/SB/Build Main Ship Upgrade UI")]
        public static void Build()
        {
            EditorSceneManager.OpenScene(ScenePath);

            GameObject upgradePanel = GameObject.Find("UpgradePanel");
            if (upgradePanel == null)
            {
                Debug.LogError("UpgradePanel not found.");
                return;
            }

            GameObject gameFrame = GameObject.Find("GameFrame");
            if (gameFrame != null)
                Stretch(gameFrame.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            GameObject battleArea = GameObject.Find("BattleArea");
            if (battleArea != null)
                Stretch(battleArea.GetComponent<RectTransform>(), new Vector2(0f, 0.6f), Vector2.one);

            RectTransform panelRect = upgradePanel.GetComponent<RectTransform>();
            Stretch(panelRect, Vector2.zero, new Vector2(1f, 0.6f));

            Transform oldRoot = upgradePanel.transform.Find(GeneratedRootName);
            if (oldRoot != null)
                Object.DestroyImmediate(oldRoot.gameObject);

            MainShipUpgradeView view = upgradePanel.GetComponent<MainShipUpgradeView>();
            if (view == null)
                view = upgradePanel.AddComponent<MainShipUpgradeView>();

            MainShipUpgradePresenter presenter = upgradePanel.GetComponent<MainShipUpgradePresenter>();
            if (presenter == null)
                presenter = upgradePanel.AddComponent<MainShipUpgradePresenter>();

            GameObject root = CreateUIObject(GeneratedRootName, upgradePanel.transform);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect, new Vector2(0f, 0f), Vector2.one, new Vector2(24f, 24f), new Vector2(-24f, -24f));

            GridLayoutGroup grid = root.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(508f, 160f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            MainShipUpgradeSlotView health = CreateSlot(root.transform, "HealthUpgradeButton", "Health", MainShipUpgradeType.Health);
            MainShipUpgradeSlotView attackPower = CreateSlot(root.transform, "AttackPowerPercentUpgradeButton", "Attack Power", MainShipUpgradeType.AttackPowerPercent);
            MainShipUpgradeSlotView attackSpeed = CreateSlot(root.transform, "AttackSpeedPercentUpgradeButton", "Attack Speed", MainShipUpgradeType.AttackSpeedPercent);
            MainShipUpgradeSlotView cargo = CreateSlot(root.transform, "CargoCapacityUpgradeButton", "Cargo", MainShipUpgradeType.CargoCapacity);
            MainShipUpgradeSlotView luck = CreateSlot(root.transform, "LuckUpgradeButton", "Luck", MainShipUpgradeType.Luck);
            MainShipUpgradeSlotView commanderGaugeMax = CreateSlot(root.transform, "CommanderGaugeMaxUpgradeButton", "Max Commander Gauge", MainShipUpgradeType.CommanderGaugeMax);
            MainShipUpgradeSlotView commanderGaugeRecovery = CreateSlot(root.transform, "CommanderGaugeRecoveryUpgradeButton", "Commander Gauge Recovery", MainShipUpgradeType.CommanderGaugeRecoveryPerSecond);

            SerializedObject viewObject = new SerializedObject(view);
            viewObject.FindProperty("_healthUpgradeButton").objectReferenceValue = health;
            viewObject.FindProperty("attackPowerPercentUpgradeButton").objectReferenceValue = attackPower;
            viewObject.FindProperty("attackSpeedPercentUpgradeButton").objectReferenceValue = attackSpeed;
            viewObject.FindProperty("cargoCapacityUpgradeButton").objectReferenceValue = cargo;
            viewObject.FindProperty("luckUpgradeButton").objectReferenceValue = luck;
            viewObject.FindProperty("commanderGaugeMaxUpgradeButton").objectReferenceValue = commanderGaugeMax;
            viewObject.FindProperty("commanderGaugeRecoveryPerSecondUpgradeButton").objectReferenceValue = commanderGaugeRecovery;
            viewObject.ApplyModifiedProperties();

            ShipUpgradeManager upgradeManager = FindUpgradeManager();
            SerializedObject presenterObject = new SerializedObject(presenter);
            presenterObject.FindProperty("view").objectReferenceValue = view;
            presenterObject.FindProperty("model").objectReferenceValue = upgradeManager;
            presenterObject.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("Main ship upgrade UI built.");
        }

        private static ShipUpgradeManager FindUpgradeManager()
        {
            GameObject managerObject = GameObject.Find("ShipUpgradeManager");
            if (managerObject != null && managerObject.TryGetComponent(out ShipUpgradeManager upgradeManager))
                return upgradeManager;

            return Object.FindFirstObjectByType<ShipUpgradeManager>();
        }

        private static MainShipUpgradeSlotView CreateSlot(Transform parent, string objectName, string displayName, MainShipUpgradeType upgradeType)
        {
            GameObject slotObject = CreateUIObject(objectName, parent);
            Image image = slotObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.16f, 0.16f, 1f);

            Button button = slotObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.16f, 0.16f, 1f);
            colors.highlightedColor = new Color(0.24f, 0.24f, 0.24f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            button.colors = colors;

            TMP_Text name = CreateText("Name", slotObject.transform, displayName, 30, TextAlignmentOptions.Left);
            TMP_Text level = CreateText("Level", slotObject.transform, "0", 24, TextAlignmentOptions.Left);
            TMP_Text cost = CreateText("Cost", slotObject.transform, "0", 24, TextAlignmentOptions.Right);

            SetTextRect(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-16f, -12f));
            SetTextRect(level.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(16f, 12f), new Vector2(-8f, 0f));
            SetTextRect(cost.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 12f), new Vector2(-16f, 0f));

            MainShipUpgradeSlotView slotView = slotObject.AddComponent<MainShipUpgradeSlotView>();
            SerializedObject slotObjectSerialized = new SerializedObject(slotView);
            slotObjectSerialized.FindProperty("_upgradeType").enumValueIndex = (int)upgradeType;
            slotObjectSerialized.FindProperty("_name").objectReferenceValue = name;
            slotObjectSerialized.FindProperty("_cost").objectReferenceValue = cost;
            slotObjectSerialized.FindProperty("_level").objectReferenceValue = level;
            slotObjectSerialized.FindProperty("_button").objectReferenceValue = button;
            slotObjectSerialized.ApplyModifiedProperties();

            return slotView;
        }

        private static GameObject CreateUIObject(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static TMP_Text CreateText(string objectName, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUIObject(objectName, parent);
            TextMeshProUGUI tmpText = textObject.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.alignment = alignment;
            tmpText.color = Color.white;
            tmpText.raycastTarget = false;
            return tmpText;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            Stretch(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetTextRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            Stretch(rect, anchorMin, anchorMax, offsetMin, offsetMax);
        }
    }
}
