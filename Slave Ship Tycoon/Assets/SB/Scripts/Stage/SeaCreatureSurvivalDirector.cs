using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Currency;
using SB.Scripts.AttackCompo;
using UnityEngine;

namespace SB.Scripts
{
    public enum SeaVoyagePhase { Sailing, Combat, Finished }

    [DisallowMultipleComponent, DefaultExecutionOrder(-80)]
    public sealed class SeaCreatureSurvivalDirector : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private StageSpawnManager fleetSpawner;
        [SerializeField] private PoolingManager poolingManager;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private SeaCreatureEnemy swarmerPrefab;
        [SerializeField] private SeaCreatureEnemy chargerPrefab;
        [SerializeField] private Transform creatureRoot;
        [SerializeField] private SeaVoyageMotor voyageMotor;
        [SerializeField] private ParallaxLayerManager parallaxLayers;
        [SerializeField] private Camera battleCamera;
        [SerializeField, Tooltip("���� �ٱ��� ���� ����. �������� ������ ������ ����մϴ�.")]
        private Transform[] entryPoints = Array.Empty<Transform>();

        [Header("Sailing and encounters")]
        [SerializeField] private bool startOnPlay = true;
        [SerializeField, Tooltip("�׽�Ʈ ���� R ������ Ű�Դϴ�. ���� é�� �帧�� ������ ���� ���ϴ�.")]
        private bool allowRestartShortcut = true;
        [SerializeField, Min(1)] private int rewardChapter = 1;
        [SerializeField, Min(1), Tooltip("�� ������ ù ���̺꿡 ��ġ�ϴ� ũ���� ���Դϴ�.")]
        private int initialCreatures = 6;
        [SerializeField, Min(1)] private int maximumAlive = 16;
        [SerializeField, Min(0.1f), Tooltip("���� �ܰ迡�� �Դ�� ī�޶� ������ �����ϴ� �ʴ� �Ÿ��Դϴ�. ���� �߿��� �����մϴ�.")]
        private float cruiseSpeed = 1.5f;
        [SerializeField, Min(0.1f), Tooltip("�� ������ ��� ������ �� ���� ������ ���濡 ��ġ�ϱ���� �̵��ϴ� �Ÿ��Դϴ�.")]
        private float groupSpacing = 2.85f;
        [SerializeField, Min(0.1f), Tooltip("���� �ռ� ��� �ִ� ȣ���԰� ���� ũ���� ������ �Ÿ��� �� ���� �����ϸ� �Դ븦 ����ϴ�.")]
        private float encounterStartDistance = 2f;
        [SerializeField, Min(0.01f), Tooltip("항해 시작과 조우 정지에 사용하는 속도 전환 시간입니다.")]
        private float sailingBlendSeconds = 0.4f;
        private float sailingSpeed;
        private bool brakingForEncounter;
        [Header("Waves in each encounter")]
        [SerializeField, Min(1)] private int wavesPerEncounter = 3;
        [SerializeField, Min(0), Tooltip("���� ���̺긶�� ù ���̺� ���� �߰��ϴ� ũ���� ���Դϴ�.")]
        private int groupSize = 2;
        [SerializeField, Range(0.01f, 1f), Tooltip("���� ���̺��� ���� ��ü �� �� ������ óġ�ϸ� ���� ���� ���� ���� ���̺갡 �շ��մϴ�.")]
        private float nextWaveKillRatio = 0.7f;
        [SerializeField, Min(0f), Tooltip("������ ���̺긦 ������ ��� ���� ������� �� �����ر��� ��ٸ��� �ð��Դϴ�.")]
        private float waveDelaySeconds = 0.6f;
        [Header("Reinforcement entrance")]
        [SerializeField, Min(0.1f), Tooltip("��ü ��ü�� ȭ�� ������ �ۿ� �ֵ��� �߰��ϴ� ���� �Ÿ��Դϴ�.")]
        private float reinforcementOffscreenPadding = 0.5f;
        [SerializeField, Min(0f), Tooltip("ȭ�� �ۿ��� ��� ��ġ�� �յڷ� �л��ϴ� �Ÿ��Դϴ�.")]
        private float reinforcementEntryDepth = 0.8f;
        [SerializeField, Min(0.1f), Tooltip("������ ȭ�� ������ ����� ������ �ӵ��Դϴ�. ���� �Ŀ��� �� ũ������ ���� �ӵ��� ����մϴ�.")]
        private float reinforcementEntrySpeed = 2.2f;
        [SerializeField, Range(0f, 1f)] private float chargerChance = 0.2f;
        [SerializeField] private Vector2 verticalBounds = new Vector2(-0.15f, 3.1f);
        [SerializeField, Min(0f)] private float entryJitter = 0.18f;
        [SerializeField, Min(0f)] private float entryDepth = 0.65f;

