using System;
using System.Collections.Generic;
using SB.Core;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.Scripts.Bounty
{
    public enum PursuitState { Inactive, Entering, Chasing, Result }
    public enum PursuitResult { Success, Escaped, MainShipLost, Retreated }

    [DisallowMultipleComponent]
    public sealed class BountyPursuitController : MonoBehaviour
    {
        [SerializeField] private GameFlowManager gameFlow;
        [SerializeField] private StageSpawnManager spawner;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BountyNightEnvironment environment;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private BountyPursuitDefinition[] pursuits = Array.Empty<BountyPursuitDefinition>();
        [SerializeField] private int defaultPursuit;
        [SerializeField, Min(1f)] private float encounterTimeoutSeconds = 15f;

        private readonly List<Ship> playerShips = new List<Ship>();
        private readonly List<ShipWakeEffect> sailingWakes = new List<ShipWakeEffect>();
        private readonly Dictionary<Vector2Int, float> previousHealth = new Dictionary<Vector2Int, float>();
        private float previousMainHealth = 1f;
        private float enteringSeconds;
        private EntityHealth targetHealth;
        private Enemy target;
        private int selectedPursuit;
        private bool restoringHealth;
        public PursuitState State { get; private set; }
        public PursuitResult Result { get; private set; }
        public bool IsActive => State != PursuitState.Inactive;
        public bool CanEnter => !IsActive && gameFlow != null && gameFlow.CanStartPursuit && Time.timeScale > 0f;
        public BountyPursuitDefinition Definition { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float RemainingSeconds => Definition == null ? 0 : Mathf.Max(0, Definition.EscapeSeconds - ElapsedSeconds);
        public float Progress => Definition == null ? 0 : Mathf.Clamp01(ElapsedSeconds / Definition.EscapeSeconds);
        public float TargetHealthRatio => targetHealth != null ? targetHealth.NormalizedHealth : 0;
        public int PursuitCount => pursuits.Length;
        public event Action Changed;

        private void OnEnable()
        {
            Bus<BattleStartEvent>.OnEvent += HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
            Bus<StageBattleInitializedEvent>.OnEvent += RestoreFarmingHealth;
        }

        private void OnDisable()
        {
            Bus<BattleStartEvent>.OnEvent -= HandleBattleStarted;
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            Bus<StageBattleInitializedEvent>.OnEvent -= RestoreFarmingHealth;
            if (!IsActive) return;
            StopWakes();
            if (battleManager != null) battleManager.SetPursuitTarget(null);
            if (environment != null) environment.Leave();
            if (gameFlow != null) gameFlow.LeavePursuit(false);
            State = PursuitState.Inactive;
        }

        public void EnterDefault() => Enter(defaultPursuit);

        public bool Enter(int index)
        {
            if (!CanEnter || index < 0 || index >= pursuits.Length || pursuits[index] == null ||
                pursuits[index].EnemyLayout == null || !gameFlow.TryEnterPursuit()) return false;
            selectedPursuit = index;
            Definition = pursuits[index];
            previousHealth.Clear();
            spawner.CollectPlayerShips(playerShips);
            foreach (Ship ship in playerShips)
            {
                var health = ship.GetCompo<EntityHealth>();
                if (ship is MainShip) previousMainHealth = health != null ? health.NormalizedHealth : 1f;
                else previousHealth[ship.myShipData.SpawnSlot] = health != null ? health.NormalizedHealth : 1f;
            }
            // Ends projectiles, commander skills and creature encounters before switching the roster.
            Bus<StageBattleEndedEvent>.Raise(new StageBattleEndedEvent(false));
            environment.Enter();
            return StartAttempt();
        }

        private bool StartAttempt()
        {
            State = PursuitState.Entering;
            ElapsedSeconds = enteringSeconds = 0f;
            Result = PursuitResult.Retreated;
            targetHealth = null;
            battleManager.SetPursuitTarget(null);
            environment.RestoreShipLighting();
            if (!spawner.SpawnPursuitBattle(Definition.EnemyLayout, Definition.MainPirateSlot,
                out target, out BattleSpawnData battle))
            {
                Debug.LogError("현상금 추적의 함대·메인 해적선·슬롯 연결을 확인하세요.", this);
                ReturnToFarming();
                return false;
            }
            targetHealth = target.GetCompo<EntityHealth>();
            environment.ApplyShipLighting(battle.MainShip);
            foreach (Ship ship in battle.EscortShips) environment.ApplyShipLighting(ship);
            foreach (Enemy enemy in battle.EnemySpawnData.Enemies) environment.ApplyShipLighting(enemy);
            StopWakes();
            AddWakes(battle.MainShip);
            foreach (Ship ship in battle.EscortShips) AddWakes(ship);
            foreach (Enemy enemy in battle.EnemySpawnData.Enemies) AddWakes(enemy);
            battleManager.SetPursuitTarget(target);
            Changed?.Invoke();
            return true;
        }

        public void Retry()
        {
            if (State != PursuitState.Result || Time.timeScale <= 0f) return;
            environment.SetScrolling(true);
            StartAttempt();
        }

        public void TryNextConvoy()
        {
            if (State != PursuitState.Result || pursuits.Length < 2 || Time.timeScale <= 0f) return;
            selectedPursuit = (selectedPursuit + 1) % pursuits.Length;
            Definition = pursuits[selectedPursuit];
            Retry();
        }

        public void ReturnToFarming()
        {
            if (!IsActive && !gameFlow.IsPursuitActive) return;
            if (State == PursuitState.Chasing) battleManager.EndPursuit(false);
            battleManager.SetPursuitTarget(null);
            StopWakes();
            environment.RestoreShipLighting();
            spawner.ClearSpawnedEnemies();
            spawner.ClearSpawnedBattleParticipants();
            environment.Leave();
            target = null;
            targetHealth = null;
            restoringHealth = true;
            State = PursuitState.Inactive;
            gameFlow.LeavePursuit(true);
            Changed?.Invoke();
        }

        public void Retreat()
        {
            if (State != PursuitState.Chasing) return;
            Result = PursuitResult.Retreated;
            battleManager.EndPursuit(false);
        }

        private void RestoreFarmingHealth(StageBattleInitializedEvent evt)
        {
            if (!restoringHealth || IsActive) return;
            restoringHealth = false;
            evt.BattleSpawnData.MainShip?.GetCompo<EntityHealth>()?.ResetHealth(previousMainHealth);
            foreach (Ship ship in evt.BattleSpawnData.EscortShips)
                if (previousHealth.TryGetValue(ship.myShipData.SpawnSlot, out float ratio))
                    ship.GetCompo<EntityHealth>()?.ResetHealth(ratio);
        }

        private void HandleBattleStarted(BattleStartEvent evt)
        {
            if (State != PursuitState.Entering) return;
            bool containsTarget = false;
            foreach (Enemy enemy in evt.SpawnData.Enemies) if (enemy == target) containsTarget = true;
            if (!containsTarget) return;
            State = PursuitState.Chasing;
            environment.SetScrolling(true);
            Changed?.Invoke();
        }

        private void LateUpdate()
        {
            if (State == PursuitState.Entering)
            {
                enteringSeconds += Time.deltaTime;
                if (enteringSeconds >= encounterTimeoutSeconds) ReturnToFarming();
                return;
            }
            if (State != PursuitState.Chasing) return;
            if (target != null && target.IsDead) { battleManager.EndPursuit(true); return; }
            ElapsedSeconds = Mathf.Min(Definition.EscapeSeconds, ElapsedSeconds + Time.deltaTime);
            if (ElapsedSeconds >= Definition.EscapeSeconds)
            {
                Result = PursuitResult.Escaped;
                battleManager.EndPursuit(false);
            }
        }

        private void HandleBattleEnded(StageBattleEndedEvent evt)
        {
            if (State != PursuitState.Chasing) return;
            if (evt.IsClear) Result = PursuitResult.Success;
            else if (Result != PursuitResult.Escaped && Result != PursuitResult.Retreated)
                Result = PursuitResult.MainShipLost;
            else if (Result == PursuitResult.Retreated)
            {
                spawner.CollectPlayerShips(playerShips);
                foreach (Ship ship in playerShips)
                    if (ship is MainShip && ship.IsDead) Result = PursuitResult.MainShipLost;
            }
            State = PursuitState.Result;
            StopWakes();
            environment.SetScrolling(false);
            if (Result == PursuitResult.Success && Definition.DiamondReward > 0)
                currencyManager.AddCurrency(CurrencyType.Diamond, Definition.DiamondReward);
            Changed?.Invoke();
        }

        private void AddWakes(Ship ship)
        {
            if (ship == null) return;
            foreach (ShipWakeEffect wake in ship.GetComponentsInChildren<ShipWakeEffect>(true))
            { wake.SetSailingOverride(true); sailingWakes.Add(wake); }
        }

        private void StopWakes()
        {
            foreach (ShipWakeEffect wake in sailingWakes) if (wake != null) wake.SetSailingOverride(false);
            sailingWakes.Clear();
        }
    }
}
