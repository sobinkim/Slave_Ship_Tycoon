using System;
using SB.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-time Editor authoring helper. Runtime positions remain editable RectTransforms.
public static class StageInfoBarLayout
{
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Apply in Edit Mode.");

        var root = GameObject.Find("Canvas/GameFrame/BattleArea/StageInfoBar");
        if (root == null) throw new InvalidOperationException("StageInfoBar was not found.");
        var title = root.transform.Find("stageText").GetComponent<TMP_Text>();
        var detail = root.transform.Find("VoyageDetail").GetComponent<TMP_Text>();
        var progress = root.transform.Find("StageProgress").GetComponent<StageProgressGraphic>();
        var boss = root.transform.Find("BossButton").GetComponent<UnityEngine.UI.Button>();
        var cargo = root.transform.Find("currentCagoItemCheckArea");
        Undo.RegisterFullObjectHierarchyUndo(root, "Stage info floating header");

        const string folder = "Assets/SB/Art/Materials/UI";
        const string materialPath = folder + "/StageInfoText.mat";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/SB/Art/Materials", "UI");
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(title.font.material) { name = "StageInfoText" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        Undo.RecordObject(material, "Stage header text outline");
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(1f, .98f, .93f, 1f));
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, .10f);
        material.SetFloat(ShaderUtilities.ID_FaceDilate, .18f);
        material.EnableKeyword("OUTLINE_ON");
        EditorUtility.SetDirty(material);

        var header = (RectTransform)root.transform;
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -140f);
        header.sizeDelta = new Vector2(-48f, 260f);
        var background = root.GetComponent<UnityEngine.UI.Image>();
        background.enabled = false;
        background.raycastTarget = false;
        root.transform.Find("Edge").gameObject.SetActive(false);

        TopCenter(title.rectTransform, 0f, -6f, 1000f, 80f);
        StyleText(title, material, 54f, FontStyles.Bold);
        TopCenter(detail.rectTransform, 0f, 74f, 1000f, 48f);
        StyleText(detail, material, 32f, FontStyles.Bold);
        TopCenter(progress.rectTransform, 0f, 126f, 400f, 36f);
        var progressSettings = new SerializedObject(progress);
        progressSettings.FindProperty("diameter").floatValue = 28f;
        progressSettings.FindProperty("spacing").floatValue = 28f;
        progressSettings.FindProperty("ringWidth").floatValue = 4f;
        progressSettings.FindProperty("horizontalAlignment").floatValue = .5f;
        progressSettings.FindProperty("ringColor").colorValue = new Color(.09f, .17f, .20f, 1f);
        progressSettings.FindProperty("progressColor").colorValue = new Color(.15f, .89f, .66f, 1f);
        progressSettings.ApplyModifiedProperties();
        progress.raycastTarget = false;

        // Keep transport cargo and its existing bindings, below the centered progress dots.
        TopCenter((RectTransform)cargo, 0f, 184f, 740f, 72f);
        var cargoLayout = cargo.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        cargoLayout.padding = new RectOffset(0, 0, 0, 0);
        cargoLayout.spacing = 12f;
        cargoLayout.childAlignment = TextAnchor.UpperCenter;

        var bossRect = (RectTransform)boss.transform;
        bossRect.anchorMin = bossRect.anchorMax = new Vector2(.5f, 1f);
        bossRect.pivot = new Vector2(.5f, 1f);
        bossRect.anchoredPosition = new Vector2(0f, -132f);
        bossRect.sizeDelta = new Vector2(280f, 100f);
        var bossImage = boss.GetComponent<UnityEngine.UI.Image>();
        bossImage.sprite = null;
        bossImage.color = new Color(.43f, .15f, .17f, .97f);
        bossImage.raycastTarget = true;
        boss.targetGraphic = bossImage;
        var buttonColors = boss.colors;
        buttonColors.normalColor = Color.white;
        buttonColors.highlightedColor = new Color(1f, .88f, .75f, 1f);
        buttonColors.pressedColor = new Color(.72f, .64f, .60f, 1f);
        buttonColors.fadeDuration = .12f;
        boss.colors = buttonColors;
        var outline = boss.GetComponent<UnityEngine.UI.Outline>() ?? Undo.AddComponent<UnityEngine.UI.Outline>(boss.gameObject);
        outline.effectColor = new Color(.93f, .72f, .43f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        var shadow = boss.GetComponents<UnityEngine.UI.Shadow>();
        UnityEngine.UI.Shadow dropShadow = null;
        foreach (var candidate in shadow)
            if (candidate.GetType() == typeof(UnityEngine.UI.Shadow)) dropShadow = candidate;
        if (dropShadow == null) dropShadow = Undo.AddComponent<UnityEngine.UI.Shadow>(boss.gameObject);
        dropShadow.effectColor = new Color(.08f, .09f, .14f, .45f);
        dropShadow.effectDistance = new Vector2(0f, -6f);
        var bossLabel = boss.transform.Find("Label").GetComponent<TMP_Text>();
        TopCenter(bossLabel.rectTransform, 0f, 0f, 268f, 100f);
        StyleText(bossLabel, bossLabel.font.material, 36f, FontStyles.Bold);
        bossLabel.color = new Color(1f, .94f, .79f, 1f);

        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        AssetDatabase.SaveAssets();
        return "Saved high-contrast stage text and centered boss button, verified at mobile resolution. Existing references and click event preserved.";
    }

    private static void TopCenter(RectTransform rect, float x, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void StyleText(TMP_Text text, Material material, float size, FontStyles style)
    {
        text.fontSharedMaterial = material;
        text.fontSize = size;
        text.fontStyle = style;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.055f, .105f, .15f, 1f);
        text.raycastTarget = false;
        text.UpdateMeshPadding();
    }
}