        private readonly List<SeaCreatureEnemy> reservedCreatures = new List<SeaCreatureEnemy>();
        private readonly HashSet<SeaCreatureEnemy> reservedSet = new HashSet<SeaCreatureEnemy>();
        private readonly HashSet<SeaCreatureEnemy> activeCreatures = new HashSet<SeaCreatureEnemy>();
        private readonly List<DyingCreature> dyingCreatures = new List<DyingCreature>();
        private readonly List<SeaCreatureTarget> fleetTargets = new List<SeaCreatureTarget>();
        private readonly List<Enemy> enemyRoster = new List<Enemy>();
        private readonly HashSet<SeaCreatureEnemy> currentWaveCreatures = new HashSet<SeaCreatureEnemy>();
        private readonly List<FleetCombatSettings> fleetCombatSettings = new List<FleetCombatSettings>();
        private int currentWavePlanned;
        private int currentWaveSpawned;
        private int currentWaveDefeated;
        private int pendingWaveSpawns;
        private float spawnRemaining;
        private float waveTransitionRemaining;
        private float timeScaleBeforePause = 1f;
        private long startingGold;

        private readonly struct FleetCombatSettings
        {
            private readonly TargetSelector selector;
            private readonly TargetSelectionMode selectionMode;
            private readonly bool requiresEngagement;

            public FleetCombatSettings(Ship ship)
            {
                selector = ship.GetCompo<TargetSelector>();
                selectionMode = selector != null ? selector.SelectionMode : default;
                requiresEngagement = selector != null && selector.RequiresEngagedSeaCreatures;
            }

            public void Restore()
            {
                if (selector == null) return;
                selector.SetSelectionMode(selectionMode);
                selector.SetRequiresEngagedSeaCreatures(requiresEngagement);
            }
        }

        private readonly struct DyingCreature
        {
            public readonly SeaCreatureEnemy Creature;
            public readonly float ReleaseAt;
            public DyingCreature(SeaCreatureEnemy creature)
            {
                Creature = creature;
                ReleaseAt = Time.time + creature.DeathSeconds;
            }
        }

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public SeaVoyagePhase Phase { get; private set; } = SeaVoyagePhase.Finished;
        public bool IsSailing => IsRunning && Phase == SeaVoyagePhase.Sailing;
        public int EncounterCount { get; private set; }
        public int WavesPerEncounter => wavesPerEncounter;
        public int Wave { get; private set; }
        public int KillCount { get; private set; }
        public int SpawnedCount { get; private set; }
        public int ActiveCount => activeCreatures.Count;
        public int CurrentWavePlanned => currentWavePlanned;
        public int CurrentWaveSpawned => currentWaveSpawned;
        public int CurrentWaveDefeated => currentWaveDefeated;
        public float CurrentWaveKillRatio => currentWavePlanned > 0 ? (float)currentWaveDefeated / currentWavePlanned : 0f;
        public float ElapsedSeconds { get; private set; }
        public float DistanceTravelled { get; private set; }
        public float CruiseSpeed => cruiseSpeed;
        public long EarnedGold => currencyManager != null
            ? Math.Max(0L, currencyManager.GetCurrency(CurrencyType.Gold) - startingGold) : 0L;
        public IReadOnlyCollection<SeaCreatureEnemy> ActiveCreatures => activeCreatures;

