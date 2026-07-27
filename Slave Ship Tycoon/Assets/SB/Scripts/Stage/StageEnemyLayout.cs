using System;
using UnityEngine;

namespace SB.Scripts
{
    [CreateAssetMenu(fileName = "StageEnemyLayout", menuName = "SB/Stage/Enemy Layout")]
    public sealed class StageEnemyLayout : ScriptableObject
    {
        public const int GridSize = 3;
        public const int SlotCount = GridSize * GridSize;

        [SerializeField, Min(1)] private int chapter = 1;
        [SerializeField, Min(1)] private int stage = 1;
        [SerializeField] private Enemy[] enemySlots = new Enemy[SlotCount];

        public int Chapter => chapter;
        public int Stage => stage;

        public int EnemyCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < enemySlots.Length; i++)
                {
                    if (enemySlots[i] != null)
                        count++;
                }

                return count;
            }
        }

        public bool Matches(int targetChapter, int targetStage)
        {
            return chapter == targetChapter && stage == targetStage;
        }

        public Enemy GetEnemyAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            return enemySlots[slotIndex];
        }

        private void OnEnable()
        {
            EnsureSlotCount();
        }

        private void OnValidate()
        {
            chapter = Mathf.Max(1, chapter);
            stage = Mathf.Max(1, stage);
            EnsureSlotCount();
        }

        private void EnsureSlotCount()
        {
            if (enemySlots == null)
                enemySlots = new Enemy[SlotCount];
            else if (enemySlots.Length != SlotCount)
                Array.Resize(ref enemySlots, SlotCount);
        }
    }
}
