using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SB.Core.EventBus;
using SB.Scripts;
using UnityEngine;

namespace SB.Editor.Tests
{
    public sealed class TransportSuspensionTests
    {
        private readonly List<Object> objects = new List<Object>();
        private readonly List<StageStartedEvent> startedStages = new List<StageStartedEvent>();
        private GameFlowManager flow;
        private Random.State randomState;
        private int sellStarts;
        private int obtainStarts;
        private int sellCompletions;
        private int manifestChanges;

        [SetUp]
        public void SetUp()
        {
            randomState = Random.state;
            sellStarts = obtainStarts = sellCompletions = manifestChanges = 0;
            startedStages.Clear();

            StageEnemyLayout farmingWave = CreateLayout(1, 1);
            var farming = CreateAsset<ChapterContainer>();
            SetField(farming, "chapter", 1);
            SetField(farming, "routeType", ChapterRouteType.Obtain);
            SetField(farming, "stages", new[] { farmingWave });
            SetField(farming, "bossLayout", farmingWave);

            var transport = CreateAsset<ChapterContainer>();
            SetField(transport, "chapter", 2);
            SetField(transport, "routeType", ChapterRouteType.Sell);
            SetField(transport, "stages", new[]
            {
                CreateLayout(2, 1), CreateLayout(2, 2), CreateLayout(2, 3)
            });

            var database = CreateAsset<ChapterDatabase>();
            SetField(database, "chapters", new[] { farming, transport });
            var root = new GameObject("Transport suspension test");
            root.SetActive(false);
            objects.Add(root);
            var cargo = root.AddComponent<CargoManager>();
            SetField(cargo, "_cargoData", new CargoData[0]);
            flow = root.AddComponent<GameFlowManager>();
            SetField(flow, "chapterDatabase", database);
            SetField(flow, "cargoManager", cargo);
            SetField(flow, "currentChapter", 2);
            SetField(flow, "currentStage", 2);
            SetField(flow, "currentRouteType", ChapterRouteType.Sell);
            SetField(flow, "currentStageType", StageRoomType.Normal);

            Bus<StageStartedEvent>.OnEvent += OnStageStarted;
            Bus<SellChapterStartedEvent>.OnEvent += OnSellStarted;
            Bus<ObtainChapterStartedEvent>.OnEvent += OnObtainStarted;
            Bus<SellChapterCompletedEvent>.OnEvent += OnSellCompleted;
            Bus<TransportCargoChangedEvent>.OnEvent += OnManifestChanged;
        }