        private void OnEnable()
        {
            Bus<EnemyEvents.EnemyDead>.OnEvent += HandleCreatureDead;
            Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        }

        private void Start()
        {
            if (startOnPlay) StartEncounter();
        }

        private void OnDisable()
        {
            Bus<EnemyEvents.EnemyDead>.OnEvent -= HandleCreatureDead;
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            StopEncounter();
        }

        private void Update()
        {
            if (allowRestartShortcut && Input.GetKeyDown(KeyCode.R)) RestartEncounter();
            if (!IsRunning || IsPaused) return;

            ElapsedSeconds += Time.deltaTime;
            ReleaseDeadCreatures();
            if (Phase == SeaVoyagePhase.Sailing) UpdateSailing();
            else if (Phase == SeaVoyagePhase.Combat) UpdateCombat();
        }

        private void UpdateSailing()
        {
            bool hasApproach = TryGetWaitingCreatureX(out float creatureX) && TryGetFrontFleetX(out _);
            float remaining = float.PositiveInfinity;
            if (hasApproach && TryGetFrontFleetX(out float frontX))
                remaining = Mathf.Max(0f, creatureX - frontX - encounterStartDistance);
            float stoppingDistance = sailingSpeed * sailingBlendSeconds * 0.5f;
            brakingForEncounter |= hasApproach && remaining <= stoppingDistance;
            sailingSpeed = Mathf.MoveTowards(sailingSpeed, brakingForEncounter ? 0f : cruiseSpeed,
                cruiseSpeed * Time.deltaTime / Mathf.Max(0.01f, sailingBlendSeconds));
            float distance = Mathf.Min(sailingSpeed * Time.deltaTime, remaining);
            DistanceTravelled += distance;
            voyageMotor.SetDistance(DistanceTravelled);
            ApplySailingPresentation(sailingSpeed / cruiseSpeed);

            if (activeCreatures.Count == 0)
            {
                spawnRemaining -= distance;
                if (spawnRemaining <= 0f) SpawnGroup(initialCreatures, false);
            }

            if (hasApproach && brakingForEncounter && sailingSpeed <= 0f)
                BeginCombat();
        }

        private void BeginCombat()
        {
            Phase = SeaVoyagePhase.Combat;
            Wave = 1;
            waveTransitionRemaining = waveDelaySeconds;
            currentWaveCreatures.Clear();
            currentWavePlanned = activeCreatures.Count;
            currentWaveSpawned = activeCreatures.Count;
            currentWaveDefeated = 0;
            pendingWaveSpawns = 0;
            foreach (SeaCreatureEnemy creature in activeCreatures)
            {
                currentWaveCreatures.Add(creature);
                creature.Engage();
            }
            SetFleetMovement(false);
            ApplySailingPresentation(0f);
            foreach (SeaCreatureTarget member in fleetTargets)
                member.Ship?.ClearSailingPresentation();
            Bus<StopEncounterSequence>.Raise(new StopEncounterSequence());
        }

        private void UpdateCombat()
        {
            SpawnPendingWave();
            int threshold = Mathf.CeilToInt(currentWavePlanned * nextWaveKillRatio);
            if (Wave < wavesPerEncounter && currentWavePlanned > 0 && pendingWaveSpawns == 0 && currentWaveDefeated >= threshold)
            {
                Wave++;
                currentWaveCreatures.Clear();
                currentWavePlanned = Mathf.Min(maximumAlive, initialCreatures + (Wave - 1) * groupSize);
                currentWaveSpawned = 0;
                currentWaveDefeated = 0;
                pendingWaveSpawns = currentWavePlanned;
                SpawnPendingWave();
                waveTransitionRemaining = waveDelaySeconds;
                return;
            }

            // Sinking bodies keep their pool lifetime while the next departure starts.
            if (Wave < wavesPerEncounter || pendingWaveSpawns > 0 || activeCreatures.Count > 0) return;
            waveTransitionRemaining -= Time.deltaTime;
            if (waveTransitionRemaining > 0f) return;

            if (!PrepareFleetForNextEncounter()) return;
            EncounterCount++;
            Wave = 0;
            Phase = SeaVoyagePhase.Sailing;
            sailingSpeed = 0f;
            brakingForEncounter = false;
            spawnRemaining = groupSpacing;
            SetFleetMovement(true);
            Bus<StartEncounterSequence>.Raise(new StartEncounterSequence());
        }

