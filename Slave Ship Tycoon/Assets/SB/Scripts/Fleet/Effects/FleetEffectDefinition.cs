using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Fleet.Effects
{
    // Shared assets contain rules and presentation data; receivers own runtime applications.
    public abstract class FleetEffectDefinition : ScriptableObject
    {
        [Header("Status icon")]
        [SerializeField, Tooltip("이 효과를 받는 함선의 체력바 아래에 표시할 아이콘. 미지정 시 표시하지 않습니다.")]
        private Sprite statusIcon;
        [SerializeField, Tooltip("아이콘의 표시 색상과 투명도")]
        private Color statusIconColor = Color.white;
        public Sprite StatusIcon => statusIcon;
        public Color StatusIconColor => statusIconColor;
        public virtual bool BlocksDirectTargeting => false;
        public virtual float IncomingDamageMultiplier => 1f;
        public abstract void CollectTargets(Ship source, IReadOnlyList<Ship> allies, List<Ship> targets);
        public virtual void OnApplied(FleetEffectReceiver receiver, FleetEffectProvider source) { }
        public virtual void OnRemoved(FleetEffectReceiver receiver, FleetEffectProvider source) { }
    }
}
