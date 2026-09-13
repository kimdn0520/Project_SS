using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition
{
    /// <summary>Projects the existing icon onto the shallow dome of the button.</summary>
    public sealed class CurvedDecalImage : Image
    {
        [SerializeField, Range(0, 12)] float crownRise = 7;
        [SerializeField, Range(0, .3f)] float farEdgeCompression = .12f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            base.OnPopulateMesh(mesh);
            if (type != Type.Simple || mesh.currentVertCount != 4) return;
            var bottomLeft = new UIVertex(); var topLeft = new UIVertex();
            var topRight = new UIVertex(); var bottomRight = new UIVertex();
            mesh.PopulateUIVertex(ref bottomLeft, 0); mesh.PopulateUIVertex(ref topLeft, 1);
            mesh.PopulateUIVertex(ref topRight, 2); mesh.PopulateUIVertex(ref bottomRight, 3);
            float centerX = (bottomLeft.position.x + bottomRight.position.x) * .5f;
            mesh.Clear();
            const int columns = 12, rows = 6;
            for (int y = 0; y <= rows; y++)
            {
                float v = y / (float)rows;
                for (int x = 0; x <= columns; x++)
                {
                    float u = x / (float)columns;
                    var vertex = bottomLeft;
                    vertex.position = Vector3.Lerp(Vector3.Lerp(bottomLeft.position, bottomRight.position, u), Vector3.Lerp(topLeft.position, topRight.position, u), v);
                    vertex.uv0 = Vector4.Lerp(Vector4.Lerp(bottomLeft.uv0, bottomRight.uv0, u), Vector4.Lerp(topLeft.uv0, topRight.uv0, u), v);
                    vertex.position.x = centerX + (vertex.position.x - centerX) * (1 - farEdgeCompression * v);
                    float across = u * 2 - 1;
                    vertex.position.y += crownRise * (1 - across * across);
                    mesh.AddVert(vertex);
                    if (x == 0 || y == 0) continue;
                    int index = y * (columns + 1) + x;
                    mesh.AddTriangle(index - columns - 2, index - 1, index);
                    mesh.AddTriangle(index - columns - 2, index, index - columns - 1);
                }
            }
        }
    }
}
