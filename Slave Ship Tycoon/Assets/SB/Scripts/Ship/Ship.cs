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

    public enum ShipStateType
    {
        Idle,
        Move,
        Attack,
        Dead
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
        protected ShipStatCompo _statCompo;
        protected ShipStatCompo _shipStatCompo;
        protected EntityHealth _healthCompo;
        protected Base_ShipAttackCompo _attackCompo;
        private ShipStateMachine _stateMachine;

        public MyShipData myShipData;
        public ShipStatCompo ShipStatCompo => _shipStatCompo;
        public int SpawnGeneration { get; private set; }
        public bool CanAttack => _attackCompo != null && _attackCompo.CanAttack;
        
        
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
            _statCompo = GetCompo<ShipStatCompo>();
            _shipStatCompo = GetCompo<ShipStatCompo>();
            _healthCompo = GetCompo<EntityHealth>();
            _attackCompo = GetCompo<Base_ShipAttackCompo>();
            _stateMachine = new ShipStateMachine(this);
        }

        protected virtual void Update()
        {
            UpdateShipState();
        }

        protected void UpdateShipState()
        {
            _stateMachine?.Update();
        }

        public void ChangeState(ShipStateType state)
        {
            _stateMachine?.ChangeState(state);
        }

        public void PrepareEncounterState()
        {
            _attackCompo?.ResetAttackState();
            ChangeState(ShipStateType.Move);
        }

        public bool EnsureAttackTarget()
        {
            return _attackCompo != null && _attackCompo.EnsureTarget();
        }

        public bool EnterAttack()
        {
            return _attackCompo != null && _attackCompo.EnterAttack();
        }

        public void TickAttackCooldown(float deltaTime)
        {
            _attackCompo?.TickCooldown(deltaTime);
        }

        public void ApplyAttackAnimationSpeed()
        {
            _attackCompo?.ApplyAttackAnimationSpeed();
        }

        public void ResetAttackAnimationSpeed()
        {
            _attackCompo?.ResetAttackAnimationSpeed();
        }

        public virtual void OnSpawnedFromPool()
        {
            unchecked
            {
                SpawnGeneration++;
            }

            IsDead = false;
            _healthCompo?.ResetHealth();
            PrepareEncounterState();
        }

        public virtual void OnDespawnedToPool()
        {
        }
        
        
    }
}
