using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // Authored pixel geometry; the exported gameplay sprites remain untouched.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RuneLobbyScenery : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Box(vh,0,0,540,960,"17282f");
            for(int i=0;i<18;i++){float x=108+(i*83)%326,y=236+(i*67)%470;Box(vh,x,y,2,2,"3a5553");}
            Box(vh,139,264,2,10,"5e7964");Box(vh,135,268,10,2,"5e7964");
            Box(vh,398,664,2,10,"5e7964");Box(vh,394,668,10,2,"5e7964");
        }
        private static void Box(VertexHelper vh,float x,float y,float w,float h,string color)=>RuneUiMesh.Quad(vh,x,-y,w,h,RuneArtLibrary.Hex(color));
    }
}