        [TearDown]
        public void TearDown()
        {
            Bus<StageStartedEvent>.OnEvent -= OnStageStarted;
            Bus<SellChapterStartedEvent>.OnEvent -= OnSellStarted;
            Bus<ObtainChapterStartedEvent>.OnEvent -= OnObtainStarted;
            Bus<SellChapterCompletedEvent>.OnEvent -= OnSellCompleted;
            Bus<TransportCargoChangedEvent>.OnEvent -= OnManifestChanged;
            for (int i = objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(objects[i]);
            objects.Clear();
            Random.state = randomState;
        }

        [Test]
        public void ReturnToFarmingSuspendsTheExactTransportStage()
        {
            Assert.That(flow.CanReturnToFarming, Is.True);
            Assert.That(flow.ReturnToFarming(), Is.True);

            Assert.That(flow.HasSuspendedTransport, Is.True);
            Assert.That(flow.SuspendedTransportChapter, Is.EqualTo(2));
            Assert.That(flow.SuspendedTransportStage, Is.EqualTo(2));
            Assert.That(flow.CanReturnToFarming, Is.False);
            Assert.That(flow.CanResumeTransport, Is.True);
            Assert.That(startedStages.Count, Is.EqualTo(1));
            AssertStage(startedStages[0], 1, 1, ChapterRouteType.Obtain);
            AssertNoDepartureReset();
        }

        [Test]
        public void ResumeRestoresTheSameStageWithoutResettingDepartureSnapshots()
        {
            Assert.That(flow.ReturnToFarming(), Is.True);
            startedStages.Clear();

            Assert.That(flow.ResumeTransport(), Is.True);

            Assert.That(flow.HasSuspendedTransport, Is.False);
            Assert.That(flow.SuspendedTransportChapter, Is.Zero);
            Assert.That(flow.SuspendedTransportStage, Is.Zero);
            Assert.That(startedStages.Count, Is.EqualTo(1));
            AssertStage(startedStages[0], 2, 2, ChapterRouteType.Sell);
            AssertNoDepartureReset();
            Assert.That(flow.ResumeTransport(), Is.False);
            Assert.That(startedStages.Count, Is.EqualTo(1));
        }

        [Test]
        public void SuspendedTransportBlocksAnotherBossDeparture()
        {
            Assert.That(flow.ReturnToFarming(), Is.True);
            startedStages.Clear();

            Assert.That(flow.CanStartBossBattle, Is.False);
            flow.StartBossBattle();

            Assert.That(startedStages, Is.Empty);
            Assert.That(flow.HasSuspendedTransport, Is.True);
            Assert.That(flow.CanResumeTransport, Is.True);
            AssertNoDepartureReset();
        }

        [Test]
        public void PendingChapterTransitionRejectsSuspensionAndResume()
        {
            flow.LoopClear();
            Assert.That(flow.CanReturnToFarming, Is.False);
            Assert.That(flow.ReturnToFarming(), Is.False);
            Assert.That(flow.HasSuspendedTransport, Is.False);
            Assert.That(startedStages, Is.Empty);

            SetField(flow, "isChapterTransitionPending", false);
            Assert.That(flow.ReturnToFarming(), Is.True);
            startedStages.Clear();
            flow.LoopClear();

            Assert.That(flow.CanResumeTransport, Is.False);
            Assert.That(flow.ResumeTransport(), Is.False);
            Assert.That(flow.HasSuspendedTransport, Is.True);
            Assert.That(flow.SuspendedTransportChapter, Is.EqualTo(2));
            Assert.That(flow.SuspendedTransportStage, Is.EqualTo(2));
            Assert.That(startedStages, Is.Empty);
            AssertNoDepartureReset();
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            objects.Add(asset);
            return asset;
        }

        private StageEnemyLayout CreateLayout(int chapter, int stage)
        {
            var layout = CreateAsset<StageEnemyLayout>();
            SetField(layout, "chapter", chapter);
            SetField(layout, "stage", stage);
            return layout;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void AssertStage(StageStartedEvent stage, int chapter, int number, ChapterRouteType route)
        {
            Assert.That(stage.Chapter, Is.EqualTo(chapter));
            Assert.That(stage.Stage, Is.EqualTo(number));
            Assert.That(stage.RouteType, Is.EqualTo(route));
            Assert.That(stage.IsBossBattle, Is.False);
        }

        private void AssertNoDepartureReset()
        {
            Assert.That(sellStarts, Is.Zero, "Sell entry would reset prices and departure equipment.");
            Assert.That(obtainStarts, Is.Zero, "Obtain entry would clear the suspended transport presentation.");
            Assert.That(sellCompletions, Is.Zero, "Suspension and resume must not settle the current cargo.");
            Assert.That(manifestChanges, Is.Zero, "The departure manifest must remain intact.");
        }

        private void OnStageStarted(StageStartedEvent evt) => startedStages.Add(evt);
        private void OnSellStarted(SellChapterStartedEvent evt) => sellStarts++;
        private void OnObtainStarted(ObtainChapterStartedEvent evt) => obtainStarts++;
        private void OnSellCompleted(SellChapterCompletedEvent evt) => sellCompletions++;
        private void OnManifestChanged(TransportCargoChangedEvent evt) => manifestChanges++;
    }
}
