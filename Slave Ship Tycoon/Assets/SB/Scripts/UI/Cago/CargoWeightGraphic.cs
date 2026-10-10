using UnityEngine;

namespace SB.Scripts
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CargoWeightGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            AddQuad(mesh, rect, new Vector2(.12f, .06f), new Vector2(.3f, .7f),
                new Vector2(.7f, .7f), new Vector2(.88f, .06f));
            AddQuad(mesh, rect, new Vector2(.35f, .68f), new Vector2(.35f, .94f),
                new Vector2(.44f, .94f), new Vector2(.44f, .68f));
            AddQuad(mesh, rect, new Vector2(.56f, .68f), new Vector2(.56f, .94f),
                new Vector2(.65f, .94f), new Vector2(.65f, .68f));
            AddQuad(mesh, rect, new Vector2(.35f, .87f), new Vector2(.35f, .96f),
                new Vector2(.65f, .96f), new Vector2(.65f, .87f));
        }

        private void AddQuad(UnityEngine.UI.VertexHelper mesh, Rect rect, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int start = mesh.currentVertCount;
            foreach (Vector2 point in new[] { a, b, c, d })
                mesh.AddVert(new Vector3(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
