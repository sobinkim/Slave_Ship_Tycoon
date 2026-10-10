using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [CreateAssetMenu(menuName = "SB/Combat/Projectile Evasion Settings")]
    public sealed class ProjectileEvasionSettings : ScriptableObject
    {
        private const float MinimumLuckForHalfMaximum = 0.01f;

        [Header("Chance Curve")]
        [SerializeField, Range(0f, 1f), Tooltip("운에 따른 회피 확률 상한")]
        private float maximumMissChance = .15f;
        [SerializeField, Min(.01f), Tooltip("상한의 절반 확률에 도달하는 운 수치")]
        private float luckForHalfMaximum = 50f;

        public float EvaluateChance(float luck)
        {
            float nonNegativeLuck = Mathf.Max(0f, luck);
            return Mathf.Clamp01(maximumMissChance) *
                   (nonNegativeLuck / (nonNegativeLuck +
                       Mathf.Max(MinimumLuckForHalfMaximum, luckForHalfMaximum)));
        }
    }
}
