using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace ProjectSS.Expedition
{
    /// <summary>Pass sprite-local UVs separately from packed texture UVs to UI masks.</summary>
    [ExecuteAlways, RequireComponent(typeof(Image))]
    public sealed class AtlasLocalUV : BaseMeshEffect
    {
        protected override void OnEnable() { base.OnEnable(); EnableChannel(); }
        protected override void OnCanvasHierarchyChanged() { base.OnCanvasHierarchyChanged(); EnableChannel(); }
        void EnableChannel()
        {
            if (graphic != null && graphic.canvas != null)
                graphic.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }
        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive()) return;
            var image = (Image)graphic;
            var sprite = image.overrideSprite != null ? image.overrideSprite : image.sprite;
            var uv = sprite != null ? DataUtility.GetOuterUV(sprite) : new Vector4(0, 0, 1, 1);
            float width = Mathf.Max(.000001f, uv.z - uv.x), height = Mathf.Max(.000001f, uv.w - uv.y);
            var vertex = new UIVertex();
            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);
                vertex.uv1 = new Vector4((vertex.uv0.x - uv.x) / width, (vertex.uv0.y - uv.y) / height, 0, 0);
                helper.SetUIVertex(vertex, i);
            }
        }
    }
}
