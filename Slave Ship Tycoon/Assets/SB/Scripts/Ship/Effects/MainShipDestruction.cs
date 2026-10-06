using UnityEngine.Events;
using SB.Core.EventBus;
using SB.Scripts.Projectiles;
using SB.Core;
using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class MainShipDestruction : MonoBehaviour, IIncomingDamageModifier, IPoolable
    {
        [Header("References")]
        [SerializeField] private MainShip owner;
        [SerializeField, Tooltip("패배 시 숨기고 재출항 시 복원할 시각 오브젝트.")]
        private GameObject[] presentationRoots = new GameObject[0];
        [SerializeField] private ShipWoodImpactFeedback hitFeedback;

        [Header("Defeat presentation")]
        [SerializeField, Tooltip("전탄 명중 후 패배 처리 대기 시작 시 실행할 연출.")]
        private UnityEvent onDefeatPresentation = new UnityEvent();
        public UnityEvent OnDefeatPresentation => onDefeatPresentation;

        [Header("Defeat camera shake")]
        [SerializeField, Min(0f), Tooltip("메인선 사망 시 화면 흔들림의 최대 월드 거리.")]
        private float shakeDistanceUnits = 0.04f;
        [SerializeField, Min(0f), Tooltip("메인선 사망 시 화면 흔들림 시간.")]
        private float shakeDurationSeconds = 0.15f;

        private void Awake()
        {
            if (owner != null &&
                presentationRoots != null && presentationRoots.Length > 0)
                return;

            Debug.LogError("MainShipDestruction requires explicit ship and presentation references.", this);
            enabled = false;
        }

        // The transport ship is defeated by the final-salvo sequence, not by HP.
        // Retain EntityHealth for the existing fleet/commander component contract.
        public float ModifyIncomingDamage(float incomingDamage) => 0f;

        public void PlayImpact(Vector3 worldImpactPosition)
        {
            hitFeedback?.Play(worldImpactPosition);
        }

        public void PlayDefeatShake()
        {
            Bus<CameraShakeRequestedEvent>.Raise(
                new CameraShakeRequestedEvent(shakeDistanceUnits, shakeDurationSeconds));
        }

        public void BeginDefeatPresentation()
        {
            if (owner == null || owner.IsDead)
                return;

            SetPresentationVisible(false);
            onDefeatPresentation.Invoke();
        }

        public void CompleteDefeat()
        {
            if (owner == null || owner.IsDead)
                return;

            SetPresentationVisible(false);
            owner.IsDead = true;
            owner.ChangeState(ShipStateType.Dead);
            owner.OnDeathEvent.Invoke();
        }

        public void OnSpawnedFromPool()
        {
            SetPresentationVisible(true);
        }

        public void OnDespawnedToPool()
        {
        }

        private void SetPresentationVisible(bool isVisible)
        {
            foreach (GameObject root in presentationRoots)
                if (root != null)
                    root.SetActive(isVisible);
        }
    }
}
