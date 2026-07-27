using System;
using System.Collections.Generic;
using SB.Scripts;

namespace SB.Scripts
{
    public readonly struct BattleSpawnData
    {
        private readonly Ship[] escortShips;

        public MainShip MainShip { get; }
        public IReadOnlyList<Ship> EscortShips => escortShips ?? Array.Empty<Ship>();
        public StageEnemySpawnData EnemySpawnData { get; }

        public BattleSpawnData(MainShip mainShip, IReadOnlyList<Ship> spawnedEscortShips, StageEnemySpawnData enemySpawnData)
        {
            MainShip = mainShip;
            EnemySpawnData = enemySpawnData;

            escortShips = new Ship[spawnedEscortShips?.Count ?? 0];

            for (int i = 0; i < escortShips.Length; i++)
                escortShips[i] = spawnedEscortShips[i];
        }
    }
}

namespace SB.Core.EventBus
{
    public readonly struct BattleStartEvent : IEvent
    {
        public readonly BattleSpawnData BattleSpawnData;
        public StageEnemySpawnData SpawnData => BattleSpawnData.EnemySpawnData;

        public BattleStartEvent(StageEnemySpawnData spawnData)
        {
            BattleSpawnData = new BattleSpawnData(null, null, spawnData);
        }

        public BattleStartEvent(BattleSpawnData battleSpawnData)
        {
            BattleSpawnData = battleSpawnData;
        }
    }

    public readonly struct StageEnemiesDefeatedEvent : IEvent
    {
    }

    public readonly struct MainShipDeadEvent : IEvent
    {
    }
}
