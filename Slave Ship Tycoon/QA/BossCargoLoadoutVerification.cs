using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Executed by Unity MCP from outside Assets. Outcomes are simulated, not a full combat playthrough.
public static class BossCargoLoadoutVerification
{
    [Serializable]
    private sealed class Check
    {
        public string name;
        public bool passed;
        public string detail;
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string scope = "Actual UI button calls and event-driven flow with simulated boss failure, boss victory, and transport completion; not a full combat playthrough.";
        public string error;
        public bool userLoadoutAndPersistenceRestored;
        public int checkCount;
        public string checkNames;
        public Check[] serializedChecks;
        [NonSerialized] public List<Check> checks = new List<Check>();
    }

    public static async Task<string> Run()
    {
        Report report = new Report();
        CargoManager cargo = null;
        BossCargoLoadoutPanel panel = null;
        string originalSaveKey = null;
        CargoLoadoutPersistence originalPersistence = CargoLoadoutPersistence.SessionOnly;
        int[] originalAmounts = null;
        float originalTimeScale = Time.timeScale;
        string qaKey = "BossCargoQA." + Guid.NewGuid().ToString("N");
        string persistenceKey = qaKey + ".Persistence";
        List<GameObject> temporaryObjects = new List<GameObject>();
        Action<StageBattleInitializedEvent> onInitialized = null;
        Action<SellChapterStartedEvent> onSellStarted = null;
        Action<SaleSettlementCompletedEvent> onSettlement = null;
        Action<CurrencyAddRequestEvent> onCurrencyAdded = null;
        BattleSpawnData lastBattleData = default(BattleSpawnData);
        int initializationCount = 0;
        int enteredSellChapter = 0;
        SaleSettlementResult settlement = null;
        Dictionary<CurrencyType, long> payoutRequests = new Dictionary<CurrencyType, long>();

        try
        {
            Require(report, Application.isPlaying, "Play mode", "Checks require the already running Play session.");
            GameObject uiRoot = GameObject.Find("Canvas/GameFrame/BossCargoLoadoutUI");
            Require(report, uiRoot != null, "Modal root", "Canvas/GameFrame/BossCargoLoadoutUI exists.");
            panel = uiRoot.GetComponent<BossCargoLoadoutPanel>();
            cargo = Get<CargoManager>(panel, "cargoManager");
            GameFlowManager flow = Get<GameFlowManager>(panel, "gameFlowManager");
            Transform modal = uiRoot.transform.Find("Panel");
            Require(report, panel != null && cargo != null && flow != null && modal != null,
                "Explicit references", "Panel, CargoManager, and GameFlowManager are connected.");

            originalSaveKey = Get<string>(cargo, "loadoutSaveKey");
            originalPersistence = Get<CargoLoadoutPersistence>(cargo, "loadoutPersistence");
            originalAmounts = CopyAmounts(cargo.CargoData);
            Set(cargo, "loadoutSaveKey", qaKey);
            Set(cargo, "loadoutPersistence", CargoLoadoutPersistence.PlayerPrefs);

            onInitialized = evt => { lastBattleData = evt.BattleSpawnData; initializationCount++; };
            onSellStarted = evt => enteredSellChapter = evt.Chapter;
            onSettlement = evt => settlement = evt.Result;
            onCurrencyAdded = evt =>
            {
                payoutRequests.TryGetValue(evt.CurrencyType, out long priorAmount);
                payoutRequests[evt.CurrencyType] = priorAmount + evt.AddedAmount;
            };
            Bus<StageBattleInitializedEvent>.OnEvent += onInitialized;
            Bus<SellChapterStartedEvent>.OnEvent += onSellStarted;
            Bus<SaleSettlementCompletedEvent>.OnEvent += onSettlement;

            Require(report, flow.CanStartBossBattle && !Get<bool>(flow, "currentBattleIsBoss"),
                "Pirate preparation state", "The current chapter can challenge its boss.");
            if (!panel.IsOpen) panel.Open();
            Require(report, panel.IsOpen && Mathf.Approximately(Time.timeScale, 0f),
                "Preparation pauses combat", "Opening the modal pauses the current battle.");
            float resumedTimeScale = Get<float>(panel, "previousTimeScale");
            Require(report, resumedTimeScale > 0f, "Battle resume scale", "The modal captured a positive battle time scale.");

            Button loadGold = ButtonAt(modal, "AvailableCargo/AvailableCargo0/Content/LoadButton");
            Button loadMaterial = ButtonAt(modal, "AvailableCargo/AvailableCargo1/Content/LoadButton");
            Button decreaseGold = ButtonAt(modal, "Loadout/SelectedCargoSlots/SelectedCargo0/Content/DecreaseButton");
            Button increaseGold = ButtonAt(modal, "Loadout/SelectedCargoSlots/SelectedCargo0/Content/IncreaseButton");
            Button removeGold = ButtonAt(modal, "Loadout/SelectedCargoSlots/SelectedCargo0/Content/RemoveButton");
            Button confirm = ButtonAt(modal, "Footer/ConfirmButton");
            Button close = ButtonAt(modal, "Footer/CloseButton");

            cargo.ClearCargo();
            Click(loadGold, 2);
            Click(loadMaterial, 3);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == 2 &&
                cargo.GetCargoAmount(ETransportItemType.ShipElement) == 3 &&
                Mathf.Approximately(cargo.GetCurrentCargoCapacity(), 112f),
                "Available-card loading", "Two gold cargo and three ship material cargo total 112 t.");
            Require(report, Mathf.Approximately(cargo.GetStatCargoCapacity(), 300f),
                "Configured capacity", "Capacity is initialized to the scene's 300 t setting.");
            Image graph = Get<Image>(panel, "capacityFill");
            Require(report, Get<TMP_Text>(panel, "capacityText").text == "112 / 300 t" &&
                graph.sprite != null && graph.type == Image.Type.Filled &&
                Mathf.Approximately(graph.fillAmount, 112f / 300f),
                "Capacity label and graph", "The text and fill amount agree with the selected weights, and the graph has a sprite with Filled rendering.");
            Require(report, RewardText(panel, "goldRewardText") == 115750700L &&
                RewardText(panel, "diamondRewardText") == 6440L &&
                RewardText(panel, "emeraldRewardText") == 660L,
                "Three-currency preview", "Gold 115,750,700 / Diamond 6,440 / Emerald 660.");

            int guard = 0;
            while (cargo.CanLoadCargo(ETransportItemType.Gold) && guard++ < 100) Click(loadGold);
            int fullGoldAmount = cargo.GetCargoAmount(ETransportItemType.Gold);
            Require(report, !loadGold.interactable && !loadMaterial.interactable &&
                !cargo.CanLoadCargo(ETransportItemType.Gold) && !cargo.CanLoadCargo(ETransportItemType.ShipElement),
                "Capacity button states", "Both load buttons disable when neither cargo fits the remaining capacity.");
            Click(loadGold);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == fullGoldAmount &&
                cargo.GetCurrentCargoCapacity() <= cargo.GetStatCargoCapacity(),
                "Capacity enforcement", "Invoking a disabled load button still cannot exceed capacity.");
            Click(decreaseGold);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == fullGoldAmount - 1,
                "Selected-card minus", "The selected-card minus button removes one unit.");
            Click(increaseGold);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == fullGoldAmount,
                "Selected-card plus", "The selected-card plus button adds one unit within capacity.");
            Click(removeGold);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == 0 &&
                modal.Find("Loadout/SelectedCargoSlots/SelectedCargo0/Empty").gameObject.activeSelf,
                "Selected-card remove", "The X button removes the complete cargo type and exposes its empty slot.");
            Click(loadGold, 2);
            Click(close);
            Require(report, !panel.IsOpen && Mathf.Approximately(Time.timeScale, resumedTimeScale) &&
                cargo.GetCargoAmount(ETransportItemType.Gold) == 2 &&
                cargo.GetCargoAmount(ETransportItemType.ShipElement) == 3,
                "Close keeps selection", "Closing restores battle time and retains both selected amounts.");
            panel.Open();
            Require(report, panel.IsOpen && Mathf.Approximately(Time.timeScale, 0f) &&
                RewardText(panel, "goldRewardText") == 115750700L,
                "Reopen keeps selection", "Reopening restores the same selected cargo and preview.");
            Require(report, SumCargo(cargo.TransportCargoData) == 0 && CrateCount(cargo) == 0,
                "No cargo before boss victory", "Preparing a loadout does not put crates on the main ship.");

            await CheckPersistence(report, cargo, persistenceKey, temporaryObjects);
            panel.Refresh();
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == 2 &&
                cargo.GetCargoAmount(ETransportItemType.ShipElement) == 3,
                "Isolated persistence checks", "Temporary managers do not alter the live selected plan.");

            foreach (CargoData entry in cargo.CargoData)
                if (entry != null && entry.Item != null && entry.Item.Type == ETransportItemType.Gold)
                    entry.Amount = -1;
            cargo.GetTotalWeight();
            Bus<ChangedCurrentCargoCapacityEvent>.Raise(new ChangedCurrentCargoCapacityEvent(cargo.CargoData));
            Require(report, modal.Find("Loadout/SelectedCargoSlots/SelectedCargo0/Content").gameObject.activeSelf &&
                removeGold.interactable && !confirm.interactable && !cargo.HasValidLoadout &&
                Get<TMP_Text>(panel, "statusText").text.Contains("화물 설정"),
                "Invalid saved quantity remains removable", "A negative quantity keeps its selected card and X button visible, disables confirmation, and displays a configuration error.");
            Click(removeGold);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == 0 && cargo.HasValidLoadout &&
                confirm.interactable && cargo.GetCargoAmount(ETransportItemType.ShipElement) == 3,
                "Invalid quantity recovery through X", "The actual X button removes the invalid quantity and restores a valid selection without changing the other cargo.");
            Click(loadGold, 2);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == 2 &&
                cargo.GetCargoAmount(ETransportItemType.ShipElement) == 3 &&
                Mathf.Approximately(cargo.GetCurrentCargoCapacity(), 112f),
                "Restored scenario loadout", "The original 2 + 3 test selection is restored before simulated boss outcomes.");

            int beforeFailureInitialization = initializationCount;
            Click(confirm);
            Require(report, !panel.IsOpen && Get<bool>(flow, "currentBattleIsBoss") && Time.timeScale > 0f,
                "Confirm starts boss", "The footer button closes preparation, resumes time, and enters the boss battle.");
            await Until(() => initializationCount > beforeFailureInitialization, "First boss initialization", 3f);
            Bus<BattleStartEvent>.Raise(new BattleStartEvent(lastBattleData));
            Bus<MainShipDeadEvent>.Raise(new MainShipDeadEvent());
            await Until(() => !Get<bool>(flow, "currentBattleIsBoss") && flow.CanStartBossBattle,
                "Normal battle after simulated boss failure", 3f);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == 2 &&
                cargo.GetCargoAmount(ETransportItemType.ShipElement) == 3 &&
                SumCargo(cargo.TransportCargoData) == 0 && CrateCount(cargo) == 0,
                "Boss failure keeps plan", "Simulated boss failure returns to pirate waves and retains the selection without actual cargo.");

            panel.Open();
            int beforeVictoryInitialization = initializationCount;
            Click(confirm);
            await Until(() => initializationCount > beforeVictoryInitialization, "Second boss initialization", 3f);
            Bus<BattleStartEvent>.Raise(new BattleStartEvent(lastBattleData));
            Bus<StageEnemiesDefeatedEvent>.Raise(new StageEnemiesDefeatedEvent());
            await Until(() => enteredSellChapter == 2 && Get<int>(flow, "currentChapter") == 2,
                "Transport entry after simulated victory", 3f);
            Time.timeScale = 0f;
            await Task.Delay(50);
            Require(report, SumCargo(cargo.TransportCargoData) == 5 && CrateCount(cargo) == 5,
                "Transport loads actual main ship", "The Sell-entry snapshot contains five cargo units and the active main ship displays five crates.");
            MarketPriceManager market = UnityEngine.Object.FindFirstObjectByType<MarketPriceManager>();
            Require(report, market != null && market.CargoTypePriceRates.Count == 2 &&
                market.TryGetMarketPrice(ETransportItemType.Gold, out TransportMarketPriceEntry _) &&
                market.TryGetMarketPrice(ETransportItemType.ShipElement, out TransportMarketPriceEntry _),
                "Transport market initialization", "Both transported cargo types have active market entries.");
            cargo.UnloadCargo(ETransportItemType.Gold);
            Require(report, cargo.GetCargoAmount(ETransportItemType.Gold) == 1 &&
                ManifestAmount(cargo.TransportCargoData, ETransportItemType.Gold) == 2 && CrateCount(cargo) == 5,
                "Manifest independent from saved plan", "Editing the next selection does not change the in-flight manifest or displayed crates.");

            CurrencyManager currencies = CurrencyManager.Instance;
            Require(report, currencies != null, "Currency manager", "A currency manager receives settlement requests.");
            long goldBeforeSale = currencies.GetCurrency(CurrencyType.Gold);
            long diamondBeforeSale = currencies.GetCurrency(CurrencyType.Diamond);
            long emeraldBeforeSale = currencies.GetCurrency(CurrencyType.Emerald);
            Bus<CurrencyAddRequestEvent>.OnEvent += onCurrencyAdded;
            Bus<SellChapterCompletedEvent>.Raise(new SellChapterCompletedEvent(2));
            Bus<CurrencyAddRequestEvent>.OnEvent -= onCurrencyAdded;
            Require(report, settlement != null && settlement.SellChapter == 2 && settlement.CargoResults.Count == 2 &&
                Payout(payoutRequests, CurrencyType.Gold) == 115750700L &&
                Payout(payoutRequests, CurrencyType.Diamond) == 6440L &&
                Payout(payoutRequests, CurrencyType.Emerald) == 660L,
                "Three-currency settlement", "Simulated transport completion settles the original 2 + 3 manifest in all three currencies.");
            Require(report, currencies.GetCurrency(CurrencyType.Gold) - goldBeforeSale == 115750700L &&
                currencies.GetCurrency(CurrencyType.Diamond) - diamondBeforeSale == 6440L &&
                currencies.GetCurrency(CurrencyType.Emerald) - emeraldBeforeSale == 660L,
                "Actual currency balances", "The settlement requests are applied to all three live currency balances.");
            Require(report, SumCargo(cargo.TransportCargoData) == 0 && CrateCount(cargo) == 0 &&
                cargo.GetCargoAmount(ETransportItemType.Gold) == 1 &&
                cargo.GetCargoAmount(ETransportItemType.ShipElement) == 3,
                "Settlement clears only actual cargo", "The manifest and crates clear while the changed next-trip selection remains intact.");
            report.passed = true;
        }
        catch (Exception exception)
        {
            report.error = exception.ToString();
            report.passed = false;
        }
        finally
        {
            if (onInitialized != null) Bus<StageBattleInitializedEvent>.OnEvent -= onInitialized;
            if (onSellStarted != null) Bus<SellChapterStartedEvent>.OnEvent -= onSellStarted;
            if (onSettlement != null) Bus<SaleSettlementCompletedEvent>.OnEvent -= onSettlement;
            if (onCurrencyAdded != null) Bus<CurrencyAddRequestEvent>.OnEvent -= onCurrencyAdded;
            foreach (GameObject temporary in temporaryObjects)
            {
                if (temporary == null) continue;
                temporary.SetActive(false);
                UnityEngine.Object.Destroy(temporary);
            }
            if (cargo != null && originalAmounts != null)
            {
                for (int index = 0; index < originalAmounts.Length; index++)
                    if (cargo.CargoData[index] != null) cargo.CargoData[index].Amount = originalAmounts[index];
                cargo.GetTotalWeight();
                Set(cargo, "loadoutSaveKey", originalSaveKey);
                Set(cargo, "loadoutPersistence", originalPersistence);
                Bus<ChangedCurrentCargoCapacityEvent>.Raise(new ChangedCurrentCargoCapacityEvent(cargo.CargoData));
                report.userLoadoutAndPersistenceRestored = true;
            }
            DeleteQAKeys(qaKey);
            DeleteQAKeys(persistenceKey);
            PlayerPrefs.Save();
            Time.timeScale = originalTimeScale;
            if (panel != null) panel.Refresh();
        }
        report.serializedChecks = report.checks.ToArray();
        report.checkCount = report.checks.Count;
        report.checkNames = string.Join("\n", report.checks.ConvertAll(check =>
            (check.passed ? "PASS " : "FAIL ") + check.name + ": " + check.detail));
        return JsonUtility.ToJson(report, true);
    }

    private static async Task CheckPersistence(Report report, CargoManager source, string key,
        List<GameObject> temporaryObjects)
    {
        CargoManager first = CreateTemporary(source, key, 2, 3, temporaryObjects);
        Require(report, first.LoadCargo(ETransportItemType.Gold), "Persistence write", "An isolated manager saves a changed amount under a unique QA key.");
        first.gameObject.SetActive(false);
        CargoManager reloaded = CreateTemporary(source, key, 0, 0, temporaryObjects);
        Require(report, reloaded.GetCargoAmount(ETransportItemType.Gold) == 3 &&
            reloaded.GetCargoAmount(ETransportItemType.ShipElement) == 3 &&
            SumCargo(reloaded.TransportCargoData) == 0,
            "Persistence restore", "A newly activated manager restores both amounts without restoring actual transported cargo.");
        reloaded.gameObject.SetActive(false);
        PlayerPrefs.SetInt(key + "." + (int)ETransportItemType.Gold, 18);
        PlayerPrefs.SetInt(key + "." + (int)ETransportItemType.ShipElement, 3);
        PlayerPrefs.Save();
        CargoManager overCapacity = CreateTemporary(source, key, 0, 0, temporaryObjects);
        Require(report, overCapacity.GetCargoAmount(ETransportItemType.Gold) == 18 &&
            Mathf.Approximately(overCapacity.GetCurrentCargoCapacity(), 432f) &&
            !overCapacity.HasValidLoadout && !overCapacity.CanLoadCargo(ETransportItemType.Gold),
            "Over-capacity persistence", "A saved 432 t plan is retained without automatic clamping and exposes invalid capacity.");
        Require(report, overCapacity.RemoveCargo(ETransportItemType.Gold) && overCapacity.HasValidLoadout &&
            overCapacity.GetCargoAmount(ETransportItemType.ShipElement) == 3,
            "Over-capacity recovery", "Explicit removal restores a valid plan while retaining the other selected cargo.");
        overCapacity.gameObject.SetActive(false);
        await Task.Delay(30);
    }

    private static CargoManager CreateTemporary(CargoManager source, string key, int goldAmount,
        int materialAmount, List<GameObject> temporaryObjects)
    {
        GameObject temporary = new GameObject("BossCargoQA_TemporaryManager");
        temporary.SetActive(false);
        temporaryObjects.Add(temporary);
        CargoManager manager = temporary.AddComponent<CargoManager>();
        CargoData[] configuration = new CargoData[source.CargoData.Length];
        for (int index = 0; index < configuration.Length; index++)
        {
            CargoData original = source.CargoData[index];
            configuration[index] = new CargoData
            {
                Item = original.Item,
                Amount = original.Item.Type == ETransportItemType.Gold ? goldAmount : materialAmount
            };
        }
        Set(manager, "_cargoData", configuration);
        Set(manager, "_statCargoCapacity", 300f);
        Set(manager, "loadoutSaveKey", key);
        Set(manager, "loadoutPersistence", CargoLoadoutPersistence.PlayerPrefs);
        temporary.SetActive(true);
        return manager;
    }

    private static async Task Until(Func<bool> condition, string description, float timeoutSeconds)
    {
        float start = Time.realtimeSinceStartup;
        while (!condition())
        {
            if (Time.realtimeSinceStartup - start >= timeoutSeconds)
                throw new TimeoutException(description + " exceeded " + timeoutSeconds + " seconds.");
            await Task.Delay(16);
        }
    }

    private static void Require(Report report, bool condition, string name, string detail)
    {
        report.checks.Add(new Check { name = name, passed = condition, detail = detail });
        if (!condition) throw new InvalidOperationException(name + ": " + detail);
    }

    private static T Get<T>(object instance, string field)
    {
        if (instance == null) throw new InvalidOperationException("Cannot read " + field + " from a null reference.");
        FieldInfo member = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (member == null) throw new MissingFieldException(instance.GetType().FullName, field);
        return (T)member.GetValue(instance);
    }

    private static void Set(object instance, string field, object value)
    {
        FieldInfo member = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (member == null) throw new MissingFieldException(instance.GetType().FullName, field);
        member.SetValue(instance, value);
    }

    private static Button ButtonAt(Transform root, string path)
    {
        Transform child = root.Find(path);
        Button button = child != null ? child.GetComponent<Button>() : null;
        if (button == null) throw new InvalidOperationException("Missing button: " + root.name + "/" + path);
        return button;
    }

    private static void Click(Button button, int count = 1)
    {
        for (int index = 0; index < count; index++) button.onClick.Invoke();
    }

    private static long RewardText(BossCargoLoadoutPanel panel, string field)
    {
        string rawText = Get<TMP_Text>(panel, field).text;
        return long.Parse(rawText, NumberStyles.AllowThousands, CultureInfo.CurrentCulture);
    }

    private static int[] CopyAmounts(CargoData[] cargoData)
    {
        int[] amounts = new int[cargoData.Length];
        for (int index = 0; index < amounts.Length; index++) amounts[index] = cargoData[index] != null ? cargoData[index].Amount : 0;
        return amounts;
    }

    private static int SumCargo(IReadOnlyList<CargoData> cargoData)
    {
        int total = 0;
        foreach (CargoData entry in cargoData) if (entry != null) total += entry.Amount;
        return total;
    }

    private static int ManifestAmount(IReadOnlyList<CargoData> manifest, ETransportItemType type)
    {
        foreach (CargoData entry in manifest) if (entry != null && entry.Item.Type == type) return entry.Amount;
        return 0;
    }

    private static int CrateCount(CargoManager cargo)
    {
        int total = 0;
        ShipCargoVisualizer[] visualizers = UnityEngine.Object.FindObjectsByType<ShipCargoVisualizer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (ShipCargoVisualizer visualizer in visualizers)
            if (visualizer.CargoManager == cargo) total += visualizer.DisplayedCrateCount;
        return total;
    }

    private static long Payout(Dictionary<CurrencyType, long> payouts, CurrencyType currency)
    {
        payouts.TryGetValue(currency, out long amount);
        return amount;
    }

    private static void DeleteQAKeys(string key)
    {
        PlayerPrefs.DeleteKey(key + "." + (int)ETransportItemType.Gold);
        PlayerPrefs.DeleteKey(key + "." + (int)ETransportItemType.ShipElement);
    }
}
