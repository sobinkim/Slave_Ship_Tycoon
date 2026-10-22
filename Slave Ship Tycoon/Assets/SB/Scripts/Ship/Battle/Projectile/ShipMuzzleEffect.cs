using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class ShipMuzzleEffect : MonoBehaviour, IPoolable
    {
        [Header("Particles")]
        [SerializeField] private ParticleSystem[] particleSystems;
        [SerializeField] private ParticleSystemRenderer[] particleRenderers;

        [Header("Sorting")]
        [SerializeField] private int baseSortingOrder = 4200;
        [SerializeField] private float sortingOrderPerUnit = 100f;

        private Transform muzzle;
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

        public void Play(Transform firePoint, Vector3 direction, float scale)
        {
            StopParticles();
            if (firePoint == null || particleSystems == null || particleSystems.Length == 0 ||
                particleRenderers == null || particleRenderers.Length != particleSystems.Length)
                return;

            muzzle = firePoint;
            transform.SetPositionAndRotation(firePoint.position,
                Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            transform.localScale = Vector3.one * scale;
            int order = baseSortingOrder + Mathf.RoundToInt(-firePoint.position.y * sortingOrderPerUnit);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleRenderers[i].sortingOrder = order + i;
                particleSystems[i].Play(false);
            }
            isPlaying = true;
        }

        private void LateUpdate()
        {
            if (!isPlaying) return;
            if (muzzle == null || !muzzle.gameObject.activeInHierarchy)
            {
                Release();
                return;
            }

            transform.position = muzzle.position;
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
            muzzle = null;
            if (particleSystems == null) return;
            foreach (ParticleSystem particles in particleSystems)
                if (particles != null)
                    particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void OnSpawnedFromPool() => StopParticles();
        public void OnDespawnedToPool() => StopParticles();
    }
}
