using System;
using SB.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only authoring recipe; layout and styles are saved on the existing scene objects.
public static class ModernStageCardLayout
{
    private static TMP_FontAsset font;
    private static Material textMaterial;
    private static Sprite roundedSprite;
    private static readonly Color White = new Color(.95f, .97f, 1f, 1f);
    private static readonly Color Muted = new Color(.71f, .78f, .83f, 1f);
    private static readonly Color Mint = new Color(.40f, .86f, .74f, 1f);

    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Apply in Edit Mode.");
        var root = GameObject.Find("Canvas/GameFrame/BattleArea/StageInfoBar");
        if (root == null) throw new InvalidOperationException("StageInfoBar was not found.");
        var view = root.GetComponent<StageInfoBarView>();
        var title = root.transform.Find("stageText").GetComponent<TMP_Text>();
        var detail = root.transform.Find("VoyageDetail").GetComponent<TMP_Text>();
        var progress = root.transform.Find("StageProgress").GetComponent<StageProgressGraphic>();
        var boss = root.transform.Find("BossButton").GetComponent<UnityEngine.UI.Button>();
        var cargo = root.transform.Find("currentCagoItemCheckArea") ?? root.transform.Find("TransportCargo/currentCagoItemCheckArea");
        Undo.RegisterFullObjectHierarchyUndo(root, "Modern stage card");
        font = title.font;
        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        textMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SB/Art/Materials/UI/StageInfoText.mat");
        Undo.RecordObject(textMaterial, "Stage card typography");
        textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        textMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, .04f);
        textMaterial.DisableKeyword("OUTLINE_ON");
        textMaterial.DisableKeyword("UNDERLAY_ON");
        EditorUtility.SetDirty(textMaterial);

        var header = (RectTransform)root.transform;
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -104f);
        header.sizeDelta = new Vector2(-96f, 140f);
        Surface(root.GetComponent<UnityEngine.UI.Image>(), new Color(.055f, .095f, .14f, .97f), .40f);
        root.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        root.transform.Find("Edge").gameObject.SetActive(false);
        var cardShadow = root.GetComponent<UnityEngine.UI.Shadow>() ?? Undo.AddComponent<UnityEngine.UI.Shadow>(root);
        cardShadow.enabled = true;
        cardShadow.effectColor = new Color(.02f, .04f, .07f, .18f);
        cardShadow.effectDistance = new Vector2(0f, -4f);

        var routeBadge = Node(root.transform, "RouteBadge");
        Left((RectTransform)routeBadge.transform, 32f, 17f, 148f, 32f);
        Surface(routeBadge.GetComponent<UnityEngine.UI.Image>(), new Color(.12f, .23f, .27f), .8f);
        var routeNumber = Label(routeBadge.transform, "RouteNumber", "항로 01", 22f, Mint, FontStyles.Bold);
        Fill(routeNumber.rectTransform, 8f);
        routeNumber.alignment = TextAlignmentOptions.Center;
        Ref(view, "routeNumberText", routeNumber);

        Left(title.rectTransform, 198f, 4f, 438f, 56f);
        Style(title, 42f, White, FontStyles.Bold);
        title.text = "해역 미리보기";
        Left(detail.rectTransform, 32f, 60f, 604f, 36f);
        Style(detail, 26f, Muted, FontStyles.Normal);
        detail.text = "항해를 시작하면 해역 정보가 표시됩니다";
        Ref(view, "routeText", title);
        Ref(view, "voyageText", detail);

        Left(progress.rectTransform, 32f, 108f, 192f, 24f);
        var progressSettings = new SerializedObject(progress);
        progressSettings.FindProperty("diameter").floatValue = 24f;
        progressSettings.FindProperty("spacing").floatValue = 18f;
        progressSettings.FindProperty("ringWidth").floatValue = 3f;
        progressSettings.FindProperty("horizontalAlignment").floatValue = 0f;
        progressSettings.FindProperty("ringColor").colorValue = new Color(.33f, .46f, .51f, 1f);
        progressSettings.FindProperty("progressColor").colorValue = Mint;
        progressSettings.ApplyModifiedProperties();
        progress.raycastTarget = false;
        var progressCaption = Label(root.transform, "ProgressCaption", "1 / 5 구간", 25f, Mint, FontStyles.Normal);
        Left(progressCaption.rectTransform, 246f, 101f, 240f, 34f);
        Ref(view, "progressText", progressCaption);

        var transport = Node(root.transform, "TransportCargo");
        Right((RectTransform)transport.transform, 32f, 6f, 256f, 128f);
        var cargoLabel = Label(transport.transform, "CargoLabel", "운송 중인 화물", 23f, Muted, FontStyles.Normal);
        Left(cargoLabel.rectTransform, 0f, 0f, 256f, 32f);
        cargoLabel.alignment = TextAlignmentOptions.Center;
        Undo.SetTransformParent(cargo, transport.transform, "Stage cargo card grouping");
        Left((RectTransform)cargo, 0f, 42f, 256f, 72f);
        var cargoLayout = cargo.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        cargoLayout.padding = new RectOffset(0, 0, 0, 0);
        cargoLayout.spacing = 16f;
        cargoLayout.childAlignment = TextAnchor.MiddleCenter;
        cargoLayout.childForceExpandHeight = cargoLayout.childForceExpandWidth = false;
        Ref(view, "transportCargoGroup", transport);

        Right((RectTransform)boss.transform, 32f, 12f, 256f, 80f);
        var bossImage = boss.GetComponent<UnityEngine.UI.Image>();
        Surface(bossImage, Mint, .65f);
        bossImage.raycastTarget = true;
        foreach (var effect in boss.GetComponents<UnityEngine.UI.Shadow>()) effect.enabled = false;
        boss.targetGraphic = bossImage;
        var colors = boss.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(.87f, 1f, .97f, 1f);
        colors.pressedColor = new Color(.72f, .86f, .83f, 1f);
        colors.disabledColor = new Color(.65f, .75f, .74f, 1f);
        colors.fadeDuration = .10f;
        boss.colors = colors;
        var bossLabel = boss.transform.Find("Label").GetComponent<TMP_Text>();
        Fill(bossLabel.rectTransform, 8f);
        Style(bossLabel, 33f, new Color(.035f, .12f, .16f, 1f), FontStyles.Bold);
        bossLabel.alignment = TextAlignmentOptions.Center;
        var bossHint = Label(boss.transform, "Hint", "승리 시 운송 항로 개방", 21f, Muted, FontStyles.Normal);
        Left(bossHint.rectTransform, -8f, 90f, 272f, 30f);
        bossHint.alignment = TextAlignmentOptions.Center;
        Ref(view, "bossChallengeButton", boss);
        Ref(view, "bossButtonText", bossLabel);
        Ref(view, "bossHintText", bossHint);

        view.SetStageInfoBarMode(StageInfoBarMode.Odd);
        view.SetProgress(0, 5, false);
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        AssetDatabase.SaveAssets();
        return "Saved compact 140-unit stage card, down from 180, preserving the progress dot size.";
    }

    private static GameObject Node(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var created = new GameObject(name, typeof(RectTransform));
        created.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(created, "Stage card element");
        created.transform.SetParent(parent, false);
        return created;
    }

    private static TMP_Text Label(Transform parent, string name, string content, float size, Color color, FontStyles style)
    {
        var node = Node(parent, name);
        var text = node.GetComponent<TMP_Text>() ?? Undo.AddComponent<TextMeshProUGUI>(node);
        text.font = font;
        text.text = content;
        Style(text, size, color, style);
        return text;
    }

    private static void Style(TMP_Text text, float size, Color color, FontStyles style)
    {
        text.fontSharedMaterial = textMaterial;
        text.fontSize = size;
        text.fontStyle = style;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = color;
        text.margin = Vector4.zero;
        text.raycastTarget = false;
        text.UpdateMeshPadding();
    }

    private static void Surface(UnityEngine.UI.Image image, Color color, float pixelsPerUnitMultiplier)
    {
        image.enabled = true;
        image.sprite = roundedSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        image.color = color;
        image.raycastTarget = false;
    }

    private static void Left(RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Right(RectTransform rect, float right, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-right, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Center(RectTransform rect, float x, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Fill(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = new Vector2(inset, 0f);
        rect.offsetMax = new Vector2(-inset, 0f);
    }

    private static void Ref(UnityEngine.Object target, string field, UnityEngine.Object reference)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = reference;
        serialized.ApplyModifiedProperties();
    }
}
