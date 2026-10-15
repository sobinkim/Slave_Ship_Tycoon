using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class SmokeBombSmokeTrail : MonoBehaviour
    {
        [SerializeField] private SmokeBombProjectile projectile;
        [SerializeField] private SpriteRenderer projectileRenderer;
        [SerializeField] private ParticleSystem smokeParticles;
        [SerializeField] private ParticleSystemRenderer smokeRenderer;

        private void OnEnable()
        {
            if (smokeParticles == null) return;
            smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = smokeParticles.emission;
            emission.enabled = false;
            smokeParticles.Play(true);
        }

        private void LateUpdate()
        {
            if (smokeParticles == null || projectile == null) return;
            var emission = smokeParticles.emission;
            emission.enabled = projectile.IsFlying;
            if (smokeRenderer == null || projectileRenderer == null) return;
            smokeRenderer.sortingLayerID = projectileRenderer.sortingLayerID;
            smokeRenderer.sortingOrder = projectileRenderer.sortingOrder - 1;
        }

        private void OnDisable()
        {
            if (smokeParticles != null)
                smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
