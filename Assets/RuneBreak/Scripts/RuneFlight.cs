using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    public sealed class RuneFlight : PoolObject
    {
        public RectTransform body;
        public Image image;
        public RectTransform[] trails;
        public Image[] trailImages;
        public RuneShot shot;
        public Vector2 from,to,c1,c2;
        public float start,duration;
        public string color,dir;
        private readonly Vector2[] route=new Vector2[64];
        private readonly Vector2[] corners=new Vector2[8];
        private int routeCount;
        public static Vector2 Center(RuneBlock b)=>new Vector2(120+b.x*48,108+b.y*48);
        public static Vector2 Exit(RuneBlock b){var p=Center(b);return b.dir=="N"?new Vector2(p.x,59):b.dir=="S"?new Vector2(p.x,401):b.dir=="W"?new Vector2(69,p.y):new Vector2(411,p.y);}
        public static Vector2 Slot(int index)=>new Vector2(96+index*72,507);
        public void Begin(RuneArtLibrary art,RuneBlock b,RuneShot shot,float time,bool reduced)
        {
            this.shot=shot;start=time;color=b.color;dir=b.dir;from=Center(b);to=Exit(b);
            image.sprite=art.Rune(b.color,shot.relaunch?"gem":b.dir);image.color=Color.white;
            if(shot.relaunch)
            {
                from=Slot(shot.slot);float p=96+(shot.damageIndex+.5f)*288/shot.max;to=shot.side==0?new Vector2(p,59):new Vector2(411,p);
                float bend=shot.side==1?70:from.x<240?-40:40;c1=from+new Vector2(bend,-130);c2=to+new Vector2(shot.side==1?-60:0,65);
                duration=reduced?.19f:.24f+Vector2.Distance(from,to)/2300;
            }
            else if(shot.store||shot.overflow)
            {
                var edge=to;to=shot.store?Slot(shot.slot):new Vector2(240,510);
                float outside=b.dir=="W"?18:b.dir=="E"?462:from.x<240?18:462;int n=0;corners[n++]=from;corners[n++]=edge;
                if(b.dir=="N"){corners[n++]=new Vector2(edge.x,30);corners[n++]=new Vector2(outside,30);corners[n++]=new Vector2(outside,432);}
                else if(b.dir=="S")corners[n++]=new Vector2(edge.x,436);
                else {corners[n++]=new Vector2(outside,edge.y);corners[n++]=new Vector2(outside,432);}
                corners[n++]=to;Smooth(n);duration=reduced?.22f:b.dir=="N"?.73f:b.dir=="S"?.45f:.56f;
            }
            else duration=reduced?.16f:.19f+Vector2.Distance(from,to)/1450;
            for(int i=0;i<trails.Length;i++){trailImages[i].enabled=!reduced;trailImages[i].color=RuneArtLibrary.Mid(color);}
            Render(time,reduced);
        }
        private void Smooth(int n)
        {
            routeCount=0;route[routeCount++]=corners[0];for(int i=1;i<n-1;i++)
            {
                var a=corners[i-1];var b=corners[i];var d=corners[i+1];float la=Vector2.Distance(a,b),lb=Vector2.Distance(b,d);float r=Mathf.Min(20,la*.3f,lb*.3f);
                var q=la>0?b+(a-b)*r/la:b;var e=lb>0?b+(d-b)*r/lb:b;route[routeCount++]=q;
                for(int j=1;j<=8;j++){float t=j/8f;route[routeCount++]=(1-t)*(1-t)*q+2*(1-t)*t*b+t*t*e;}
            }route[routeCount++]=corners[n-1];
        }
        public Vector2 Position(float time)
        {
            float p=Mathf.Clamp01((time-start)/duration);
            if(shot.relaunch){p=1-Mathf.Pow(1-p,1.3f);return Mathf.Pow(1-p,3)*from+3*(1-p)*(1-p)*p*c1+3*(1-p)*p*p*c2+p*p*p*to;}
            if(!shot.store&&!shot.overflow){if(p<.13f)return from-(to-from)*.012f*Mathf.Sin(p/.13f*Mathf.PI);p=(p-.13f)/.87f;return Vector2.LerpUnclamped(from,to,p*p);}
            p=p<.06f?0:(p-.06f)/.94f;float total=0;for(int i=1;i<routeCount;i++)total+=Vector2.Distance(route[i-1],route[i]);float distance=p*total;
            for(int i=1;i<routeCount;i++){float length=Vector2.Distance(route[i-1],route[i]);if(distance<=length||i==routeCount-1)return Vector2.Lerp(route[i-1],route[i],length==0?1:distance/length);distance-=length;}return to;
        }
        public void Render(float time,bool reduced)
        {
            var p=Position(time);body.anchoredPosition=new Vector2(Mathf.Round(p.x),-Mathf.Round(p.y));
            float t=(time-start)/duration,size=shot.relaunch?25:shot.store||shot.overflow?Mathf.Max(24,36-12*t):36;
            if(!shot.relaunch&&!shot.store&&!shot.overflow&&t<.13f)size*=.94f;body.sizeDelta=Vector2.one*(size*1.2f);body.anchoredPosition=new Vector2(Mathf.Floor(p.x+.5f),-Mathf.Floor(p.y+.5f)-size/20);
            for(int i=0;i<trails.Length;i++)
            {
                trailImages[i].enabled=!reduced;if(reduced)continue;int j=i+1;var q=Position(time-j*.014f);trails[i].anchoredPosition=new Vector2(Mathf.Round(q.x),-Mathf.Round(q.y));trails[i].sizeDelta=Vector2.one*((9-j)*2);var c=RuneArtLibrary.Mid(color);c.a=(1-j/7f)*.45f;trailImages[i].color=c;
            }
        }
    }
}
