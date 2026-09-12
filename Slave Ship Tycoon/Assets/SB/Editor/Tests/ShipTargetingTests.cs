using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SB.Core;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.Scripts.Projectiles;
using SB.Tests;
using UnityEngine;

namespace SB.Editor.Tests
{
    public sealed class ShipTargetingTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private Ship _attacker;
        private ShipAttackProbe _attack;
        private TargetSelector _selector;
        private Enemy _first;
        private Enemy _second;

        [SetUp]
        public void SetUp()
        {
            GameObject root = CreateObject("Attacker");
            _selector = root.AddComponent<TargetSelector>();
            _attack = root.AddComponent<ShipAttackProbe>();
            _attacker = root.AddComponent<Ship>();
            typeof(Entity).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_attacker, null);
            _attacker.myShipData = new MyShipData { ShipType = ShipType.EscortShip };
            _first = CreateObject("First enemy").AddComponent<Enemy>();
            _second = CreateObject("Second enemy").AddComponent<Enemy>();
            _first.SetSpawnSlot(new Vector2Int(0, 0));
            _second.SetSpawnSlot(new Vector2Int(1, 0));
            StartBattle();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [TestCase("dead")]
        [TestCase("inactive")]
        [TestCase("disabled")]
        public void SelectorSkipsUnavailableTarget(string state)
        {
            MakeUnavailable(_first, state);
            Assert.That(_selector.GetTarget(), Is.SameAs(_second));
        }

        [TestCase("dead")]
        [TestCase("inactive")]
        [TestCase("disabled")]
        public void PendingShotRetargetsBeforeFiring(string state)
        {
            Assert.That(_attack.HasAliveTarget, Is.True);
            MakeUnavailable(_first, state);
            TriggerShot();
            Assert.That(_attack.FireCount, Is.EqualTo(1));
            Assert.That(_attack.FiredTarget, Is.SameAs(_second));
        }

        [Test]
        public void NoShotWhenNoLivingTargetRemains()
        {
            _first.IsDead = true;
            _second.IsDead = true;
            TriggerShot();
            Assert.That(_attack.FireCount, Is.Zero);
            Assert.That(_attack.HasAliveTarget, Is.False);
        }

        [TestCase("dead")]
        [TestCase("inactive")]
        [TestCase("disabled")]
        public void UnavailableAttackerCannotFireLateAnimationEvent(string state)
        {
            MakeUnavailable(_attacker, state);
            TriggerShot();
            Assert.That(_attack.FireCount, Is.Zero);
        }

        [Test]
        public void BattleEndStopsPendingShotAndNextBattleCanFire()
        {
            InvokeAttack("HandleBattleEnded", new StageBattleEndedEvent(true));
            TriggerShot();
            Assert.That(_attack.FireCount, Is.Zero);
            Assert.That(_attack.CanAttack, Is.False);
            StartBattle();
            TriggerShot();
            Assert.That(_attack.FireCount, Is.EqualTo(1));
        }

        [Test]
        public void ReusedShipInvalidatesCachedTargetGeneration()
        {
            _first.OnSpawnedFromPool();
            Assert.That(_attack.HasAliveTarget, Is.False);
            Assert.That(_attack.EnsureTarget(), Is.True);
        }

        [TestCase("dead")]
        [TestCase("inactive")]
        [TestCase("disabled")]
        [TestCase("reused")]
        public void InFlightProjectileDoesNotHitUnavailableOriginalTarget(string state)
        {
            NormalProjectile projectile = CreateObject("Projectile").AddComponent<NormalProjectile>();
            projectile.Launch(new ProjectileLaunchData(_first, Vector3.zero, Vector3.right,
                Vector3.down, ProjectileFlightData.Default, 10f));
            if (state == "reused")
                _first.OnSpawnedFromPool();
            else
                MakeUnavailable(_first, state);
            bool alive = (bool)typeof(BaseProjectile)
                .GetMethod("IsOriginalTargetAlive", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(projectile, null);
            Assert.That(alive, Is.False);
        }

        private void StartBattle()
        {
            BattleSpawnData data = new BattleSpawnData(null, null, new StageEnemySpawnData(new[] { _first, _second }));
            InvokeAttack("RequestTarget", new BattleStartEvent(data));
        }

        private void TriggerShot()
        {
            InvokeAttack("HandleAttackStartTrigger");
        }

        private void InvokeAttack(string name, params object[] arguments)
        {
            typeof(Base_ShipAttackCompo).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_attack, arguments);
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private static void MakeUnavailable(Ship ship, string state)
        {
            if (state == "dead") ship.IsDead = true;
            else if (state == "inactive") ship.gameObject.SetActive(false);
            else ship.enabled = false;
        }
    }
}
