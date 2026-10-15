using System.Collections.Generic;
using UnityEngine;

namespace SB.Scripts
{
    // Surface traces in the moving camera frame of the advancing fleet.
    public sealed class SeaVoyagePresentation : MonoBehaviour
    {
        [SerializeField] private SeaCreatureSurvivalDirector director;
        [SerializeField] private LineRenderer[] wakeLines;
        [SerializeField] private LineRenderer[] waterLines;
        [SerializeField] private Vector2 horizontalBounds = new Vector2(-3.5f, 5.5f);
        [SerializeField] private Vector2 verticalBounds = new Vector2(-0.6f, 3.05f);
        [SerializeField, Min(0.1f)] private float wakeLength = 0.85f;
        [SerializeField] private Vector2 wakeOriginOffset = new Vector2(-0.55f, -0.12f);
        [SerializeField, Min(0f)] private float wakeSpread = 0.22f;
        [SerializeField, Min(0.1f)] private float foregroundSpeedMultiplier = 1.25f;
        private readonly List<SeaCreatureTarget> fleet = new List<SeaCreatureTarget>();

        internal void BindFleet(IReadOnlyList<SeaCreatureTarget> ships)
        {
            fleet.Clear();
            foreach (SeaCreatureTarget ship in ships) fleet.Add(ship);
        }

        private void LateUpdate()
        {
            bool sailing = director != null && director.IsSailing;
            for (int i = 0; i < wakeLines.Length; i++)
            {
                LineRenderer line = wakeLines[i];
                int shipIndex = i / 2;
                bool visible = sailing && shipIndex < fleet.Count && fleet[shipIndex].IsAlive;
                line.enabled = visible;
                if (!visible) continue;
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 stern = fleet[shipIndex].Ship.transform.position + (Vector3)wakeOriginOffset;
                stern.y += side * 0.12f;
                float ripple = Mathf.Sin(director.DistanceTravelled * 7f + i) * 0.04f;
                line.SetPosition(0, stern);
                line.SetPosition(1, stern + new Vector3(-wakeLength * 0.5f, side * (wakeSpread * 0.5f + ripple), 0f));
                line.SetPosition(2, stern + new Vector3(-wakeLength, side * wakeSpread, 0f));
            }
            float width = horizontalBounds.y - horizontalBounds.x;
            for (int i = 0; i < waterLines.Length; i++)
            {
                LineRenderer line = waterLines[i];
                line.enabled = sailing;
                if (!sailing) continue;
                float initialX = Mathf.Repeat(i * 3.713f, width);
                float depth = Mathf.Repeat(i * 0.618034f, 1f);
                float speed = Mathf.Lerp(0.85f, foregroundSpeedMultiplier, 1f - depth);
                float x = director.DistanceTravelled + horizontalBounds.x + Mathf.Repeat(initialX - director.DistanceTravelled * speed, width);
                float y = Mathf.Lerp(verticalBounds.x, verticalBounds.y, depth);
                float length = Mathf.Lerp(0.18f, 0.42f, 1f - depth);
                line.SetPosition(0, new Vector3(x, y, 0f));
                line.SetPosition(1, new Vector3(x - length, y + 0.015f, 0f));
            }
        }

        private void OnDisable()
        {
            foreach (LineRenderer line in wakeLines) if (line != null) line.enabled = false;
            foreach (LineRenderer line in waterLines) if (line != null) line.enabled = false;
            fleet.Clear();
        }
    }
}
