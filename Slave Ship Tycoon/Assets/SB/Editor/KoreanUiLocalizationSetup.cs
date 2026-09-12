using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SB.Editor
{
    public static class KoreanUiLocalizationSetup
    {
        private const string SourceFontPath = "Assets/SB/Fonts/MalgunGothic.ttf";
        private const string FontAssetPath = "Assets/SB/Fonts/MalgunGothic SDF.asset";
        private const string MainScenePath = "Assets/SB/Animations/Scenes/SampleScene.unity";

        private static readonly Dictionary<string, string> TextMap = new Dictionary<string, string>
        {
            { "UPGRADE", "강화" },
            { "CARGO", "화물" },
            { "FLEET", "함대" },
            { "EQUIP", "장비" },
            { "COMMAND", "지휘" },
            { "SKILL", "스킬" },
            { "AUTO", "자동" },
            { "STAGE", "스테이지" },
            { "STAGE CLEAR", "스테이지 클리어" },
            { "SALE COMPLETE", "판매 완료" },
            { "SHIP UPGRADES", "함선 강화" },
            { "Health", "체력" },
            { "Attack Power", "공격력" },
            { "Attack Speed", "공격 속도" },
            { "Luck", "운" },
            { "Cargo capacity", "최대 적재량" },
            { "Command capacity", "지휘 게이지 최대치" },
            { "Command recovery", "지휘 게이지 회복" },
            { "TRANSPORT EQUIPMENT", "운송 장비" },
            { "FORMATION", "편성" },
            { "OWNED", "보유" },
            { "SELECT ESCORT", "호위선 선택" },
            { "SUMMON", "소환" },
            { "MERGE x3", "3개 합성" },
            { "MERGE ESCORTS", "호위선 합성" },
            { "MERGE", "합성" },
            { "CANCEL", "취소" },
            { "EMPTY", "비어 있음" },
            { "NO EQUIPMENT", "장착 장비 없음" },
            { "No escort selected", "선택한 호위선 없음" },
            { "Ship Blueprint", "함선 설계도" },
            { "Mixed Manifest", "혼합 적하 목록" },
            { "Merchant Ledger", "상인 장부" },
            { "Command Orders", "지휘 명령서" },
            { "Gold", "골드" },
            { "Diamond", "다이아" },
            { "Emerald", "에메랄드" }
        };

        [MenuItem("Tools/SB/Apply Korean UI")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Stop Play Mode before applying Korean UI.");
                return;
            }

            TMP_FontAsset fontAsset = GetOrCreateFontAsset();
            if (fontAsset == null)
                return;

            ApplyScene(fontAsset);
            ApplyPrefabs(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Korean UI font and static text have been applied.");
        }

        private static TMP_FontAsset GetOrCreateFontAsset()
        {
            TMP_FontAsset existingFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existingFontAsset != null)
                return existingFontAsset;

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"Korean source font was not found: {SourceFontPath}");
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9,
                GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogError("Could not create the Korean TMP font asset.");
                return null;
            }

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            foreach (Texture2D atlas in fontAsset.atlasTextures)
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void ApplyScene(TMP_FontAsset fontAsset)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MainScenePath)
            {
                Debug.LogWarning($"Open {MainScenePath} before applying the Korean UI.");
                return;
            }

            ApplyToTextComponents(Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), fontAsset);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyPrefabs(TMP_FontAsset fontAsset)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/SB/Prefabs" });

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                bool changed = ApplyToTextComponents(prefabRoot.GetComponentsInChildren<TMP_Text>(true), fontAsset);

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static bool ApplyToTextComponents(TMP_Text[] textComponents, TMP_FontAsset fontAsset)
        {
            bool changed = false;

            for (int i = 0; i < textComponents.Length; i++)
            {
                TMP_Text text = textComponents[i];
                if (text == null)
                    continue;

                if (text.font != fontAsset)
                {
                    text.font = fontAsset;
                    text.fontSharedMaterial = fontAsset.material;
                    changed = true;
                }

                if (TextMap.TryGetValue(text.text, out string koreanText))
                {
                    text.text = koreanText;
                    changed = true;
                }

                if (RequiresSmallerText(text.text))
                {
                    text.enableAutoSizing = true;
                    text.fontSizeMin = Mathf.Min(text.fontSizeMin, 20f);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool RequiresSmallerText(string text)
        {
            return text == "지휘 게이지 최대치" ||
                   text == "지휘 게이지 회복" ||
                   text == "장착 장비 없음" ||
                   text == "선택한 호위선 없음";
        }
    }
}
