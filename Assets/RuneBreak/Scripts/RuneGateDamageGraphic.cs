using UnityEngine;
using UnityEngine.UI;
namespace RuneBreak
{
    // Fixed crack data on the authored gate face. No particle objects or runtime UI creation.
    public sealed class RuneGateDamageGraphic : MaskableGraphic
    {
        public bool vertical;
        private readonly Vector2[] hits=new Vector2[28];
        private readonly float[] ages=new float[28];
        private int count;
        public int HitCount=>count;
        public void Clear(){count=0;SetVerticesDirty();}
        public void Hit(float along)
        {if(count==hits.Length)return;hits[count]=new Vector2(Mathf.Clamp(along,10,326),vertical?3:25);ages[count++]=0;SetVerticesDirty();}
        public void Tick(float dt){bool changed=false;for(int i=0;i<count;i++)if(ages[i]<.22f){ages[i]+=dt;changed=true;}if(changed)SetVerticesDirty();}
        private Vector2 Point(Vector2 p){p.x=Mathf.Clamp(p.x,2,334);p.y=Mathf.Clamp(p.y,2,26);return vertical?new Vector2(p.y,-p.x):new Vector2(p.x,-p.y);}
        private static float Noise(int seed){uint n=(uint)seed*747796405u+2891336453u;n=((n>>((int)(n>>28)+4))^n)*277803737u;return ((n>>22)^n)%1000/999f;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();for(int hit=0;hit<count;hit++)
            {
                float reveal=Mathf.Clamp01(ages[hit]/.18f);var start=hits[hit];float inward=vertical?1:-1;
                for(int branch=0;branch<3;branch++)
                {
                    var prev=start;float spread=(branch-1)*(19+Noise(hit*17+branch)*16);
                    for(int n=1;n<=6;n++)
                    {
                        float u=n/6f;if(u>reveal+.12f)break;
                        var next=start+new Vector2(spread*u+(Noise(hit*137+branch*31+n)-.5f)*6,inward*(22*u));
                        float width=(1.35f-.65f*u)*(1+Mathf.Min(count,5)*.045f);
                        Segment(vh,Point(prev)+new Vector2(.65f,-.55f),Point(next)+new Vector2(.65f,-.55f),width+.45f,new Color(1,.95f,.78f,.65f));
                        Segment(vh,Point(prev),Point(next),width,new Color(.10f,.075f,.10f,.86f));
                        if(n==3||n==5){var fork=next+new Vector2((branch==0?-1:1)*(5+Noise(n+hit)*7),-inward*4);Segment(vh,Point(next),Point(fork),.65f,new Color(.12f,.07f,.10f,.67f));}
                        prev=next;
                    }
                }
            }
        }
        private static void Segment(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;int i=vh.currentVertCount;vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    }
}
