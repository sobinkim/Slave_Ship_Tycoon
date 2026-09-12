using System;
using UnityEngine;

namespace SB.Scripts.Visual
{
    public enum PanelType
    {
        Upgrade,
        Cago,
        Fleet,
        TransportEquipment
    }
    [Serializable]
    struct PanelInfo
    {
        public PanelType type;
        public PanelView panel;
    }

    public class PannelManager : MonoBehaviour
    {
        [SerializeField] private PanelInfo[] _panelInfo;
        private PanelType currentPanel = PanelType.Upgrade;
        private PanelType panelBeforeFleet = PanelType.Upgrade;

        public void CloseFleet()
        {
            ClickPanelButton(panelBeforeFleet);
        }

        private void Start()
        {
            //초기
            ClickPanelButton(PanelType.Upgrade);

            foreach (PanelInfo panelInfo in _panelInfo)
            {
                panelInfo.panel.OnClicked += () => ClickPanelButton(panelInfo.type);
            }
        }

        public void ClickPanelButton(PanelType type)
        {
            if (type == PanelType.Fleet && currentPanel != PanelType.Fleet)
                panelBeforeFleet = currentPanel;
            currentPanel = type;
            foreach (PanelInfo panelInfo in _panelInfo)
            {
                if (panelInfo.type == type)
                {
                    panelInfo.panel.PanelActive(true);
                }
                else
                {
                    panelInfo.panel.PanelActive(false);
                }
            }
        }
    }
}
