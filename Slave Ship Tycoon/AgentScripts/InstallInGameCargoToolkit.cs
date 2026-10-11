using System;
using System.IO;
using System.Linq;
using SB.Scripts;
using SB.Scripts.Currency;
using SB.Scripts.Commander;
using SB.Scripts.Upgrade;
using SB.Scripts.Visual;
using SB.Scripts.UI.HUD;
using SB.Scripts.UI.Upgrade;
using SB.UI.Cargo;
using SB.UI.InGame;
using SB.UI.Recruitment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class InstallInGameCargoToolkit
{
    public static object Main()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.isDirty || scene.path != "Assets/SB/Animations/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Expected stopped, saved SampleScene.");
        if (GameObject.Find("InGameToolkitUI") != null) throw new InvalidOperationException("Already installed.");
        var legacyCargo = Find<BossCargoLoadoutPanel>();
        var legacyStage = Find<StageInfoBarController>();
        var legacyUpgrade = Find<MainShipUpgradePresenter>();
        var legacySettings = Find<CommanderSettingsPanel>();
        var legacyHud = UnityEngine.Object.FindObjectsByType<CurrencyHudPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(x => x.name == "currencyBar");
        var legacyLog = Find<CurrencyGainLogController>();
        var legacySettlement = Find<SaleSettlementView>();
        var legacyFade = Find<ScreenFadePanelView>();
        var legacyPopup = UnityEngine.Object.FindObjectsByType<StageResultPopupView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(x => x.name == "ResultPopUI");
        var panels = Find<PannelManager>();
        var recruitment = Find<RecruitmentToolkitScreen>();
        var baseSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/SB/UI/Shared/Settings/HarborPanelSettings.asset");
        var hudSettings = Settings(baseSettings, "InGamePanelSettings", 100);
        var cargoSettings = Settings(baseSettings, "CargoPanelSettings", 300);
        var feedbackSettings = Settings(baseSettings, "FeedbackPanelSettings", 500);
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Migrate in-game and cargo UI to Toolkit");
        var owner = new GameObject("InGameToolkitUI");
        Undo.RegisterCreatedObjectUndo(owner, "Create in-game Toolkit root");
        var hudObject = Document(owner.transform, "InGameScreen", hudSettings, "Assets/SB/UI/InGame/InGameScreen.uxml");
        var cargoObject = Document(owner.transform, "CargoScreen", cargoSettings, "Assets/SB/UI/Cargo/CargoScreen.uxml");
        var feedbackObject = Document(owner.transform, "InGameFeedback", feedbackSettings, "Assets/SB/UI/InGame/InGameFeedback.uxml");
        var hud = Undo.AddComponent<InGameToolkitScreen>(hudObject);
        var upgrade = Undo.AddComponent<UpgradeToolkitPanel>(hudObject);
        var commander = Undo.AddComponent<CommanderToolkitController>(hudObject);
        var cargo = Undo.AddComponent<CargoToolkitScreen>(cargoObject);
        var feedback = Undo.AddComponent<InGameFeedbackToolkit>(feedbackObject);
        var hudDocument = hudObject.GetComponent<UIDocument>();
        foreach (var component in new UnityEngine.Object[] { hud, upgrade, commander }) Set(component, "document", hudDocument);
        Set(cargo, "document", cargoObject.GetComponent<UIDocument>());
        Set(feedback, "document", feedbackObject.GetComponent<UIDocument>());
        Set(hud, "currencyManager", Ref(legacyHud, "currencyManager"));
        Set(hud, "cargoManager", Ref(legacyCargo, "cargoManager"));
        Set(hud, "gameFlowManager", Ref(legacyCargo, "gameFlowManager"));
        Set(hud, "chapterDatabase", Ref(legacyStage, "chapterDatabase"));
        Set(hud, "panelManager", panels); Set(hud, "recruitmentScreen", recruitment);
        Set(hud, "cargoScreen", cargo); Set(hud, "upgradePanel", upgrade);
        foreach (var field in new[] { "pirateTitle", "transportTitle", "bossTitle", "pirateDescription", "transportDescription", "bossDescription" }) Copy(legacyStage, field, hud, field);
        Copy(legacyLog, "_maxVisibleCount", hud, "maximumVisibleLogs");
        Copy(legacyLog, "_visibleDuration", hud, "logVisibleSeconds");
        Copy(legacyLog, "_fadeOutDuration", hud, "logFadeSeconds");
        Copy(legacyLog, "_overflowFadeOutDuration", hud, "logOverflowFadeSeconds");
        Copy(legacyLog, "_spawnInterval", hud, "logSpawnIntervalSeconds");
        var oldCurrencies = new SerializedObject(legacyHud).FindProperty("currencyViews");
        var hudFields = new SerializedObject(hud);
        var newCurrencies = hudFields.FindProperty("currencySlots"); newCurrencies.arraySize = oldCurrencies.arraySize;
        string[] hints = { "골드 · 함대 강화", "다이아 · 호위선 모집", "에메랄드 · 지휘 강화" };
        for (int i = 0; i < oldCurrencies.arraySize; i++)
        {
            var old = oldCurrencies.GetArrayElementAtIndex(i); var item = newCurrencies.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("currencyType").enumValueIndex = old.FindPropertyRelative("currencyType").enumValueIndex;
            var slot = (CurrencyHudSlotView)old.FindPropertyRelative("view").objectReferenceValue;
            item.FindPropertyRelative("icon").objectReferenceValue = slot.Icon.sprite;
            item.FindPropertyRelative("iconColor").colorValue = slot.Icon.color;
            item.FindPropertyRelative("hint").stringValue = hints[i];
        }
        hudFields.ApplyModifiedPropertiesWithoutUndo();
        Set(upgrade, "upgradeManager", Ref(legacyUpgrade, "model"));
        Set(upgrade, "currencyManager", Ref(legacyHud, "currencyManager"));
        var oldUpgradeView = (MainShipUpgradeView)Ref(legacyUpgrade, "view");
        var oldSlots = new[] { oldUpgradeView.HealthUpgradeButton, oldUpgradeView.AttackPowerPercentUpgradeButton, oldUpgradeView.AttackSpeedPercentUpgradeButton, oldUpgradeView.CargoCapacityUpgradeButton, oldUpgradeView.LuckUpgradeButton, oldUpgradeView.CommanderGaugeMaxUpgradeButton, oldUpgradeView.CommanderGaugeRecoveryPerSecondUpgradeButton, oldUpgradeView.HealthRecoveryPerSecondUpgradeButton }.Where(x => x != null).ToArray();
        var upgradeFields = new SerializedObject(upgrade); var definitions = upgradeFields.FindProperty("upgradeRows"); definitions.arraySize = oldSlots.Length;
        for (int i = 0; i < oldSlots.Length; i++)
        {
            var old = new SerializedObject(oldSlots[i]); var row = definitions.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("upgradeType").enumValueIndex = old.FindProperty("_upgradeType").enumValueIndex;
            row.FindPropertyRelative("displayName").stringValue = ((TMPro.TMP_Text)old.FindProperty("_name").objectReferenceValue).text;
        }
        upgradeFields.ApplyModifiedPropertiesWithoutUndo();
        Copy(oldSlots[0], "_repeatStartDelay", upgrade, "repeatStartDelaySeconds");
        Copy(oldSlots[0], "_repeatInterval", upgrade, "repeatIntervalSeconds");
        Copy(oldSlots[0], "feedbackSeconds", upgrade, "feedbackSeconds");
        Set(commander, "manager", Ref(legacySettings, "manager"));
        foreach (var field in new[] { "cargoManager", "gameFlowManager", "transportEquipmentManager" }) Set(cargo, field, Ref(legacyCargo, field));
        Set(cargo, "currencyManager", Ref(legacyHud, "currencyManager"));
        Set(cargo, "cardTemplate", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/SB/UI/Cargo/CargoCard.uxml"));
        Copy(legacyCargo, "minimumSelectedSlots", cargo, "minimumSelectedSlots");
        var card = (CargoLoadoutCardView)Ref(legacyCargo, "availableCardPrefab");
        var hold = (HoldRepeatButton)Ref(card, "loadButtonRepeat");
        if (hold != null) { Copy(hold, "_repeatStartDelay", cargo, "repeatStartDelaySeconds"); Copy(hold, "_repeatInterval", cargo, "repeatIntervalSeconds"); }
        Copy(legacySettlement, "_fadeInDuration", feedback, "settlementFadeInSeconds");
        Copy(legacySettlement, "_holdDuration", feedback, "settlementHoldSeconds");
        Copy(legacySettlement, "_fadeOutDuration", feedback, "settlementFadeOutSeconds");
        Copy(legacySettlement, "rewardCountSeconds", feedback, "rewardCountSeconds");
        Copy(legacySettlement, "rewardRowDelaySeconds", feedback, "rewardRowDelaySeconds");
        Copy(legacySettlement, "entryScale", feedback, "settlementEntryScale");
        Copy(legacyPopup, "fadeInSpeed", feedback, "popupFadeInSpeed");
        Copy(legacyPopup, "holdTime", feedback, "popupHoldSeconds");
        Copy(legacyPopup, "fadeOutSpeed", feedback, "popupFadeOutSpeed");
        Copy(legacyFade, "fadeInDuration", feedback, "stageFadeInSeconds");
        Copy(legacyFade, "holdDuration", feedback, "stageFadeHoldSeconds");
        Copy(legacyFade, "fadeOutDuration", feedback, "stageFadeOutSeconds");

        // Navigation uses PanelChanged for the Toolkit upgrade region; the fleet PanelView retains its explicit Toolkit target.
        var oldUpgradeNav = UnityEngine.Object.FindObjectsByType<PanelView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(x => Ref(x, "_panel") == legacyUpgrade.gameObject);
        Set(oldUpgradeNav, "_panel", null);
        for (int i = 0; i < panels.transform.childCount; i++) Retire(panels.transform.GetChild(i).gameObject);
        foreach (var path in new[] { "Canvas/GameFrame/BottomUIBackground", "Canvas/GameFrame/CurrencyLogRoot", "Canvas/GameFrame/currencyBar", "Canvas/GameFrame/UpgradePanel", "Canvas/GameFrame/Commander", "Canvas/GameFrame/CommandStrip", "Canvas/GameFrame/SaleSettlementUI", "Canvas/GameFrame/BossCargoLoadoutUI", "Canvas/GameFrame/BattleArea/StageInfoBar", "Canvas/ScreenFadePanelController" }) Retire(GameObject.Find(path));
        var oldResultController = Find<StageResultPopupController>(); Undo.RecordObject(oldResultController, "Retire result event listener"); oldResultController.enabled = false;
        foreach (var popup in UnityEngine.Object.FindObjectsByType<StageResultPopupView>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Retire(popup.gameObject);
        hudObject.SetActive(true); feedbackObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        return new { scene = scene.path, hud = hudObject.name, cargo = cargoObject.name, feedback = feedbackObject.name, retiredUpgradeTarget = oldUpgradeNav.name };
    }
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    private static UnityEngine.Object Ref(UnityEngine.Object obj, string field) => new SerializedObject(obj).FindProperty(field).objectReferenceValue;
    private static void Set(UnityEngine.Object obj, string field, UnityEngine.Object value)
    {
        Undo.RecordObject(obj, "Wire Toolkit UI"); var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(obj);
    }
    private static void Copy(UnityEngine.Object source, string sourceField, UnityEngine.Object target, string targetField)
    {
        var from = new SerializedObject(source).FindProperty(sourceField); var so = new SerializedObject(target); var to = so.FindProperty(targetField);
        if (from.propertyType == SerializedPropertyType.String) to.stringValue = from.stringValue;
        else if (from.propertyType == SerializedPropertyType.Integer) to.intValue = from.intValue;
        else if (from.propertyType == SerializedPropertyType.Float) to.floatValue = from.floatValue;
        else throw new InvalidOperationException(sourceField);
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
    }
    private static PanelSettings Settings(PanelSettings source, string name, int order)
    {
        var asset = UnityEngine.Object.Instantiate(source); asset.name = name; asset.sortingOrder = order;
        AssetDatabase.CreateAsset(asset, "Assets/SB/UI/Shared/Settings/" + name + ".asset"); return asset;
    }
    private static GameObject Document(Transform parent, string name, PanelSettings settings, string source)
    {
        var go = new GameObject(name); go.SetActive(false); Undo.RegisterCreatedObjectUndo(go, "Create UI document"); go.transform.SetParent(parent, false);
        var document = Undo.AddComponent<UIDocument>(go); document.panelSettings = settings; document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(source); return go;
    }
    private static void Retire(GameObject go) { if (go == null) return; Undo.RecordObject(go, "Retire migrated UI"); go.SetActive(false); }
}
