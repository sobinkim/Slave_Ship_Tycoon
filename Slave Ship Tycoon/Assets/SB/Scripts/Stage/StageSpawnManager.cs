using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.AttackCompo;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class StageSpawnManager : MonoBehaviour
    {
        [Header("Player Fleet")]
        [SerializeField] private PlayerFleetLoadout playerFleetLoadout;
        [SerializeField] private PlayerFleetUpgradeProvider playerFleetUpgradeProvider;
        [SerializeField] private Transform mainShipSpawnPoint;
        [SerializeField] private Transform[] escortShipSpawnPoints = Array.Empty<Transform>();
        [SerializeField] private Transform spawnedPlayerFleetParent;
        [SerializeField] private StageEncounterDirector stageEncounterDirector;

        [Header("Enemies")]
        [SerializeField] private ChapterDatabase chapterDatabase;
        [SerializeField] private Transform[] spawnPoints = new Transform[StageEnemyLayout.SlotCount];
        [SerializeField] private Transform spawnedEnemyParent;

        [Header("Gizmos")]
        [SerializeField, Min(0.05f)] private float gizmoRadius = 0.35f;

        private readonly BattleParticipantTracker participantTracker = new BattleParticipantTracker();
        private PlayerFleetLoadout runtimePlayerFleetLoadout;

        public PlayerFleetLoadout PlayerFleetLoadout => runtimePlayerFleetLoadout;

        private void Awake()
        {
            if (playerFleetLoadout == null)
                return;

            runtimePlayerFleetLoadout = Instantiate(playerFleetLoadout);
            runtimePlayerFleetLoadout.name = $"{playerFleetLoadout.name} (Runtime)";
        }

        private void OnEnable()
        {
            Bus<StageStartedEvent>.OnEvent += SpawnCurrentStageBattle;
            participantTracker.StartListening();
        }

        private void OnDisable()
        {
            Bus<StageStartedEvent>.OnEvent -= SpawnCurrentStageBattle;
            participantTracker.StopListening();
            participantTracker.ClearBattleParticipants();
        }

        private void OnDestroy()
        {
            if (runtimePlayerFleetLoadout != null)
                Destroy(runtimePlayerFleetLoadout);
        }

        public void SpawnStageBattle(int chapter, int stage)
        {
            ClearSpawnedBattleParticipants();

            if (!TrySpawnPlayerFleet(out MainShip mainShip, out List<Ship> escortShips))
                return;

            if (!TrySpawnEnemies(chapter, stage, out List<Enemy> spawnedEnemies))
            {
                ClearSpawnedPlayerFleet();
                return;
            }

            StageEnemySpawnData enemySpawnData = new StageEnemySpawnData(spawnedEnemies);
            BattleSpawnData battleSpawnData = new BattleSpawnData(mainShip, escortShips, enemySpawnData);

            if (stageEncounterDirector == null)
            {
                Debug.LogError($"{nameof(StageSpawnManager)} needs a {nameof(StageEncounterDirector)}.", this);
                return;
            }

            stageEncounterDirector.PlayEncounterSequence(battleSpawnData);
        }

        public void SpawnStageEnemies(int chapter, int stage)
        {
            SpawnStageBattle(chapter, stage);
        }

        public void ClearSpawnedBattleParticipants()
        {
            participantTracker.ClearBattleParticipants();
        }

        public void ClearSpawnedEnemies()
        {
            participantTracker.ClearEnemies();
        }

        private void SpawnCurrentStageBattle(StageStartedEvent evt)
        {
            SpawnStageBattle(evt.Chapter, evt.Stage);
        }

        private bool TrySpawnPlayerFleet(out MainShip mainShip, out List<Ship> escortShips)
        {
            mainShip = null;
            escortShips = new List<Ship>();

            if (runtimePlayerFleetLoadout == null)
            {
                Debug.LogError($"{nameof(StageSpawnManager)} needs a player fleet loadout.", this);
                return false;
            }

            MainShip mainShipPrefab = runtimePlayerFleetLoadout.MainShipPrefab;

            if (mainShipPrefab != null)
            {
                if (mainShipSpawnPoint == null)
                {
                    Debug.LogError($"{nameof(StageSpawnManager)} needs a main ship spawn point.", this);
                    return false;
                }

                mainShip = SpawnShip(
                    mainShipPrefab,
                    mainShipSpawnPoint,
                    mainShipSpawnPoint,
                    out bool isMainShipPooled);

                if (mainShip == null)
                    return false;

                mainShip.SetSpawnSlot(new Vector2Int(-1, -1));
                SetPlayerFleetUpgradeProvider(mainShip);
                participantTracker.RegisterMainShip(mainShip, isMainShipPooled);
            }

            int count = runtimePlayerFleetLoadout.EscortSlotCount;

            for (int i = 0; i < count; i++)
            {
                Ship prefab = runtimePlayerFleetLoadout.GetEscortAt(i);

                if (prefab == null)
                    continue;

                if (escortShipSpawnPoints == null || i >= escortShipSpawnPoints.Length)
                {
                    Debug.LogError($"Escort Ship Spawn Point {i + 1} is required by {runtimePlayerFleetLoadout.name}.", this);
                    ClearSpawnedPlayerFleet();
                    return false;
                }

                Transform spawnPoint = escortShipSpawnPoints[i];

                if (spawnPoint == null)
                {
                    Debug.LogError($"Escort Ship Spawn Point {i + 1} is required.", this);
                    ClearSpawnedPlayerFleet();
                    return false;
                }

                Ship escortShip = SpawnShip(
                    prefab,
                    spawnPoint,
                    spawnPoint,
                    out bool isEscortShipPooled);

                if (escortShip == null)
                    continue;

                escortShip.SetSpawnSlot(new Vector2Int(
                    i % PlayerFleetLoadout.ColumnCount,
                    i / PlayerFleetLoadout.ColumnCount));

                SetPlayerFleetUpgradeProvider(escortShip);
                participantTracker.RegisterEscortShip(escortShip, isEscortShipPooled);
                escortShips.Add(escortShip);
            }

            return true;
        }

        private bool TrySpawnEnemies(int chapter, int stage, out List<Enemy> spawnedEnemies)
        {
            spawnedEnemies = null;

            if (chapterDatabase == null)
            {
                Debug.LogError($"{nameof(StageSpawnManager)} needs a chapter database.", this);
                return false;
            }

            if (!chapterDatabase.TryGetStage(chapter, stage, out StageEnemyLayout layout))
            {
                Debug.LogError($"Enemy layout not found for Chapter {chapter}, Stage {stage}.", this);
                return false;
            }

            if (!ValidateLayout(layout))
                return false;

            spawnedEnemies = new List<Enemy>(layout.EnemyCount);

            for (int slotIndex = 0; slotIndex < StageEnemyLayout.SlotCount; slotIndex++)
            {
                Enemy prefab = layout.GetEnemyAt(slotIndex);

                if (prefab == null)
                    continue;

                Enemy enemy = SpawnEnemy(prefab, spawnPoints[slotIndex], out bool isPooled);

                if (enemy == null)
                    continue;

                enemy.SetSpawnSlot(new Vector2Int(
                    slotIndex % StageEnemyLayout.GridSize,
                    slotIndex / StageEnemyLayout.GridSize));

                participantTracker.RegisterEnemy(enemy, isPooled);
                spawnedEnemies.Add(enemy);
            }

            if (spawnedEnemies.Count > 0)
                return true;

            Debug.LogError($"Chapter {chapter}, Stage {stage} did not spawn any enemies.", this);
            return false;
        }

        private void ClearSpawnedPlayerFleet()
        {
            participantTracker.ClearPlayerFleet();
        }

        private void SetPlayerFleetUpgradeProvider(Ship ship)
        {
            if (ship == null)
                return;

            ShipCombatStatCompo combatStatCompo = ship.GetCompo<ShipCombatStatCompo>();
            combatStatCompo?.SetPlayerFleetUpgradeProvider(playerFleetUpgradeProvider);
        }

        private T SpawnShip<T>(T prefab, Transform spawnPoint, Transform parent, out bool isPooled)
            where T : Ship
        {
            PoolingManager poolingManager = PoolingManager.Instance;
            T ship;
            isPooled = poolingManager != null;

            if (poolingManager != null)
            {
                ship = poolingManager.Get(prefab, spawnPoint.position, spawnPoint.rotation, parent);
            }
            else
            {
                ship = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, parent);
                Debug.LogWarning($"{nameof(PoolingManager)} was not found. Ship was instantiated normally.", this);
            }

            return ship;
        }

        private Enemy SpawnEnemy(Enemy prefab, Transform spawnPoint, out bool isPooled)
        {
            PoolingManager poolingManager = PoolingManager.Instance;
            Enemy enemy;
            isPooled = poolingManager != null;

            if (poolingManager != null)
            {
                enemy = poolingManager.Get(prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            }
            else
            {
                enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                Debug.LogWarning($"{nameof(PoolingManager)} was not found. Enemy was instantiated normally.", this);
            }

            return enemy;
        }
        private bool ValidateLayout(StageEnemyLayout layout)
        {
            if (layout.EnemyCount == 0)
            {
                Debug.LogError($"{layout.name} has no enemies.", layout);
                return false;
            }

            HashSet<Transform> usedSpawnPoints = new HashSet<Transform>();

            for (int slotIndex = 0; slotIndex < StageEnemyLayout.SlotCount; slotIndex++)
            {
                if (layout.GetEnemyAt(slotIndex) == null)
                    continue;

                Transform spawnPoint = spawnPoints[slotIndex];

                if (spawnPoint == null)
                {
                    Debug.LogError($"Spawn Point {slotIndex + 1} is required by {layout.name}.", this);
                    return false;
                }

                if (!usedSpawnPoints.Add(spawnPoint))
                {
                    Debug.LogError($"Spawn Point {slotIndex + 1} is assigned more than once.", this);
                    return false;
                }
            }

            return true;
        }

        private void OnValidate()
        {
            if (spawnPoints == null)
                spawnPoints = new Transform[StageEnemyLayout.SlotCount];
            else if (spawnPoints.Length != StageEnemyLayout.SlotCount)
                Array.Resize(ref spawnPoints, StageEnemyLayout.SlotCount);
        }

        private void OnDrawGizmos()
        {
            DrawSpawnPointGizmos(spawnPoints, new Color(1f, 0.35f, 0.15f, 0.9f));
            DrawSpawnPointGizmos(escortShipSpawnPoints, new Color(0.2f, 0.7f, 1f, 0.9f));

            if (mainShipSpawnPoint != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.9f);
                Gizmos.DrawWireSphere(mainShipSpawnPoint.position, gizmoRadius);
            }
        }

        private void DrawSpawnPointGizmos(Transform[] points, Color color)
        {
            if (points == null)
                return;

            Gizmos.color = color;

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] != null)
                    Gizmos.DrawWireSphere(points[i].position, gizmoRadius);
            }
        }
    }
}
