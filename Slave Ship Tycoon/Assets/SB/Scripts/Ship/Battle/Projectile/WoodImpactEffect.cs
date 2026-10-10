using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class WoodImpactEffect : MonoBehaviour, IPoolable
    {
        [Header("References")]
        [SerializeField] private ParticleSystem fragments;
        [SerializeField] private ParticleSystemRenderer fragmentRenderer;

        [Header("Presentation")]
        [SerializeField, Min(1)] private int fragmentCount = 5;
        [SerializeField, Tooltip("함선보다 앞에 표시할 파편의 기준 정렬 오더")]
        private int baseSortingOrder = 4051;
        [SerializeField] private float sortingOrderPerUnit = 100f;
        private bool isPlaying;
        public void OnSpawnedFromPool()
        {
            if (fragments == null || fragmentRenderer == null)
            {
                Debug.LogError("WoodImpactEffect requires particle references.", this);
                return;
            }
            fragments.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fragmentRenderer.sortingOrder = baseSortingOrder + Mathf.RoundToInt(-transform.position.y * sortingOrderPerUnit);
            fragments.Play();
            fragments.Emit(fragmentCount);
            isPlaying = true;
        }
        private void Update()
        {
            if (!isPlaying || fragments.IsAlive(true)) return;
            isPlaying = false;
            if (PoolingManager.Instance != null) PoolingManager.Instance.Release(this);
            else gameObject.SetActive(false);
        }
        public void OnDespawnedToPool()
        {
            isPlaying = false;
            if (fragments != null) fragments.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
