using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // Authored pixel geometry; the exported gameplay sprites remain untouched.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RuneLobbyScenery : MaskableGraphic
    {
        public bool punchStyle;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(punchStyle)
            {
                Color top=RuneArtLibrary.Hex("17182f"),bottom=RuneArtLibrary.Hex("101120");int start=vh.currentVertCount;
                var bounds=rectTransform.rect;
                vh.AddVert(new Vector3(bounds.xMin,bounds.yMax),top,Vector2.zero);vh.AddVert(new Vector3(bounds.xMax,bounds.yMax),top,Vector2.zero);vh.AddVert(new Vector3(bounds.xMax,bounds.yMin),bottom,Vector2.zero);vh.AddVert(new Vector3(bounds.xMin,bounds.yMin),bottom,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
                Glow(vh,bounds.center+new Vector2(0,bounds.height*.094f),bounds.width*.56f,bounds.height*.42f,RuneArtLibrary.Hex("36306c"));
                for(int i=0;i<24;i++){float x=24+(i*137)%492,y=115+(i*97)%670;var c=RuneArtLibrary.Hex(i%3==0?"a9a2e8":"474570");RuneUiMesh.Quad(vh,bounds.xMin+x/540*bounds.width,bounds.yMax-y/960*bounds.height,i%3==0?2:1,i%3==0?2:1,c);}
                return;
            }
            Box(vh,0,0,540,960,"17282f");
            for(int i=0;i<18;i++){float x=108+(i*83)%326,y=236+(i*67)%470;Box(vh,x,y,2,2,"3a5553");}
            Box(vh,139,264,2,10,"5e7964");Box(vh,135,268,10,2,"5e7964");
            Box(vh,398,664,2,10,"5e7964");Box(vh,394,668,10,2,"5e7964");
        }
        private static void Glow(VertexHelper vh,Vector2 center,float rx,float ry,Color color)
        {
            int start=vh.currentVertCount;vh.AddVert(center,color,Vector2.zero);color.a=0;
            for(int i=0;i<48;i++){float a=i*Mathf.PI/24;vh.AddVert(center+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),color,Vector2.zero);}
            for(int i=0;i<48;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%48);
        }
        private static void Box(VertexHelper vh,float x,float y,float w,float h,string color)=>RuneUiMesh.Quad(vh,x,-y,w,h,RuneArtLibrary.Hex(color));
    }
}
