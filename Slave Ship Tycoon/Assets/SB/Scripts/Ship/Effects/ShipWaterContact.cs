using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class ShipWaterContact : MonoBehaviour
    {
        [Header("Explicit references")]
        [SerializeField] private Ship owner;
        [SerializeField] private SpriteRenderer contactRenderer;

        [Header("Death fade")]
        [SerializeField, Min(0.01f), Tooltip("함선 사망 후 물 접촉 표시가 사라지는 시간입니다.")]
        private float fadeOutSeconds = 0.3f;

        private Color originalTint;
        private float visibility;
        private bool isInitialized;

        private void Awake()
        {
            if (owner == null || contactRenderer == null)
            {
                Debug.LogError("ShipWaterContact requires a ship and contact renderer.", this);
                enabled = false;
                return;
            }

            originalTint = contactRenderer.color;
            isInitialized = true;
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
        }

        private void OnDisable()
        {
            HideAndReset();
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
