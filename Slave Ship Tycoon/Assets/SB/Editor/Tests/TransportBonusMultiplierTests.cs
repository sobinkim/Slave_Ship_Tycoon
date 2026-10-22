using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SB.Scripts;
using SB.Scripts.Upgrade;
using UnityEngine;

namespace SB.Editor.Tests
{
    public sealed class TransportBonusMultiplierTests
    {
        [TestCase(1)]
        [TestCase(17)]
        public void BonusRollsPreserveExistingValueWhenZeroIsDrawn(int previousMultiplier)
        {
            Random.State originalRandom = Random.state;
            var go = new GameObject("Bonus roll test");
            go.SetActive(false);
            try
            {
                var manager = go.AddComponent<MarketPriceManager>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(MarketPriceManager).GetField("bonusChanceScale", flags).SetValue(manager, 1f);
                typeof(MarketPriceManager).GetField("_mainShipUpgradeData", flags)
                    .SetValue(manager, new MainShipUpgradeData { luck = 100 });
                var prices = (Dictionary<ETransportItemType, TransportMarketPriceEntry>)
                    typeof(MarketPriceManager).GetField("_cargoTypePriceRates", flags).GetValue(manager);
                MethodInfo roll = typeof(MarketPriceManager).GetMethod("RollAndAccumulateMarketPrices", flags);
                Random.InitState(7241);
                bool sawZero = false, sawOne = false, sawHigher = false;
                for (int i = 0; i < 200; i++)
                {
                    prices[ETransportItemType.Gold] = new TransportMarketPriceEntry
                    { CargoType = ETransportItemType.Gold, AccumulatedPriceMultiplier = previousMultiplier };
                    roll.Invoke(manager, null);
                    TransportMarketPriceEntry entry = prices[ETransportItemType.Gold];
                    Assert.That(entry.HasBonusMultiplier, Is.True);
                    Assert.That(entry.BonusPriceMultiplier, Is.InRange(0, 9));
                    if (entry.BonusPriceMultiplier == 0)
                    {
                        sawZero = true;
                        Assert.That(entry.GetAppliedMultiplier(), Is.EqualTo(previousMultiplier), "Zero must not consume previous market value.");
                    }
                    else if (entry.BonusPriceMultiplier == 1)
                    {
                        sawOne = true;
                        Assert.That(entry.GetAppliedMultiplier(), Is.EqualTo(previousMultiplier + entry.PriceMultiplier - 1));
                    }
                    else
                    {
                        sawHigher = true;
                        Assert.That(entry.GetAppliedMultiplier(), Is.EqualTo(previousMultiplier + entry.PriceMultiplier * entry.BonusPriceMultiplier - 1));
                    }
                }
                Assert.That(sawZero && sawOne && sawHigher, Is.True, "Exercise all bonus outcomes.");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Random.state = originalRandom;
            }
        }
    }
}
