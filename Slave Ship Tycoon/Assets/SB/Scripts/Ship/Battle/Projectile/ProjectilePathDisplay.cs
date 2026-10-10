using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts.Projectiles
{
    public sealed class ProjectilePathDisplay : MonoBehaviour
    {
        private const int InitialTravelledPositionCapacity = 128;

        [Header("References")]
        [SerializeField] private BaseProjectile projectile;
        [SerializeField] private LineRenderer pathRenderer;
        [SerializeField] private SpriteRenderer projectileRenderer;

        [Header("Travelled Flight Path")]
        [SerializeField, Min(0.001f), Tooltip("실제 지나간 위치를 저장하는 최소 월드 거리")]
        private float pointSpacingUnits = 0.035f;
        [SerializeField, Min(0.001f), Tooltip("경로 선의 월드 좌표 두께")]
        private float widthUnits = 0.025f;
        [SerializeField] private Color pathColor = new Color(1f, 0.12f, 0.16f, 0.45f);
        [SerializeField, Tooltip("탄 본체 기준 경로의 렌더링 순서 차이")]
        private int sortingOrderOffset = -2;
        private readonly List<Vector3> travelledPositions =
            new List<Vector3>(InitialTravelledPositionCapacity);

        private void Awake()
        {
            if (projectile == null || pathRenderer == null || projectileRenderer == null)
            {
                Debug.LogError("ProjectilePathDisplay requires projectile, path renderer and sprite renderer references.", this);
                enabled = false;
                return;
            }
            pathRenderer.useWorldSpace = true;
            pathRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (!projectile.IsFlying)
            {
                ClearPath();
                return;
            }

            if (travelledPositions.Count == 0)
                travelledPositions.Add(projectile.FlightStartPosition);
            Vector3 currentPosition = projectile.transform.position;
            if ((currentPosition - travelledPositions[travelledPositions.Count - 1]).sqrMagnitude >= pointSpacingUnits * pointSpacingUnits)
                travelledPositions.Add(currentPosition);
            pathRenderer.positionCount = travelledPositions.Count + 1;
            pathRenderer.widthMultiplier = widthUnits;
            pathRenderer.startColor = pathColor;
            pathRenderer.endColor = pathColor;
            pathRenderer.sortingLayerID = projectileRenderer.sortingLayerID;
            pathRenderer.sortingOrder = projectileRenderer.sortingOrder + sortingOrderOffset;
            // Preserve travelled positions even when target loss changes the remaining flight curve.
            for (int i = 0; i < travelledPositions.Count; i++)
                pathRenderer.SetPosition(i, travelledPositions[i]);
            pathRenderer.SetPosition(travelledPositions.Count, currentPosition);
            pathRenderer.enabled = true;
        }

        private void OnDisable()
        {
            ClearPath();
        }

        private void ClearPath()
        {
            travelledPositions.Clear();
            if (pathRenderer == null) return;
            pathRenderer.enabled = false;
            pathRenderer.positionCount = 0;
        }
    }
}
