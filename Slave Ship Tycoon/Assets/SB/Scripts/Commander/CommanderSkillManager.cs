using System;
using System.Collections.Generic;
using SB.Core;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts.Commander
{
    public class CommanderSkillManager : MonoBehaviour
    {
        [SerializeField] private CommanderSkillDatabase _skillDatabase;
        [SerializeField] private CommanderSkillLoadout _skillLoadout;
        [SerializeField, Min(0.05f)] private float _autoUseCheckInterval = 0.25f;
        [SerializeField] private bool _isAutoUseEnabled;

        private readonly Dictionary<CommanderSkillType, CommanderSkillRuntimeState> _runtimeStates =
            new Dictionary<CommanderSkillType, CommanderSkillRuntimeState>();
        private readonly Dictionary<CommanderSkillType, ICommanderSkillEffect> _skillEffects =
            new Dictionary<CommanderSkillType, ICommanderSkillEffect>();

        private CommanderSkillData[] _equippedSkills = Array.Empty<CommanderSkillData>();
        private BattleSpawnData _battleSpawnData;
        private StatSO _maxGaugeStat;
        private StatSO _gaugeRecoveryPerSecondStat;
        private float _currentGauge;
        private float _autoUseCheckTimer;
        private bool _hasBattleSpawnData;
        private bool _isBattleRunning;

        public float CurrentGauge => _currentGauge;
        public float MaxGauge => _maxGaugeStat != null ? _maxGaugeStat.Value : 0f;
        public float GaugeRecoveryPerSecond => _gaugeRecoveryPerSecondStat != null
            ? _gaugeRecoveryPerSecondStat.Value
            : 0f;
        public bool IsAutoUseEnabled => _isAutoUseEnabled;
        public bool IsBattleRunning => _isBattleRunning;
        public int EquippedSlotCount => _equippedSkills.Length;

        private void Awake()
        {
            InitializeSkills();
        }

        private void OnEnable()
        {
            Bus<StageBattleInitializedEvent>.OnEvent += OnStageBattleInitialized;
            Bus<BattleStartEvent>.OnEvent += OnBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent += OnStageBattleEnded;
            Bus<UpgradeEvent>.OnEvent += OnUpgradeUpdated;
        }

        private void OnDisable()
        {
            Bus<StageBattleInitializedEvent>.OnEvent -= OnStageBattleInitialized;
            Bus<BattleStartEvent>.OnEvent -= OnBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent -= OnStageBattleEnded;
            Bus<UpgradeEvent>.OnEvent -= OnUpgradeUpdated;
            StopAllActiveSkills();
            UnsubscribeCommanderStats();
        }

        private void Update()
        {
            if (_isBattleRunning == false)
                return;

            if (GaugeRecoveryPerSecond > 0f && _currentGauge < MaxGauge)
                SetCurrentGauge(_currentGauge + GaugeRecoveryPerSecond * Time.deltaTime);

            TickSkillStates(Time.deltaTime);
            TickAutoUse(Time.deltaTime);
        }

        public bool TryUseSkill(CommanderSkillType skillType)
        {
            if (CanUseSkill(skillType) == false)
                return false;

            CommanderSkillRuntimeState runtimeState = _runtimeStates[skillType];
            ICommanderSkillEffect skillEffect = _skillEffects[skillType];

            SetCurrentGauge(_currentGauge - runtimeState.SkillData.GaugeCost);
            runtimeState.StartSkill();
            skillEffect.Activate(_battleSpawnData);
            RaiseSkillStateChanged(skillType);
            Bus<CommanderSkillUsedEvent>.Raise(new CommanderSkillUsedEvent(skillType));
            return true;
        }

        public bool CanUseSkill(CommanderSkillType skillType)
        {
            if (_isBattleRunning == false || IsEquipped(skillType) == false)
                return false;

            if (_runtimeStates.TryGetValue(skillType, out CommanderSkillRuntimeState runtimeState) == false ||
                _skillEffects.TryGetValue(skillType, out ICommanderSkillEffect skillEffect) == false)
            {
                return false;
            }

            return runtimeState.IsActive == false &&
                   runtimeState.RemainingCooldown <= 0f &&
                   _currentGauge >= runtimeState.SkillData.GaugeCost &&
                   skillEffect.CanActivate(_battleSpawnData);
        }

        public void SetAutoUseEnabled(bool isEnabled)
        {
            if (_isAutoUseEnabled == isEnabled)
                return;

            _isAutoUseEnabled = isEnabled;
            _autoUseCheckTimer = 0f;
        }

        public CommanderSkillData GetEquippedSkill(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _equippedSkills.Length
                ? _equippedSkills[slotIndex]
                : null;
        }

        public bool TryEquipSkill(int slotIndex, CommanderSkillType skillType)
        {
            if (slotIndex < 0 || slotIndex >= _equippedSkills.Length ||
                _skillDatabase == null ||
                _skillDatabase.TryGetSkill(skillType, out CommanderSkillData skillData) == false ||
                _runtimeStates.ContainsKey(skillType) == false ||
                IsEquipped(skillType))
            {
                return false;
            }

            _equippedSkills[slotIndex] = skillData;
            Bus<CommanderSkillLoadoutChangedEvent>.Raise(new CommanderSkillLoadoutChangedEvent());
            return true;
        }

        public void UnequipSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _equippedSkills.Length || _equippedSkills[slotIndex] == null)
                return;

            _equippedSkills[slotIndex] = null;
            Bus<CommanderSkillLoadoutChangedEvent>.Raise(new CommanderSkillLoadoutChangedEvent());
        }

        public bool TryGetRuntimeState(
            CommanderSkillType skillType,
            out CommanderSkillRuntimeState runtimeState)
        {
            return _runtimeStates.TryGetValue(skillType, out runtimeState);
        }

        private void InitializeSkills()
        {
            _runtimeStates.Clear();
            _skillEffects.Clear();

            if (_skillDatabase != null)
            {
                IReadOnlyList<CommanderSkillData> skills = _skillDatabase.Skills;

                for (int i = 0; i < skills.Count; i++)
                    RegisterSkill(skills[i]);
            }

            IReadOnlyList<CommanderSkillData> loadoutSkills = _skillLoadout != null
                ? _skillLoadout.EquippedSkills
                : Array.Empty<CommanderSkillData>();
            _equippedSkills = new CommanderSkillData[loadoutSkills.Count];

            for (int i = 0; i < loadoutSkills.Count; i++)
            {
                CommanderSkillData skillData = loadoutSkills[i];

                if (skillData != null &&
                    _runtimeStates.ContainsKey(skillData.SkillType) &&
                    IsEquipped(skillData.SkillType) == false)
                {
                    _equippedSkills[i] = skillData;
                }
            }
        }

        private void RegisterSkill(CommanderSkillData skillData)
        {
            if (skillData == null ||
                skillData.SkillType == CommanderSkillType.None ||
                _runtimeStates.ContainsKey(skillData.SkillType))
            {
                return;
            }

            ICommanderSkillEffect skillEffect = CreateSkillEffect(skillData);

            if (skillEffect == null)
                return;

            _runtimeStates.Add(skillData.SkillType, new CommanderSkillRuntimeState(skillData));
            _skillEffects.Add(skillData.SkillType, skillEffect);
        }

        private ICommanderSkillEffect CreateSkillEffect(CommanderSkillData skillData)
        {
            switch (skillData)
            {
                case TotalOffensiveSkillData totalOffensiveSkillData:
                    return new TotalOffensiveSkillEffect(totalOffensiveSkillData);
                case FocusFireSkillData _:
                    return new FocusFireSkillEffect();
                default:
                    Debug.LogError($"Skill effect is not implemented: {skillData.name}", skillData);
                    return null;
            }
        }

        private void TickSkillStates(float deltaTime)
        {
            foreach (CommanderSkillRuntimeState runtimeState in _runtimeStates.Values)
            {
                bool wasActive = runtimeState.IsActive;
                runtimeState.Tick(deltaTime);

                if (wasActive == false)
                    continue;

                ICommanderSkillEffect skillEffect = _skillEffects[runtimeState.SkillData.SkillType];
                bool canContinue = runtimeState.IsActive && skillEffect.Tick(_battleSpawnData);

                if (canContinue)
                    continue;

                runtimeState.StopSkill();
                skillEffect.Deactivate(_battleSpawnData);
                RaiseSkillStateChanged(runtimeState.SkillData.SkillType);
            }
        }

        private void TickAutoUse(float deltaTime)
        {
            if (_isAutoUseEnabled == false)
                return;

            _autoUseCheckTimer -= deltaTime;

            if (_autoUseCheckTimer > 0f)
                return;

            _autoUseCheckTimer = _autoUseCheckInterval;

            if (TryUseFirstAvailableSkill(CommanderSkillCategory.Support) == false)
                TryUseFirstAvailableSkill(CommanderSkillCategory.Attack);
        }

        private bool TryUseFirstAvailableSkill(CommanderSkillCategory category)
        {
            for (int i = 0; i < _equippedSkills.Length; i++)
            {
                CommanderSkillData skillData = _equippedSkills[i];

                if (skillData != null &&
                    skillData.Category == category &&
                    TryUseSkill(skillData.SkillType))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsEquipped(CommanderSkillType skillType)
        {
            for (int i = 0; i < _equippedSkills.Length; i++)
            {
                if (_equippedSkills[i] != null && _equippedSkills[i].SkillType == skillType)
                    return true;
            }

            return false;
        }

        private void OnStageBattleInitialized(StageBattleInitializedEvent evt)
        {
            StopAllActiveSkills();
            _battleSpawnData = evt.BattleSpawnData;
            _hasBattleSpawnData = true;
            SetCommanderStats(evt.BattleSpawnData.MainShip);
        }

        private void OnBattleStarted(BattleStartEvent evt)
        {
            _battleSpawnData = evt.BattleSpawnData;
            _hasBattleSpawnData = true;

            if (SetCommanderStats(evt.BattleSpawnData.MainShip) == false)
                return;

            _isBattleRunning = true;
            _autoUseCheckTimer = 0f;
            SetCurrentGauge(_currentGauge);
        }

        private void OnStageBattleEnded(StageBattleEndedEvent evt)
        {
            _isBattleRunning = false;
            StopAllActiveSkills();
        }

        private void OnUpgradeUpdated(UpgradeEvent evt)
        {
            SetCurrentGauge(_currentGauge);
        }

        private bool SetCommanderStats(MainShip mainShip)
        {
            if (mainShip == null || mainShip.ShipStatCompo == null)
            {
                Debug.LogError($"{nameof(CommanderSkillManager)} needs a main ship commander stat.", this);
                _isBattleRunning = false;
                return false;
            }

            UnsubscribeCommanderStats();
            ShipStatCompo shipStatCompo = mainShip.ShipStatCompo;
            _maxGaugeStat = shipStatCompo.GetStat(
                shipStatCompo.GetCommanderStatSO(CommanderStatType.MaxGauge));
            _gaugeRecoveryPerSecondStat = shipStatCompo.GetStat(
                shipStatCompo.GetCommanderStatSO(CommanderStatType.GaugeRecoveryPerSecond));

            if (_maxGaugeStat != null)
                _maxGaugeStat.OnValueChanged += OnCommanderStatChanged;

            if (_gaugeRecoveryPerSecondStat != null)
                _gaugeRecoveryPerSecondStat.OnValueChanged += OnCommanderStatChanged;

            SetCurrentGauge(_currentGauge);
            return true;
        }

        private void UnsubscribeCommanderStats()
        {
            if (_maxGaugeStat != null)
                _maxGaugeStat.OnValueChanged -= OnCommanderStatChanged;

            if (_gaugeRecoveryPerSecondStat != null)
                _gaugeRecoveryPerSecondStat.OnValueChanged -= OnCommanderStatChanged;

            _maxGaugeStat = null;
            _gaugeRecoveryPerSecondStat = null;
        }

        private void OnCommanderStatChanged(StatSO stat, float currentValue, float previousValue)
        {
            SetCurrentGauge(_currentGauge);
        }

        private void StopAllActiveSkills()
        {
            if (_hasBattleSpawnData == false)
                return;

            foreach (CommanderSkillRuntimeState runtimeState in _runtimeStates.Values)
            {
                if (runtimeState.IsActive == false)
                    continue;

                runtimeState.StopSkill();
                _skillEffects[runtimeState.SkillData.SkillType].Deactivate(_battleSpawnData);
                RaiseSkillStateChanged(runtimeState.SkillData.SkillType);
            }
        }

        private void SetCurrentGauge(float gauge)
        {
            _currentGauge = Mathf.Clamp(gauge, 0f, MaxGauge);
            Bus<CommanderGaugeChangedEvent>.Raise(
                new CommanderGaugeChangedEvent(_currentGauge, MaxGauge));
        }

        private static void RaiseSkillStateChanged(CommanderSkillType skillType)
        {
            Bus<CommanderSkillStateChangedEvent>.Raise(
                new CommanderSkillStateChangedEvent(skillType));
        }
    }
}
