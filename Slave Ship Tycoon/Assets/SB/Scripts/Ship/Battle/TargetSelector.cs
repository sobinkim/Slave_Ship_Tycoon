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

    [System.Serializable]
    public struct TargetRule
    {
        public TargetLine Line;
        public ShipRole Role;
    }

    public class TargetSelector : MonoBehaviour, IEntityComponent
    {
        [SerializeField] private TargetRule[] targetRules;
        private Ship myship;
        private BattleSpawnData currentStageBattleSpawnData;


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
            currentStageBattleSpawnData = evt.BattleSpawnData;
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
                    if (enemies[i] != null && enemies[i].IsDead == false)
                        targetList.Add(enemies[i]);
                }

                return targetList;
            }

            if (myship.myShipData.ShipType == ShipType.Enemy)
            {
                IReadOnlyList<Ship> escortShips = currentStageBattleSpawnData.EscortShips;

                for (int i = 0; i < escortShips.Count; i++)
                {
                    if (escortShips[i] != null && escortShips[i].IsDead == false)
                        targetList.Add(escortShips[i]);
                }

                if (targetList.Count == 0 &&
                    currentStageBattleSpawnData.MainShip != null &&
                    currentStageBattleSpawnData.MainShip.IsDead == false)
                {
                    targetList.Add(currentStageBattleSpawnData.MainShip);
                }

                return targetList;
            }

            return targetList;
        }

        private Ship SelectTargetByRules(List<Ship> targetList)
        {
            if (targetRules == null || targetRules.Length == 0)
                return targetList[0];

            for (int i = 0; i < targetRules.Length; i++)
            {
                Ship target = FindTargetByRule(targetList, targetRules[i]);

                if (target != null)
                    return target;
            }

            return targetList[0];
        }

        private Ship FindTargetByRule(List<Ship> targetList, TargetRule rule)
        {
            for (int i = 0; i < targetList.Count; i++)
            {
                Ship target = targetList[i];

                if (IsMatchRule(target, rule))
                    return target;
            }

            return null;
        }

        private bool IsMatchRule(Ship target, TargetRule rule)
        {
            if (target == null || target.IsDead)
                return false;

            return IsMatchLine(target, rule.Line) && IsMatchRole(target, rule.Role);
        }

        private bool IsMatchLine(Ship target, TargetLine line)
        {
            switch (line)
            {
                case TargetLine.Any:
                    return true;
                case TargetLine.Front:
                    return target.myShipData.SpawnSlot.x == 0;
                case TargetLine.Middle:
                    return target.myShipData.SpawnSlot.x == 1;
                case TargetLine.Back:
                    return target.myShipData.SpawnSlot.x == 2;
                default:
                    return false;
            }
        }

        private bool IsMatchRole(Ship target, ShipRole role)
        {
            return role == ShipRole.None || (target.myShipData.Role & role) != 0;
        }
    }
}
