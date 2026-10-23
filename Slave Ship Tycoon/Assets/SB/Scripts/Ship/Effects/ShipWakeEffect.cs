using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class ShipWakeEffect : MonoBehaviour
    {
        private bool sailingOverride;
        public void SetSailingOverride(bool sailing) => sailingOverride = sailing;
        private bool hasWaterPresentation;
        private Material originalWakeMaterial;
        private Material[] originalFoamMaterials;
        private Color waterTint = Color.white;
        private float waterMotionMultiplier = 1f;
        private float foamTime;
        private static readonly int wakeRoleId = Shader.PropertyToID("_WakeRole");
        private static readonly int wakeTimeId = Shader.PropertyToID("_WakeTime");
        private static readonly int wakeSeedId = Shader.PropertyToID("_WakeSeed");
        private MaterialPropertyBlock wakeProperties;

        public void SetWaterPresentation(Material material, Color tint, float motionMultiplier)
        {
            ResetWaterPresentation();
            originalWakeMaterial = wakeRenderer != null ? wakeRenderer.sharedMaterial : null;
            originalFoamMaterials = new Material[trailingFoam.Length];
            for (int i = 0; i < trailingFoam.Length; i++)
            {
                if (trailingFoam[i] == null) continue;
                originalFoamMaterials[i] = trailingFoam[i].sharedMaterial;
                if (material != null) trailingFoam[i].sharedMaterial = material;
            }
            if (wakeRenderer != null && material != null) wakeRenderer.sharedMaterial = material;
            waterTint = tint;
            waterMotionMultiplier = Mathf.Max(.01f, motionMultiplier);
            hasWaterPresentation = true;
        }

        public void ResetWaterPresentation()
        {
            if (!hasWaterPresentation) return;
            if (wakeRenderer != null) wakeRenderer.sharedMaterial = originalWakeMaterial;
            for (int i = 0; i < trailingFoam.Length; i++)
                if (trailingFoam[i] != null) trailingFoam[i].sharedMaterial = originalFoamMaterials[i];
            waterTint = Color.white;
            waterMotionMultiplier = 1f;
            hasWaterPresentation = false;
        }
        [Header("References")]
        [SerializeField] private Ship owner;
        [SerializeField] private Animator shipAnimator;
        [SerializeField] private SpriteRenderer wakeRenderer;

        [Header("Movement")]
        [SerializeField] private string moveParameter = "MOVE";
        [SerializeField, Min(0.01f)] private float fadeInSeconds = 0.2f;
        [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.4f;

        [Header("Water animation")]
        [SerializeField] private Color foamColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField, Min(0f)] private float rippleCyclesPerSecond = 2f;
        [SerializeField, Range(0f, 0.2f)] private float rippleScaleAmount = 0.035f;
        [SerializeField, Range(0f, 1f)] private float rippleOpacityAmount = 0.12f;

        [Header("Trailing foam")]
        [SerializeField, Tooltip("명시적으로 연결한 포말 조각. 배열 순서대로 위상을 나눠 뒤로 흘립니다.")]
        private SpriteRenderer[] trailingFoam = new SpriteRenderer[0];
        [SerializeField, Min(0.05f)] private float foamCycleSeconds = 0.7f;
        [SerializeField, Tooltip("물살 오브젝트 기준 포말 시작 위치")]
        private Vector2 foamStartLocalPosition = new Vector2(-0.12f, -0.03f);
        [SerializeField, Tooltip("한 주기 동안 이동할 로컬 거리. 오른쪽 진행 함선은 X를 음수로 설정합니다.")]
        private Vector2 foamTravelLocalOffset = new Vector2(-0.65f, -0.02f);
        [SerializeField] private Vector2 foamStartLocalScale = new Vector2(0.55f, 0.35f);
        [SerializeField] private Vector2 foamEndLocalScale = new Vector2(0.85f, 0.2f);
        [SerializeField, Range(0f, 1f)] private float trailingFoamOpacity = 0.6f;

        private int moveHash;
        private float visibility;
        private float rippleTime;
        private Vector3 baseScale;
        private bool isInitialized;

        private void Awake()
        {
            baseScale = transform.localScale;
            wakeProperties = new MaterialPropertyBlock();
            isInitialized = true;
            moveHash = Animator.StringToHash(moveParameter);
            if (owner == null || shipAnimator == null || wakeRenderer == null)
            {
                Debug.LogError("ShipWakeEffect requires a ship, animator and wake renderer.", this);
                enabled = false;
                return;
            }
            bool hasMoveParameter = false;
            foreach (AnimatorControllerParameter parameter in shipAnimator.parameters)
                hasMoveParameter |= parameter.nameHash == moveHash && parameter.type == AnimatorControllerParameterType.Bool;
            if (!hasMoveParameter)
            {
                Debug.LogError("ShipWakeEffect movement bool is missing from the animator.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            sailingOverride = false;
            visibility = 0f;
            rippleTime = 0f;
            foamTime = 0f;
            if (wakeRenderer != null) wakeRenderer.enabled = false;
            HideTrailingFoam();
        }

        private void LateUpdate()
        {
            bool isMoving = !owner.IsDead && shipAnimator.isActiveAndEnabled &&
                (sailingOverride || shipAnimator.GetBool(moveHash));
            float fadeSeconds = isMoving ? fadeInSeconds : fadeOutSeconds;
            visibility = Mathf.MoveTowards(visibility, isMoving ? 1f : 0f,
                Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            rippleTime += Time.deltaTime * rippleCyclesPerSecond * Mathf.PI * 2f;
            foamTime += Time.deltaTime * waterMotionMultiplier;
            float ripple = Mathf.Sin(rippleTime);
            transform.localScale = Vector3.Scale(baseScale,
                new Vector3(1f + ripple * rippleScaleAmount, 1f - ripple * rippleScaleAmount, 1f));
            Color tint = foamColor * waterTint;
            tint.a *= visibility * (1f - rippleOpacityAmount * (ripple * 0.5f + 0.5f));
            wakeRenderer.color = tint;
            wakeRenderer.enabled = visibility > 0f;
            SetWaveProperties(wakeRenderer, 0f, 0f);
            UpdateTrailingFoam();
        }

        private void SetWaveProperties(SpriteRenderer renderer, float role, float seed)
        {
            renderer.GetPropertyBlock(wakeProperties);
            wakeProperties.SetFloat(wakeRoleId, role);
            wakeProperties.SetFloat(wakeTimeId, foamTime);
            wakeProperties.SetFloat(wakeSeedId, seed);
            renderer.SetPropertyBlock(wakeProperties);
        }

        private void UpdateTrailingFoam()
        {
            for (int i = 0; i < trailingFoam.Length; i++)
            {
                SpriteRenderer foam = trailingFoam[i];
                if (foam == null) continue;
                float phase = Mathf.Repeat(foamTime / foamCycleSeconds + (float)i / trailingFoam.Length, 1f);
                Vector2 localPosition = foamStartLocalPosition + foamTravelLocalOffset * phase;
                Vector2 localScale = Vector2.Lerp(foamStartLocalScale, foamEndLocalScale, phase);
                foam.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
                foam.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
                Color tint = foamColor * waterTint;
                // 양 끝을 투명하게 만들어 위치가 되돌아오는 순간을 숨깁니다.
                tint.a *= visibility * trailingFoamOpacity * Mathf.Sin(phase * Mathf.PI);
                foam.color = tint;
                foam.enabled = visibility > 0f;
                SetWaveProperties(foam, 1f, i * 2.37f);
            }
        }

        private void HideTrailingFoam()
        {
            foreach (SpriteRenderer foam in trailingFoam)
                if (foam != null) foam.enabled = false;
        }

        private void OnDisable()
        {
            ResetWaterPresentation();
            visibility = 0f;
            if (wakeRenderer != null) wakeRenderer.enabled = false;
            HideTrailingFoam();
            if (isInitialized) transform.localScale = baseScale;
        }
    }
}
