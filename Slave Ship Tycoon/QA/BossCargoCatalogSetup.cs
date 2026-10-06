using System;
using System.Collections.Generic;
using SB.Scripts;
using SB.Scripts.Currency;
using SB.SO.TransportItemSOs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BossCargoCatalogSetup
{
    private const string ItemFolder = "Assets/SB/SO/TransportItemSOs";

    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before authoring the cargo catalog.");

        GameObject managers = GameObject.Find("Managers");
        if (managers == null)
            throw new InvalidOperationException("The active scene requires the Managers root.");

        Transform cargoTransform = managers.transform.Find("Cago/CargoManager");
        CargoManager cargoManager = cargoTransform != null
            ? cargoTransform.GetComponent<CargoManager>()
            : null;
        if (cargoManager == null)
            throw new InvalidOperationException("The active scene requires its explicit CargoManager.");

        Dictionary<ETransportItemType, int> existingAmounts = new Dictionary<ETransportItemType, int>();
        if (cargoManager.CargoData != null)
        {
            foreach (CargoData cargo in cargoManager.CargoData)
            {
                if (cargo != null && cargo.Item != null)
                    existingAmounts[cargo.Item.Type] = cargo.Amount;
            }
        }

        // Display order follows the reference; stable enum values preserve saved selections.
        TransportItemSO[] items =
        {
            Configure("GoldBox.asset", ETransportItemType.Gold, "금괴 화물", 5f,
                new Color(.82f, .2f, .92f, 1f), 20480200, 1120, 90, false),
            Configure("DiamondBox.asset", ETransportItemType.Diamond, "다이아 화물", 6f,
                new Color(1f, .64f, .04f, 1f), 28320500, 1840, 120, true),
            Configure("EmeraldBox.asset", ETransportItemType.Emerald, "에메랄드 화물", 4f,
                new Color(.02f, .9f, .98f, 1f), 16204300, 960, 240, true),
            Configure("ShipElementBox.asset", ETransportItemType.ShipElement, "선박 자재", 5f,
                new Color(.62f, .71f, .76f, 1f), 24930100, 1400, 160, false),
            Configure("OtherCargoBox.asset", ETransportItemType.Other, "기타 화물", 5f,
                new Color(.03f, .85f, .55f, 1f), 18420700, 1050, 110, true)
        };

        Undo.RecordObject(cargoManager, "Configure five cargo kinds");
        SerializedObject managerProperties = new SerializedObject(cargoManager);
        SerializedProperty cargoEntries = managerProperties.FindProperty("_cargoData");
        cargoEntries.arraySize = items.Length;
        for (int index = 0; index < items.Length; index++)
        {
            SerializedProperty entry = cargoEntries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Item").objectReferenceValue = items[index];
            entry.FindPropertyRelative("Amount").intValue = existingAmounts.TryGetValue(items[index].Type, out int amount)
                ? amount
                : 0;
        }
        managerProperties.ApplyModifiedProperties();
        EditorUtility.SetDirty(cargoManager);
        EditorSceneManager.MarkSceneDirty(cargoManager.gameObject.scene);
        EditorSceneManager.SaveScene(cargoManager.gameObject.scene);
        AssetDatabase.SaveAssets();

        return "Five explicitly assigned cargo kinds saved. Existing amounts and persistence keys preserved; capacity unchanged. Assign generated Icon sprites separately.";
    }

    private static TransportItemSO Configure(
        string filename, ETransportItemType type, string displayName, float weightTonnes,
        Color accentColor, int gold, int diamonds, int emeralds, bool canCreate)
    {
        string path = ItemFolder + "/" + filename;
        TransportItemSO item = AssetDatabase.LoadAssetAtPath<TransportItemSO>(path);
        if (item == null)
        {
            if (!canCreate)
                throw new InvalidOperationException("Required existing cargo asset is missing: " + path);

            item = ScriptableObject.CreateInstance<TransportItemSO>();
            AssetDatabase.CreateAsset(item, path);
            Undo.RegisterCreatedObjectUndo(item, "Create cargo kind");
        }

        Undo.RecordObject(item, "Configure cargo balance and presentation");
        item.Type = type;
        item.BaseWeight = weightTonnes;
        item.Rewards = new[]
        {
            new CargoSalesRewardData { CurrencyType = CurrencyType.Gold, CurrencyAmount = gold },
            new CargoSalesRewardData { CurrencyType = CurrencyType.Diamond, CurrencyAmount = diamonds },
            new CargoSalesRewardData { CurrencyType = CurrencyType.Emerald, CurrencyAmount = emeralds }
        };
        SerializedObject presentation = new SerializedObject(item);
        presentation.FindProperty("displayName").stringValue = displayName;
        presentation.FindProperty("accentColor").colorValue = accentColor;
        presentation.ApplyModifiedProperties();
        EditorUtility.SetDirty(item);
        return item;
    }
}
