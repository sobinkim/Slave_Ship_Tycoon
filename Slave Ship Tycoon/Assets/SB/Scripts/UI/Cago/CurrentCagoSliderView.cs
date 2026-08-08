using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    public class CurrentCagoSliderView : MonoBehaviour
    {
        [SerializeField] private Slider _currentCagoSlider;
        [SerializeField] private TMP_Text _MaxCargoCapacityValueText;


        public void SetCargoCapacityText(float statCargoCapacity, float _currentCargoCapacity)
        {
            string text = $"{_currentCargoCapacity}/{statCargoCapacity}";
            _MaxCargoCapacityValueText.text = text;
        }
        public void SetCurrentCagoText(float statCargoCapacity, float _currentCargoCapacity)
        {
            _currentCagoSlider.maxValue = statCargoCapacity;
            _currentCagoSlider.value = _currentCargoCapacity;
        }
    }
}