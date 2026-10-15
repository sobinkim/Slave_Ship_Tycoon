using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class SmokeCloudEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] private ParticleSystem smokeParticles;
        [SerializeField, Range(0f, 1f), Tooltip("연막에 가려진 함선이 발사하는 공격의 최종 명중 확률 배율")]
        private float hitChanceMultiplier = 0.5f;
        [SerializeField, Tooltip("피격 함선 중심 기준 월드 오프셋. 연막이 함선을 감싸도록 조절합니다.")]
        private Vector3 worldOffset = new Vector3(0f, 0.04f, 0f);

        private Ship target;
        private int targetGeneration;
        public float HitChanceMultiplier => hitChanceMultiplier;
        public bool IsAffecting(Ship ship) => target == ship && target != null &&
            target.SpawnGeneration == targetGeneration && target.isActiveAndEnabled && !target.IsDead &&
            smokeParticles != null && smokeParticles.IsAlive(true);

        private void OnEnable() => Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            Clear();
        }

        public void Play(Ship recipient)
        {
            Clear();
            if (recipient == null || recipient.IsDead || smokeParticles == null)
            {
                Release();
                return;
            }
            target = recipient;
            targetGeneration = target.SpawnGeneration;
            target.SmokeCloud = this;
            target.OnDeathEvent.AddListener(Release);
            transform.position = target.transform.position + worldOffset;
            smokeParticles.Play(true);
        }

        private void LateUpdate()
        {
            if (!IsAffecting(target)) { Release(); return; }
            transform.position = target.transform.position + worldOffset;
        }

        private void HandleBattleEnded(StageBattleEndedEvent evt) => Release();
        public void Release()
        {
            Clear();
            if (!gameObject.activeSelf) return;
            if (PoolingManager.Instance != null) PoolingManager.Instance.Release(this);
            else gameObject.SetActive(false);
        }

        private void Clear()
        {
            if (target != null)
            {
                target.OnDeathEvent.RemoveListener(Release);
                if (target.SmokeCloud == this) target.SmokeCloud = null;
            }
            target = null;
            if (smokeParticles != null)
                smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void OnSpawnedFromPool() => Clear();
        public void OnDespawnedToPool() => Clear();
    }
}
