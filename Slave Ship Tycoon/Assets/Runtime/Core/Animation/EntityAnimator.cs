using UnityEngine;
using UnityEngine.Events;

namespace SB.Core
{
    [DisallowMultipleComponent]
    public class EntityAnimator : EntityComponent
    {
        [SerializeField] private Animator animator;
        [SerializeField] private bool disableRootMotion;
        [SerializeField] private bool applyRootMotionToAnimatorTransform;
        [SerializeField] private bool resetTransformAfterAppliedRootMotion = true;

        private Vector3 cachedAnimatorLocalPosition;
        private Quaternion cachedAnimatorLocalRotation;
        private bool hasCachedAnimatorTransform;

        public UnityEvent<Vector3, Quaternion> OnAnimatorMoveEvent { get; } = new();
        public Animator RuntimeAnimator => animator;
        public bool SuppressRuntimeRootMotion { get; set; }

        public bool ApplyRootMotion
        {
            get => animator != null && animator.applyRootMotion;
            set
            {
                if (animator != null)
                    animator.applyRootMotion = !disableRootMotion && value;
            }
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            CacheAnimatorRootMotionTransform();
            ApplyRootMotionSettings();
        }

        private void OnAnimatorMove()
        {
            if (disableRootMotion || SuppressRuntimeRootMotion || animator == null)
                return;

            if (applyRootMotionToAnimatorTransform)
                ApplyAnimatorRootMotionToTransform();

            OnAnimatorMoveEvent.Invoke(animator.deltaPosition, animator.deltaRotation);
        }

        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
            CacheAnimatorRootMotionTransform();
            ApplyRootMotionSettings();
        }

        public void CacheAnimatorRootMotionTransform()
        {
            if (animator == null)
                return;

            Transform animatorTransform = animator.transform;
            cachedAnimatorLocalPosition = animatorTransform.localPosition;
            cachedAnimatorLocalRotation = animatorTransform.localRotation;
            hasCachedAnimatorTransform = true;
        }

        public void ResetAnimatorRootMotionTransform()
        {
            if (!applyRootMotionToAnimatorTransform || !resetTransformAfterAppliedRootMotion || animator == null || !hasCachedAnimatorTransform)
                return;

            Transform animatorTransform = animator.transform;
            animatorTransform.localPosition = cachedAnimatorLocalPosition;
            animatorTransform.localRotation = cachedAnimatorLocalRotation;
        }

        public void SetParam(int hash, float value)
        {
            if (animator != null)
                animator.SetFloat(hash, value);
        }

        public void SetParam(int hash, float value, float dampTime)
        {
            if (animator != null)
                animator.SetFloat(hash, value, dampTime, Time.deltaTime);
        }

        public void SetParam(int hash, bool value)
        {
            if (animator != null)
                animator.SetBool(hash, value);
        }

        public void SetParam(int hash, int value)
        {
            if (animator != null)
                animator.SetInteger(hash, value);
        }

        public void SetParam(int hash)
        {
            if (animator != null)
                animator.SetTrigger(hash);
        }

        public void SetParam(string parameterName, float value)
        {
            if (animator != null)
                animator.SetFloat(parameterName, value);
        }

        public void SetParam(string parameterName, bool value)
        {
            if (animator != null)
                animator.SetBool(parameterName, value);
        }

        public void SetParam(string parameterName, int value)
        {
            if (animator != null)
                animator.SetInteger(parameterName, value);
        }

        public void SetParam(string parameterName)
        {
            if (animator != null)
                animator.SetTrigger(parameterName);
        }

        public void ResetTrigger(int hash)
        {
            if (animator != null)
                animator.ResetTrigger(hash);
        }

        public void ResetTrigger(string parameterName)
        {
            if (animator != null)
                animator.ResetTrigger(parameterName);
        }

        public void SetAnimatorEnabled(bool isEnabled)
        {
            if (animator != null)
                animator.enabled = isEnabled;
        }

        private void ApplyRootMotionSettings()
        {
            if (animator == null)
                return;

            if (disableRootMotion)
            {
                animator.applyRootMotion = false;
                return;
            }

            if (applyRootMotionToAnimatorTransform)
                animator.applyRootMotion = true;
        }

        private void ApplyAnimatorRootMotionToTransform()
        {
            Transform animatorTransform = animator.transform;
            animatorTransform.position += animator.deltaPosition;
            animatorTransform.rotation *= animator.deltaRotation;
        }
    }
}
