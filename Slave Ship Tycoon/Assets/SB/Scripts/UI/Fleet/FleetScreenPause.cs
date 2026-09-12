using SB.Scripts.Visual;
using UnityEngine;

namespace SB.Scripts.UI.Fleet
{
    public sealed class FleetScreenPause : MonoBehaviour
    {
        [SerializeField] private PannelManager panelManager;
        private float previousTimeScale;
        private bool hasPaused;

        private void OnEnable()
        {
            if (!Application.isPlaying || hasPaused) return;
            previousTimeScale = Time.timeScale;
            hasPaused = true;
            Time.timeScale = 0f;
        }

        private void OnDisable()
        {
            if (!hasPaused) return;
            Time.timeScale = previousTimeScale;
            hasPaused = false;
        }

        public void Close()
        {
            if (panelManager != null) panelManager.CloseFleet();
            else gameObject.SetActive(false);
        }
    }
}
