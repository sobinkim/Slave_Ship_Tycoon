using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class ShipWaterContact : MonoBehaviour
    {
        [Header("Explicit references")]
        [SerializeField] private Ship owner;
        [SerializeField] private SpriteRenderer contactRenderer;

        [Header("Hull at the water surface")]
        [SerializeField] private SpriteRenderer hullRenderer;
        [SerializeField, Tooltip("Ship 원점 기준 수면 높이입니다. 선체마다 실제 바닥에 맞춰 지정합니다.")]
        private float surfaceLocalY = -0.45f;
        [SerializeField, Min(0.001f), Tooltip("물과 맞닿은 선체에 물빛이 번지는 월드 거리입니다.")]
        private float contactTintDepth = 0.055f;
        [SerializeField, Min(0.001f), Tooltip("수면 아래 선체가 사라지는 월드 거리입니다.")]
        private float edgeFadeDepth = 0.025f;
        [SerializeField, Min(0f), Tooltip("수면 경계의 작은 물결 높이입니다. 월드 거리 기준입니다.")]
        private float surfaceWaveHeight = 0.008f;
        [SerializeField] private Color waterTint = new Color(0.2f, 0.52f, 0.57f, 0.55f);

        [Header("Death fade")]
        [SerializeField, Min(0.01f), Tooltip("함선 사망 후 물 접촉 표시가 사라지는 시간입니다.")]
        private float fadeOutSeconds = 0.3f;

        private Color originalTint;
        private float visibility;
        private bool isInitialized;
        private bool hasSurfaceShader;
        private MaterialPropertyBlock hullProperties;
        private static readonly int SurfaceSubmersionId = Shader.PropertyToID("_SurfaceSubmersion");
        private static readonly int SurfacePlaneId = Shader.PropertyToID("_SurfacePlane");
        private static readonly int SurfaceParamsId = Shader.PropertyToID("_SurfaceParams");
        private static readonly int SurfaceTintId = Shader.PropertyToID("_SurfaceTint");

        private void Awake()
        {
            if (owner == null || contactRenderer == null)
            {
                Debug.LogError("ShipWaterContact requires a ship and contact renderer.", this);
                enabled = false;
                return;
            }

            originalTint = contactRenderer.color;
            hasSurfaceShader = hullRenderer != null && hullRenderer.sharedMaterial != null
                && hullRenderer.sharedMaterial.HasProperty(SurfacePlaneId);
            if (hasSurfaceShader) hullProperties = new MaterialPropertyBlock();
            isInitialized = true;
            ApplyHullSurface(true);
        }

        private void OnEnable()
        {
            RestoreContact();
        }

        private void LateUpdate()
        {
            visibility = owner.IsDead
                ? Mathf.MoveTowards(visibility, 0f, Time.deltaTime / Mathf.Max(0.01f, fadeOutSeconds))
                : 1f;
            ApplyVisibility();
            ApplyHullSurface(true);
        }

        private void OnDisable()
        {
            HideAndReset();
            ApplyHullSurface(false);
        }

        private void ApplyHullSurface(bool visible)
        {
            if (!hasSurfaceShader || hullRenderer == null || owner == null) return;

            // A horizontal sea surface stays level while the illustrated hull rolls and heaves.
            Transform hull = hullRenderer.transform;
            float height = owner.transform.TransformPoint(new Vector3(0f, surfaceLocalY, 0f)).y;
            var plane = new Vector4(hull.TransformVector(Vector3.right).y,
                hull.TransformVector(Vector3.up).y, hull.position.y - height, 0f);
            hullRenderer.GetPropertyBlock(hullProperties);
            hullProperties.SetFloat(SurfaceSubmersionId, visible ? 1f : 0f);
            hullProperties.SetVector(SurfacePlaneId, plane);
            hullProperties.SetVector(SurfaceParamsId,
                new Vector4(contactTintDepth, edgeFadeDepth, surfaceWaveHeight, 0f));
            hullProperties.SetColor(SurfaceTintId, waterTint);
            hullRenderer.SetPropertyBlock(hullProperties);
        }

        private void RestoreContact()
        {
            if (!isInitialized)
                return;

            visibility = owner.IsDead ? 0f : 1f;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            Color tint = originalTint;
            tint.a *= visibility;
            contactRenderer.color = tint;
            contactRenderer.enabled = visibility > 0f;
        }

        private void HideAndReset()
        {
            visibility = 0f;
            if (contactRenderer == null)
                return;

            if (isInitialized)
                contactRenderer.color = originalTint;
            contactRenderer.enabled = false;
        }
    }
}
