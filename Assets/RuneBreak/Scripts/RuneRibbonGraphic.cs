using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // Prefab-authored, fixed 13-point ribbon. No objects or buffers are created in flight.
    public sealed class RuneRibbonGraphic : MaskableGraphic
    {
        private readonly Vector2[] points=new Vector2[13];
        private Color mid,light;
        private float fade;
        private bool visible,speedLines;
        public void Render(RuneFlight flight,float time,float release,Color mid,Color light,bool reduced)
        {
            this.mid=mid;this.light=light;visible=!reduced&&flight.HasRebound&&time>=release;
            fade=Mathf.Min(1,(flight.start+flight.duration-time)/.045f);speedLines=time<flight.start+flight.duration-.035f;
            if(visible)for(int i=0;i<13;i++){var p=flight.Position(Mathf.Max(release,time-.075f*(12-i)/12));points[i]=new Vector2(p.x,-p.y);}
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!visible||Vector2.Distance(points[0],points[12])<2)return;
            for(int pass=0;pass<2;pass++)for(int i=1;i<13;i++)
            {
                float q=i/12f;var c=pass==0?mid:light;c.a=(pass==0?.22f:.40f)*q*fade;
                RuneUiMesh.Line(vh,points[i-1],points[i],pass==0?2+q*15:1+q*3,c);
            }
            if(speedLines)
            {
                var d=(points[12]-points[9]).normalized;var n=new Vector2(-d.y,d.x);float length=Mathf.Min(64,Vector2.Distance(points[0],points[12])*.7f);var c=RuneArtLibrary.Hex("fff4d9");c.a=.45f;
                for(int sign=-1;sign<=1;sign+=2)RuneUiMesh.Line(vh,points[12]-d*14+n*15*sign,points[12]-d*(14+length)+n*11.25f*sign,1.2f,c);
            }
        }
    }
}
