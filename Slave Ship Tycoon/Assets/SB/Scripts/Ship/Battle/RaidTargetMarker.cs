using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class RaidTargetMarker : MonoBehaviour, IPoolable
    {
        [Header("References")]
        [SerializeField] private Ship ship;
        [SerializeField] private SpriteRenderer hull;
        [SerializeField] private MeshRenderer hookIcon;
        [Header("Hostile target marker")]
        [SerializeField, Tooltip("적의 고유 효과 아이콘과 구분하는 습격 표식 색상")]
        private Color markerColor = new Color(1f, .08f, .08f, 1f);
        [SerializeField] private int sortingOrderOffset = 20;

        private readonly HashSet<Ship> attackers = new HashSet<Ship>();
        private MaterialPropertyBlock properties;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        public int AttackerCount => attackers.Count;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            if (ship == null || hull == null || hookIcon == null)
            {
                Debug.LogError("RaidTargetMarker requires ship, hull and hook icon references.", this);
                enabled = false;
            }
            Clear();
        }

        private void OnEnable() => Bus<StageBattleEndedEvent>.OnEvent += OnBattleEnded;
        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= OnBattleEnded;
            Clear();
        }

        public void Register(Ship attacker)
        {
            if (isActiveAndEnabled && ship != null && !ship.IsDead && attacker != null)
                attackers.Add(attacker);
        }

        public void Unregister(Ship attacker) => attackers.Remove(attacker);

        private void LateUpdate()
        {
            if (ship.IsDead) Clear();
            hookIcon.enabled = false;
        }

        private void OnBattleEnded(StageBattleEndedEvent evt) => Clear();
        public void OnSpawnedFromPool() => Clear();
        public void OnDespawnedToPool() => Clear();
        private void Clear()
        {
            attackers.Clear();
            if (hookIcon != null) hookIcon.enabled = false;
        }
    }
}
