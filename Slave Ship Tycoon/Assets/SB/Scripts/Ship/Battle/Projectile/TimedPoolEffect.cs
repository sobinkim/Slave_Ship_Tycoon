using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public class TimedPoolEffect : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.01f)] private float lifeTime = 0.5f;

        private float remainingLifeTime;
        private bool isActive;

        private void Update()
        {
            if (isActive == false)
                return;

            remainingLifeTime -= Time.deltaTime;
            if (remainingLifeTime > 0f)
                return;

            isActive = false;

            if (PoolingManager.Instance != null)
                PoolingManager.Instance.Release(this);
            else
                gameObject.SetActive(false);
        }

        public void OnSpawnedFromPool()
        {
            remainingLifeTime = lifeTime;
            isActive = true;
        }

        public void OnDespawnedToPool()
        {
            isActive = false;
        }
    }
}
