using System;
using UnityEngine;

namespace SB.Scripts
{
    [CreateAssetMenu(fileName = "StageEnemyLayout", menuName = "SB/Stage/Enemy Layout")]
    public sealed class StageEnemyLayout : ScriptableObject
    {
        public const int ColumnCount = 3;
        public const int RowCount = 4;
        public const int SlotCount = ColumnCount * RowCount;

        [SerializeField, Min(1)] private int chapter = 1;
        [SerializeField, Min(1)] private int stage = 1;
        [SerializeField, Range(-1, SlotCount - 1), Tooltip("보스 조우에서 먼저 보여줄 본체의 슬롯입니다. -1이면 본체 강조 없이 함대가 진입합니다.")]
        private int encounterFocusSlot = -1;
        [SerializeField, Tooltip("행 우선 12칸: 각 행은 전열, 중열, 후열 순서입니다. 비워 둔 칸은 생성하지 않습니다.")] private Enemy[] enemySlots = new Enemy[SlotCount];

        public int Chapter => chapter;
        public int Stage => stage;
        public int EncounterFocusSlot => encounterFocusSlot;

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

            return enemySlots != null && slotIndex < enemySlots.Length ? enemySlots[slotIndex] : null;
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

