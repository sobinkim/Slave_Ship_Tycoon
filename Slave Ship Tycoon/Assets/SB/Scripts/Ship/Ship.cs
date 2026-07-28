using System;
using SB.Core;
using SB.Scripts.AttackCompo;
using UnityEngine;
using UnityEngine.Serialization;

namespace SB.Scripts
{
    public enum ShipType
    {
        MainShip,
        EscortShip,
        Enemy
    }

    [System.Flags]
    public enum ShipRole
    {
        None = 0,
        Tanker = 1 << 0,
        Dealer = 1 << 1,
        Support = 1 << 2,
        Speedster = 1 << 3,
        AoE = 1 << 4,
        Piercer = 1 << 5
    }

    public struct MyShipData
    {
        public ShipType ShipType;
        public ShipRole Role;
        public Vector2Int SpawnSlot;
    }

    public class Ship : Entity, IPoolable
    {
        [FormerlySerializedAs("myShipType")] [SerializeField]
        private ShipType _myShipType;

        [SerializeField] private ShipRole _shipRole;

        protected EntityAnimator _animator;
        protected EntityAnimatorTrigger _animatorTrigger;
        protected EntityStatCompo _statCompo;
        protected EntityHealth _healthCompo;
        protected Base_ShipAttackCompo _attackCompo;

        public MyShipData myShipData;
        
        
        protected virtual void OnEnable()
        {
            myShipData = new MyShipData();
            myShipData.Role = _shipRole;
            myShipData.ShipType = _myShipType;
        }

        public void SetSpawnSlot(Vector2Int spawnSlot)
        {
            myShipData.SpawnSlot = spawnSlot;
        }


        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _animator = GetCompo<EntityAnimator>();
            _animatorTrigger = GetCompo<EntityAnimatorTrigger>();
            _statCompo = GetCompo<EntityStatCompo>();
            _healthCompo = GetCompo<EntityHealth>();
            _attackCompo = GetCompo<Base_ShipAttackCompo>();
        }

        public virtual void OnSpawnedFromPool()
        {
            IsDead = false;
            _healthCompo?.ResetHealth();
            _attackCompo?.ResetAttackState();
        }

        public virtual void OnDespawnedToPool()
        {
        }
        
        
    }
}
