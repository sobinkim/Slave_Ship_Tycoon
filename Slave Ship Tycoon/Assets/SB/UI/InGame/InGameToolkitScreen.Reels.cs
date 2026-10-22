using System.Collections;
using System.Collections.Generic;
using SB.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.InGame
{
    public sealed partial class InGameToolkitScreen
    {
        [Header("Transport reels (unscaled seconds)")]
        [SerializeField, Min(0.1f)] private float transportBaseSpinSeconds = 2.6f;
        [SerializeField, Min(0f)] private float transportReelStaggerSeconds = 0.18f;
        [SerializeField, Min(0f)] private float transportBonusQuietSeconds = 0.95f;
        [SerializeField, Min(0.1f)] private float transportBonusCreepSeconds = 0.55f;
        [SerializeField, Min(0.1f)] private float transportReelCountSeconds = 1.1f;
        [SerializeField, Min(0.1f)] private float transportReelResultHoldSeconds = 0.65f;
        [SerializeField, Range(0.5f, 2f)] private float transportDetentTimeScale = 1f;

        private readonly System.Random transportReelRandom = new System.Random();
        private const float ReelRowHeight = 44f;

        private sealed class TransportReelView
        {
            public VisualElement root, baseWindow, extraWindow, baseTrack, extraTrack;
            public Label sign, result;
            public readonly Label[] baseRows = new Label[3];
            public readonly Label[] extraRows = new Label[3];
        }

        private sealed class ReelMotionPoint
        {
            public float time, position;
        }

        private sealed class CargoReelResult
        {
            public TransportItemView item;
            public TransportMarketPriceEntry price;
            public int before;
            public List<ReelMotionPoint> motion;
        }

        private TransportReelView CreateTransportReel(TransportItemView item)
        {
            var view = new TransportReelView();
            view.root = TransportElement("transport-roll");
            view.baseWindow = TransportElement("transport-roll-base");
            view.extraWindow = TransportElement("transport-roll-extra");
            view.baseTrack = TransportElement("transport-roll-track");
            view.extraTrack = TransportElement("transport-roll-track");
            view.sign = TransportLabel("×", "transport-roll-sign");
            view.result = TransportLabel("", "transport-roll-result");
            for (int i = 0; i < 3; i++)
            {
                view.baseRows[i] = TransportLabel("", "transport-roll-number");
                view.extraRows[i] = TransportLabel("", "transport-roll-number");
                view.baseTrack.Add(view.baseRows[i]);
                view.extraTrack.Add(view.extraRows[i]);
            }
            view.baseWindow.Add(view.baseTrack);
            view.extraWindow.Add(view.extraTrack);
            view.root.Add(view.baseWindow); view.root.Add(view.sign); view.root.Add(view.extraWindow);
            view.root.Add(view.result);
            item.value.Add(view.root);
            return view;
        }

        private static void RenderReel(TransportReelView view, float position, bool extra, bool product = false)
        {
            int index = Mathf.FloorToInt(position);
            Label[] rows = extra ? view.extraRows : view.baseRows;
            for (int i = 0; i < rows.Length; i++)
            {
                int count = extra ? 10 : 9;
                int value = ((index + i - 1) % count + count) % count;
                rows[i].text = extra ? value.ToString() : (product ? "" : "×") + (value + 1);
            }
            (extra ? view.extraTrack : view.baseTrack).style.translate =
                new Translate(0, -(1 + position - index) * ReelRowHeight, 0);
        }

        private float ReelRandom(float min, float max) => Mathf.Lerp(min, max, (float)transportReelRandom.NextDouble());

        private List<ReelMotionPoint> CreateBonusReelMotion(int result)
        {
            bool crosses = transportReelRandom.Next(2) == 0;
            int candidate = crosses ? (result + 9) % 10 : result;
            int boundary = 20 + candidate;
            var points = new List<ReelMotionPoint> { new ReelMotionPoint() };
            AddReelMotion(points, transportBonusCreepSeconds, 1);
            AddReelMotion(points, 0.12f, 1);
            float time = points[points.Count - 1].time;
            for (int i = 2; i <= boundary; i++)
            {
                float progress = (float)(i - 1) / Mathf.Max(1, boundary - 1);
                time += (0.045f + 0.32f * Mathf.Pow(progress, 3.5f)) *
                    ReelRandom(0.88f, 1.12f) * transportDetentTimeScale;
                points.Add(new ReelMotionPoint { time = time, position = i });
            }
            AddReelMotion(points, ReelRandom(0.35f, 0.57f), boundary + ReelRandom(0.44f, 0.52f));
            int wavers = transportReelRandom.Next(1, 3);
            for (int i = 0; i < wavers; i++)
            {
                AddReelMotion(points, ReelRandom(0.14f, 0.26f), boundary + ReelRandom(0.4f, 0.44f));
                AddReelMotion(points, ReelRandom(0.22f, 0.38f), boundary + ReelRandom(0.48f, 0.525f));
            }
            AddReelMotion(points, ReelRandom(0.08f, 0.13f), points[points.Count - 1].position);
            AddReelMotion(points, ReelRandom(0.24f, 0.34f), boundary + (crosses ? 1.035f : -0.035f));
            AddReelMotion(points, 0.17f, boundary + (crosses ? 1 : 0));
            return points;
        }

        private static void AddReelMotion(List<ReelMotionPoint> points, float seconds, float position) =>
            points.Add(new ReelMotionPoint { time = points[points.Count - 1].time + seconds, position = position });

        private static float SampleReelMotion(List<ReelMotionPoint> points, float elapsed)
        {
            int i = 1;
            while (i < points.Count - 1 && elapsed > points[i].time) i++;
            ReelMotionPoint a = points[i - 1], b = points[i];
            float p = Mathf.Clamp01((elapsed - a.time) / Mathf.Max(0.001f, b.time - a.time));
            float blend = p * p * (3 - 2 * p);
            if (i > 2 && i < points.Count - 2 && b.position == Mathf.Round(b.position) && b.position > a.position)
                blend = p < 0.72f ? 1.025f * (1 - Mathf.Pow(1 - p / 0.72f, 2)) :
                    1 + 0.025f * Mathf.Exp(-12 * (p - 0.72f)) * Mathf.Cos((p - 0.72f) * 24);
            if (i == points.Count - 2) blend = p * p * p;
            return Mathf.LerpUnclamped(a.position, b.position, blend);
        }

        private IEnumerator RevealTransportSnapshot(Dictionary<ETransportItemType, TransportMarketPriceEntry> prices)
        {
            var results = new List<CargoReelResult>();
            foreach (TransportItemView item in transportItems)
            {
                if (!prices.TryGetValue(item.cargo.Item.Type, out TransportMarketPriceEntry price)) continue;
                if (price.GetAppliedMultiplier() < item.appliedMultiplier)
                {
                    yield return RevealLegacyTransportSnapshot(prices);
                    yield break;
                }
                results.Add(new CargoReelResult { item = item, price = price, before = item.appliedMultiplier });
            }
            if (results.Count == 0) yield break;
            transportRevenueClose?.Pause();
            transportReveal.AddToClassList("hidden");
            foreach (CargoReelResult result in results)
            {
                result.item.cell.AddToClassList("is-rolling");
                result.item.cell.RemoveFromClassList("is-extra-roll");
            }
            float spin = Mathf.Max(0.1f, transportBaseSpinSeconds);
            float total = spin + (results.Count - 1) * transportReelStaggerSeconds;
            for (float elapsed = 0; elapsed < total; elapsed += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    float p = Mathf.Clamp01(elapsed / (spin + i * transportReelStaggerSeconds));
                    int target = Mathf.Clamp(results[i].price.PriceMultiplier, 1, 9);
                    float position = (18 + target - 1) * (1 - Mathf.Pow(1 - p, 2.2f));
                    RenderReel(results[i].item.reel, position, false);
                }
                yield return null;
            }
            foreach (CargoReelResult result in results)
            {
                RenderReel(result.item.reel, 18 + Mathf.Clamp(result.price.PriceMultiplier, 1, 9) - 1, false);
                result.item.appliedMultiplier = result.before + Mathf.Max(0, result.price.PriceMultiplier - 1);
                result.item.multiplier.text = "×" + result.item.appliedMultiplier;
            }
            yield return CountReelRewards();
            foreach (CargoReelResult result in results) result.item.cell.RemoveFromClassList("is-rolling");
            transportRevenueAutomatic = false;
            RefreshTransportRevenueExpansion();

            bool hasBonus = results.Exists(result => result.price.HasBonusMultiplier);
            if (hasBonus)
            {
                yield return new WaitForSecondsRealtime(transportBonusQuietSeconds);
                float end = 0;
                foreach (CargoReelResult result in results)
                {
                    if (!result.price.HasBonusMultiplier) continue;
                    result.motion = CreateBonusReelMotion(Mathf.Clamp(result.price.BonusPriceMultiplier, 0, 9));
                    end = Mathf.Max(end, result.motion[result.motion.Count - 1].time);
                    result.item.cell.AddToClassList("is-rolling");
                    result.item.cell.AddToClassList("is-extra-roll");
                    RenderReel(result.item.reel, Mathf.Clamp(result.price.PriceMultiplier, 1, 9) - 1, false, true);
                }
                for (float elapsed = 0; elapsed < end; elapsed += Time.unscaledDeltaTime)
                {
                    foreach (CargoReelResult result in results)
                        if (result.motion != null)
                            RenderReel(result.item.reel, SampleReelMotion(result.motion, elapsed), true);
                    yield return null;
                }
                foreach (CargoReelResult result in results)
                {
                    if (result.motion == null) continue;
                    RenderReel(result.item.reel, result.motion[result.motion.Count - 1].position, true);
                    result.item.cell.AddToClassList("is-reel-locked");
                    result.item.reel.result.text = "×" + result.price.GetStageMultiplier();
                    result.item.reel.root.tooltip = result.price.BonusPriceMultiplier == 0 ?
                        "이번 보너스 없음 · 원금과 이전 누적 유지" :
                        $"이번 시세 ×{result.price.PriceMultiplier} ×{result.price.BonusPriceMultiplier} = ×{result.price.GetStageMultiplier()}";
                }
                for (float elapsed = 0; elapsed < 0.28f; elapsed += Time.unscaledDeltaTime)
                {
                    float p = Mathf.Clamp01(elapsed / 0.28f);
                    float punch = Mathf.Sin(p * Mathf.PI * 3) * Mathf.Pow(1 - p, 2);
                    foreach (CargoReelResult result in results)
                        if (result.motion != null)
                            result.item.reel.root.style.scale = new Scale(Vector3.one * (1 + punch * 0.12f * cargoImpactStrength));
                    yield return null;
                }
                foreach (CargoReelResult result in results) result.item.reel.root.style.scale = new Scale(Vector3.one);
            }
            foreach (CargoReelResult result in results)
                result.item.appliedMultiplier = result.price.GetAppliedMultiplier();
            if (hasBonus) yield return CountReelRewards();
            foreach (CargoReelResult result in results)
            {
                result.item.multiplier.text = "×" + result.item.appliedMultiplier;
                result.item.cell.RemoveFromClassList("is-rolling");
                result.item.cell.RemoveFromClassList("is-extra-roll");
                result.item.cell.RemoveFromClassList("is-reel-locked");
            }
            transportRevenueClose = transportCargoRoot.schedule.Execute(() =>
            {
                transportRevenueAutomatic = false;
                RefreshTransportRevenueExpansion();
            }).StartingIn((long)(transportRevenueHoldSeconds * 1000));
        }

        private IEnumerator CountReelRewards()
        {
            float seconds = Mathf.Max(0.1f, transportReelCountSeconds);
            transportRevenueAutomatic = true;
            RefreshTransportRevenueExpansion();
            foreach (TransportRewardView reward in transportRewards) reward.baseline = reward.target;
            SetTransportRewardTargets(false);
            RefreshTransportGoal();
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                AnimateTransportRewards(Time.unscaledDeltaTime, seconds);
                yield return null;
            }
            AnimateTransportRewards(seconds, seconds);
            foreach (TransportRewardView reward in transportRewards)
                reward.delta.style.opacity = reward.target == reward.baseline ? 0 : 1;
            yield return new WaitForSecondsRealtime(transportReelResultHoldSeconds);
            foreach (TransportRewardView reward in transportRewards) reward.delta.style.opacity = 0;
        }
    }
}
