using System.Reflection;
using NUnit.Framework;
using SB.Core;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.Scripts.Projectiles;
using UnityEngine;

namespace SB.Editor.Tests
{
    public sealed class ProjectileEvasionTests
    {
        private GameObject targetObject;
        private GameObject projectileObject;
        private Ship target;
        private EntityHealth health;
        private ShipCombatStatCompo stats;
        private ShipProjectileEvasion evasion;
        private ShipIncomingFireDisplay display;
        private ProjectileEvasionSettings settings;
        private NormalProjectile projectile;
        private Random.State randomState;

        [SetUp]
        public void SetUp()
        {
            randomState = Random.state;
            Random.InitState(138);
            targetObject = new GameObject("Evasion test target");
            health = targetObject.AddComponent<EntityHealth>();
            stats = targetObject.AddComponent<ShipCombatStatCompo>();
            target = targetObject.AddComponent<Ship>();
            typeof(Entity).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
            settings = ScriptableObject.CreateInstance<ProjectileEvasionSettings>();
            evasion = targetObject.AddComponent<ShipProjectileEvasion>();
            Set(evasion, "combatStats", stats);
            Set(evasion, "settings", settings);
            var left = new GameObject("Left").transform;
            left.SetParent(targetObject.transform); left.localPosition = new Vector3(-1, -.5f, 0);
            var right = new GameObject("Right").transform;
            right.SetParent(targetObject.transform); right.localPosition = new Vector3(1, -.5f, 0);
            Set(evasion, "leftWaterPoint", left); Set(evasion, "rightWaterPoint", right);
            display = targetObject.AddComponent<ShipIncomingFireDisplay>();
            Set(display, "ship", target); Set(display, "health", health);
            projectileObject = new GameObject("Evasion test projectile");
            projectile = projectileObject.AddComponent<NormalProjectile>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(settings);
            Random.state = randomState;
        }

        [Test]
        public void LuckCurveHasZeroOriginDiminishingReturnsAndCap()
        {
            Assert.That(settings.EvaluateChance(0), Is.Zero);
            Assert.That(settings.EvaluateChance(-10), Is.Zero);
            Assert.That(settings.EvaluateChance(50), Is.EqualTo(.075f).Within(.00001f));
            Assert.That(settings.EvaluateChance(100), Is.EqualTo(.1f).Within(.00001f));
            Assert.That(settings.EvaluateChance(100000), Is.LessThanOrEqualTo(.15f));
        }

        [Test]
        public void WaterPointUsesIncomingSideAndSampleIsOnlyComparedOnce()
        {
            Set(stats, "baseLuck", 50f);
            Assert.That(evasion.TryResolveMiss(Vector3.left * 5, .01f, out var left), Is.True);
            Assert.That(left.x, Is.EqualTo(-1));
            Assert.That(evasion.TryResolveMiss(Vector3.right * 5, .01f, out var right), Is.True);
            Assert.That(right.x, Is.EqualTo(1));
            Assert.That(evasion.TryResolveMiss(Vector3.right, .5f, out _), Is.False);
        }

        [Test]
        public void LuckyMissStaysCountedUntilLandingAndDoesNotDamage()
        {
            ForceMiss(); Launch();
            Assert.That(projectile.IsLuckMiss, Is.True);
            Assert.That(display.IncomingCount, Is.EqualTo(1));
            float before = health.CurrentHealth;
            Set(stats, "baseLuck", 0f); // Changing luck in flight must not reroll this shot.
            Invoke("ResolveImpact");
            Assert.That(health.CurrentHealth, Is.EqualTo(before));
            Assert.That(display.IncomingCount, Is.Zero);
        }

        [Test]
        public void NormalHitDamagesAndPooledProjectileDoesNotRetainMissOutcome()
        {
            ForceMiss(); Launch(); projectile.OnDespawnedToPool();
            Assert.That(display.IncomingCount, Is.Zero);
            projectile.OnSpawnedFromPool(); Set(stats, "baseLuck", 0f); Launch();
            Assert.That(projectile.IsLuckMiss, Is.False);
            float before = health.CurrentHealth;
            Invoke("ResolveImpact");
            Assert.That(health.CurrentHealth, Is.EqualTo(before - 10));
            Assert.That(display.IncomingCount, Is.Zero);
        }

        [TestCase("dead")]
        [TestCase("reused")]
        public void LostTargetRemovesWarningWithoutRedirectingLuckyWaterArc(string reason)
        {
            ForceMiss(); Launch();
            var field = typeof(BaseProjectile).GetField("pathEndPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            var destination = (Vector3)field.GetValue(projectile);
            if (reason == "dead") target.IsDead = true; else target.OnSpawnedFromPool();
            Invoke("Update");
            Assert.That(display.IncomingCount, Is.Zero);
            Assert.That((Vector3)field.GetValue(projectile), Is.EqualTo(destination));
        }

        private void ForceMiss()
        {
            Set(settings, "maximumMissChance", 1f);
            Set(settings, "luckForHalfMaximum", .01f);
            Set(stats, "baseLuck", 100000000f);
        }
        private void Launch() => projectile.Launch(new ProjectileLaunchData(target, Vector3.right * 5,
            Vector3.zero, Vector3.down, ProjectileFlightData.Default, 10));
        private void Invoke(string method) => typeof(BaseProjectile)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(projectile, null);
        private static void Set(object instance, string field, object value) => instance.GetType()
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
    }
}
