using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ButtonImpactRing : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;var center=r.center;const int steps=64;const float thickness=2.2f;
            for(int i=0;i<=steps;i++)
            {
                float angle=i*Mathf.PI*2/steps;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                mesh.AddVert(center+Vector2.Scale(direction,r.size*.5f),color,Vector2.zero);
                mesh.AddVert(center+Vector2.Scale(direction,r.size*.5f-Vector2.one*thickness),color,Vector2.zero);
                if(i>0){int k=(i-1)*2;mesh.AddTriangle(k,k+2,k+1);mesh.AddTriangle(k+1,k+2,k+3);}
            }
        }
    }
}
