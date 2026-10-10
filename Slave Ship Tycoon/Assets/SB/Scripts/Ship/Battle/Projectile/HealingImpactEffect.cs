using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class HealingImpactEffect : MonoBehaviour, IPoolable
    {
        [Header("References")]
        [SerializeField] private ParticleSystem crossParticles;
        [SerializeField] private ParticleSystemRenderer crossParticleRenderer;

        [Header("Sorting")]
        [SerializeField] private int baseSortingOrder = 4500;
        [SerializeField] private float sortingOrderPerUnit = 100f;

        private bool isPlaying;

        private void Awake()
        {
            if (crossParticles == null || crossParticleRenderer == null)
                Debug.LogError("Healing impact requires cross particle system and renderer references.", this);
        }

        private void OnEnable()
        {
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        }

        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            StopParticles();
        }

        public void Play(float restoredHealth)
        {
            if (restoredHealth <= 0f || crossParticles == null || crossParticleRenderer == null)
                return;

            StopParticles();
            crossParticleRenderer.sortingOrder = baseSortingOrder +
                Mathf.RoundToInt(-transform.position.y * sortingOrderPerUnit);
            crossParticles.Play(true);
            isPlaying = true;
        }

        private void Update()
        {
            // Pool lifetime follows the authored particle lifetimes, including later bursts.
            if (isPlaying && !crossParticles.IsAlive(true))
                Release();
        }

        private void HandleBattleEnded(StageBattleEndedEvent evt) { Release(); }

        private void Release()
        {
            if (!isPlaying) return;
            StopParticles();
            if (PoolingManager.Instance != null) PoolingManager.Instance.Release(this);
            else gameObject.SetActive(false);
        }

        private void StopParticles()
        {
            isPlaying = false;
            if (crossParticles != null)
                crossParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void OnSpawnedFromPool() { StopParticles(); }
        public void OnDespawnedToPool() { StopParticles(); }
    }
}
