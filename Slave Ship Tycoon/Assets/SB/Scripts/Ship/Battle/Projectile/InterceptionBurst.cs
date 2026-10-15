using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class InterceptionBurst : MonoBehaviour, IPoolable
    {
        [Header("Particle references")]
        [SerializeField] private ParticleSystem[] particleSystems;
        [SerializeField] private ParticleSystemRenderer[] particleRenderers;

        [Header("Sorting")]
        [SerializeField] private int baseSortingOrder = 4200;
        [SerializeField] private float sortingOrderPerUnit = 100f;
        [SerializeField] private int[] sortingOrderOffsets = { 3, 2, 1, 4 };

        private bool isPlaying;

        private void OnEnable()
        {
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        }

        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            StopParticles();
        }

        public void OnSpawnedFromPool()
        {
            StopParticles();
            if (particleSystems == null || particleSystems.Length == 0 ||
                particleRenderers == null || particleRenderers.Length != particleSystems.Length ||
                sortingOrderOffsets == null || sortingOrderOffsets.Length != particleSystems.Length)
            {
                Debug.LogError("InterceptionBurst requires explicit particle and renderer references.", this);
                return;
            }
            int order = baseSortingOrder + Mathf.RoundToInt(-transform.position.y * sortingOrderPerUnit);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleRenderers[i].sortingOrder = order + sortingOrderOffsets[i];
                particleSystems[i].Play(false);
            }
            isPlaying = true;
        }

        private void Update()
        {
            if (!isPlaying) return;
            // Follow the authored particle lifetime, including smoke and delayed bursts.
            foreach (ParticleSystem particles in particleSystems)
                if (particles.IsAlive(false)) return;
            Release();
        }

        private void HandleBattleEnded(StageBattleEndedEvent evt) => Release();

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
            if (particleSystems == null) return;
            foreach (ParticleSystem particles in particleSystems)
                if (particles != null)
                    particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void OnDespawnedToPool() => StopParticles();
    }
}