        private bool PrepareFleetForNextEncounter()
        {
            // Use the normal cleared-battle preparation even when the formation is unchanged:
            // destroyed escorts recover their hull/HP, while surviving escorts retain current HP.
            RestoreFleetCombatSettings();
            if (!fleetSpawner.TryPreparePlayerFleet(out MainShip mainShip, out List<Ship> escorts))
            {
                StopEncounter();
                return false;
            }
            ConfigureFleet(mainShip, escorts);
            PublishBattle(mainShip, escorts);
            return true;
        }

        private void SpawnPendingWave()
        {
            if (pendingWaveSpawns <= 0) return;
            pendingWaveSpawns -= SpawnGroup(pendingWaveSpawns, true);
        }

        private bool TryGetWaitingCreatureX(out float creatureX)
        {
            creatureX = float.PositiveInfinity;
            foreach (SeaCreatureEnemy creature in activeCreatures)
                if (creature != null && !creature.IsDead && !creature.IsEngaged)
                    creatureX = Mathf.Min(creatureX, creature.transform.position.x);
            return !float.IsPositiveInfinity(creatureX);
        }

        private bool TryGetFrontFleetX(out float frontX)
        {
            frontX = float.NegativeInfinity;
            foreach (SeaCreatureTarget member in fleetTargets)
                if (member.IsAlive && member.Ship.myShipData.ShipType != ShipType.MainShip)
                    frontX = Mathf.Max(frontX, member.Ship.transform.position.x);
            if (!float.IsNegativeInfinity(frontX)) return true;
            foreach (SeaCreatureTarget member in fleetTargets)
                if (member.IsAlive) frontX = Mathf.Max(frontX, member.Ship.transform.position.x);
            return !float.IsNegativeInfinity(frontX);
        }

        public void StartEncounter()
        {
            if (IsRunning) return;
            if (!ValidateReferences()) return;
            Bus<StageStartedEvent>.Raise(new StageStartedEvent(rewardChapter, 1, ChapterRouteType.Obtain, false));
            if (!fleetSpawner.TryPreparePlayerFleet(out MainShip mainShip, out List<Ship> escorts)) return;
            StartPreparedEncounter(mainShip, escorts);
        }

        private bool ValidateReferences()
        {
            if (fleetSpawner == null || poolingManager == null || swarmerPrefab == null ||
                chargerPrefab == null || creatureRoot == null || voyageMotor == null || battleCamera == null || entryPoints.Length == 0)
            {
                Debug.LogError("SeaCreatureSurvivalDirector requires fleet, pool, creature prefabs, root, camera and entry points.", this);
                return false;
            }
            foreach (Transform entry in entryPoints)
                if (entry == null)
                {
                    Debug.LogError("SeaCreatureSurvivalDirector has an unassigned entry point.", this);
                    return false;
                }
            return true;
        }

        // Production uses the StageStartedEvent already raised by GameFlowManager.
        // The spawner supplies the fleet so no stage event or fleet preparation is repeated.
        public void StartPreparedEncounter(MainShip mainShip, List<Ship> escorts)
        {
            if (IsRunning || !ValidateReferences()) return;
            EnsureCreatureReserve();
            Wave = 0;
            EncounterCount = 0;
            KillCount = 0;
            SpawnedCount = 0;
            currentWaveCreatures.Clear();
            currentWavePlanned = 0;
            currentWaveSpawned = 0;
            currentWaveDefeated = 0;
            pendingWaveSpawns = 0;
            ElapsedSeconds = 0f;
            DistanceTravelled = 0f;
            voyageMotor.SetDistance(0f);
            startingGold = currencyManager != null ? currencyManager.GetCurrency(CurrencyType.Gold) : 0L;
            ConfigureFleet(mainShip, escorts);

            IsRunning = true;
            Phase = SeaVoyagePhase.Sailing;
            sailingSpeed = 0f;
            brakingForEncounter = false;
            spawnRemaining = groupSpacing;
            SpawnGroup(initialCreatures, false);
            PublishBattle(mainShip, escorts);
            // Battle initialization puts ships in Idle. Resume Move afterwards so
            // both routes use the prefab's animation and ShipWakeEffect.
            SetFleetMovement(true);
            Bus<StartEncounterSequence>.Raise(new StartEncounterSequence());
        }

