using System.Reflection;
using NUnit.Framework;
using SB.Scripts;
using UnityEngine;

namespace SB.Editor.Tests
{
    public sealed class StageEnemyLayoutTests
    {
        [Test]
        public void LegacyNineSlotsKeepTheirIndicesAndAppendEmptyFourthRow()
        {
            var layout = ScriptableObject.CreateInstance<StageEnemyLayout>();
            var enemyObject = new GameObject("Legacy enemy");
            try
            {
                var enemy = enemyObject.AddComponent<Enemy>();
                var legacySlots = new Enemy[9];
                legacySlots[2] = enemy;
                legacySlots[8] = enemy;
                typeof(StageEnemyLayout).GetField("enemySlots", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(layout, legacySlots);
                typeof(StageEnemyLayout).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(layout, null);
                Assert.That(layout.GetEnemyAt(2), Is.SameAs(enemy));
                Assert.That(layout.GetEnemyAt(8), Is.SameAs(enemy));
                for (int i = 9; i < 12; i++)
                    Assert.That(layout.GetEnemyAt(i), Is.Null);
                Assert.That(layout.EnemyCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(layout);
            }
        }

        [Test]
        public void FourthRowEnemyCountsAndEmptySlotsStayEmpty()
        {
            var layout = ScriptableObject.CreateInstance<StageEnemyLayout>();
            var enemyObject = new GameObject("Fourth row enemy");
            try
            {
                var slots = new Enemy[StageEnemyLayout.SlotCount];
                slots[11] = enemyObject.AddComponent<Enemy>();
                typeof(StageEnemyLayout).GetField("enemySlots", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(layout, slots);
                Assert.That(layout.EnemyCount, Is.EqualTo(1));
                Assert.That(layout.GetEnemyAt(11), Is.SameAs(slots[11]));
                Assert.That(layout.GetEnemyAt(10), Is.Null);
                Assert.Throws<System.ArgumentOutOfRangeException>(() => layout.GetEnemyAt(12));
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(layout);
            }
        }
    }
}
