using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public class CommanderGaugeSliderController : MonoBehaviour
    {
        [SerializeField] private CommanderGaugeSliderView _commanderGaugeSliderView;

        private void OnEnable()
        {
            Bus<CommanderGaugeChangedEvent>.OnEvent += UpdateCommanderGauge;
        }

        private void OnDisable()
        {
            Bus<CommanderGaugeChangedEvent>.OnEvent -= UpdateCommanderGauge;
        }

        private void UpdateCommanderGauge(CommanderGaugeChangedEvent evt)
        {
            _commanderGaugeSliderView.SetCommanderGauge(evt.MaxGauge, evt.CurrentGauge);
        }
    }
}
