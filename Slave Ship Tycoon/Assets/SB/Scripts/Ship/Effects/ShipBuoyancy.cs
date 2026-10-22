using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class ShipBuoyancy : MonoBehaviour
    {
        [Header("Explicit visual references")]
        [SerializeField] private Ship owner;
        [SerializeField] private Transform motionRoot;
        [SerializeField] private Animator shipAnimator;

        [Header("Water motion")]
        [SerializeField, Range(0f, 8f), Tooltip("살아 있는 함선이 좌우로 기울어지는 최대 각도입니다.")]
        private float rollDegrees = 3f;
        [SerializeField, Min(0.5f), Tooltip("좌우로 한 번 왕복하는 시간입니다. 함선별로 시작 위상을 나눕니다.")]
        private float cycleSeconds = 3.2f;
        [SerializeField, Min(0f), Tooltip("수면을 타며 위아래로 움직이는 로컬 거리입니다.")]
        private float heaveAmplitude = 0.018f;
        [SerializeField, Min(0f), Tooltip("재출항 시 흔들림이 자연스럽게 시작되는 시간입니다.")]
        private float blendInSeconds = 0.35f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private int deadHash;
        private bool hasDeadParameter;
        private bool initialized;
        private float phaseOffset;
        private float phase;
        private float blendElapsed;

        private void Awake()
        {
            if (owner == null || motionRoot == null || shipAnimator == null ||
                motionRoot == owner.transform || !motionRoot.IsChildOf(owner.transform))
            {
                Debug.LogError("ShipBuoyancy requires a ship, its visual motion root and animator.", this);
                enabled = false;
                return;
            }

            baseLocalPosition = motionRoot.localPosition;
            baseLocalRotation = motionRoot.localRotation;
            deadHash = Animator.StringToHash("Dead");
            foreach (AnimatorControllerParameter parameter in shipAnimator.parameters)
                hasDeadParameter |= parameter.nameHash == deadHash && parameter.type == AnimatorControllerParameterType.Bool;
            uint hash = unchecked((uint)GetInstanceID() * 2654435761u);
            phaseOffset = (hash & 0xffffu) / 65536f * Mathf.PI * 2f;
            initialized = true;
        }

        private void OnEnable()
        {
            phase = 0f;
            blendElapsed = 0f;
            ResetMotion();
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            if (owner.IsDead || (hasDeadParameter && shipAnimator.GetBool(deadHash)))
            {
                ResetMotion();
                blendElapsed = 0f;
                return;
            }
            if (Time.deltaTime <= 0f) return;

            phase = Mathf.Repeat(phase + Time.deltaTime / Mathf.Max(0.5f, cycleSeconds), 1f);
            blendElapsed += Time.deltaTime;
            float blend = blendInSeconds > 0f
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blendElapsed / blendInSeconds)) : 1f;
            float wave = phase * Mathf.PI * 2f + phaseOffset;
            float roll = Mathf.Sin(wave) * rollDegrees * blend;
            float heave = Mathf.Sin(wave + Mathf.PI * 0.5f) * heaveAmplitude * blend;
            motionRoot.localPosition = baseLocalPosition + Vector3.up * heave;
            motionRoot.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, roll);
        }

        private void OnDisable()
        {
            ResetMotion();
            phase = 0f;
            blendElapsed = 0f;
        }

        private void ResetMotion()
        {
            if (!initialized || motionRoot == null) return;
            motionRoot.localPosition = baseLocalPosition;
            motionRoot.localRotation = baseLocalRotation;
        }
    }
}
