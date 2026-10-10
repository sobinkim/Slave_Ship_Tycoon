using UnityEngine;

namespace SB.Scripts
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CargoSlotBorderGraphic : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField, Min(1f)] private float dashLength = 8f;
        [SerializeField, Min(1f)] private float dashGap = 6f;
        [SerializeField, Min(0.5f)] private float thickness = 1.5f;

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            for (float x = rect.xMin; x < rect.xMax; x += dashLength + dashGap)
            {
                float width = Mathf.Min(dashLength, rect.xMax - x);
                AddQuad(mesh, new Rect(x, rect.yMin, width, thickness));
                AddQuad(mesh, new Rect(x, rect.yMax - thickness, width, thickness));
            }
            for (float y = rect.yMin; y < rect.yMax; y += dashLength + dashGap)
            {
                float height = Mathf.Min(dashLength, rect.yMax - y);
                AddQuad(mesh, new Rect(rect.xMin, y, thickness, height));
                AddQuad(mesh, new Rect(rect.xMax - thickness, y, thickness, height));
            }
        }

        private void AddQuad(UnityEngine.UI.VertexHelper mesh, Rect rect)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
