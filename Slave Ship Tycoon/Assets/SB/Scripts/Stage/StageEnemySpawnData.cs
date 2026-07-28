using System;
using System.Collections.Generic;

namespace SB.Scripts
{
    public readonly struct StageEnemySpawnData
    {
        private readonly Enemy[] enemies;

        public IReadOnlyList<Enemy> Enemies => enemies ?? Array.Empty<Enemy>();
        public int SpawnedEnemyCount => enemies?.Length ?? 0;

        public StageEnemySpawnData(IReadOnlyList<Enemy> spawnedEnemies)
        {
            enemies = new Enemy[spawnedEnemies?.Count ?? 0];

            for (int i = 0; i < enemies.Length; i++)
                enemies[i] = spawnedEnemies[i];
        }
    }
}
