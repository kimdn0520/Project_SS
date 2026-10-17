using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DividerShineGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            for(int i=0;i<3;i++)
            {
                var c=color;c.a*=i==1?1f:0f;float x=Mathf.Lerp(r.xMin,r.xMax,i*.5f);
                mesh.AddVert(new Vector3(x,r.yMin),c,Vector2.zero);mesh.AddVert(new Vector3(x,r.yMax),c,Vector2.up);
            }
            for(int i=0;i<2;i++){int k=i*2;mesh.AddTriangle(k,k+1,k+2);mesh.AddTriangle(k+2,k+1,k+3);}
        }
    }
}
