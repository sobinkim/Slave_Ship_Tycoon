using SB.Core;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public class ShipProjectileAnchorCompo : EntityComponent
    {
        [Header("Anchors")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform hitPoint;
        [SerializeField] private Transform waterImpactPoint;

        [Header("Fallback Local Offsets")]
        [SerializeField] private Vector3 fireOffset;
        [SerializeField] private Vector3 hitOffset;
        [SerializeField] private Vector3 waterImpactOffset = new Vector3(0f, -0.25f, 0f);

        public Vector3 FirePosition => ResolvePosition(firePoint, fireOffset);
        public Vector3 HitPosition => ResolvePosition(hitPoint, hitOffset);
        public Vector3 WaterImpactPosition => ResolvePosition(waterImpactPoint, waterImpactOffset);

        private Vector3 ResolvePosition(Transform anchor, Vector3 fallbackLocalOffset)
        {
            if (anchor != null)
                return anchor.position;

            Transform ownerTransform = Owner != null ? Owner.transform : transform;
            return ownerTransform.TransformPoint(fallbackLocalOffset);
        }
    }
}
