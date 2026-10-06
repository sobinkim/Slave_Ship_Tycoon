using System;
using SB.Scripts;
using SB.SO.TransportItemSOs;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Editor authoring only. Runtime creates catalog cards from these explicit prefabs.
public static class BossCargoReferenceUISetup
{
    private static readonly Color Background = Hex("09131B");
    private static readonly Color Surface = Hex("091C24");
    private static readonly Color Section = Hex("0B1A21");
    private static readonly Color Edge = Hex("1F4C54");
    private static readonly Color TextColor = Hex("DBF0F2");
    private static readonly Color Muted = Hex("66A6AD");
    private static readonly Color Cyan = Hex("00BFCC");
    private static TMP_FontAsset font;
    private static UnityEngine.UI.Image[] currencyIcons;

    public static string ApplyHoldAndHud()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing the cargo screen.");
        var frame = GameObject.Find("Canvas").transform.Find("GameFrame");
        StyleCurrencyHeader(frame.Find("currencyBar"));
        StyleCurrencyHeader(frame.Find("BossCargoLoadoutUI/Panel/CurrencyHeader"));
        string[] paths = {
            "Assets/SB/Prefabs/UI/CargoLoadoutSelectedCard.prefab",
            "Assets/SB/Prefabs/UI/CargoLoadoutAvailableCard.prefab"
        };
        foreach (string path in paths)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigureRepeatCard(prefabRoot.GetComponent<CargoLoadoutCardView>());
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
        }
        EditorSceneManager.MarkSceneDirty(frame.gameObject.scene);
        EditorSceneManager.SaveScene(frame.gameObject.scene);
        AssetDatabase.SaveAssets();
        return "Connected both cargo load buttons for hold repeat and styled both currency headers.";
    }

    private static void ConfigureRepeatCard(CargoLoadoutCardView card)
    {
        var button = card.LoadButton;
        var repeat = button.GetComponent<HoldRepeatButton>();
        if (repeat == null) repeat = button.gameObject.AddComponent<HoldRepeatButton>();
        // Submit must load before the Button click closes the cargo picker.
        var components = button.GetComponents<Component>();
        while (Array.IndexOf(components, repeat) > Array.IndexOf(components, button))
        {
            if (!UnityEditorInternal.ComponentUtility.MoveComponentUp(repeat)) break;
            components = button.GetComponents<Component>();
        }
        Ref(repeat, "button", button);
        Ref(card, "loadButtonRepeat", repeat);
        var properties = new SerializedObject(repeat);
        properties.FindProperty("_repeatStartDelay").floatValue = .25f;
        properties.FindProperty("_repeatInterval").floatValue = .06f;
        properties.ApplyModifiedProperties();
        // Pointer-down performs the first load; a release click must not load a second item.
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            if (button.onClick.GetPersistentTarget(i) == card && button.onClick.GetPersistentMethodName(i) == nameof(CargoLoadoutCardView.AddCargo))
                UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    private static void StyleCurrencyHeader(Transform header)
    {
        foreach (var image in header.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            if (image.name == "icon") continue;
            Undo.RecordObject(image, "Currency bar colors");
            image.color = image.name == "SectionTopEdge" || image.name == "SectionBottomEdge" || image.name.StartsWith("Divider") ? Edge : Background;
            EditorUtility.SetDirty(image);
        }
        foreach (var label in header.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.name != "CurrencyName" && label.name != "CurrencyValue") continue;
            Undo.RecordObject(label, "Currency bar colors");
            label.color = label.name == "CurrencyName" ? Muted : TextColor;
            EditorUtility.SetDirty(label);
        }
        foreach (var outline in header.GetComponentsInChildren<UnityEngine.UI.Outline>(true))
        {
            Undo.RecordObject(outline, "Currency bar borders");
            outline.effectColor = Edge;
            EditorUtility.SetDirty(outline);
        }
    }

    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing the cargo screen.");
        var frame = GameObject.Find("Canvas").transform.Find("GameFrame");
        var root = frame.Find("BossCargoLoadoutUI");
        var controller = root.GetComponent<BossCargoLoadoutPanel>();
        var panel = root.Find("Panel");
        var modalCanvas = root.GetComponent<Canvas>();
        if (modalCanvas == null) modalCanvas = Undo.AddComponent<Canvas>(root.gameObject);
        modalCanvas.overrideSorting = true; modalCanvas.sortingOrder = 200;
        if (root.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) Undo.AddComponent<UnityEngine.UI.GraphicRaycaster>(root.gameObject);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SB/Fonts/MalgunGothic SDF.asset");
        var manager = GameObject.Find("Managers").transform.Find("Cago/CargoManager").GetComponent<CargoManager>();
        ConfigureCargoIcons(manager);
        var sourceHeader = frame.Find("currencyBar");
        StyleCurrencyHeader(sourceHeader);
        string[] currencyNames = { "GoldCurrencyElement", "DiamondCurrencyElement", "EmeraldCurrencyElement" };
        currencyIcons = new UnityEngine.UI.Image[3];
        for (int i = 0; i < 3; i++) currencyIcons[i] = sourceHeader.Find(currencyNames[i] + "/icon").GetComponent<UnityEngine.UI.Image>();

        Box(panel.parent, "Panel", 0, 0, 1080, 1920, Background);
        Stretch(panel);
        panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        root.SetAsLastSibling();
        var header = panel.Find("CurrencyHeader");
        Place(header, 0, 0, 1080, 94);
        header.GetComponent<UnityEngine.UI.Image>().color = Background;
        for (int i = 0; i < 3; i++)
        {
            var entry = header.Find(currencyNames[i]);
            Place(entry, i * 360, 0, 360, 94);
            var image = entry.GetComponent<UnityEngine.UI.Image>(); if (image != null) image.color = Background;
            var entryOutline = entry.GetComponent<UnityEngine.UI.Outline>(); if (entryOutline != null) Undo.DestroyObjectImmediate(entryOutline);
            var headerOutline = header.GetComponent<UnityEngine.UI.Outline>(); if (headerOutline != null) Undo.DestroyObjectImmediate(headerOutline);
            var currencyView = entry.GetComponent<SB.Scripts.UI.HUD.CurrencyHudSlotView>();
            if (currencyView != null) { var valueProperties = new SerializedObject(currencyView); valueProperties.FindProperty("numberDisplayMode").enumValueIndex = (int)SB.Scripts.UI.HUD.CurrencyNumberDisplayMode.Full; valueProperties.ApplyModifiedProperties(); }
            Place(entry.Find("CurrencyName"), 87, 12, 258, 29);
            var currencyName = entry.Find("CurrencyName").GetComponent<TMP_Text>(); currencyName.fontSize = 20; currencyName.color = Muted;
            Place(entry.Find("CurrencyValue"), 87, 44, 258, 37);
            entry.Find("CurrencyValue").GetComponent<TMP_Text>().fontSize = 29;
            Place(entry.Find("icon"), 37, 49, 34, 34);
            Box(header, "Divider" + i, i * 360, 0, 1, 94, Edge);
        }
        StyleCurrencyHeader(header);
        Box(panel, "HeaderRule", 0, 94, 1080, 1, Edge);
        Label(panel, "Title", "적재", 32, 140, 1016, 65, 40, TextAlignmentOptions.Center);
        Hide(panel, "Subtitle");

        var loadout = Box(panel, "Loadout", 32, 240, 1016, 908, Section, true).transform;
        Label(loadout, "CapacityLabel", "현재 적재량", 24, 38, 440, 43, 28);
        Ref(controller, "capacityText", Label(loadout, "Capacity", "0 / 300 t", 518, 38, 470, 43, 30, TextAlignmentOptions.MidlineRight));
        Box(loadout, "CapacityTrack", 24, 97, 964, 48, Background, true);
        var fill = Box(loadout, "CapacityFill", 30, 103, 952, 36, Cyan);
        fill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SB/Art/Sprites/UI/CargoCapacityFill.png");
        fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0;
        Ref(controller, "capacityFill", fill);
        var reward = Box(loadout, "RewardPreview", 24, 168, 964, 145, Background, true).transform;
        Label(reward, "Heading", "예상 획득 재화", 28, 12, 430, 33, 25);
        Label(reward, "Estimate", "기본 시세 x1 · 장비 효과 포함", 426, 14, 510, 28, 17, TextAlignmentOptions.MidlineRight, Muted);
        string[] labels = { "골드", "다이아", "에메랄드" };
        string[] fields = { "goldRewardText", "diamondRewardText", "emeraldRewardText" };
        for (int i = 0; i < 3; i++)
        {
            Icon(reward, "Icon" + i, currencyIcons[i].sprite, currencyIcons[i].color, 31 + i * 314, 67, 42, 42);
            var value = Label(reward, "Reward" + i, "0", 92 + i * 314, 55, 210, 39, 28);
            Autosize(value, 18, 28); Ref(controller, fields[i], value);
            Label(reward, "Name" + i, labels[i], 92 + i * 314, 96, 210, 28, 22, TextAlignmentOptions.MidlineLeft, Muted);
            if (i > 0) Box(reward, "Divider" + i, i * 314, 59, 1, 60, Edge);
        }
        Hide(loadout, "EstimateHint"); Hide(loadout, "SelectedHeading"); Hide(loadout, "SelectedHint"); Hide(loadout, "StackHint");
        DestroyOwned(loadout, "SelectedCargoSlots");
        var selected = Scroll(loadout, "SelectedScroll", 24, 332, 964, 550, 6, new Vector2(148, 264), new Vector2(12, 22));
        Ref(controller, "selectedScroll", selected);
        Ref(controller, "selectedContent", selected.content);

        var available = Box(panel, "AvailableCargo", 32, 1168, 1016, 478, Section, true).transform;
        DestroyOwned(available, "AvailableCargo0"); DestroyOwned(available, "AvailableCargo1");
        Label(available, "Heading", "보유 화물", 24, 18, 390, 38, 26);
        var hint = Label(available, "Hint", "적재할 화물을 선택하세요. (최대 300 t)", 370, 18, 618, 38, 21, TextAlignmentOptions.MidlineRight, Cyan);
        Autosize(hint, 18, 21); Ref(controller, "catalogHintText", hint);
        var inventory = Scroll(available, "InventoryScroll", 22, 72, 972, 384, 5, new Vector2(178, 380), new Vector2(15, 24));
        Ref(controller, "availableScroll", inventory); Ref(controller, "availableContent", inventory.content);
        Ref(controller, "statusText", Label(panel, "Status", "", 38, 1659, 1004, 29, 18, TextAlignmentOptions.Center, Cyan));

        var footer = Box(panel, "Footer", 0, 1700, 1080, 220, Hex("052B33")).transform;
        Button(footer, "CloseButton", "닫기", 42, 43, 320, 116, controller.Close, Background, Edge, 29);
        var confirm = Button(footer, "ConfirmButton", "적재완료/보스 도전", 390, 43, 648, 116, controller.ConfirmBossChallenge, Hex("068085"), Cyan, 29);
        Ref(controller, "confirmButton", confirm);
        Hide(footer, "RememberHint");

        var picker = Box(panel, "CargoPicker", 0, 0, 1080, 1920, new Color(0, 0, 0, .78f)).transform;
        Stretch(picker); picker.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        var choice = Box(picker, "ChoicePanel", 24, 550, 1032, 670, Section, true).transform;
        DestroyOwned(choice, "ChoiceCargo0"); DestroyOwned(choice, "ChoiceCargo1");
        Label(choice, "Heading", "적재할 화물을 선택하세요", 24, 22, 984, 44, 28, TextAlignmentOptions.Center);
        var pickerScroll = Scroll(choice, "InventoryScroll", 30, 92, 972, 428, 5, new Vector2(178, 380), new Vector2(15, 24));
        Ref(controller, "pickerScroll", pickerScroll); Ref(controller, "pickerContent", pickerScroll.content);
        Button(choice, "CloseButton", "닫기", 348, 559, 336, 71, controller.CloseCargoPicker, Background, Edge, 27);

        EnsureFolder("Assets/SB/Prefabs/UI");
        var selectedPrefab = CreateCardPrefab("Assets/SB/Prefabs/UI/CargoLoadoutSelectedCard.prefab", true);
        var availablePrefab = CreateCardPrefab("Assets/SB/Prefabs/UI/CargoLoadoutAvailableCard.prefab", false);
        var emptyPrefab = CreateEmptyPrefab("Assets/SB/Prefabs/UI/CargoLoadoutEmptySlot.prefab");
        Ref(controller, "selectedCardPrefab", selectedPrefab); Ref(controller, "availableCardPrefab", availablePrefab); Ref(controller, "emptySlotPrefab", emptyPrefab);
        var properties = new SerializedObject(controller); properties.FindProperty("minimumSelectedSlots").intValue = 12; properties.ApplyModifiedProperties();
        Ref(controller, "panelRoot", panel.gameObject); Ref(controller, "cargoPickerRoot", picker.gameObject);
        picker.gameObject.SetActive(false); panel.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene); AssetDatabase.SaveAssets();
        return "Cargo UI restyled to reference/fleet palette, five cargo icons assigned, card prefabs and three vertical scroll areas connected.";
    }

    private static void ConfigureCargoIcons(CargoManager manager)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SB/Art/Sprites/Cargo/CargoInventoryAtlas.png");
        if (texture == null) throw new InvalidOperationException("Import the generated cargo atlas first.");
        string[] names = { "Gold", "Diamond", "Emerald", "ShipElement", "Other" };
        for (int index = 0; index < names.Length; index++)
        {
            string path = "Assets/SB/Art/Sprites/Cargo/" + names[index] + "InventoryIcon.asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                // Standalone Sprite assets follow the existing TransportFrontRailMask authoring pattern.
                // The imported atlas texture is preserved; each asset references an explicit rectangle.
                int column = index % 3; int row = index / 3;
                sprite = Sprite.Create(texture, new Rect(column * 512 + 64, row == 0 ? 594 : 80, 420, 334), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                sprite.name = names[index] + "InventoryIcon"; AssetDatabase.CreateAsset(sprite, path);
            }
            Undo.RecordObject(manager.CargoData[index].Item, "Cargo inventory icon");
            manager.CargoData[index].Item.Icon = sprite; EditorUtility.SetDirty(manager.CargoData[index].Item);
        }
    }

    private static CargoLoadoutCardView CreateCardPrefab(string path, bool selected)
    {
        var root = new GameObject(selected ? "SelectedCargoCard" : "AvailableCargoCard", typeof(RectTransform));
        root.SetActive(false); root.layer = 5;
        float width = selected ? 148 : 178; float height = selected ? 264 : 380;
        var border = Box(root.transform, "CardBorder", 0, 0, width, height, Hex("D43EEB"));
        var card = root.AddComponent<CargoLoadoutCardView>();
        Place(root.transform, 0, 0, width, height);
        Box(root.transform, "Surface", 2, 2, width - 4, height - 4, Surface);
        var content = Node(root.transform, "Content"); Stretch(content.transform);
        Ref(card, "contentRoot", content); Ref(card, "borderImage", border);
        if (!selected) Ref(card, "nameText", Label(content.transform, "Name", "금괴 화물", 6, 12, width - 12, 32, 22, TextAlignmentOptions.Center));
        Ref(card, "icon", Icon(content.transform, "CargoIcon", null, Color.white, selected ? 14 : 17, selected ? 32 : 51, selected ? 120 : 144, selected ? 88 : 112));
        Ref(card, "amountText", Label(content.transform, "Amount", selected ? "x0" : "보유 ∞", selected ? 37 : 6, selected ? 122 : 172, selected ? 74 : width - 12, 30, 22, TextAlignmentOptions.Center));
        Weight(content.transform, "WeightIcon", selected ? 14 : 16, selected ? 163 : 209, 22, 26);
        Ref(card, "weightText", Label(content.transform, "Weight", "0 t", selected ? 43 : 49, selected ? 162 : 208, width - (selected ? 48 : 54), 29, 21, TextAlignmentOptions.MidlineLeft, Muted));
        string[] fields = { "goldText", "diamondText", "emeraldText" };
        Color[] colors = { Hex("FFD346"), Hex("75D9E5"), Hex("72F4BD") };
        for (int i = 0; i < 3; i++)
        {
            float y = selected ? 194 + i * 22 : 244 + i * 24;
            Icon(content.transform, "RewardIcon" + i, currencyIcons[i].sprite, currencyIcons[i].color, 12, y, 20, 20);
            var amount = Label(content.transform, "Reward" + i, "0", 39, y - 2, width - 47, 25, selected ? 16 : 18, TextAlignmentOptions.MidlineLeft, colors[i]);
            Autosize(amount, selected ? 11 : 13, selected ? 16 : 18); Ref(card, fields[i], amount);
        }
        if (selected)
        {
            Ref(card, "unloadButton", Button(content.transform, "DecreaseButton", "−", 5, 122, 31, 30, card.SubtractCargo, Surface, Edge, 24));
            Ref(card, "loadButton", Button(content.transform, "IncreaseButton", "+", 112, 122, 31, 30, card.AddCargo, Surface, Edge, 24));
            Ref(card, "removeButton", Button(content.transform, "RemoveButton", "×", 109, 6, 32, 30, card.RemoveCargo, Background, Edge, 25));
        }
        else
        {
            var load = Button(content.transform, "LoadButton", "적재", 12, 324, width - 24, 44, card.AddCargo, Hex("093742"), Hex("D43EEB"), 23);
            Ref(card, "loadButton", load); Ref(card, "loadButtonSurface", load.targetGraphic); Ref(card, "loadButtonOutline", load.GetComponent<UnityEngine.UI.Outline>());
        }
        ConfigureRepeatCard(card);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
        return prefab.GetComponent<CargoLoadoutCardView>();
    }

    private static UnityEngine.UI.Button CreateEmptyPrefab(string path)
    {
        var root = new GameObject("EmptyCargoSlot", typeof(RectTransform)); root.SetActive(false); root.layer = 5; Place(root.transform, 0, 0, 148, 264);
        var image = root.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.035f, .095f, .12f, .24f);
        var button = root.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; SetButtonColors(button);
        var border = Node(root.transform, "DashedBorder"); Stretch(border.transform);
        var graphic = border.AddComponent<CargoSlotBorderGraphic>(); graphic.color = Hex("365865"); graphic.raycastTarget = false;
        Label(root.transform, "Label", "+", 0, 0, 148, 264, 47, TextAlignmentOptions.Center, Hex("768996"));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); return prefab.GetComponent<UnityEngine.UI.Button>();
    }

    private static UnityEngine.UI.ScrollRect Scroll(Transform parent, string name, float x, float y, float width, float height, int columns, Vector2 cellSize, Vector2 spacing)
    {
        var root = Node(parent, name); Place(root.transform, x, y, width, height);
        var scroll = root.GetComponent<UnityEngine.UI.ScrollRect>() ?? Undo.AddComponent<UnityEngine.UI.ScrollRect>(root);
        var viewport = Box(root.transform, "Viewport", 0, 0, width, height, new Color(1, 1, 1, .005f));
        viewport.raycastTarget = true; Stretch(viewport.transform);
        var mask = viewport.GetComponent<UnityEngine.UI.Mask>() ?? Undo.AddComponent<UnityEngine.UI.Mask>(viewport.gameObject); mask.showMaskGraphic = false;
        var content = Node(viewport.transform, "Content"); var rect = (RectTransform)content.transform;
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(0, cellSize.y);
        var grid = content.GetComponent<UnityEngine.UI.GridLayoutGroup>() ?? Undo.AddComponent<UnityEngine.UI.GridLayoutGroup>(content);
        grid.cellSize = cellSize; grid.spacing = spacing; grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = columns; grid.childAlignment = TextAnchor.UpperLeft;
        var fitter = content.GetComponent<UnityEngine.UI.ContentSizeFitter>() ?? Undo.AddComponent<UnityEngine.UI.ContentSizeFitter>(content);
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.rectTransform; scroll.content = rect; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 45; scroll.inertia = true; scroll.decelerationRate = .12f;
        var barImage = Box(root.transform, "Scrollbar", width - 8, 0, 6, height, Hex("0F2930"));
        barImage.raycastTarget = true;
        var bar = barImage.GetComponent<UnityEngine.UI.Scrollbar>() ?? Undo.AddComponent<UnityEngine.UI.Scrollbar>(barImage.gameObject);
        bar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        var slide = Node(bar.transform, "SlidingArea"); Stretch(slide.transform);
        var handle = Box(slide.transform, "Handle", 0, 0, 6, height, Hex("4C9EA3")); Stretch(handle.transform);
        handle.raycastTarget = true;
        bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
        scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }

    private static GameObject Node(Transform parent, string name)
    {
        var existing = parent.Find(name); if (existing != null) { existing.gameObject.SetActive(true); return existing.gameObject; }
        var root = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(root, "Cargo UI"); root.layer = 5; root.transform.SetParent(parent, false); return root;
    }
    private static void Place(Transform transform, float x, float y, float width, float height)
    { var rect = (RectTransform)transform; Undo.RecordObject(rect, "Cargo layout"); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); rect.localScale = Vector3.one; }
    private static void Stretch(Transform transform)
    { var rect = (RectTransform)transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
    private static UnityEngine.UI.Image Box(Transform parent, string name, float x, float y, float width, float height, Color color, bool border = false)
    {
        var root = Node(parent, name); Place(root.transform, x, y, width, height); var image = root.GetComponent<UnityEngine.UI.Image>() ?? Undo.AddComponent<UnityEngine.UI.Image>(root);
        image.color = color; image.raycastTarget = false;
        var oldOutline = root.GetComponent<UnityEngine.UI.Outline>(); if (oldOutline != null) Undo.DestroyObjectImmediate(oldOutline);
        if (border)
        {
            Box(root.transform, "TopEdge", 0, 0, width, 1, Edge); Box(root.transform, "BottomEdge", 0, height - 1, width, 1, Edge);
            Box(root.transform, "LeftEdge", 0, 0, 1, height, Edge); Box(root.transform, "RightEdge", width - 1, 0, 1, height, Edge);
        }
        return image;
    }
    private static UnityEngine.UI.Image Icon(Transform parent, string name, Sprite sprite, Color color, float x, float y, float width, float height)
    { var image = Box(parent, name, x, y, width, height, color); image.sprite = sprite; image.preserveAspect = true; return image; }
    private static TMP_Text Label(Transform parent, string name, string text, float x, float y, float width, float height, float size, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft, Color? color = null)
    {
        var root = Node(parent, name); Place(root.transform, x, y, width, height); var label = root.GetComponent<TMP_Text>() ?? Undo.AddComponent<TextMeshProUGUI>(root);
        label.font = font; label.fontSharedMaterial = font.material; label.fontSize = size; label.enableAutoSizing = false; label.text = text; label.alignment = alignment; label.color = color ?? TextColor;
        label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Overflow; return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string name, string text, float x, float y, float width, float height, UnityEngine.Events.UnityAction action, Color color, Color borderColor, float fontSize)
    {
        var surface = Box(parent, name, x, y, width, height, color); surface.raycastTarget = true;
        var outline = surface.gameObject.AddComponent<UnityEngine.UI.Outline>(); outline.effectColor = borderColor; outline.effectDistance = new Vector2(1, -1);
        var button = surface.GetComponent<UnityEngine.UI.Button>() ?? Undo.AddComponent<UnityEngine.UI.Button>(surface.gameObject); button.targetGraphic = surface;
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); UnityEventTools.AddPersistentListener(button.onClick, action); SetButtonColors(button);
        Label(surface.transform, "Label", text, 0, 0, width, height, fontSize, TextAlignmentOptions.Center); return button;
    }
    private static void SetButtonColors(UnityEngine.UI.Button button)
    { var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.86f, 1, .94f); colors.pressedColor = new Color(.65f, .8f, .75f); colors.disabledColor = new Color(.46f, .5f, .5f, .7f); colors.fadeDuration = .12f; button.colors = colors; }
    private static void Weight(Transform parent, string name, float x, float y, float width, float height)
    { var root = Node(parent, name); Place(root.transform, x, y, width, height); var graphic = root.AddComponent<CargoWeightGraphic>(); graphic.color = Hex("7D8C99"); graphic.raycastTarget = false; }
    private static void Autosize(TMP_Text text, float min, float max) { text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = max; }
    private static void Ref(Object target, string field, Object assigned)
    { var serialized = new SerializedObject(target); var property = serialized.FindProperty(field); if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + field); property.objectReferenceValue = assigned; serialized.ApplyModifiedProperties(); }
    private static void Hide(Transform parent, string name) { var child = parent.Find(name); if (child != null) child.gameObject.SetActive(false); }
    private static void DestroyOwned(Transform parent, string name) { var child = parent.Find(name); if (child != null) Undo.DestroyObjectImmediate(child.gameObject); }
    private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }
    private static void EnsureFolder(string path)
    { if (AssetDatabase.IsValidFolder(path)) return; int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash)); AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1)); }
}
