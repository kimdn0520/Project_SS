using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // One prefab-authored graphic. At most 28 tethers and one 192-segment guide;
    // no hierarchy creation or per-particle objects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RuneMechanicGraphic : MaskableGraphic
    {
        public bool surfaceStyle;
        private RuneRules rules;
        private RuneState state,visual;
        private float time;
        private int focus=-1;
        public void Render(RuneRules rules,RuneState state,RuneState visual,float time,int focus)
        {this.rules=rules;this.state=state;this.visual=visual;this.time=time;this.focus=focus;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(rules==null||state==null)return;
            for(int i=0;i<rules.Level.blocks.Length;i++)
            {
                if(!rules.IsVisible(state,i)||!rules.IsChained(visual,i))continue;
                var b=rules.Level.blocks[i];var p=RuneFlight.Center(b,surfaceStyle);int side=rules.ChainSides[i];
                var color=RuneArtLibrary.Mid(rules.Level.Layers(side)[b.chainLayer].color);color.a=.42f+.12f*Mathf.Sin(time*3+i);
                var anchor=side==0?new Vector2(p.x,surfaceStyle?88:73):new Vector2(surfaceStyle?RuneFlight.SurfaceEastGateLeft:393,p.y);
                Tether(vh,anchor,p,color);
                Pixel(vh,anchor.x-3,anchor.y-3,6,6,RuneArtLibrary.Light(rules.Level.Layers(side)[b.chainLayer].color));
            }
            // Keep actual chain links, but the shipped Surface UI has no trajectory guides.
            if(surfaceStyle)return;
            int id=focus;
            if(id<0||id>=rules.Routes.Length||!rules.IsVisible(state,id)||rules.Routes[id].turns.Length==0)
            {
                id=-1;for(int i=0;i<rules.Level.blocks.Length;i++)if(rules.IsFree(state,i)&&rules.Routes[i].turns.Length>0){id=i;break;}
            }
            if(id<0)return;
            var route=rules.Routes[id];if(route.loop)return;
            var tint=RuneArtLibrary.Light(rules.Level.blocks[id].color);tint.a=.55f;
            Vector2 start=RuneFlight.Center(rules.Level.blocks[id],surfaceStyle);
            for(int i=0;i<=route.turns.Length;i++)
            {
                Vector2 end=i<route.turns.Length?RuneFlight.Turn(route.turns[i],surfaceStyle):RuneFlight.RouteExit(route,surfaceStyle);
                bool blocked=false;float shortest=Vector2.Distance(start,end);
                for(int j=0;j<rules.Level.blocks.Length;j++)
                {
                    if((state.board&rules.Blockers[id]&(1u<<j))==0)continue;
                    var p=RuneFlight.Center(rules.Level.blocks[j],surfaceStyle);
                    bool onLine=Mathf.Abs(start.x-end.x)<.1f?Mathf.Abs(p.x-start.x)<.1f:Mathf.Abs(p.y-start.y)<.1f;
                    float d=Vector2.Distance(start,p);
                    if(onLine&&Vector2.Dot(p-start,end-start)>0&&d<=shortest){shortest=d;end=p;blocked=true;}
                }
                Guide(vh,start,end,tint);if(blocked)break;start=end;
            }
        }
        private static void Tether(VertexHelper vh,Vector2 a,Vector2 b,Color c)
        {
            float length=Vector2.Distance(a,b);var d=(b-a).normalized;
            for(float t=6;t<length-15;t+=10)
            {
                var p=a+d*t;bool horizontal=Mathf.Abs(d.x)>.5f;
                Pixel(vh,p.x-2,p.y-2,horizontal?7:4,horizontal?4:7,c);
                var hollow=RuneArtLibrary.Hex("17282f");hollow.a=.8f;Pixel(vh,p.x-1,p.y-1,horizontal?5:2,horizontal?2:5,hollow);
            }
        }
        private void Guide(VertexHelper vh,Vector2 a,Vector2 b,Color c)
        {
            float length=Vector2.Distance(a,b);var d=(b-a).normalized;
            for(float t=(time*22)%12;t<length;t+=12){var p=a+d*t;Pixel(vh,p.x-1,p.y-1,3,3,c);}
            if(length<26)return;var end=b-d*12;var n=new Vector2(-d.y,d.x);
            for(int i=0;i<5;i++){var p=end-d*i+n*i;Pixel(vh,p.x,p.y,2,2,c);p=end-d*i-n*i;Pixel(vh,p.x,p.y,2,2,c);}
        }
        private static void Pixel(VertexHelper vh,float x,float y,float w,float h,Color c)=>RuneUiMesh.Quad(vh,Mathf.Round(x),-Mathf.Round(y),w,h,c);
    }
}
