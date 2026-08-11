using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public class CommanderGaugeSliderView : MonoBehaviour
    {
        [SerializeField] private Slider _commanderGaugeSlider;
        [SerializeField] private TMP_Text _commanderGaugeValueText;

        public void SetCommanderGauge(float maxGauge, float currentGauge)
        {
            _commanderGaugeSlider.maxValue = maxGauge;
            _commanderGaugeSlider.value = currentGauge;
            _commanderGaugeValueText.text = $"{currentGauge:0}/{maxGauge:0}";
        }
    }
}
