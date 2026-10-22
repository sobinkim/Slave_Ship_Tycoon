using System;
using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Bounty
{
    [DisallowMultipleComponent]
    public sealed class BountyNightEnvironment : MonoBehaviour
    {
        [SerializeField] private Camera battleCamera;
        [SerializeField] private Transform pursuitCameraPose;
        [SerializeField] private GameObject farmingBackground;
        [SerializeField] private GameObject pursuitBackground;
        [SerializeField] private ParallaxLayer[] pursuitLayers = Array.Empty<ParallaxLayer>();
        [Header("Ship lighting")]
        [SerializeField, Tooltip("밤 해역에서 선체·돛에 곱할 주변광입니다. RGB만 적용하며 체력바·포구 섬광에는 적용하지 않습니다.")]
        private Color shipNightTint = new Color(.55f, .67f, .85f, 1f);
        private static readonly int EnvironmentTintId = Shader.PropertyToID("_EnvironmentTint");
        private readonly List<ShipLightingState> shipLighting = new List<ShipLightingState>();
        private MaterialPropertyBlock lightingProperties;
        private Vector3 cameraPosition;
        private Quaternion cameraRotation;
        private bool farmingWasActive;
        public bool IsActive { get; private set; }

        private struct ShipLightingState
        {
            public SpriteRenderer Renderer;
            public Color OriginalTint;
        }

        public void ApplyShipLighting(Ship ship)
        {
            if (!IsActive || ship == null) return;
            if (lightingProperties == null) lightingProperties = new MaterialPropertyBlock();
            foreach (SpriteRenderer renderer in ship.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material == null || !material.HasProperty(EnvironmentTintId)) continue;
                renderer.GetPropertyBlock(lightingProperties);
                Color original = lightingProperties.HasColor(EnvironmentTintId)
                    ? lightingProperties.GetColor(EnvironmentTintId) : material.GetColor(EnvironmentTintId);
                shipLighting.Add(new ShipLightingState { Renderer = renderer, OriginalTint = original });
                lightingProperties.SetColor(EnvironmentTintId, original * shipNightTint);
                renderer.SetPropertyBlock(lightingProperties);
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
        }

        public void Enter()
        {
            if (IsActive) return;
            cameraPosition = battleCamera.transform.position;
            cameraRotation = battleCamera.transform.rotation;
            farmingWasActive = farmingBackground.activeSelf;
            farmingBackground.SetActive(false);
            pursuitBackground.SetActive(true);
            battleCamera.transform.SetPositionAndRotation(pursuitCameraPose.position, pursuitCameraPose.rotation);
            IsActive = true;
            SetScrolling(true);
        }

        public void SetScrolling(bool scrolling)
        {
            foreach (ParallaxLayer layer in pursuitLayers)
                if (layer != null) { if (scrolling) layer.StartScroll(); else layer.StopScroll(); }
        }

        public void Leave()
        {
            RestoreShipLighting();
            if (!IsActive) return;
            SetScrolling(false);
            if (pursuitBackground != null) pursuitBackground.SetActive(false);
            if (farmingBackground != null) farmingBackground.SetActive(farmingWasActive);
            if (battleCamera != null) battleCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            IsActive = false;
        }

        private void OnDisable() => Leave();
    }
}
