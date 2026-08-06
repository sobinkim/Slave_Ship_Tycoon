using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts
{
    public enum ChapterRouteType
    {
        Obtain,
        Sell
    }

    public enum ChapterEnemyFaction
    {
        Pirate,
        Navy
    }

    [CreateAssetMenu(fileName = "ChapterContainer", menuName = "SB/Stage/Chapter Container")]
    public sealed class ChapterContainer : ScriptableObject
    {
        [SerializeField, Min(1)] private int chapter = 1;
        [SerializeField] private ChapterRouteType routeType;
        [SerializeField] private ChapterEnemyFaction enemyFaction;
        [SerializeField] private StageEnemyLayout bossLayout;
        [SerializeField] private StageEnemyLayout[] stages = Array.Empty<StageEnemyLayout>();

        public int Chapter => chapter;
        public ChapterRouteType RouteType => routeType;
        public ChapterEnemyFaction EnemyFaction => enemyFaction;
        public StageEnemyLayout BossLayout => bossLayout;
        public int MaxStage => stages?.Length ?? 0;
        public IReadOnlyList<StageEnemyLayout> Stages => stages ?? Array.Empty<StageEnemyLayout>();

        public bool IsFinalStage(int stage)
        {
            return routeType == ChapterRouteType.Sell && stage > 0 && stage == MaxStage;
        }

        public bool TryGetRandomWave(out StageEnemyLayout layout)
        {
            if (stages != null && stages.Length > 0)
            {
                int startIndex = UnityEngine.Random.Range(0, stages.Length);

                for (int i = 0; i < stages.Length; i++)
                {
                    int index = (startIndex + i) % stages.Length;

                    if (stages[index] != null)
                    {
                        layout = stages[index];
                        return true;
                    }
                }
            }

            layout = null;
            return false;
        }

        public bool TryGetBoss(out StageEnemyLayout layout)
        {
            layout = bossLayout;
            return layout != null;
        }

        public bool TryGetStage(int stage, out StageEnemyLayout layout)
        {
            int index = stage - 1;

            if (stages == null || index < 0 || index >= stages.Length)
            {
                layout = null;
                return false;
            }

            layout = stages[index];
            return layout != null;
        }

        private void OnValidate()
        {
            chapter = Mathf.Max(1, chapter);
            stages ??= Array.Empty<StageEnemyLayout>();
        }
    }
}
