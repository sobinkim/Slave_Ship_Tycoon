using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [DisallowMultipleComponent]
    public sealed class ShipWoodImpactFeedback : MonoBehaviour
    {
        [Header("Impact")]
        [SerializeField] private WoodImpactEffect effectPrefab;
        [SerializeField, Min(0f), Tooltip("같은 함선의 파편 생성 간격. 피해와 탄 수 집계는 제한하지 않습니다.")]
        private float minimumIntervalSeconds = .08f;
        private float nextEffectTime;
        private void OnEnable() => nextEffectTime = 0f;

        public void Play(Vector3 worldPosition)
        {
            if (Time.time < nextEffectTime || effectPrefab == null || PoolingManager.Instance == null) return;
            nextEffectTime = Time.time + minimumIntervalSeconds;
            PoolingManager.Instance.Get(effectPrefab, worldPosition, Quaternion.identity);
        }
    }
}
