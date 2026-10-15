using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SB.Core;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.AttackCompo;
using SB.Scripts.Fleet;
using SB.Scripts.Projectiles;
using UnityEditor;
using UnityEngine;

namespace SB.Editor.Tests
{
    public sealed class InterceptionTests
    {
        private readonly List<GameObject> objects = new();
        private Ship ally;
        private NormalProjectile incoming;

        [SetUp]
        public void SetUp()
        {
            var root = Create("Protected ally");
            root.AddComponent<EntityHealth>();
            ally = root.AddComponent<Ship>();
            incoming = Create("Incoming cannonball").AddComponent<NormalProjectile>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        private GameObject Create(string name)
        {
            var go = new GameObject(name); objects.Add(go); return go;
        }

        private void Launch(ProjectileTravelMode mode = ProjectileTravelMode.Arc, float speed = 3f)
        {
            incoming.gameObject.SetActive(true);
            var data = new ProjectileFlightData { FlightSpeed = speed, ArcHeight = .8f, MinimumFlightDuration = .01f, LifeTime = 5f };
            incoming.Launch(new ProjectileLaunchData(ally, new Vector3(4, .2f), Vector3.zero,
                Vector3.down, data, 30f, ProjectileHitMode.Guaranteed), mode, 2f, 8f, 1.2f, .4f, .7f);
        }

        [TestCase(ProjectileTravelMode.Arc)]
        [TestCase(ProjectileTravelMode.AcceleratingStraight)]
        [TestCase(ProjectileTravelMode.AcceleratingArc)]
        [TestCase(ProjectileTravelMode.BallisticArc)]
        public void PredictionMeetsActualCurveAtConstantInterceptorSpeed(ProjectileTravelMode mode)
        {
            Launch(mode);
            Vector3 origin = new Vector3(-.5f, -.2f);
            Assert.That(InterceptionPrediction.TrySolve(incoming, origin, 12f, .02f, .05f,
                out Vector3 point, out float seconds), Is.True);
            Assert.That(Vector3.Distance(origin, point), Is.EqualTo(12f * seconds).Within(.001f));
            Assert.That(seconds, Is.LessThan(incoming.RemainingPredictionSeconds - .049f));
            incoming.TryPredictPosition(seconds, out var predicted);
            Assert.That(Vector3.Distance(point, predicted), Is.LessThan(.0001f));
        }

        [Test]
        public void TooLateToInterceptDoesNotSelectIncomingShot()
        {
            Launch(ProjectileTravelMode.Arc, 1000f);
            Assert.That(InterceptionPrediction.TrySolve(incoming, Vector3.left * 5, 3f, .04f, .06f,
                out _, out _), Is.False);
        }

        [Test]
        public void SweepDetectsFastCrossingAndRejectsParallelNearMiss()
        {
            Assert.That(InterceptionPrediction.SweptHit(Vector3.left * 5, Vector3.right * 5,
                Vector3.right * 5, Vector3.left * 5, .05f, out float time), Is.True);
            Assert.That(time, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(InterceptionPrediction.SweptHit(Vector3.left, Vector3.right,
                Vector3.left + Vector3.up, Vector3.right + Vector3.up, .05f, out _), Is.False);
        }

        [Test]
        public void ReservationIsExclusiveAndTargetCancellationReleasesIt()
        {
            Launch();
            var first = Create("First interceptor").AddComponent<InterceptorProjectile>();
            var second = Create("Second interceptor").AddComponent<InterceptorProjectile>();
            Assert.That(first.Launch(incoming, Vector3.zero, Vector3.one, 12f, .2f), Is.True);
            Assert.That(second.Launch(incoming, Vector3.zero, Vector3.one, 12f, .2f), Is.False);
            Assert.That(incoming.CanBeIntercepted, Is.False);
            incoming.CancelFlight();
            Assert.That(first.IsFlying, Is.False);
            Assert.That(BaseProjectile.InterceptableFlights, Has.No.Member(incoming));
            incoming.OnSpawnedFromPool();
            Launch();
            Assert.That(second.Launch(incoming, Vector3.zero, Vector3.one, 12f, .2f), Is.True);
        }

        [Test]
        public void InterceptedShotResolvesOnceAndRemovesIncomingDamageWarning()
        {
            var display = ally.gameObject.AddComponent<ShipIncomingFireDisplay>();
            typeof(ShipIncomingFireDisplay).GetField("ship", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(display, ally);
            typeof(ShipIncomingFireDisplay).GetField("health", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(display, ally.GetComponent<EntityHealth>());
            var first = Create("Interceptor").AddComponent<InterceptorProjectile>();
            Launch();
            Assert.That(display.IncomingCount, Is.EqualTo(1));
            Assert.That(first.Launch(incoming, Vector3.zero, Vector3.one, 12f, .2f), Is.True);
            int generation = incoming.FlightGeneration;
            int count = 0;
            ProjectileResolution resolution = ProjectileResolution.TargetHit;
            incoming.Resolved += (_, value) => { count++; resolution = value; };
            var intercept = typeof(BaseProjectile).GetMethod("TryIntercept", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That((bool)intercept.Invoke(incoming, new object[] { first, generation }), Is.True);
            incoming.CancelFlight();
            Assert.That(count, Is.EqualTo(1));
            Assert.That(resolution, Is.EqualTo(ProjectileResolution.Intercepted));
            Assert.That(display.IncomingCount, Is.Zero);
            Assert.That(first.IsFlying, Is.False);
            incoming.OnSpawnedFromPool(); Launch();
            Assert.That((bool)intercept.Invoke(incoming, new object[] { first, generation }), Is.False);
        }

        [Test]
        public void StageEndCancelsBothFlightsAndClearsRegistry()
        {
            Launch();
            var interceptor = Create("Interceptor").AddComponent<InterceptorProjectile>();
            interceptor.Launch(incoming, Vector3.zero, Vector3.one, 12f, .2f);
            // EditMode objects do not receive the runtime activation callbacks automatically.
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(BaseProjectile).GetMethod("OnEnable", flags).Invoke(incoming, null);
            typeof(InterceptorProjectile).GetMethod("OnEnable", flags).Invoke(interceptor, null);
            try { Bus<StageBattleEndedEvent>.Raise(new StageBattleEndedEvent(true)); }
            finally
            {
                typeof(BaseProjectile).GetMethod("OnDisable", flags).Invoke(incoming, null);
                typeof(InterceptorProjectile).GetMethod("OnDisable", flags).Invoke(interceptor, null);
            }
            Assert.That(interceptor.IsFlying, Is.False);
            Assert.That(BaseProjectile.InterceptableFlights, Has.No.Member(incoming));
        }

        [Test]
        public void HealingPayloadIsExcludedAndSmokeBombCanBeIntercepted()
        {
            var heal = Create("Healing capsule").AddComponent<HealingProjectile>();
            var smoke = Create("Smoke bomb").AddComponent<SmokeBombProjectile>();
            var data = new ProjectileLaunchData(ally, Vector3.right * 5, Vector3.zero, Vector3.down,
                ProjectileFlightData.Default, 30f, ProjectileHitMode.Guaranteed);
            heal.Launch(data); smoke.Launch(data);
            Assert.That(heal.CanBeIntercepted, Is.False);
            Assert.That(smoke.CanBeIntercepted, Is.True);
            Assert.That(BaseProjectile.InterceptableFlights, Does.Contain(smoke));
        }

        [Test]
        public void RecruitmentContainsOneConfiguredInterceptorWithoutReorderingEarlierShips()
        {
            var data = AssetDatabase.LoadAssetAtPath<EscortShipData>(InterceptorShipSetup.DataPath);
            var catalog = AssetDatabase.LoadAssetAtPath<EscortShipCatalog>("Assets/SB/Data/PlayerFleet/EscortShipCatalog.asset");
            var table = AssetDatabase.LoadAssetAtPath<EscortGachaTable>("Assets/SB/Data/PlayerFleet/EscortGachaTable.asset");
            Assert.That(data.IsConfigured, Is.True);
            Assert.That(data.Icon, Is.Not.Null);
            Assert.That(data.ShipPrefab.GetComponentInChildren<InterceptorAttackCompo>(true), Is.Not.Null);
            Assert.That(catalog.TryValidate(out _), Is.True);
            Assert.That(table.TryValidate(out _), Is.True);
            int count = 0;
            for (int i = 0; i < table.EntryCount; i++)
                if (table.GetEntryAt(i).ShipData == data) { count++; Assert.That(table.GetEntryAt(i).Weight, Is.GreaterThan(0)); }
            Assert.That(count, Is.EqualTo(1));
            Assert.That(catalog.GetAt(catalog.Count - 1), Is.SameAs(data));
        }
    }
}
