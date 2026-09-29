using System.Collections.Generic;
using SB.Core;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public enum TargetLine
    {
        Any = -1,
        Front = 0,
        Middle = 1,
        Back = 2
    }

    public enum TargetSelectionMode
    {
        FormationDistribution,
        HighestCurrentHealth
    }

    [System.Serializable]
    public struct TargetRule
    {
        public TargetLine Line;
        public ShipRole Role;
    }

    public class TargetSelector : MonoBehaviour, IEntityComponent
    {
        [SerializeField] private TargetRule[] targetRules;
        [SerializeField, Tooltip("각 우선순위 규칙 안에서의 선택 방식. 고체력은 현재 체력 기준이며 동률이면 가까운 함선을 선택합니다.")]
        private TargetSelectionMode selectionMode;
        public TargetSelectionMode SelectionMode => selectionMode;
        public IReadOnlyList<TargetRule> TargetRules => targetRules;
        private Ship myship;
        private BattleSpawnData currentStageBattleSpawnData;
        private int _commanderTargetColumn = -1;


        private void OnEnable()
        {
            Bus<BattleStartEvent>.OnEvent += GetCurrentStageSpawnData;
        }

        private void OnDisable()
        {
            Bus<BattleStartEvent>.OnEvent -= GetCurrentStageSpawnData;
        }

        public void Initialize(Entity entity)
        {
            myship = entity as Ship;
        }

        private void GetCurrentStageSpawnData(BattleStartEvent evt)
        {
            SetBattleSpawnData(evt.BattleSpawnData);
        }

        public void SetBattleSpawnData(BattleSpawnData battleSpawnData)
        {
            currentStageBattleSpawnData = battleSpawnData;
        }

        public Ship GetTarget()
        {
            if (myship == null)
            {
                Debug.LogError($"{nameof(TargetSelector)} needs a {nameof(Ship)} entity.", this);
                return null;
            }

            List<Ship> targetList = new List<Ship>();
            targetList = GetTargetList();

            if (targetList.Count == 0)
                return null;

            if (_commanderTargetColumn >= 0)
                return SelectCommanderTarget(targetList);

            return SelectTargetByRules(targetList);
        }

        public void SetCommanderTargetColumn(int targetColumn)
        {
            _commanderTargetColumn = targetColumn;
        }

        public void ClearCommanderTargetCommand()
        {
            _commanderTargetColumn = -1;
        }

        private Ship SelectCommanderTarget(List<Ship> targetList)
        {
            int ownerRow = myship.myShipData.SpawnSlot.y;

            for (int i = 0; i < targetList.Count; i++)
            {
                Ship target = targetList[i];

                if (target.myShipData.SpawnSlot.x == _commanderTargetColumn &&
                    target.myShipData.SpawnSlot.y == ownerRow)
                {
                    return target;
                }
            }

            for (int i = 0; i < targetList.Count; i++)
            {
                Ship target = targetList[i];

                if (target.myShipData.SpawnSlot.x == _commanderTargetColumn)
                    return target;
            }

            return SelectTargetByRules(targetList);
        }


        private List<Ship> GetTargetList()
        {
            List<Ship> targetList = new List<Ship>();

            if (myship.myShipData.ShipType == ShipType.EscortShip ||
                myship.myShipData.ShipType == ShipType.MainShip)
            {
                IReadOnlyList<Enemy> enemies = currentStageBattleSpawnData.EnemySpawnData.Enemies;

                for (int i = 0; i < enemies.Count; i++)
                {
                    if (CanTarget(enemies[i]))
                        targetList.Add(enemies[i]);
                }

                return targetList;
            }

            if (myship.myShipData.ShipType == ShipType.Enemy)
            {
                IReadOnlyList<Ship> escortShips = currentStageBattleSpawnData.EscortShips;

                for (int i = 0; i < escortShips.Count; i++)
                {
                    if (CanTarget(escortShips[i]))
                        targetList.Add(escortShips[i]);
                }

                if (targetList.Count == 0 &&
                    currentStageBattleSpawnData.MainShip != null &&
                    currentStageBattleSpawnData.MainShip.isActiveAndEnabled &&
                    currentStageBattleSpawnData.MainShip.IsDead == false &&
                    CanTarget(currentStageBattleSpawnData.MainShip))
                {
                    targetList.Add(currentStageBattleSpawnData.MainShip);
                }

                return targetList;
            }

            return targetList;
        }

        private bool CanTarget(Ship target)
        {
            return target != null && target.isActiveAndEnabled && target.IsDead == false &&
                   (target.IsDirectTargetProtected == false || myship.CanTargetProtectedShips);
        }

        private Ship SelectTargetByRules(List<Ship> targetList)
        {
            if (targetRules == null || targetRules.Length == 0)
                return SelectDistributedTarget(targetList);

            for (int i = 0; i < targetRules.Length; i++)
            {
                Ship target = SelectDistributedTargetByRule(targetList, targetRules[i]);

                if (target != null)
                    return target;
            }

            return SelectDistributedTarget(targetList);
        }

        private Ship SelectDistributedTargetByRule(List<Ship> targetList, TargetRule rule)
        {
            List<Ship> matchingTargets = new List<Ship>();

            for (int i = 0; i < targetList.Count; i++)
            {
                Ship target = targetList[i];

                if (IsMatchRule(target, rule))
                    matchingTargets.Add(target);
            }

            return SelectDistributedTarget(matchingTargets);
        }

        private Ship SelectDistributedTarget(List<Ship> targetList)
        {
            if (targetList == null || targetList.Count == 0)
                return null;

            if (selectionMode == TargetSelectionMode.HighestCurrentHealth)
                return SelectHighestCurrentHealth(targetList);

            int selectionIndex = GetFormationSelectionIndex() % targetList.Count;
            return targetList[selectionIndex];
        }

        private Ship SelectHighestCurrentHealth(List<Ship> targetList)
        {
            Ship selectedTarget = null;
            float highestHealth = float.NegativeInfinity;
            float nearestDistanceSquared = float.PositiveInfinity;

            foreach (Ship candidate in targetList)
            {
                EntityHealth health = candidate.GetCompo<EntityHealth>();
                float currentHealth = health != null ? health.CurrentHealth : 0f;
                float distanceSquared = (candidate.transform.position - myship.transform.position).sqrMagnitude;
                if (selectedTarget == null || currentHealth > highestHealth ||
                    (currentHealth == highestHealth && distanceSquared < nearestDistanceSquared))
                {
                    selectedTarget = candidate;
                    highestHealth = currentHealth;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return selectedTarget;
        }

        private int GetFormationSelectionIndex()
        {
            Vector2Int spawnSlot = myship.myShipData.SpawnSlot;

            if (spawnSlot.x < 0 || spawnSlot.y < 0)
                return 0;

            int columnCount = myship.myShipData.ShipType == ShipType.Enemy
                ? StageEnemyLayout.ColumnCount
                : PlayerFleetLoadout.ColumnCount;

            return spawnSlot.y * columnCount + spawnSlot.x;
        }

        private bool IsMatchRule(Ship target, TargetRule rule)
        {
            if (target == null || target.isActiveAndEnabled == false || target.IsDead)
                return false;

            return IsMatchLine(target, rule.Line) && IsMatchRole(target, rule.Role);
        }

        private bool IsMatchLine(Ship target, TargetLine line)
        {
            if (line == TargetLine.Any)
                return true;

            if (target.myShipData.ShipType == ShipType.Enemy)
            {
                return line switch
                {
                    TargetLine.Front => target.myShipData.SpawnSlot.x == 0,
                    TargetLine.Middle => target.myShipData.SpawnSlot.x == 1,
                    TargetLine.Back => target.myShipData.SpawnSlot.x == 2,
                    _ => false
                };
            }

            return line switch
            {
                TargetLine.Front => target.myShipData.SpawnSlot.x == PlayerFleetLoadout.ColumnCount - 1,
                TargetLine.Back => target.myShipData.SpawnSlot.x == 0,
                _ => false
            };
        }

        private bool IsMatchRole(Ship target, ShipRole role)
        {
            return role == ShipRole.None || (target.myShipData.Role & role) != 0;
        }
    }
}

