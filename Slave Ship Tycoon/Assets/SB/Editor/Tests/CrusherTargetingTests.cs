using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SB.Core;
using SB.Scripts;
using UnityEngine;

namespace SB.Editor.Tests
{
    public sealed class CrusherTargetingTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private TargetSelector selector;
        private Enemy defender;
        private Enemy heavy;

        [SetUp]
        public void SetUp()
        {
            var root = CreateObject("Crusher");
            selector = root.AddComponent<TargetSelector>();
            var attacker = root.AddComponent<Ship>();
            Initialize(attacker);
            attacker.myShipData = new MyShipData { ShipType = ShipType.EscortShip };
            SetField(selector, "selectionMode", TargetSelectionMode.HighestCurrentHealth);
            SetField(selector, "targetRules", new[] {
                new TargetRule { Line = TargetLine.Any, Role = ShipRole.Tanker },
                new TargetRule { Line = TargetLine.Any, Role = ShipRole.None }
            });
            defender = CreateEnemy("Defender", ShipRole.Tanker, 40f, new Vector3(3, 0, 0));
            heavy = CreateEnemy("Heavy", ShipRole.Dealer, 200f, new Vector3(2, 0, 0));
            defender.SetSpawnSlot(new Vector2Int(0, 0));
            heavy.SetSpawnSlot(new Vector2Int(1, 0));
            selector.SetBattleSpawnData(new BattleSpawnData(null, null,
                new StageEnemySpawnData(new[] { defender, heavy })));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void DefenderTakesPriorityOverHigherHealthDealer()
        {
            Assert.That(selector.GetTarget(), Is.SameAs(defender));
        }

        [Test]
        public void WithoutDefenderSelectsHighestCurrentHealth()
        {
            defender.myShipData.Role = ShipRole.Dealer;
            Assert.That(selector.GetTarget(), Is.SameAs(heavy));
            heavy.GetCompo<EntityHealth>().ResetHealth(0.1f);
            Assert.That(selector.GetTarget(), Is.SameAs(defender));
        }

        [Test]
        public void EqualHealthSelectsNearestTarget()
        {
            defender.myShipData.Role = ShipRole.Dealer;
            heavy.GetCompo<EntityHealth>().ResetHealth(0.2f);
            Assert.That(selector.GetTarget(), Is.SameAs(heavy));
        }

        [TestCase("dead")]
        [TestCase("inactive")]
        [TestCase("disabled")]
        public void UnavailableDefenderDoesNotBlockFallback(string state)
        {
            if (state == "dead") defender.IsDead = true;
            else if (state == "inactive") defender.gameObject.SetActive(false);
            else defender.enabled = false;
            Assert.That(selector.GetTarget(), Is.SameAs(heavy));
        }

        [Test]
        public void CommanderColumnStillOverridesNormalPriority()
        {
            selector.SetCommanderTargetColumn(1);
            Assert.That(selector.GetTarget(), Is.SameAs(heavy));
            selector.ClearCommanderTargetCommand();
            Assert.That(selector.GetTarget(), Is.SameAs(defender));
        }

        private Enemy CreateEnemy(string name, ShipRole role, float health, Vector3 position)
        {
            var root = CreateObject(name);
            root.transform.position = position;
            var healthComponent = root.AddComponent<EntityHealth>();
            SetField(healthComponent, "fallbackMaxHealth", health);
            var enemy = root.AddComponent<Enemy>();
            Initialize(enemy);
            enemy.myShipData = new MyShipData { ShipType = ShipType.Enemy, Role = role };
            return enemy;
        }

        private GameObject CreateObject(string name)
        {
            var root = new GameObject(name);
            objects.Add(root);
            return root;
        }

        private static void Initialize(Entity entity)
        {
            typeof(Entity).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(entity, null);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
