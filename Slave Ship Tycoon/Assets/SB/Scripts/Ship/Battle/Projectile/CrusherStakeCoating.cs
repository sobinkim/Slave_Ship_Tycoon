using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [DisallowMultipleComponent]
    public sealed class CrusherStakeCoating : MonoBehaviour, IPoolable
    {
        [Header("References")]
        [SerializeField] private BaseProjectile projectile;
        [SerializeField] private SpriteRenderer projectileRenderer;

        [Header("Coating Timing")]
        [SerializeField, Min(0f), Tooltip("발사 후 붉은 막이 시작되기까지의 시간")]
        private float delaySeconds = 0.045f;
        [SerializeField, Min(0.01f), Tooltip("탄 뒤쪽에서 앞쪽까지 막이 씌워지는 시간")]
        private float durationSeconds = 0.26f;

        private static readonly int CoatingProgressId = Shader.PropertyToID("_CoatingProgress");
        private MaterialPropertyBlock propertyBlock;
        private float flightSeconds;

        private void Awake()
        {
            if (projectile == null || projectileRenderer == null)
            {
                Debug.LogError("CrusherStakeCoating requires projectile and sprite renderer references.", this);
                enabled = false;
            }
        }

        private void OnEnable() => ResetCoating();
        private void OnDisable() => ResetCoating();

        private void LateUpdate()
        {
            if (projectile == null || !projectile.IsFlying)
            {
                ResetCoating();
                return;
            }

            // Keep coating time independent of retargeting/miss flight-curve restarts.
            flightSeconds += Time.deltaTime;
            float progress = Mathf.Clamp01((flightSeconds - delaySeconds) / Mathf.Max(0.01f, durationSeconds));
            ApplyProgress(progress);
        }

        private void ApplyProgress(float progress)
        {
            if (projectileRenderer == null) return;
            propertyBlock ??= new MaterialPropertyBlock();
            projectileRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(CoatingProgressId, progress);
            projectileRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ResetCoating()
        {
            flightSeconds = 0f;
            ApplyProgress(0f);
        }

        public void OnSpawnedFromPool() => ResetCoating();
        public void OnDespawnedToPool() => ResetCoating();
    }
}
