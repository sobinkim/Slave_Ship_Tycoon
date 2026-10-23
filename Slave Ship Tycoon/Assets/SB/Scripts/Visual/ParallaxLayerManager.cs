using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class ParallaxLayerManager : MonoBehaviour
    {
        private ParallaxLayer[] childRarallaxLayers;

        private void Awake()
        {
            childRarallaxLayers = GetComponentsInChildren<ParallaxLayer>();
        }

        private void OnEnable()
        {
            Bus<StartEncounterSequence>.OnEvent += PlayEncounterSequence;
            Bus<StopEncounterSequence>.OnEvent += StopEncounterSequence;
        }

        private void OnDisable()
        {
            Bus<StartEncounterSequence>.OnEvent -= PlayEncounterSequence;
            Bus<StopEncounterSequence>.OnEvent -= StopEncounterSequence;
        }

        public void SetTravelStrength(float strength)
        {
            foreach (ParallaxLayer layer in childRarallaxLayers)
                if (layer != null) layer.SetTravelStrength(strength);
        }

        private void PlayEncounterSequence(StartEncounterSequence evt)
        {
            foreach (ParallaxLayer rarallaxLayer in childRarallaxLayers)
            {
                rarallaxLayer.StartScroll();
            }
        }

        private void StopEncounterSequence(StopEncounterSequence evt)
        {
            foreach (ParallaxLayer rarallaxLayer in childRarallaxLayers)
            {
                rarallaxLayer.StopScroll();
            }
        }
    }
}
