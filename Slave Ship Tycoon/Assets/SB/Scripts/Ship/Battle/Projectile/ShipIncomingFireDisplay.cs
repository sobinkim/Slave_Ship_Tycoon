using System.Collections.Generic;
using SB.Core;
using SB.Core.EventBus;
using TMPro;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    [DisallowMultipleComponent]
    public sealed class ShipIncomingFireDisplay : MonoBehaviour
    {
        private const float MinimumFadeSeconds = 0.01f;

        [Header("References")]
        [SerializeField] private Ship ship;
        [SerializeField] private EntityHealth health;
        [SerializeField] private SpriteRenderer hull;
        [SerializeField] private MeshRenderer waterDecal;
        [SerializeField] private TMP_Text countLabel;

        [Header("Incoming fire")]
        [SerializeField] private Color warningColor = new Color(1f, .06f, .08f);
        [SerializeField, Range(0f, 1f)] private float idleAlpha = .75f;
        [SerializeField, Range(0f, 1f)] private float dangerAlpha = 1f;
        [SerializeField, Min(.01f)] private float fadeSeconds = .12f;
        [SerializeField, Min(.01f), Tooltip("위험도에는 이 시간 안에 도착하는 탄의 피해만 반영합니다. 숫자는 모든 비행 중 탄을 셉니다.")]
        private float dangerWindowSeconds = 1f;
        [SerializeField, Min(.01f), Tooltip("예상 피해 / 현재 체력이 이 값이면 최대 강조")]
        private float dangerHealthRatio = .6f;
        [SerializeField, Tooltip("선체보다 뒤에 렌더링할 정렬 순서 차이")]
        private int decalSortingOffset = -2;

        private readonly Dictionary<BaseProjectile, float> incoming = new Dictionary<BaseProjectile, float>();
        private MaterialPropertyBlock properties;
        private int generation;
        private int displayedCount = -1;
        private float alpha;
        public int IncomingCount => incoming.Count;
        public float Danger01 { get; private set; }
        private static readonly int WarningColor = Shader.PropertyToID("_WarningColor");

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            if (ship == null || health == null || hull == null || waterDecal == null || countLabel == null)
            {
                Debug.LogError("ShipIncomingFireDisplay requires explicit ship, health, hull, water decal and label references.", this);
                enabled = false;
            }
            Clear();
        }

        private void OnEnable() => Bus<StageBattleEndedEvent>.OnEvent += HandleBattleEnded;
        private void OnDisable()
        {
            Bus<StageBattleEndedEvent>.OnEvent -= HandleBattleEnded;
            Clear();
        }
        private void HandleBattleEnded(StageBattleEndedEvent evt)
        {
            Clear();
        }

        public void Register(BaseProjectile projectile, float damage, int spawnGeneration)
        {
            if (!isActiveAndEnabled || ship.IsDead || ship.SpawnGeneration != spawnGeneration) return;
            if (generation != spawnGeneration) Clear();
            generation = spawnGeneration;
            incoming[projectile] = Mathf.Max(0f, damage);
        }
        public void Unregister(BaseProjectile projectile) => incoming.Remove(projectile);

        private void LateUpdate()
        {
            if (ship.IsDead || generation != ship.SpawnGeneration) { Clear(); return; }
            float damageSoon = 0f;
            foreach (var entry in incoming)
                if (entry.Key != null && entry.Key.RemainingFlightSeconds <= dangerWindowSeconds)
                    damageSoon += health.EstimateIncomingDamage(entry.Value);
            Danger01 = health.CurrentHealth > 0f
                ? Mathf.Clamp01(damageSoon / health.CurrentHealth / dangerHealthRatio) : 0f;
            float targetAlpha = incoming.Count > 0
                ? Mathf.Lerp(idleAlpha, dangerAlpha, Danger01) : 0f;
            alpha = Mathf.MoveTowards(alpha, targetAlpha,
                Time.deltaTime / Mathf.Max(MinimumFadeSeconds, fadeSeconds));
            if (displayedCount != incoming.Count)
            {
                displayedCount = incoming.Count;
                // Monospaced digits preserve the marker's position as counts change.
                countLabel.text = "×<mspace=0.6em>" + displayedCount + "</mspace>";
            }
            countLabel.enabled = incoming.Count > 0;
            waterDecal.enabled = ship.myShipData.ShipType != ShipType.Enemy && alpha > 0f && hull.enabled;
            if (!waterDecal.enabled) return;
            waterDecal.sortingLayerID = hull.sortingLayerID;
            waterDecal.sortingOrder = hull.sortingOrder + decalSortingOffset;
            properties.SetColor(WarningColor, new Color(warningColor.r, warningColor.g, warningColor.b, alpha));
            waterDecal.SetPropertyBlock(properties);
        }
        private void Clear()
        {
            incoming.Clear();
            alpha = 0f;
            Danger01 = 0f;
            displayedCount = -1;
            if (waterDecal != null) waterDecal.enabled = false;
            if (countLabel != null) countLabel.enabled = false;
        }
    }
}