        private void ConfigureFleet(MainShip mainShip, List<Ship> escorts)
        {
            fleetTargets.Clear();
            if (mainShip != null) fleetTargets.Add(new SeaCreatureTarget(mainShip));
            foreach (Ship escort in escorts)
            {
                fleetTargets.Add(new SeaCreatureTarget(escort));
                fleetCombatSettings.Add(new FleetCombatSettings(escort));
                TargetSelector selector = escort.GetCompo<TargetSelector>();
                if (selector != null && selector.Team == TargetTeam.Opponent)
                {
                    selector.ClearCommanderTargetCommand();
                    selector.SetSelectionMode(TargetSelectionMode.Nearest);
                    selector.SetRequiresEngagedSeaCreatures(true);
                }
            }
        }

        private void SetFleetMovement(bool isMoving)
        {
            foreach (SeaCreatureTarget member in fleetTargets)
                if (member.IsAlive)
                    member.Ship.ChangeState(isMoving ? ShipStateType.Move : ShipStateType.Idle);
        }

        private void ApplySailingPresentation(float strength)
        {
            parallaxLayers?.SetTravelStrength(strength);
            foreach (SeaCreatureTarget member in fleetTargets)
                if (member.IsAlive) member.Ship.SetSailingPresentation(strength);
        }

        private void PublishBattle(MainShip mainShip, List<Ship> escorts)
        {
            var battle = new BattleSpawnData(mainShip, escorts, new StageEnemySpawnData(enemyRoster));
            Bus<StageBattleInitializedEvent>.Raise(new StageBattleInitializedEvent(battle));
            Bus<BattleStartEvent>.Raise(new BattleStartEvent(battle));
        }

        private void EnsureCreatureReserve()
        {
            if (reservedCreatures.Count > 0) return;
            // Every wave can overlap with earlier sinking bodies. Reserve the entire
            // encounter per kind so even an all-charger encounter needs no new roster entries.
            int reservePerKind = 0;
            for (int wave = 0; wave < wavesPerEncounter; wave++)
                reservePerKind += Mathf.Min(maximumAlive, initialCreatures + wave * groupSize);
            for (int i = 0; i < reservePerKind; i++)
            {
                Reserve(swarmerPrefab);
                Reserve(chargerPrefab);
            }
            // Keep every instance in the battle roster. Inactive/dead instances are already
            // filtered by TargetSelector; pool reuse preserves references across continuous spawns.
            foreach (SeaCreatureEnemy creature in reservedCreatures) poolingManager.Release(creature);
        }

        private void Reserve(SeaCreatureEnemy prefab)
        {
            SeaCreatureEnemy creature = poolingManager.Get(prefab, creatureRoot.position, Quaternion.identity, creatureRoot);
            reservedCreatures.Add(creature);
            reservedSet.Add(creature);
            enemyRoster.Add(creature);
        }

