using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class BattleManager : MonoBehaviour
    {
        private bool isBattleEnded;
        private Enemy pursuitTarget;

        public void SetPursuitTarget(Enemy target)
        {
            if (pursuitTarget != null) pursuitTarget.OnDeathEvent.RemoveListener(PursuitTargetDestroyed);
            pursuitTarget = target;
            if (pursuitTarget != null) pursuitTarget.OnDeathEvent.AddListener(PursuitTargetDestroyed);
        }

        private void PursuitTargetDestroyed() => EndBattle(true);
        public void EndPursuit(bool success) => EndBattle(success);

        private void OnEnable()
        {
            Bus<BattleStartEvent>.OnEvent += StartBattle;
            Bus<StageEnemiesDefeatedEvent>.OnEvent += StageEnemiesDefeated;
            Bus<MainShipDeadEvent>.OnEvent += DeadMainShip;
        }

        private void OnDisable()
        {
            SetPursuitTarget(null);
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
            if (pursuitTarget != null && !pursuitTarget.IsDead) return;
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
