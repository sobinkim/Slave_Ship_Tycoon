using System;
using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    [DisallowMultipleComponent]
    public sealed class StageSpawnManager : MonoBehaviour
    {
        [Header("Player Fleet")]
        [SerializeField] private PlayerFleetLoadout playerFleetLoadout;
        [SerializeField] private Transform mainShipSpawnPoint;
        [SerializeField] private Transform[] escortShipSpawnPoints = Array.Empty<Transform>();
        [SerializeField] private Transform spawnedPlayerFleetParent;

        [Header("Enemies")]
        [SerializeField] private StageEnemyLayoutDatabase layoutDatabase;
        [SerializeField] private Transform[] spawnPoints = new Transform[StageEnemyLayout.SlotCount];
        [SerializeField] private Transform spawnedEnemyParent;

        [Header("Gizmos")]
        [SerializeField, Min(0.05f)] private float gizmoRadius = 0.35f;

        private readonly HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
        private readonly HashSet<Enemy> pooledEnemies = new HashSet<Enemy>();
        private readonly HashSet<Ship> pooledPlayerShips = new HashSet<Ship>();
        private readonly List<Ship> spawnedEscortShips = new List<Ship>();

        private MainShip spawnedMainShip;
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
            Bus<EnemyEvents.EnemyDead>.OnEvent += HandleEnemyDead;
        }

        private void OnDisable()
        {
            Bus<StageStartedEvent>.OnEvent -= SpawnCurrentStageBattle;
            Bus<EnemyEvents.EnemyDead>.OnEvent -= HandleEnemyDead;
            ClearSpawnedBattleParticipants();
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

            Bus<BattleStartEvent>.Raise(new BattleStartEvent(battleSpawnData));
        }

        public void SpawnStageEnemies(int chapter, int stage)
        {
            SpawnStageBattle(chapter, stage);
        }

        public void ClearSpawnedBattleParticipants()
        {
            ClearSpawnedPlayerFleet();
            ClearSpawnedEnemies();
        }

        public void ClearSpawnedEnemies()
        {
            if (activeEnemies.Count == 0)
                return;

            Enemy[] enemies = new Enemy[activeEnemies.Count];
            activeEnemies.CopyTo(enemies);
            activeEnemies.Clear();

            for (int i = 0; i < enemies.Length; i++)
                ReleaseEnemy(enemies[i]);
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

                mainShip = SpawnShip(mainShipPrefab, mainShipSpawnPoint, spawnedPlayerFleetParent);

                if (mainShip == null)
                    return false;

                spawnedMainShip = mainShip;
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

                Ship escortShip = SpawnShip(prefab, spawnPoint, spawnedPlayerFleetParent);

                if (escortShip == null)
                    continue;

                spawnedEscortShips.Add(escortShip);
                escortShips.Add(escortShip);
            }

            return true;
        }

        private bool TrySpawnEnemies(int chapter, int stage, out List<Enemy> spawnedEnemies)
        {
            spawnedEnemies = null;

            if (layoutDatabase == null)
            {
                Debug.LogError($"{nameof(StageSpawnManager)} needs a layout database.", this);
                return false;
            }

            if (!layoutDatabase.TryGetLayout(chapter, stage, out StageEnemyLayout layout))
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

                Enemy enemy = SpawnEnemy(prefab, spawnPoints[slotIndex]);

                if (enemy == null)
                    continue;

                activeEnemies.Add(enemy);
                spawnedEnemies.Add(enemy);
            }

            if (spawnedEnemies.Count > 0)
                return true;

            Debug.LogError($"Chapter {chapter}, Stage {stage} did not spawn any enemies.", this);
            return false;
        }

        private void ClearSpawnedPlayerFleet()
        {
            for (int i = 0; i < spawnedEscortShips.Count; i++)
                ReleaseShip(spawnedEscortShips[i]);

            spawnedEscortShips.Clear();

            if (spawnedMainShip != null)
            {
                ReleaseShip(spawnedMainShip);
                spawnedMainShip = null;
            }
        }

        private void HandleEnemyDead(EnemyEvents.EnemyDead evt)
        {
            if (evt.Entity is not Enemy enemy || !activeEnemies.Remove(enemy))
                return;

            ReleaseEnemy(enemy);

            if (activeEnemies.Count == 0)
                Bus<StageEnemiesDefeatedEvent>.Raise(new StageEnemiesDefeatedEvent());
        }

        private T SpawnShip<T>(T prefab, Transform spawnPoint, Transform parent) where T : Ship
        {
            PoolingManager poolingManager = PoolingManager.Instance;
            T ship;

            if (poolingManager != null)
            {
                ship = poolingManager.Get(prefab, spawnPoint.position, spawnPoint.rotation, parent);

                if (ship != null)
                    pooledPlayerShips.Add(ship);
            }
            else
            {
                ship = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, parent);
                Debug.LogWarning($"{nameof(PoolingManager)} was not found. Ship was instantiated normally.", this);
            }

            return ship;
        }

        private Enemy SpawnEnemy(Enemy prefab, Transform spawnPoint)
        {
            PoolingManager poolingManager = PoolingManager.Instance;
            Enemy enemy;

            if (poolingManager != null)
            {
                enemy = poolingManager.Get(prefab, spawnPoint.position, spawnPoint.rotation, spawnedEnemyParent);

                if (enemy != null)
                    pooledEnemies.Add(enemy);
            }
            else
            {
                enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnedEnemyParent);
                Debug.LogWarning($"{nameof(PoolingManager)} was not found. Enemy was instantiated normally.", this);
            }

            return enemy;
        }

        private void ReleaseShip(Ship ship)
        {
            if (ship == null)
                return;

            if (pooledPlayerShips.Remove(ship) && PoolingManager.Instance != null)
                PoolingManager.Instance.Release(ship);
            else
                Destroy(ship.gameObject);
        }

        private void ReleaseEnemy(Enemy enemy)
        {
            if (enemy == null)
                return;

            if (pooledEnemies.Remove(enemy) && PoolingManager.Instance != null)
                PoolingManager.Instance.Release(enemy);
            else
                Destroy(enemy.gameObject);
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

        private void OnDrawGizmosSelected()
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
