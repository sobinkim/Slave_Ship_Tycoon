using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Visual
{
    [Serializable]
    public class FloatingTextViewData
    {
        public FloatingTextType textType;
        public FloatingTextView viewPrefab;
        public string prefix;
        public string suffix;

        public string GetMessage(int value)
        {
            return $"{prefix}{value}{suffix}";
        }
    }

    public class FloatingTextManager : MonoBehaviour
    {
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform textRoot;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private FloatingTextViewData[] viewDatas = Array.Empty<FloatingTextViewData>();

        private RectTransform canvasRectTransform;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            Bus<FloatingTextRequestedEvent>.OnEvent += HandleFloatingTextRequested;
        }

        private void OnDisable()
        {
            Bus<FloatingTextRequestedEvent>.OnEvent -= HandleFloatingTextRequested;
        }

        private void HandleFloatingTextRequested(FloatingTextRequestedEvent evt)
        {
            ResolveReferences();

            if (PoolingManager.Instance == null)
            {
                Debug.LogError($"{nameof(FloatingTextManager)} needs a {nameof(PoolingManager)} in the scene.", this);
                return;
            }

            if (textRoot == null || canvasRectTransform == null)
            {
                Debug.LogError($"{nameof(FloatingTextManager)} needs a canvas root.", this);
                return;
            }

            if (!TryGetViewData(evt.TextType, out FloatingTextViewData viewData) || viewData.viewPrefab == null)
            {
                Debug.LogWarning($"Floating text view not found. Type: {evt.TextType}", this);
                return;
            }

            if (!TryWorldToAnchoredPosition(evt.WorldPosition, out Vector2 anchoredPosition))
                return;

            FloatingTextView view = GetView(viewData.viewPrefab);

            if (view != null)
                view.Play(viewData.GetMessage(evt.Value), anchoredPosition);
        }

        private FloatingTextView GetView(FloatingTextView viewPrefab)
        {
            return PoolingManager.Instance.Get(
                viewPrefab,
                Vector3.zero,
                Quaternion.identity,
                textRoot
            );
        }

        private bool TryGetViewData(FloatingTextType textType, out FloatingTextViewData viewData)
        {
            if (viewDatas != null)
            {
                for (int i = 0; i < viewDatas.Length; i++)
                {
                    FloatingTextViewData candidate = viewDatas[i];
                    if (candidate != null && candidate.textType == textType)
                    {
                        viewData = candidate;
                        return true;
                    }
                }
            }

            viewData = null;
            return false;
        }

        private bool TryWorldToAnchoredPosition(Vector3 worldPosition, out Vector2 anchoredPosition)
        {
            Camera targetCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera;
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPosition,
                targetCamera,
                out anchoredPosition
            );
        }

        private void ResolveReferences()
        {
            if (targetCanvas == null)
                targetCanvas = GetComponentInParent<Canvas>();

            if (textRoot == null)
                textRoot = transform as RectTransform;

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (targetCanvas != null)
                canvasRectTransform = targetCanvas.transform as RectTransform;
        }
    }
}