        private int SpawnGroup(int count, bool isReinforcement)
        {
            int spawned = 0;
            for (int i = 0; i < count && activeCreatures.Count < maximumAlive; i++)
            {
                int entryIndex = isReinforcement ? currentWaveSpawned : i;
                Transform entry = entryPoints[entryIndex % entryPoints.Length];
                Vector3 position = entry.position;
                SeaCreatureEnemy prefab = UnityEngine.Random.value < chargerChance ? chargerPrefab : swarmerPrefab;
                float cameraDepth = Vector3.Dot(position - battleCamera.transform.position, battleCamera.transform.forward);
                float rightEdge = battleCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, cameraDepth)).x;
                if (isReinforcement)
                    position.x = rightEdge + prefab.VisualHalfWidth + reinforcementOffscreenPadding + UnityEngine.Random.Range(0f, reinforcementEntryDepth);
                else position.x += UnityEngine.Random.Range(0f, entryDepth);
                position.y = Mathf.Clamp(position.y + UnityEngine.Random.Range(-entryJitter, entryJitter), verticalBounds.x, verticalBounds.y);
                SeaCreatureEnemy creature = poolingManager.Get(prefab, position, Quaternion.identity, creatureRoot);
                if (!reservedSet.Contains(creature))
                {
                    Debug.LogError("Creature reserve was exhausted. Restart after changing the population limit.", this);
                    poolingManager.Release(creature);
                    return spawned;
                }
                creature.SetSpawnSlot(new Vector2Int(0, 0));
                creature.BeginEncounter(fleetTargets, reservedCreatures, verticalBounds);
                if (isReinforcement) creature.BeginReinforcementEntry(rightEdge, reinforcementEntrySpeed);
                activeCreatures.Add(creature);
                if (isReinforcement)
                {
                    currentWaveCreatures.Add(creature);
                    currentWaveSpawned++;
                }
                SpawnedCount++;
                spawned++;
            }
            return spawned;
        }

        private void HandleCreatureDead(EnemyEvents.EnemyDead evt)
        {
            if (!IsRunning || evt.Entity is not SeaCreatureEnemy creature || !activeCreatures.Remove(creature)) return;
            KillCount++;
            if (currentWaveCreatures.Remove(creature)) currentWaveDefeated++;
            creature.BeginDying();
            dyingCreatures.Add(new DyingCreature(creature));
        }

        private void ReleaseDeadCreatures()
        {
            for (int i = dyingCreatures.Count - 1; i >= 0; i--)
            {
                if (Time.time < dyingCreatures[i].ReleaseAt) continue;
                poolingManager.Release(dyingCreatures[i].Creature);
                dyingCreatures.RemoveAt(i);
            }
        }

        private void HandleBattleEnded(StageBattleEndedEvent evt) => StopEncounter();

        public void StopEncounter()
        {
            bool wasRunning = IsRunning;
            IsRunning = false;
            Phase = SeaVoyagePhase.Finished;
            RestoreTimeScale();
            if (poolingManager != null)
            {
                foreach (SeaCreatureEnemy creature in activeCreatures)
                    if (creature != null) poolingManager.Release(creature);
                foreach (DyingCreature entry in dyingCreatures)
                    if (entry.Creature != null) poolingManager.Release(entry.Creature);
            }
            activeCreatures.Clear();
            currentWaveCreatures.Clear();
            pendingWaveSpawns = 0;
            dyingCreatures.Clear();
            RestoreFleetCombatSettings();
            if (wasRunning) SetFleetMovement(false);
            if (wasRunning) ApplySailingPresentation(0f);
            foreach (SeaCreatureTarget member in fleetTargets) member.Ship?.ClearSailingPresentation();
            fleetTargets.Clear();
            if (voyageMotor != null && wasRunning) voyageMotor.SetDistance(0f);
            if (wasRunning) Bus<StopEncounterSequence>.Raise(new StopEncounterSequence());
        }

        private void RestoreFleetCombatSettings()
        {
            foreach (FleetCombatSettings settings in fleetCombatSettings) settings.Restore();
            fleetCombatSettings.Clear();
        }

        public void RestartEncounter()
        {
            RestoreTimeScale();
            if (IsRunning) Bus<StageBattleEndedEvent>.Raise(new StageBattleEndedEvent(false));
            StopEncounter();
            fleetSpawner.ClearSpawnedBattleParticipants();
            StartEncounter();
        }

        public void TogglePause()
        {
            if (!IsRunning) return;
            if (IsPaused) RestoreTimeScale();
            else
            {
                timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
                IsPaused = true;
            }
        }

        private void RestoreTimeScale()
        {
            if (!IsPaused) return;
            Time.timeScale = timeScaleBeforePause;
            IsPaused = false;
        }
    }
}

