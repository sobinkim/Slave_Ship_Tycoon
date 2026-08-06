using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class BattleManager : MonoBehaviour
    {
        private bool isBattleEnded;

        private void OnEnable()
        {
            Bus<BattleStartEvent>.OnEvent += StartBattle;
            Bus<StageEnemiesDefeatedEvent>.OnEvent += StageEnemiesDefeated;
            Bus<MainShipDeadEvent>.OnEvent += DeadMainShip;
        }

        private void OnDisable()
        {
            Bus<BattleStartEvent>.OnEvent -= StartBattle;
            Bus<StageEnemiesDefeatedEvent>.OnEvent -= StageEnemiesDefeated;
            Bus<MainShipDeadEvent>.OnEvent -= DeadMainShip;
        }

        private void StartBattle(BattleStartEvent evt)
        {
            isBattleEnded = false;
        }

        private void DeadMainShip(MainShipDeadEvent evt)
        {
            EndBattle(false);
        }

        private void StageEnemiesDefeated(StageEnemiesDefeatedEvent evt)
        {
            EndBattle(true);
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
