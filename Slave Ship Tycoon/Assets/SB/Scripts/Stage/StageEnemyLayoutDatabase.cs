using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts
{
    [CreateAssetMenu(fileName = "StageEnemyLayoutDatabase", menuName = "SB/Stage/Enemy Layout Database")]
    public sealed class StageEnemyLayoutDatabase : ScriptableObject
    {
        [SerializeField] private List<StageEnemyLayout> layouts = new List<StageEnemyLayout>();

        public IReadOnlyList<StageEnemyLayout> Layouts => layouts;

        public bool TryGetLayout(int chapter, int stage, out StageEnemyLayout layout)
        {
            for (int i = 0; i < layouts.Count; i++)
            {
                StageEnemyLayout candidate = layouts[i];

                if (candidate != null && candidate.Matches(chapter, stage))
                {
                    layout = candidate;
                    return true;
                }
            }

            layout = null;
            return false;
        }

        private void OnValidate()
        {
            if (layouts == null)
                layouts = new List<StageEnemyLayout>();
        }
    }
}
