using SB.Core;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Commander
{
    public class CommanderSkillManager : MonoBehaviour
    {
        private StatSO _maxGaugeStat;
        private StatSO _gaugeRecoveryPerSecondStat;
        private float _currentGauge;
        private bool _isBattleRunning;

        public float CurrentGauge => _currentGauge;
        public float MaxGauge => _maxGaugeStat != null ? _maxGaugeStat.Value : 0f;
        public float GaugeRecoveryPerSecond => _gaugeRecoveryPerSecondStat != null
            ? _gaugeRecoveryPerSecondStat.Value
            : 0f;

        private void OnEnable()
        {
            Bus<StageBattleInitializedEvent>.OnEvent += HandleStageBattleInitialized;
            Bus<BattleStartEvent>.OnEvent += HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent += HandleStageBattleEnded;
        }

        private void OnDisable()
        {
            Bus<StageBattleInitializedEvent>.OnEvent -= HandleStageBattleInitialized;
            Bus<BattleStartEvent>.OnEvent -= HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent -= HandleStageBattleEnded;
        }

        private void Update()
        {
            if (_isBattleRunning == false || GaugeRecoveryPerSecond <= 0f)
                return;

            SetCurrentGauge(_currentGauge + GaugeRecoveryPerSecond * Time.deltaTime);
        }

        public bool TryUseGauge(float gaugeCost)
        {
            if (_isBattleRunning == false || gaugeCost <= 0f || _currentGauge < gaugeCost)
                return false;

            SetCurrentGauge(_currentGauge - gaugeCost);
            return true;
        }

        private void HandleStageBattleInitialized(StageBattleInitializedEvent evt)
        {
            if (SetCommanderStats(evt.BattleSpawnData.MainShip))
                SetCurrentGauge(_currentGauge);
        }

        private void HandleBattleStarted(BattleStartEvent evt)
        {
            if (SetCommanderStats(evt.BattleSpawnData.MainShip) == false)
                return;

            _isBattleRunning = true;
            SetCurrentGauge(_currentGauge);
        }

        private void HandleStageBattleEnded(StageBattleEndedEvent evt)
        {
            _isBattleRunning = false;
        }

        private bool SetCommanderStats(MainShip mainShip)
        {
            if (mainShip == null || mainShip.ShipStatCompo == null)
            {
                Debug.LogError($"{nameof(CommanderSkillManager)} needs a main ship commander stat.", this);
                _isBattleRunning = false;
                return false;
            }

            _maxGaugeStat = mainShip.ShipStatCompo.GetStat(
                mainShip.ShipStatCompo.GetCommanderStatSO(CommanderStatType.MaxGauge));
            _gaugeRecoveryPerSecondStat = mainShip.ShipStatCompo.GetStat(
                mainShip.ShipStatCompo.GetCommanderStatSO(CommanderStatType.GaugeRecoveryPerSecond));
            return true;
        }

        private void SetCurrentGauge(float gauge)
        {
            _currentGauge = Mathf.Clamp(gauge, 0f, MaxGauge);
            Bus<CommanderGaugeChangedEvent>.Raise(new CommanderGaugeChangedEvent(_currentGauge, MaxGauge));
        }
    }
}
