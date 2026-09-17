using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SB.Scripts;
using SB.Scripts.Fleet;
using UnityEngine;

namespace SB.Editor.Tests
{
    public sealed class FleetMergeAllTests
    {
        private readonly List<Object> objects = new List<Object>();
        private PlayerFleetManager manager;
        private EscortShipData[] ships;

        private void Configure(bool includeUncommon = true)
        {
            var root = new GameObject("Batch merge test");
            root.SetActive(false);
            objects.Add(root);
            var prefab = root.AddComponent<Ship>();
            ships = new EscortShipData[6];
            var entries = new List<EscortShipData>();
            for (int i = 0; i < ships.Length; i++)
            {
                var ship = ScriptableObject.CreateInstance<EscortShipData>();
                objects.Add(ship);
                Set(ship, "_stableId", "batch-test-" + i);
                Set(ship, "_displayName", "Test " + i);
                Set(ship, "_shipPrefab", prefab);
                Set(ship, "_grade", i == 5 ? EscortShipGrade.Common : (EscortShipGrade)i);
                ships[i] = ship;
                if (includeUncommon || i != 1) entries.Add(ship);
            }
            var catalog = ScriptableObject.CreateInstance<EscortShipCatalog>();
            objects.Add(catalog);
            Set(catalog, "_ships", entries.ToArray());
            manager = root.AddComponent<PlayerFleetManager>();
            Set(manager, "_catalog", catalog);
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(target, value);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void BatchPreservesFormationCascadesAndSecondClickDoesNothing()
        {
            Configure();
            Assert.That(manager.TryGrantEscort(ships[0], 11), Is.True);
            Assert.That(manager.TryPlaceEscort(0, ships[0], out _), Is.True);
            Assert.That(manager.TryPlaceEscort(1, ships[0], out _), Is.True);
            Assert.That(manager.MergeAllUnequipped(out var rewards), Is.EqualTo(4));
            Assert.That(manager.GetOwnedCount(ships[0]), Is.EqualTo(2));
            Assert.That(manager.GetOwnedCount(ships[1]), Is.Zero);
            Assert.That(manager.GetOwnedCount(ships[2]), Is.EqualTo(1));
            Assert.That(manager.GetFormationAt(0), Is.SameAs(ships[0]));
            Assert.That(manager.GetFormationAt(1), Is.SameAs(ships[0]));
            Assert.That(rewards.Count, Is.EqualTo(1));
            Assert.That(rewards[0].Key, Is.SameAs(ships[2]));
            Assert.That(manager.MergeAllUnequipped(out _), Is.Zero);
        }

        [Test]
        public void BatchCombinesSameGradeTypesAndKeepsHighestGrade()
        {
            Configure();
            manager.TryGrantEscort(ships[0], 1);
            manager.TryGrantEscort(ships[5], 2);
            manager.TryGrantEscort(ships[4], 3);
            Assert.That(manager.MergeAllUnequipped(out _), Is.EqualTo(1));
            Assert.That(manager.GetOwnedCount(ships[0]), Is.Zero);
            Assert.That(manager.GetOwnedCount(ships[5]), Is.Zero);
            Assert.That(manager.GetOwnedCount(ships[1]), Is.EqualTo(1));
            Assert.That(manager.GetOwnedCount(ships[4]), Is.EqualTo(3));
        }

        [Test]
        public void MissingNextGradeNeverConsumesMaterials()
        {
            Configure(false);
            manager.TryGrantEscort(ships[0], 9);
            Assert.That(manager.MergeAllUnequipped(out _), Is.Zero);
            Assert.That(manager.GetOwnedCount(ships[0]), Is.EqualTo(9));
        }

        [Test]
        public void LargeInventoryDoesNotOverflowOrLoopPerShip()
        {
            Configure();
            manager.TryGrantEscort(ships[0], int.MaxValue);
            manager.TryGrantEscort(ships[5], int.MaxValue);
            long before = (long)int.MaxValue * 2;
            long count = manager.MergeAllUnequipped(out _);
            long after = 0;
            foreach (var ship in ships) after += manager.GetOwnedCount(ship);
            Assert.That(before - after, Is.EqualTo(count * 2));
            foreach (var ship in ships) Assert.That(manager.GetOwnedCount(ship), Is.GreaterThanOrEqualTo(0));
        }
    }
}
