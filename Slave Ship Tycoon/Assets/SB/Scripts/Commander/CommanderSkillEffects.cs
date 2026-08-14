using System.Collections.Generic;
using SB.Scripts.AttackCompo;

namespace SB.Scripts.Commander
{
    internal interface ICommanderSkillEffect
    {
        bool CanActivate(BattleSpawnData battleSpawnData);
        void Activate(BattleSpawnData battleSpawnData);
        bool Tick(BattleSpawnData battleSpawnData);
        void Deactivate(BattleSpawnData battleSpawnData);
    }

    internal sealed class TotalOffensiveSkillEffect : ICommanderSkillEffect
    {
        private readonly TotalOffensiveSkillData _skillData;
        private readonly List<ShipCombatStatCompo> _appliedCombatStats = new List<ShipCombatStatCompo>();

        public TotalOffensiveSkillEffect(TotalOffensiveSkillData skillData)
        {
            _skillData = skillData;
        }

        public bool CanActivate(BattleSpawnData battleSpawnData)
        {
            return HasLivingPlayerShip(battleSpawnData);
        }

        public void Activate(BattleSpawnData battleSpawnData)
        {
            Deactivate(battleSpawnData);
            ApplyToShip(battleSpawnData.MainShip);

            IReadOnlyList<Ship> escortShips = battleSpawnData.EscortShips;

            for (int i = 0; i < escortShips.Count; i++)
                ApplyToShip(escortShips[i]);
        }

        public bool Tick(BattleSpawnData battleSpawnData)
        {
            return HasLivingPlayerShip(battleSpawnData);
        }

        public void Deactivate(BattleSpawnData battleSpawnData)
        {
            for (int i = 0; i < _appliedCombatStats.Count; i++)
            {
                ShipCombatStatCompo combatStatCompo = _appliedCombatStats[i];
                combatStatCompo?.RemoveCommanderAttackSpeedPercent(this);
            }

            _appliedCombatStats.Clear();
        }

        private void ApplyToShip(Ship ship)
        {
            if (ship == null || ship.IsDead)
                return;

            ShipCombatStatCompo combatStatCompo = ship.GetCompo<ShipCombatStatCompo>();

            if (combatStatCompo == null)
                return;

            combatStatCompo.AddCommanderAttackSpeedPercent(
                this,
                _skillData.AttackSpeedIncreasePercent);
            _appliedCombatStats.Add(combatStatCompo);
        }

        private static bool HasLivingPlayerShip(BattleSpawnData battleSpawnData)
        {
            if (battleSpawnData.MainShip != null && battleSpawnData.MainShip.IsDead == false)
                return true;

            IReadOnlyList<Ship> escortShips = battleSpawnData.EscortShips;

            for (int i = 0; i < escortShips.Count; i++)
            {
                if (escortShips[i] != null && escortShips[i].IsDead == false)
                    return true;
            }

            return false;
        }
    }

    internal sealed class FocusFireSkillEffect : ICommanderSkillEffect
    {
        private int _currentTargetColumn = -1;

        public bool CanActivate(BattleSpawnData battleSpawnData)
        {
            return FindFrontLivingEnemyColumn(battleSpawnData) >= 0;
        }

        public void Activate(BattleSpawnData battleSpawnData)
        {
            _currentTargetColumn = -1;
            UpdateTargetColumn(battleSpawnData);
        }

        public bool Tick(BattleSpawnData battleSpawnData)
        {
            return UpdateTargetColumn(battleSpawnData);
        }

        public void Deactivate(BattleSpawnData battleSpawnData)
        {
            ApplyTargetColumn(battleSpawnData.MainShip, -1);

            IReadOnlyList<Ship> escortShips = battleSpawnData.EscortShips;

            for (int i = 0; i < escortShips.Count; i++)
                ApplyTargetColumn(escortShips[i], -1);

            _currentTargetColumn = -1;
        }

        private bool UpdateTargetColumn(BattleSpawnData battleSpawnData)
        {
            int targetColumn = FindFrontLivingEnemyColumn(battleSpawnData);

            if (targetColumn < 0)
                return false;

            if (_currentTargetColumn == targetColumn)
                return true;

            _currentTargetColumn = targetColumn;
            ApplyTargetColumn(battleSpawnData.MainShip, targetColumn);

            IReadOnlyList<Ship> escortShips = battleSpawnData.EscortShips;

            for (int i = 0; i < escortShips.Count; i++)
                ApplyTargetColumn(escortShips[i], targetColumn);

            return true;
        }

        private static int FindFrontLivingEnemyColumn(BattleSpawnData battleSpawnData)
        {
            int frontColumn = int.MaxValue;
            IReadOnlyList<Enemy> enemies = battleSpawnData.EnemySpawnData.Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];

                if (enemy != null && enemy.IsDead == false)
                    frontColumn = UnityEngine.Mathf.Min(frontColumn, enemy.myShipData.SpawnSlot.x);
            }

            return frontColumn == int.MaxValue ? -1 : frontColumn;
        }

        private static void ApplyTargetColumn(Ship ship, int targetColumn)
        {
            if (ship == null)
                return;

            if (targetColumn >= 0)
            {
                if (ship.IsDead == false)
                    ship.SetCommanderTargetColumn(targetColumn);
            }
            else
                ship.ClearCommanderTargetCommand();
        }
    }
}
