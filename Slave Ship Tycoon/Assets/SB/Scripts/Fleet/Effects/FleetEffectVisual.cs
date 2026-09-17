using UnityEngine;

namespace SB.Scripts.Fleet.Effects
{
    public abstract class FleetEffectVisual : MonoBehaviour
    {
        public abstract void Show(bool isSource);
        public abstract void Hide();
        public abstract void PlayBreak();
        public virtual void PlayHit() { }
    }
}
