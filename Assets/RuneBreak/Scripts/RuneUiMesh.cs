using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // Geometry shared by particles and decorative icons. Text is authored with TMP fonts.
    public static class RuneUiMesh
    {
        public static void Quad(VertexHelper vh,float x,float y,float w,float h,Color color)
        {
            int i=vh.currentVertCount;vh.AddVert(new Vector3(x,y),color,Vector2.zero);vh.AddVert(new Vector3(x+w,y),color,Vector2.zero);vh.AddVert(new Vector3(x+w,y-h),color,Vector2.zero);vh.AddVert(new Vector3(x,y-h),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
