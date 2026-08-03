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
            Bus<StartEncounterSequence>.OnEvent += PlayEncounterSequence;
            Bus<StopEncounterSequence>.OnEvent += StopEncounterSequence;
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