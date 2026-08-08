using System;
using SB.SO.TransportItemSOs;

namespace SB.Scripts
{
    [Serializable]
    public class CargoData
    {
        public TransportItemSO Item;
        public int Amount;
    }
}