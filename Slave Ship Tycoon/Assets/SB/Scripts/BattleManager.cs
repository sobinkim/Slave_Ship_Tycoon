using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class BattleManager : MonoBehaviour
    {
        private int spawnedEnemyCount = 0;
        private bool isBattleEnded;

        [SerializeField] private MainShip mainShip;

        private void OnEnable()
        {
            Bus<StageStartedEvent>.OnEvent += SpawnStageEnemies;
            Bus<EnemyEvents.EnemyDead>.OnEvent += EnemyDead;

            if (mainShip != null)
                mainShip.OnDeathEvent.AddListener(DeadMainShip);
            else
                Debug.LogWarning($"{nameof(BattleManager)} has no {nameof(MainShip)} assigned.");
        }

        private void OnDisable()
        {
            Bus<StageStartedEvent>.OnEvent -= SpawnStageEnemies;
            Bus<EnemyEvents.EnemyDead>.OnEvent -= EnemyDead;

            if (mainShip != null)
                mainShip.OnDeathEvent.RemoveListener(DeadMainShip);
        }

        private void DeadMainShip()
        {
            EndBattle(false);
        }

        private void AllEnemiesDead()
        {
            EndBattle(true);
        }

        private void EnemyDead(EnemyEvents.EnemyDead evt)
        {
            if (spawnedEnemyCount <= 0)
                return;

            spawnedEnemyCount--;

            if (spawnedEnemyCount == 0)
            {
                AllEnemiesDead();
            }
        }

        private void SpawnStageEnemies(StageStartedEvent evt)
        {
            isBattleEnded = false;
        }

        private void EndBattle(bool isClear)
        {
            if (isBattleEnded)
                return;

            isBattleEnded = true;
            Bus<StageBattleEndedEvent>.Raise(new StageBattleEndedEvent(isClear));
        }
    }
}
