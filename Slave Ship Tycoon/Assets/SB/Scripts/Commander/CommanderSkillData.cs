using UnityEngine;

namespace SB.Scripts.Commander
{
    public enum CommanderSkillType
    {
        None = 0,
        TotalOffensive = 1,
        FocusFire = 2
    }

    public enum CommanderSkillCategory
    {
        Support,
        Attack
    }

    public abstract class CommanderSkillData : ScriptableObject
    {
        [SerializeField] private string _skillId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(0f)] private float _gaugeCost;
        [SerializeField, Min(0f)] private float _cooldown;
        [SerializeField, Min(0.01f)] private float _duration = 1f;

        public abstract CommanderSkillType SkillType { get; }
        public abstract CommanderSkillCategory Category { get; }
        public string SkillId => string.IsNullOrWhiteSpace(_skillId) ? SkillType.ToString() : _skillId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public float GaugeCost => _gaugeCost;
        public float Cooldown => _cooldown;
        public float Duration => _duration;

        protected virtual void OnValidate()
        {
            _gaugeCost = Mathf.Max(0f, _gaugeCost);
            _cooldown = Mathf.Max(0f, _cooldown);
            _duration = Mathf.Max(0.01f, _duration);
        }
    }
}
