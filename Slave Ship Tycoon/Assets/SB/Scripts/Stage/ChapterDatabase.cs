using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts
{
    [CreateAssetMenu(fileName = "ChapterDatabase", menuName = "SB/Stage/Chapter Database")]
    public sealed class ChapterDatabase : ScriptableObject
    {
        [SerializeField] private ChapterContainer[] chapters = Array.Empty<ChapterContainer>();

        public IReadOnlyList<ChapterContainer> Chapters => chapters ?? Array.Empty<ChapterContainer>();

        public bool TryGetChapter(int chapter, out ChapterContainer container)
        {
            if (chapters != null)
            {
                for (int i = 0; i < chapters.Length; i++)
                {
                    ChapterContainer candidate = chapters[i];

                    if (candidate != null && candidate.Chapter == chapter)
                    {
                        container = candidate;
                        return true;
                    }
                }
            }

            container = null;
            return false;
        }

        public bool TryGetStage(int chapter, int stage, out StageEnemyLayout layout)
        {
            if (TryGetChapter(chapter, out ChapterContainer container))
                return container.TryGetStage(stage, out layout);

            layout = null;
            return false;
        }

        public bool IsFinalStage(int chapter, int stage, out bool isFinalStage)
        {
            if (TryGetChapter(chapter, out ChapterContainer container))
            {
                isFinalStage = container.IsFinalStage(stage);
                return true;
            }

            isFinalStage = false;
            return false;
        }

        private void OnValidate()
        {
            chapters ??= Array.Empty<ChapterContainer>();
        }
    }
}
