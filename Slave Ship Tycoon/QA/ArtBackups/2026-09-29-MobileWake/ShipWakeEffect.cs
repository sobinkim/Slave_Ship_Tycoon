using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class ShipWakeEffect : MonoBehaviour
    {
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

        private int moveHash;
        private float visibility;
        private float rippleTime;
        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
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
            visibility = 0f;
            rippleTime = 0f;
            if (wakeRenderer != null) wakeRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            bool isMoving = !owner.IsDead && shipAnimator.isActiveAndEnabled && shipAnimator.GetBool(moveHash);
            float fadeSeconds = isMoving ? fadeInSeconds : fadeOutSeconds;
            visibility = Mathf.MoveTowards(visibility, isMoving ? 1f : 0f,
                Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            rippleTime += Time.deltaTime * rippleCyclesPerSecond * Mathf.PI * 2f;
            float ripple = Mathf.Sin(rippleTime);
            transform.localScale = Vector3.Scale(baseScale,
                new Vector3(1f + ripple * rippleScaleAmount, 1f - ripple * rippleScaleAmount, 1f));
            Color tint = foamColor;
            tint.a *= visibility * (1f - rippleOpacityAmount * (ripple * 0.5f + 0.5f));
            wakeRenderer.color = tint;
            wakeRenderer.enabled = visibility > 0f;
        }

        private void OnDisable()
        {
            visibility = 0f;
            if (wakeRenderer != null) wakeRenderer.enabled = false;
            transform.localScale = baseScale;
        }
    }
}
