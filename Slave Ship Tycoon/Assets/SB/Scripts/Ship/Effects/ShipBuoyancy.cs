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
        [SerializeField, Min(1f), Tooltip("MOVE 상태에서 좌우 기울기를 키우는 배율입니다.")]
        private float movingRollMultiplier = 2f;
        [SerializeField, Min(0.01f), Tooltip("이동 상태 변경 시 기울기 강도가 전환되는 시간입니다.")]
        private float movementBlendSeconds = 0.4f;
        [SerializeField, Min(0.5f), Tooltip("좌우로 한 번 왕복하는 시간입니다. 함선별로 시작 위상을 나눕니다.")]
        private float cycleSeconds = 3.2f;
        [SerializeField, Min(1f), Tooltip("기준 항해 속도에서 흔들림 주기를 빠르게 하는 배율입니다.")]
        private float movingCycleSpeedMultiplier = 1.2f;
        [SerializeField, Min(0.01f), Tooltip("흔들림 속도 배율의 기준이 되는 초당 항해 거리입니다.")]
        private float referenceSailingSpeed = 2.6f;
        [SerializeField, Min(1f), Tooltip("항해 속도가 높아져도 흔들림 속도가 이 배율을 넘지 않습니다.")]
        private float maximumCycleSpeedMultiplier = 1.4f;
        [SerializeField, Min(0f), Tooltip("수면을 타며 위아래로 움직이는 로컬 거리입니다.")]
        private float heaveAmplitude = 0.018f;
        [SerializeField, Min(0f), Tooltip("재출항 시 흔들림이 자연스럽게 시작되는 시간입니다.")]
        private float blendInSeconds = 0.35f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private int deadHash;
        private int moveHash;
        private bool hasDeadParameter;
        private bool hasMoveParameter;
        private bool initialized;
        private float phaseOffset;
        private float phase;
        private float blendElapsed;
        private float movementBlend;
        private bool hasSailingSpeedOverride;
        private float sailingSpeedOverride;
        private float cycleSpeedMultiplier = 1f;

        public void SetSailingSpeedOverride(float speed)
        {
            hasSailingSpeedOverride = true;
            sailingSpeedOverride = Mathf.Max(0f, speed);
        }

        public void ClearSailingSpeedOverride()
        {
            hasSailingSpeedOverride = false;
            sailingSpeedOverride = 0f;
        }

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
            moveHash = Animator.StringToHash("MOVE");
            foreach (AnimatorControllerParameter parameter in shipAnimator.parameters)
            {
                hasDeadParameter |= parameter.nameHash == deadHash && parameter.type == AnimatorControllerParameterType.Bool;
                hasMoveParameter |= parameter.nameHash == moveHash && parameter.type == AnimatorControllerParameterType.Bool;
            }
            uint hash = unchecked((uint)GetInstanceID() * 2654435761u);
            phaseOffset = (hash & 0xffffu) / 65536f * Mathf.PI * 2f;
            initialized = true;
        }

        private void OnEnable()
        {
            phase = 0f;
            blendElapsed = 0f;
            movementBlend = 0f;
            cycleSpeedMultiplier = 1f;
            ClearSailingSpeedOverride();
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

            blendElapsed += Time.deltaTime;
            float blend = blendInSeconds > 0f
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blendElapsed / blendInSeconds)) : 1f;
            float targetMovement = hasMoveParameter && shipAnimator.GetBool(moveHash) ? 1f : 0f;
            movementBlend = owner.HasSailingPresentationOverride
                ? owner.SailingPresentationStrength
                : Mathf.MoveTowards(movementBlend, targetMovement, Time.deltaTime / Mathf.Max(0.01f, movementBlendSeconds));
            float sailingSpeed = hasSailingSpeedOverride ? sailingSpeedOverride : movementBlend * referenceSailingSpeed;
            float targetCycleSpeed = Mathf.Min(maximumCycleSpeedMultiplier,
                1f + (movingCycleSpeedMultiplier - 1f) * sailingSpeed / Mathf.Max(0.01f, referenceSailingSpeed));
            cycleSpeedMultiplier = Mathf.MoveTowards(cycleSpeedMultiplier, targetCycleSpeed,
                Mathf.Max(0.01f, maximumCycleSpeedMultiplier - 1f) * Time.deltaTime / Mathf.Max(0.01f, movementBlendSeconds));
            // Integrate frequency so changing speed never jumps the current roll angle.
            phase = Mathf.Repeat(phase + Time.deltaTime * cycleSpeedMultiplier / Mathf.Max(0.5f, cycleSeconds), 1f);
            float wave = phase * Mathf.PI * 2f + phaseOffset;
            float movementMultiplier = Mathf.Lerp(1f, movingRollMultiplier, movementBlend);
            float roll = Mathf.Sin(wave) * rollDegrees * movementMultiplier * blend;
            float heave = Mathf.Sin(wave + Mathf.PI * 0.5f) * heaveAmplitude * blend;
            motionRoot.localPosition = baseLocalPosition + Vector3.up * heave;
            motionRoot.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, roll);
        }

        private void OnDisable()
        {
            ResetMotion();
            phase = 0f;
            blendElapsed = 0f;
            ClearSailingSpeedOverride();
        }

        private void ResetMotion()
        {
            if (!initialized || motionRoot == null) return;
            motionRoot.localPosition = baseLocalPosition;
            motionRoot.localRotation = baseLocalRotation;
        }
    }
}
