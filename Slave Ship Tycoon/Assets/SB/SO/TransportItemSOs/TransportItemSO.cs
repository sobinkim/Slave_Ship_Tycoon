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
        [Header("Presentation")]
        [SerializeField, Tooltip("적재 화면에 표시할 화물 이름입니다.")]
        private string displayName;
        [SerializeField, Tooltip("적재 카드 테두리와 버튼에 사용할 강조 색상입니다.")]
        private Color accentColor = Color.white;

        public Sprite Icon;

        [Header("Cargo Balance")]
        public ETransportItemType Type;
        public CargoSalesRewardData [] Rewards;
        [Min(0f), Tooltip("화물 한 개의 무게(t)입니다.")]
        public float BaseWeight;

        public string DisplayName => displayName;
        public Color AccentColor => accentColor;
    }
}
