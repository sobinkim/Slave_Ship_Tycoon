using SB.Core.EventBus;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class SaleSettlementController : MonoBehaviour
    {
        [SerializeField] private SaleSettlementView _view;

        private void OnEnable()
        {
            Bus<SaleSettlementCompletedEvent>.OnEvent += OnSaleSettlementCompleted;
        }

        private void OnDisable()
        {
            Bus<SaleSettlementCompletedEvent>.OnEvent -= OnSaleSettlementCompleted;
        }

        private void OnSaleSettlementCompleted(SaleSettlementCompletedEvent evt)
        {
            _view?.Show(evt.Result);
        }
    }
}
