using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Bounty
{
    [DisallowMultipleComponent]
    public sealed class BountyNightEnvironment : MonoBehaviour
    {
        [SerializeField] private GameObject farmingBackground;
        [SerializeField] private GameObject pursuitBackground;
        [SerializeField] private ParallaxLayer[] pursuitLayers = Array.Empty<ParallaxLayer>();
        [Header("Ship lighting")]
        [SerializeField, Tooltip("밤 해역에서 선체·돛에 곱할 주변광입니다. RGB만 적용하며 체력바·포구 섬광에는 적용하지 않습니다.")]
        private Color shipNightTint = new Color(.55f, .67f, .85f, 1f);
        [Header("Pursuit water")]
        [SerializeField] private Material nightWakeMaterial;
        [SerializeField] private Material nightWaterContactMaterial;
        [SerializeField] private Color wakeTint = new Color(.48f, .60f, .70f, .65f);
        [SerializeField, Min(.01f)] private float wakeMotionMultiplier = 2.8f;
        [SerializeField] private SpriteRenderer[] seaRenderers = Array.Empty<SpriteRenderer>();
        [SerializeField, Min(0f), Tooltip("추적 중 해면이 뒤로 흐르는 초당 월드 거리입니다.")]
        private float seaFlowSpeed = 8f;
        [SerializeField, Min(.01f)] private float accelerationSeconds = .45f;
        private static readonly int EnvironmentTintId = Shader.PropertyToID("_EnvironmentTint");
        private static readonly int TravelOffsetId = Shader.PropertyToID("_TravelOffset");
        private readonly List<ShipLightingState> shipLighting = new List<ShipLightingState>();
        private readonly List<RendererMaterialState> waterContacts = new List<RendererMaterialState>();
        private readonly List<ShipWakeEffect> waterWakes = new List<ShipWakeEffect>();
        private readonly List<ShipBuoyancy> sailingBuoyancies = new List<ShipBuoyancy>();
        private MaterialPropertyBlock lightingProperties;
        private MaterialPropertyBlock seaProperties;
        private float seaDistance;
        private float currentFlowSpeed;
        private bool isScrolling;
        private bool farmingWasActive;
        public bool IsActive { get; private set; }

        private struct ShipLightingState
        {
            public SpriteRenderer Renderer;
            public Color OriginalTint;
        }

        private struct RendererMaterialState
        {
            public SpriteRenderer Renderer;
            public Material OriginalMaterial;
        }

        public void ApplyShipLighting(Ship ship)
        {
            if (!IsActive || ship == null) return;
            if (lightingProperties == null) lightingProperties = new MaterialPropertyBlock();
            foreach (SpriteRenderer renderer in ship.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material != null && material.shader.name == "Fleet/Ship Water Contact" && nightWaterContactMaterial != null)
                {
                    waterContacts.Add(new RendererMaterialState { Renderer = renderer, OriginalMaterial = material });
                    renderer.sharedMaterial = nightWaterContactMaterial;
                }
                if (material == null || !material.HasProperty(EnvironmentTintId)) continue;
                renderer.GetPropertyBlock(lightingProperties);
                Color original = lightingProperties.HasColor(EnvironmentTintId)
                    ? lightingProperties.GetColor(EnvironmentTintId) : material.GetColor(EnvironmentTintId);
                shipLighting.Add(new ShipLightingState { Renderer = renderer, OriginalTint = original });
                lightingProperties.SetColor(EnvironmentTintId, original * shipNightTint);
                renderer.SetPropertyBlock(lightingProperties);
            }
            foreach (ShipWakeEffect wake in ship.GetComponentsInChildren<ShipWakeEffect>(true))
            {
                wake.SetWaterPresentation(nightWakeMaterial, wakeTint, wakeMotionMultiplier);
                waterWakes.Add(wake);
            }
            foreach (ShipBuoyancy buoyancy in ship.GetComponentsInChildren<ShipBuoyancy>(true))
            {
                buoyancy.SetSailingSpeedOverride(currentFlowSpeed);
                sailingBuoyancies.Add(buoyancy);
            }
        }

        public void RestoreShipLighting()
        {
            foreach (ShipLightingState state in shipLighting)
            {
                if (state.Renderer == null) continue;
                // Read the current block so other effects retain their own shader values.
                state.Renderer.GetPropertyBlock(lightingProperties);
                lightingProperties.SetColor(EnvironmentTintId, state.OriginalTint);
                state.Renderer.SetPropertyBlock(lightingProperties);
            }
            shipLighting.Clear();
            foreach (RendererMaterialState state in waterContacts)
                if (state.Renderer != null) state.Renderer.sharedMaterial = state.OriginalMaterial;
            waterContacts.Clear();
            foreach (ShipWakeEffect wake in waterWakes)
                if (wake != null) wake.ResetWaterPresentation();
            waterWakes.Clear();
            foreach (ShipBuoyancy buoyancy in sailingBuoyancies)
                if (buoyancy != null) buoyancy.ClearSailingSpeedOverride();
            sailingBuoyancies.Clear();
        }

        public void Enter()
        {
            if (IsActive) return;
            farmingWasActive = farmingBackground.activeSelf;
            farmingBackground.SetActive(false);
            pursuitBackground.SetActive(true);
            IsActive = true;
            seaDistance = currentFlowSpeed = 0f;
            SetScrolling(true);
        }

        public void SetScrolling(bool scrolling)
        {
            isScrolling = scrolling;
            foreach (ParallaxLayer layer in pursuitLayers)
                if (layer != null) { if (scrolling) layer.StartScroll(); else layer.StopScroll(); }
        }

        private void Update()
        {
            if (!IsActive) return;
            currentFlowSpeed = Mathf.MoveTowards(currentFlowSpeed, isScrolling ? seaFlowSpeed : 0f,
                seaFlowSpeed * Time.deltaTime / Mathf.Max(.01f, accelerationSeconds));
            seaDistance += currentFlowSpeed * Time.deltaTime;
            SetSeaDistance(seaDistance);
            foreach (ShipBuoyancy buoyancy in sailingBuoyancies)
                if (buoyancy != null) buoyancy.SetSailingSpeedOverride(currentFlowSpeed);
        }

        private void SetSeaDistance(float distance)
        {
            if (seaProperties == null) seaProperties = new MaterialPropertyBlock();
            foreach (SpriteRenderer renderer in seaRenderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(seaProperties);
                seaProperties.SetFloat(TravelOffsetId, distance);
                renderer.SetPropertyBlock(seaProperties);
            }
        }

        public void Leave()
        {
            RestoreShipLighting();
            if (!IsActive) return;
            SetScrolling(false);
            SetSeaDistance(0f);
            if (pursuitBackground != null) pursuitBackground.SetActive(false);
            if (farmingBackground != null) farmingBackground.SetActive(farmingWasActive);
            IsActive = false;
        }

        private void OnDisable() => Leave();
    }
}
