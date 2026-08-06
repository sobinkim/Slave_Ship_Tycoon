using SB.Scripts.Visual;
using UnityEngine;

namespace SB.Core.EventBus
{
    public readonly struct FloatingTextRequestedEvent : IEvent
    {
        public readonly FloatingTextType TextType;
        public readonly int Value;
        public readonly Vector3 WorldPosition;

        public FloatingTextRequestedEvent(FloatingTextType textType, int value, Vector3 worldPosition)
        {
            TextType = textType;
            Value = value;
            WorldPosition = worldPosition;
        }
    }
}
