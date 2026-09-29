using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SB.Scripts;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static class StageCardPlayReview
{
    public static async Task<string> Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var windowType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var window = EditorWindow.GetWindow(windowType);
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var selectedSize = windowType.GetProperty("selectedSizeIndex", flags);
        int previousSize = (int)selectedSize.GetValue(window);
        selectedSize.SetValue(window, 9);
        window.Repaint();
        var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowManager>();
        var view = UnityEngine.Object.FindFirstObjectByType<StageInfoBarView>();
        var boss = view.transform.Find("BossButton").GetComponent<UnityEngine.UI.Button>();
        try
        {
            Time.timeScale = 1f;
            flow.StartStage();
            await Task.Delay(700);
            RequirePlaying();
            Canvas.ForceUpdateCanvases();
            ScreenCapture.CaptureScreenshot("QA/CompactStageCardPirate.png");
            await Task.Delay(250);

            var rect = (RectTransform)boss.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0 || hits[0].gameObject != boss.gameObject)
                throw new InvalidOperationException("The boss button is obstructed.");
            ExecuteEvents.Execute(boss.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            await Task.Delay(200);
            RequirePlaying();
            bool bossStateVerified = !boss.interactable && boss.transform.Find("Label").GetComponent<TMP_Text>().text == "전투 중";
            if (!bossStateVerified) throw new InvalidOperationException("Boss button state did not update.");
            ScreenCapture.CaptureScreenshot("QA/CompactStageCardBoss.png");
            await Task.Delay(250);

            // Preview the transport presentation without changing persistent game data.
            flow.GetType().GetField("currentChapter", flags).SetValue(flow, 2);
            flow.GetType().GetField("currentStage", flags).SetValue(flow, 3);
            flow.StartStage();
            var cargo = UnityEngine.Object.FindFirstObjectByType<CargoManager>();
            view.AddCargoItems(cargo.CargoData.Where(c => c.Item != null)
                .Select(c => new CargoData { Item = c.Item, Amount = 12 }).ToArray());
            await Task.Delay(300);
            RequirePlaying();
            Canvas.ForceUpdateCanvases();
            var overflowing = view.GetComponentsInChildren<TMP_Text>().Where(t => t.isTextOverflowing).Select(t => t.name).ToArray();
            if (overflowing.Length > 0) throw new InvalidOperationException("Text overflow: " + string.Join(", ", overflowing));
            if (boss.gameObject.activeSelf || !view.transform.Find("TransportCargo").gameObject.activeSelf)
                throw new InvalidOperationException("Transport visibility did not update.");
            ScreenCapture.CaptureScreenshot("QA/CompactStageCardTransport.png");
            await Task.Delay(300);
            return "360x640: pirate, boss and transport frames captured; boss hit target and disabled battle state verified; transport cargo/progress visible; no text overflow.";
        }
        finally
        {
            selectedSize.SetValue(window, previousSize);
            window.Repaint();
        }
    }

    private static void RequirePlaying()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode was stopped during verification.");
    }
}
