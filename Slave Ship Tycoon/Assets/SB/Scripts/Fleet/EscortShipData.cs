using UnityEngine;

namespace SB.Scripts.Fleet
{
    public enum EscortShipGrade
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "EscortShipData", menuName = "SB/Fleet/Escort Ship Data")]
    public sealed class EscortShipData : ScriptableObject
    {
        [SerializeField] private string _stableId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private EscortShipGrade _grade;
        [SerializeField] private Ship _shipPrefab;

        public string StableId => _stableId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public EscortShipGrade Grade => _grade;
        public Ship ShipPrefab => _shipPrefab;

        public bool IsConfigured =>
            string.IsNullOrWhiteSpace(_stableId) == false &&
            string.IsNullOrWhiteSpace(_displayName) == false &&
            _shipPrefab != null;
    }
}
