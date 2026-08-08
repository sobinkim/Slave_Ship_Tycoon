using System;
using SB.Scripts;
using SB.Scripts.Currency;
using UnityEngine;

namespace SB.SO.TransportItemSOs
{
    [Serializable]
    public struct CargoSalesRewardData
    {
       public CurrencyType  CurrencyType;
       public int CurrencyAmount;
    }
    
    [CreateAssetMenu(
        fileName = "TransportItem_",
        menuName = "Transport/Transport Item",
        order = 0)]

  
    public class TransportItemSO : ScriptableObject
    {
        public Sprite Icon;
        public ETransportItemType Type;
        public CargoSalesRewardData [] Rewards;
        public float BaseWeight;
    }
}