using System;
using SB.Core.EventBus;
using UnityEngine;

namespace SB.Core.Samples
{
    public sealed class SampleEventUsage : MonoBehaviour
    {
        private IDisposable subscription;

        private void OnEnable()
        {
            subscription = Bus<SamplePingEvent>.Subscribe(HandlePing);
        }

        private void OnDisable()
        {
            subscription?.Dispose();
            subscription = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Bus<SamplePingEvent>.Raise(new SamplePingEvent(Time.time));
        }

        private void HandlePing(SamplePingEvent evt)
        {
            Debug.Log($"Ping at {evt.Time:0.00}", this);
        }
    }

    public readonly struct SamplePingEvent : IEvent
    {
        public readonly float Time;

        public SamplePingEvent(float time)
        {
            Time = time;
        }
    }
}
