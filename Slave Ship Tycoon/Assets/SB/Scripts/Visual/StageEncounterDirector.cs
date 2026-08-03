using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class StageEncounterDirector : MonoBehaviour
    {
        [SerializeField] private Transform enemyFleetRoot;
        [SerializeField] private Transform enemyFleetRootDestination;
        [SerializeField] private float moveSpeed = 2f;
        private Vector3 enemyFleetRootStartPosition;
        private BattleSpawnData currentBattleSpawnData;
        private bool _isPlaying;

        private void OnEnable()
        {
            if (enemyFleetRoot != null)
                enemyFleetRootStartPosition = enemyFleetRoot.position;

            Bus<StageBattleEndedEvent>.OnEvent += ResetEnemyFleetRootPosition;
        }

        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= ResetEnemyFleetRootPosition;
        }

        private void Update()
        {
            if (_isPlaying == false)
                return;

            MoveEnemyFleetRoot();
        }


        private void MoveEnemyFleetRoot()
        {
            Vector3 currentPosition = enemyFleetRoot.position;
            Vector3 targetPosition = currentPosition;
            targetPosition.x = enemyFleetRootDestination.position.x;

            enemyFleetRoot.position = Vector3.MoveTowards(
                currentPosition,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (Mathf.Approximately(enemyFleetRoot.position.x, enemyFleetRootDestination.position.x))
            {
                _isPlaying = false;
                Bus<StopEncounterSequence>.Raise(new StopEncounterSequence());
                Bus<BattleStartEvent>.Raise(new BattleStartEvent(currentBattleSpawnData));
            }
        }

        public void PlayEncounterSequence(BattleSpawnData battleSpawnData)
        {
            enemyFleetRoot.position = enemyFleetRootStartPosition;
            currentBattleSpawnData = battleSpawnData;
            _isPlaying = true;
            Bus<StartEncounterSequence>.Raise(new StartEncounterSequence());
        }

        private void ResetEnemyFleetRootPosition(StageBattleEndedEvent evt)
        {
            _isPlaying = false;
            enemyFleetRoot.position = enemyFleetRootStartPosition;
        }
    }
}
