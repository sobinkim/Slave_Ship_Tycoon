using System;
using UnityEngine;

namespace SB.Core
{
    [Serializable]
    public class StatOverride
    {
        [SerializeField] private StatSO stat;
        [SerializeField] private bool useOverride;
        [SerializeField] private float overrideBaseValue;

        public StatSO Stat => stat;
        public bool UseOverride => useOverride;
        public float OverrideBaseValue => overrideBaseValue;

        public StatOverride(StatSO stat)
        {
            this.stat = stat;
        }

        public StatSO CreateStat()
        {
            if (stat == null)
                return null;

            StatSO newStat = stat.Clone();
            if (newStat == null)
                return null;

            if (useOverride)
                newStat.BaseValue = overrideBaseValue;

            return newStat;
        }
    }
}
