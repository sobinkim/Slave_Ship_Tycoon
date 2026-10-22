using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB.Scripts;

public static class PreviewCreatureChargeCue
{
    public static string Main()
    {
        var preview = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(1600, 400, 24);
        var originalTarget = RenderTexture.active;
        Texture2D image = null;
        Camera camera = null;
        try
        {
            var cameraObject = new GameObject("Charge cue art preview");
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            camera = cameraObject.AddComponent<Camera>();
            camera.scene = preview;
            camera.orthographic = true;
            camera.orthographicSize = 0.85f;
            camera.transform.position = new Vector3(2.55f, 0.06f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.23f, 0.56f, 0.62f, 1f);
            camera.targetTexture = target;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SB/Prefabs/SeaCreatures/ReefbackLeviathan.prefab");
            float[] checkpoints = { 0f, 0.48f, 0.85f, 1f };
            for (int index = 0; index < checkpoints.Length; index++)
            {
                var creature = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
                creature.transform.position = new Vector3(index * 1.7f, 0f, 0f);
                foreach (var feedback in creature.GetComponentsInChildren<ShipHullFeedback>(true))
                {
                    var healthBar = new SerializedObject(feedback).FindProperty("healthBar").objectReferenceValue as GameObject;
                    if (healthBar != null) healthBar.SetActive(false);
                }
                var presentation = creature.GetComponent<SeaCreatureSurfacePresentation>();
                presentation.ResetPresentation(Vector2.left);
                // Authoring preview of the poses, without starting game simulation.
                presentation.TickPresentation(SeaCreatureState.Cruising, 1f / 60f, 0f, Vector2.zero, Vector2.left, 0f);
                for (int frame = 1; frame <= Mathf.RoundToInt(checkpoints[index] * 39f); frame++)
                    presentation.TickPresentation(SeaCreatureState.WindingUp, 1f / 60f, 0f, Vector2.zero, Vector2.left, frame / 39f);
                if (index == 3)
                    presentation.TickPresentation(SeaCreatureState.Charging, 1f / 60f, 0f, Vector2.left * 5.5f, Vector2.left, 0f);
            }
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            string folder = Path.GetFullPath("Artifacts/SeaCreatures");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "ChargeCueAuthoringPreview.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            return path;
        }
        finally
        {
            RenderTexture.active = originalTarget;
            if (camera != null) camera.targetTexture = null;
            if (image != null) Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }
}
