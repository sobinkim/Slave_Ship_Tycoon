using System;

namespace SB.Core.EventBus
{
    public static class Bus<T> where T : IEvent
    {
        public static event Action<T> OnEvent;

        public static void Raise(T evt)
        {
            OnEvent?.Invoke(evt);
        }

        public static IDisposable Subscribe(Action<T> listener)
        {
            OnEvent += listener;
            return new Subscription(listener);
        }

        public static void Unsubscribe(Action<T> listener)
        {
            OnEvent -= listener;
        }

        public static void Clear()
        {
            OnEvent = null;
        }

        private sealed class Subscription : IDisposable
        {
            private Action<T> listener;

            public Subscription(Action<T> listener)
            {
                this.listener = listener;
            }

            public void Dispose()
            {
                if (listener == null)
                    return;

                Unsubscribe(listener);
                listener = null;
            }
        }
    }
}
