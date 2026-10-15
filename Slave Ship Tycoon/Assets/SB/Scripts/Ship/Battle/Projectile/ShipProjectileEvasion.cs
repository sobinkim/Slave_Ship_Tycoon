using SB.Scripts.AttackCompo;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [DisallowMultipleComponent]
    public sealed class ShipProjectileEvasion : MonoBehaviour
    {
        [SerializeField] private ShipCombatStatCompo combatStats;
        [SerializeField] private ProjectileEvasionSettings settings;
        [Header("Water landing points (incoming side)")]
        [SerializeField] private Transform leftWaterPoint;
        [SerializeField] private Transform rightWaterPoint;

        public float MissChance => settings != null && combatStats != null
            ? settings.EvaluateChance(combatStats.FinalLuck) : 0f;

        private void Awake()
        {
            if (combatStats == null || settings == null || leftWaterPoint == null || rightWaterPoint == null)
            {
                Debug.LogError("ShipProjectileEvasion requires stats, settings and both water points.", this);
                enabled = false;
            }
        }

        // The caller supplies one random sample per launch, so long flights do not roll more often.
        public bool TryResolveMiss(Vector3 shotOrigin, float randomSample01, out Vector3 waterPosition)
            => TryResolveMiss(shotOrigin, randomSample01, 1f, out waterPosition);

        public bool TryResolveMiss(Vector3 shotOrigin, float randomSample01, float hitChanceMultiplier, out Vector3 waterPosition)
        {
            waterPosition = default;
            if (!isActiveAndEnabled || leftWaterPoint == null || rightWaterPoint == null ||
                randomSample01 >= 1f - (1f - MissChance) * Mathf.Clamp01(hitChanceMultiplier)) return false;
            waterPosition = shotOrigin.x < transform.position.x ? leftWaterPoint.position : rightWaterPoint.position;
            return true;
        }
    }
}
