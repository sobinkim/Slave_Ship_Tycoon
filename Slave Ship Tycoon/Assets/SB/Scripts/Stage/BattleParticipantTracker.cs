using System.Collections.Generic;
using SB.Core.EventBus;
using UnityEngine;
using UnityEngine.Events;

namespace SB.Scripts
{
    public sealed class BattleParticipantTracker
    {
        private readonly HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
        private readonly HashSet<Enemy> pooledEnemies = new HashSet<Enemy>();
        private readonly HashSet<Ship> playerShips = new HashSet<Ship>();
        private readonly HashSet<Ship> pooledPlayerShips = new HashSet<Ship>();
        private readonly Dictionary<Ship, UnityAction> escortDeathHandlers =
            new Dictionary<Ship, UnityAction>();

        private bool isListening;

        public void StartListening()
        {
            if (isListening)
                return;

            Bus<EnemyEvents.EnemyDead>.OnEvent += HandleEnemyDead;
            isListening = true;
        }

        public void StopListening()
        {
            if (!isListening)
                return;

            Bus<EnemyEvents.EnemyDead>.OnEvent -= HandleEnemyDead;
            isListening = false;
        }

        public void RegisterMainShip(MainShip mainShip, bool isPooled)
        {
            RegisterPlayerShip(mainShip, isPooled);
        }

        public void RegisterEscortShip(Ship escortShip, bool isPooled)
        {
            if (!RegisterPlayerShip(escortShip, isPooled))
                return;

            UnityAction handler = () => HandleEscortShipDead(escortShip);
            escortShip.OnDeathEvent.AddListener(handler);
            escortDeathHandlers.Add(escortShip, handler);
        }

        public void RegisterEnemy(Enemy enemy, bool isPooled)
        {
            if (enemy == null || !activeEnemies.Add(enemy))
                return;

            if (isPooled)
                pooledEnemies.Add(enemy);
        }

        public void ClearBattleParticipants()
        {
            ClearPlayerFleet();
            ClearEnemies();
        }

        public void ClearPlayerFleet()
        {
            if (playerShips.Count == 0)
                return;

            Ship[] ships = new Ship[playerShips.Count];
            playerShips.CopyTo(ships);
            playerShips.Clear();

            for (int i = 0; i < ships.Length; i++)
                ReleasePlayerShip(ships[i]);
        }

        public void ClearEnemies()
        {
            if (activeEnemies.Count == 0)
                return;

            Enemy[] enemies = new Enemy[activeEnemies.Count];
            activeEnemies.CopyTo(enemies);
            activeEnemies.Clear();

            for (int i = 0; i < enemies.Length; i++)
                ReleaseEnemy(enemies[i]);
        }

        private bool RegisterPlayerShip(Ship ship, bool isPooled)
        {
            if (ship == null || !playerShips.Add(ship))
                return false;

            if (isPooled)
                pooledPlayerShips.Add(ship);

            return true;
        }

        private void HandleEnemyDead(EnemyEvents.EnemyDead evt)
        {
            if (evt.Entity is not Enemy enemy || !activeEnemies.Remove(enemy))
                return;

            ReleaseEnemy(enemy);

            if (activeEnemies.Count == 0)
                Bus<StageEnemiesDefeatedEvent>.Raise(new StageEnemiesDefeatedEvent());
        }

        private void HandleEscortShipDead(Ship escortShip)
        {
            if (escortShip == null || !playerShips.Remove(escortShip))
                return;

            ReleasePlayerShip(escortShip);
        }

        private void ReleasePlayerShip(Ship ship)
        {
            if (ship == null)
                return;

            RemoveEscortDeathHandler(ship);

            if (pooledPlayerShips.Remove(ship) && PoolingManager.Instance != null)
                PoolingManager.Instance.Release(ship);
            else
                Object.Destroy(ship.gameObject);
        }

        private void ReleaseEnemy(Enemy enemy)
        {
            if (enemy == null)
                return;

            if (pooledEnemies.Remove(enemy) && PoolingManager.Instance != null)
                PoolingManager.Instance.Release(enemy);
            else
                Object.Destroy(enemy.gameObject);
        }

        private void RemoveEscortDeathHandler(Ship escortShip)
        {
            if (!escortDeathHandlers.Remove(escortShip, out UnityAction handler))
                return;

            escortShip.OnDeathEvent.RemoveListener(handler);
        }
    }
}
