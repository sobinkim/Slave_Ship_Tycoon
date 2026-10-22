using UnityEngine;

namespace SB.Scripts.Bounty
{
    [CreateAssetMenu(menuName = "SB/Bounty/Pursuit", fileName = "BountyPursuit")]
    public sealed class BountyPursuitDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "붉은 돛 해적단";
        [SerializeField] private StageEnemyLayout enemyLayout;
        [SerializeField, Range(0, StageEnemyLayout.SlotCount - 1)] private int mainPirateSlot = 4;
        [SerializeField, Min(1)] private float escapeSeconds = 60;
        [SerializeField, Min(0)] private int diamondReward = 25;
        public string DisplayName => displayName;
        public StageEnemyLayout EnemyLayout => enemyLayout;
        public int MainPirateSlot => mainPirateSlot;
        public float EscapeSeconds => escapeSeconds;
        public int DiamondReward => diamondReward;
    }
}
