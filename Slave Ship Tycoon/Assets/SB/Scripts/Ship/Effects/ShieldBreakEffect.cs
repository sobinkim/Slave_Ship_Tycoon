using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class ShieldBreakEffect : MonoBehaviour, IPoolable
    {
        [Serializable]
        public struct Fragment
        {
            public MeshRenderer Renderer;
            public Vector3 LocalVelocity;
            public float SpinDegreesPerSecond;
        }
        [SerializeField] private Fragment[] fragments = Array.Empty<Fragment>();
        [SerializeField, Min(.01f)] private float durationSeconds = .55f;
        [SerializeField, Min(0f)] private float flashSeconds = .04f;
        [SerializeField] private float gravityUnitsPerSecondSquared = 1.2f;
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private MaterialPropertyBlock properties;
        private float elapsedSeconds;
        private Color tint;
        private bool isPlaying;
        public void Play(Color color, Vector3 worldScale, int sortingLayerId, int sortingOrder)
        {
            tint = color;
            transform.localScale = worldScale;
            elapsedSeconds = 0f;
            isPlaying = true;
            if (properties == null) properties = new MaterialPropertyBlock();
            foreach (Fragment fragment in fragments)
            {
                fragment.Renderer.transform.localPosition = Vector3.zero;
                fragment.Renderer.transform.localRotation = Quaternion.identity;
                fragment.Renderer.sortingLayerID = sortingLayerId;
                fragment.Renderer.sortingOrder = sortingOrder;
            }
            RefreshFragments();
        }
        private void Update()
        {
            if (!isPlaying) return;
            elapsedSeconds += Time.deltaTime;
            if (elapsedSeconds >= durationSeconds) { ReturnToPool(); return; }
            RefreshFragments();
        }
        private void RefreshFragments()
        {
            float time = Mathf.Max(0f, elapsedSeconds - flashSeconds);
            float fade = 1f - Mathf.Clamp01(time / Mathf.Max(.01f, durationSeconds - flashSeconds));
            Color color = elapsedSeconds < flashSeconds ? Color.white : tint;
            color.a *= fade;
            properties.SetColor(TintId, color);
            foreach (Fragment fragment in fragments)
            {
                fragment.Renderer.transform.localPosition = fragment.LocalVelocity * time +
                    Vector3.down * (.5f * gravityUnitsPerSecondSquared * time * time);
                fragment.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, fragment.SpinDegreesPerSecond * time);
                fragment.Renderer.SetPropertyBlock(properties);
            }
        }
        private void OnEnable() { Bus<StageStartedEvent>.OnEvent += HandleStageStarted; }
        private void OnDisable() { Bus<StageStartedEvent>.OnEvent -= HandleStageStarted; isPlaying = false; }
        private void HandleStageStarted(StageStartedEvent evt) { ReturnToPool(); }
        private void ReturnToPool()
        {
            if (!isPlaying) return;
            isPlaying = false;
            if (PoolingManager.Instance != null) PoolingManager.Instance.Release(this);
            else gameObject.SetActive(false);
        }
        public void OnSpawnedFromPool() { isPlaying = false; }
        public void OnDespawnedToPool() { isPlaying = false; transform.localScale = Vector3.one; }
    }
}
