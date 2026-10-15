using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class HealingProjectileCrossTrail : MonoBehaviour
    {
        [SerializeField] private HealingProjectile projectile;
        [SerializeField] private SpriteRenderer projectileRenderer;
        [SerializeField] private ParticleSystem crossParticles;
        [SerializeField] private ParticleSystemRenderer crossRenderer;

        private void OnEnable()
        {
            if (crossParticles == null) return;
            crossParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = crossParticles.emission;
            emission.enabled = false;
            crossParticles.Play(true);
        }

        private void LateUpdate()
        {
            if (crossParticles == null || projectile == null) return;
            var emission = crossParticles.emission;
            emission.enabled = projectile.IsFlying;
            if (crossRenderer == null || projectileRenderer == null) return;
            crossRenderer.sortingLayerID = projectileRenderer.sortingLayerID;
            crossRenderer.sortingOrder = projectileRenderer.sortingOrder - 1;
        }

        private void OnDisable()
        {
            if (crossParticles != null)
                crossParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
