using System;
using SB.Core;
using SB.Core.EventBus;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts
{
    public class MainShip : Ship
    {
        [Header("Cargo presentation")]
        [SerializeField] private ShipCargoVisualizer cargoVisualizer;
        public ShipCargoVisualizer CargoVisualizer => cargoVisualizer;
        [SerializeField] private MainShipDestruction destruction;
        public MainShipDestruction Destruction => destruction;

    
        protected override void OnEnable()
        {
            base.OnEnable();
            OnDeathEvent.AddListener(Death);
            Bus<UpgradeEvent>.OnEvent += StatUpdate;
            Bus<BattleStartEvent>.OnEvent += HandleBattleStarted;
        }

        private void OnDisable()
        {
            OnDeathEvent.RemoveListener(Death);
            Bus<UpgradeEvent>.OnEvent -= StatUpdate;
            Bus<BattleStartEvent>.OnEvent -= HandleBattleStarted;
        }

        private void Start()
        {
            RefreshMainShipStats();
        }

        public override void OnSpawnedFromPool()
        {
            base.OnSpawnedFromPool();
            RefreshMainShipStats();
        }

        private void HandleBattleStarted(BattleStartEvent evt)
        {
            if (evt.BattleSpawnData.MainShip != this || IsDead)
                return;

            // 공격 컴포넌트가 없는 운송선도 조우 이동을 끝내야 물살이 사라집니다.
            ChangeState(ShipStateType.Idle);
        }

        private void Death()
        {
            Bus<MainShipDeadEvent>.Raise(new MainShipDeadEvent());
        }

        protected override void Update()
        {
            base.Update();
        }

        
        private void StatUpdate(UpgradeEvent evt)
        {
            if (_shipStatCompo == null)
                _shipStatCompo = GetCompo<ShipStatCompo>();

            if (_shipStatCompo == null)
            {
                Debug.LogError($"{nameof(MainShip)} needs a {nameof(ShipStatCompo)}.", this);
                return;
            }

            _shipStatCompo.SetShipBaseValue(MainShipUpgradeType.AttackPowerPercent, evt._mainShipUpgradeData.attackPowerPercent);
            _shipStatCompo.SetShipBaseValue(MainShipUpgradeType.AttackSpeedPercent, evt._mainShipUpgradeData.attackSpeedPercent);
            _shipStatCompo.SetShipBaseValue(MainShipUpgradeType.CargoCapacity, evt._mainShipUpgradeData.cargoCapacity);
            _shipStatCompo.SetShipBaseValue(MainShipUpgradeType.Luck, evt._mainShipUpgradeData.luck);
            _shipStatCompo.SetShipBaseValue(MainShipUpgradeType.Health, evt._mainShipUpgradeData.health);
            _shipStatCompo.SetCommanderBaseValue(CommanderStatType.MaxGauge,
                evt._mainShipUpgradeData.commanderGaugeMax);
            _shipStatCompo.SetCommanderBaseValue(CommanderStatType.GaugeRecoveryPerSecond,
                evt._mainShipUpgradeData.commanderGaugeRecoveryPerSecond);
        }

        private void RefreshMainShipStats()
        {
            Bus<RefreshMainShipStatsEvent>.Raise(new RefreshMainShipStatsEvent());
        }
        
     
    }
}
