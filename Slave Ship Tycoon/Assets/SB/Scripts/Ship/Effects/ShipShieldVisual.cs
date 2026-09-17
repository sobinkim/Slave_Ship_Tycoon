using SB.Scripts.Fleet.Effects;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class ShipShieldVisual : FleetEffectVisual
    {
        [Header("References")]
        [SerializeField] private MeshRenderer shieldRenderer;
        [SerializeField] private ShieldBreakEffect breakPrefab;
        [Header("Appearance")]
        [SerializeField] private Color sourceColor = new Color(.08f, .52f, 1f, .68f);
        [SerializeField] private Color protectedColor = new Color(.86f, .90f, .94f, .45f);
        [SerializeField, Min(.1f)] private float pulseSeconds = 1.8f;
        [SerializeField, Range(0f, .5f)] private float pulseAmount = .1f;
        [SerializeField, Min(.01f)] private float hitFlashSeconds = .12f;
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private MaterialPropertyBlock properties;
        private Color tint;
        private bool isVisible;
        private float hitRemaining;
        public bool IsVisible => isVisible;
        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            Hide();
        }
        public override void Show(bool isSource)
        {
            tint = isSource ? sourceColor : protectedColor;
            isVisible = true;
            shieldRenderer.enabled = true;
            RefreshColor();
        }
        public override void Hide()
        {
            isVisible = false;
            hitRemaining = 0f;
            if (shieldRenderer != null) shieldRenderer.enabled = false;
        }
        public override void PlayHit() { if (isVisible) hitRemaining = hitFlashSeconds; }
        public override void PlayBreak()
        {
            if (!isVisible) return;
            if (breakPrefab != null && PoolingManager.Instance != null)
            {
                ShieldBreakEffect burst = PoolingManager.Instance.Get(breakPrefab, transform.position, transform.rotation);
                burst.Play(tint, transform.lossyScale, shieldRenderer.sortingLayerID, shieldRenderer.sortingOrder);
            }
            Hide();
        }
        private void Update()
        {
            if (!isVisible) return;
            hitRemaining = Mathf.Max(0f, hitRemaining - Time.deltaTime);
            RefreshColor();
        }
        private void RefreshColor()
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            Color color = Color.Lerp(tint, Color.white, hitRemaining / hitFlashSeconds);
            color.a *= 1f - pulseAmount + pulseAmount * Mathf.Sin(Time.time * Mathf.PI * 2f / pulseSeconds);
            properties.SetColor(TintId, color);
            shieldRenderer.SetPropertyBlock(properties);
        }
        private void OnDisable() { Hide(); }
    }
}
