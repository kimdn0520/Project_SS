using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    public sealed class RuneFlight : PoolObject
    {
        public const float SurfaceStoredSize=40;
        public const float SurfaceStoredSpriteSize=SurfaceStoredSize*1.4f;
        public const float SurfaceStorageFlightOffset=-SurfaceStoredSize/20;
        public const float SurfaceEastGateLeft=412,SurfaceEastGateCenter=426;
        public RectTransform body;
        public Image image;
        public RectTransform[] trails;
        public Image[] trailImages;
        public Image shadow;
        public Image[] afterImages;
        public RuneRibbonGraphic ribbon;
        public string ReboundDirection=>reboundMotion.dir;
        public RuneShot shot;
        public Vector2 from,to,c1,c2;
        public float start,duration;
        public string color,dir;
        private readonly Vector2[] route=new Vector2[192];
        private readonly Vector2[] corners=new Vector2[160];
        private readonly float[] mirrorDistances=new float[144];
        private readonly Vector2[] mirrorPoints=new Vector2[144];
        private int mirrorCount,nextMirror;
        private float routeLength;
        private bool reflected;
        private RuneArtLibrary art;
        private int routeCount;
        private bool rebound,bounced,swooshed;
        private RuneReboundMotion reboundMotion;
        private float reboundOffset;
        public bool HasRebound=>rebound;
        public float ReboundContactTime=>start+reboundOffset+reboundMotion.launch;
        public bool TakeReboundImpact(float time,out Vector2 point)
        {
            point=reboundMotion.surface;if(!rebound||bounced||time<ReboundContactTime)return false;bounced=true;return true;
        }
        public bool TakeReboundRelease(float time){if(!rebound||swooshed||time<start+reboundOffset+reboundMotion.Release)return false;swooshed=true;return true;}
        public static Vector2 Center(RuneBlock b,bool surface=false)=>surface?new Vector2(100+b.x*56,128+b.y*56):new Vector2(120+b.x*48,108+b.y*48);
        public static Vector2 Exit(RuneBlock b,bool surface=false){var p=Center(b,surface);return Edge(p,b.dir,surface);}
        private static Vector2 Edge(Vector2 p,string dir,bool surface)=>dir=="N"?new Vector2(p.x,surface?74:59):dir=="S"?new Vector2(p.x,surface?462:401):dir=="W"?new Vector2(surface?46:69,p.y):new Vector2(surface?SurfaceEastGateCenter:411,p.y);
        public static Vector2 Slot(int index,bool surface=false)=>surface?new Vector2(108+index*66,580):new Vector2(96+index*72,507);
        public static Vector2 Turn(RuneCell cell,bool surface=false)=>surface?new Vector2(100+cell.x*56,128+cell.y*56):new Vector2(120+cell.x*48,108+cell.y*48);
        public static Vector2 RouteExit(RuneRoute path,bool surface=false)=>Edge(surface?new Vector2(100+path.exitX*56,128+path.exitY*56):new Vector2(120+path.exitX*48,108+path.exitY*48),path.exitDir,surface);
        public void Begin(RuneArtLibrary art,RuneBlock b,RuneShot shot,float time,bool reduced,RuneRoute path=null,bool emptyBoundary=false)
        {
            this.art=art;this.shot=shot;start=time;color=b.color;dir=b.dir;from=Center(b,art.surfaceStyle);to=path==null?Exit(b,art.surfaceStyle):RouteExit(path,art.surfaceStyle);
            reflected=!shot.relaunch&&path!=null&&path.turns.Length>0;mirrorCount=nextMirror=0;routeCount=0;routeLength=0;
            rebound=art.punchStyle&&!shot.relaunch&&(shot.store||shot.overflow);bounced=swooshed=false;reboundOffset=0;
            body.localRotation=Quaternion.identity;body.localScale=Vector3.one;
            image.sprite=art.Rune(b.color,shot.relaunch?"gem":b.dir);image.color=Color.white;
            if(shot.relaunch)
            {
                from=Slot(shot.slot,art.surfaceStyle);float p=(art.surfaceStyle?72:96)+(shot.damageIndex+.5f)*(art.surfaceStyle?336:288)/shot.max;to=shot.side==0?new Vector2(p,art.surfaceStyle?74:59):new Vector2(art.surfaceStyle?SurfaceEastGateCenter:411,art.surfaceStyle?p+28:p);
                if(art.surfaceStyle)from.y+=SurfaceStorageFlightOffset;
                float bend=shot.side==1?70:from.x<240?-40:40;c1=from+new Vector2(bend,-130);c2=to+new Vector2(shot.side==1?-60:0,65);
                duration=reduced?.19f:.24f+Vector2.Distance(from,to)/2300;
            }
            else if(rebound)
            {
                var edge=to;to=shot.store?Slot(shot.slot,art.surfaceStyle)+new Vector2(0,art.surfaceStyle?SurfaceStorageFlightOffset:-5):new Vector2(240,art.surfaceStyle?580:510);
                string exit=path==null?b.dir:path.exitDir;var approach=from;
                if(art.surfaceStyle&&emptyBoundary)edge=exit=="N"?new Vector2(edge.x,45):exit=="E"?new Vector2(451,edge.y):exit=="S"?new Vector2(edge.x,486):new Vector2(29,edge.y);
                if(reflected)
                {
                    route[routeCount++]=from;
                    foreach(var cell in path.turns){var point=Turn(cell,art.surfaceStyle);routeLength+=Vector2.Distance(route[routeCount-1],point);route[routeCount++]=point;mirrorPoints[mirrorCount]=point;mirrorDistances[mirrorCount++]=routeLength;}
                    approach=route[routeCount-1];reboundOffset=(reduced?.04f:.07f)+routeLength/(reduced?2400:1050);
                }
                reboundMotion=art.surfaceStyle?RuneReboundMotion.CreateSurface(approach,edge,to,exit,reduced):RuneReboundMotion.Create(approach,edge,to,exit,reduced);duration=reboundOffset+reboundMotion.Duration;
            }
            else if(shot.store||shot.overflow)
            {
                var edge=to;to=shot.store?Slot(shot.slot):new Vector2(240,510);
                string exit=path==null?b.dir:path.exitDir;
                float outside=exit=="W"?18:exit=="E"?462:edge.x<240?18:462;int n=0;corners[n++]=from;
                if(reflected)foreach(var cell in path.turns)corners[n++]=Turn(cell,art.surfaceStyle);
                corners[n++]=edge;
                if(exit=="N"){corners[n++]=new Vector2(edge.x,30);corners[n++]=new Vector2(outside,30);corners[n++]=new Vector2(outside,432);}
                else if(exit=="S")corners[n++]=new Vector2(edge.x,436);
                else {corners[n++]=new Vector2(outside,edge.y);corners[n++]=new Vector2(outside,432);}
                corners[n++]=to;if(reflected){routeCount=n;System.Array.Copy(corners,route,n);}else Smooth(n);duration=reduced?.22f:exit=="N"?.73f:exit=="S"?.45f:.56f;
            }
            else duration=reduced?.16f:.19f+Vector2.Distance(from,to)/1450;
            if(reflected&&!rebound)
            {
                if(!shot.store&&!shot.overflow){routeCount=0;route[routeCount++]=from;foreach(var cell in path.turns)route[routeCount++]=Turn(cell,art.surfaceStyle);route[routeCount++]=to;}
                for(int i=1;i<routeCount;i++){routeLength+=Vector2.Distance(route[i-1],route[i]);if(i<=path.turns.Length){mirrorPoints[mirrorCount]=route[i];mirrorDistances[mirrorCount++]=routeLength;}}
                duration=reduced?.25f:.16f+routeLength/780;
            }
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
            if(rebound)
            {
                float elapsed=time-start;
                if(elapsed>=reboundOffset)return reboundMotion.Sample(elapsed-reboundOffset);
                float travel=Mathf.Clamp01(elapsed/reboundOffset)*routeLength;
                for(int i=1;i<routeCount;i++){float length=Vector2.Distance(route[i-1],route[i]);if(travel<=length||i==routeCount-1)return Vector2.Lerp(route[i-1],route[i],length==0?1:travel/length);travel-=length;}
                return from;
            }
            if(reflected)
            {
                float reflectedTravel=ReflectedDistance(time);
                for(int i=1;i<routeCount;i++){float length=Vector2.Distance(route[i-1],route[i]);if(reflectedTravel<=length||i==routeCount-1)return Vector2.Lerp(route[i-1],route[i],length==0?1:reflectedTravel/length);reflectedTravel-=length;}
                return to;
            }
            if(shot.relaunch){p=1-Mathf.Pow(1-p,1.3f);return Mathf.Pow(1-p,3)*from+3*(1-p)*(1-p)*p*c1+3*(1-p)*p*p*c2+p*p*p*to;}
            if(!shot.store&&!shot.overflow){if(p<.13f)return from-(to-from)*.012f*Mathf.Sin(p/.13f*Mathf.PI);p=(p-.13f)/.87f;return Vector2.LerpUnclamped(from,to,p*p);}
            p=p<.06f?0:(p-.06f)/.94f;float total=0;for(int i=1;i<routeCount;i++)total+=Vector2.Distance(route[i-1],route[i]);float distance=p*total;
            for(int i=1;i<routeCount;i++){float length=Vector2.Distance(route[i-1],route[i]);if(distance<=length||i==routeCount-1)return Vector2.Lerp(route[i-1],route[i],length==0?1:distance/length);distance-=length;}return to;
        }
        private float ReflectedDistance(float time)=>rebound?Mathf.Clamp01((time-start)/reboundOffset)*routeLength:Mathf.Clamp01(((time-start)/duration-.1f)/.9f)*routeLength;
        public bool TakeMirrorImpact(float time,out Vector2 point)
        {
            point=default;if(!reflected||nextMirror>=mirrorCount||ReflectedDistance(time)<mirrorDistances[nextMirror])return false;
            point=mirrorPoints[nextMirror++];return true;
        }
        public void Render(float time,bool reduced)
        {
            var p=Position(time);body.anchoredPosition=new Vector2(Mathf.Round(p.x),-Mathf.Round(p.y));
            if(ribbon!=null)ribbon.Render(this,time,start+reboundOffset+reboundMotion.Release,art.MidTone(color),art.LightTone(color),reduced);
            if(shadow!=null)shadow.enabled=false;
            if(afterImages!=null)foreach(var ghost in afterImages)ghost.enabled=false;
            if(rebound&&time>=start+reboundOffset)
            {
                float age=time-start-reboundOffset;var pose=reboundMotion.GetPose(age);
                if(art.surfaceStyle){float compression=1-(reboundMotion.direction.x==0?pose.sy:pose.sx);if(compression>0)p+=reboundMotion.direction*compression*18;}
                // Finish at the same size and texture center as the authored storage block.
                if(art.surfaceStyle&&shot.store)pose.size=Mathf.Lerp(pose.size,SurfaceStoredSize,Mathf.SmoothStep(0,1,pose.hop));
                image.sprite=art.Rune(color,reboundMotion.dir);image.color=Color.Lerp(Color.white,new Color(1,.96f,.82f),pose.flash);
                body.anchoredPosition=new Vector2(p.x,-p.y-pose.size/20);body.sizeDelta=Vector2.one*(pose.size*(art.surfaceStyle?1.4f:1.2f));body.localScale=new Vector3(pose.sx,pose.sy,1);body.localRotation=Quaternion.Euler(0,0,-pose.angle*Mathf.Rad2Deg);
                if(shadow!=null){shadow.enabled=pose.airborne&&!reduced;shadow.rectTransform.anchoredPosition=new Vector2(p.x,-p.y-17-pose.lift);shadow.rectTransform.sizeDelta=new Vector2(Mathf.Max(10,28-pose.lift*.4f),8);}
                if(afterImages!=null)for(int i=0;i<afterImages.Length;i++)
                {
                    var ghost=afterImages[i];ghost.enabled=!reduced&&pose.airborne&&pose.hop<.8f;if(!ghost.enabled)continue;
                    float tm=Mathf.Max(reboundMotion.Release,age-(i+1)*.016f);var q=reboundMotion.Sample(tm);var pr=reboundMotion.GetPose(tm);
                    ghost.sprite=image.sprite;ghost.color=new Color(1,1,1,.12f/(i+1));var rt=ghost.rectTransform;rt.anchoredPosition=new Vector2(q.x,-q.y);rt.sizeDelta=Vector2.one*(pr.size*(art.surfaceStyle?1.4f:1.2f));rt.localScale=new Vector3(pr.sx,pr.sy,1);rt.localRotation=Quaternion.Euler(0,0,-pr.angle*Mathf.Rad2Deg);
                }
                for(int i=0;i<trails.Length;i++)trailImages[i].enabled=false;
                return;
            }
            if(reflected&&time>=start)
            {
                var delta=Position(time+.002f)-p;string facing=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?(delta.x>0?"E":"W"):(delta.y>0?"S":"N");
                if(delta.sqrMagnitude>.001f)image.sprite=art.Rune(color,facing);
            }
            float t=Mathf.Clamp01((time-start)/duration),size=shot.relaunch?25:shot.store||shot.overflow?Mathf.Max(24,36-12*t):36;
            if(art.surfaceStyle&&shot.relaunch){float launch=Mathf.Clamp01(t/.35f);size=Mathf.Lerp(SurfaceStoredSize,25,1-(1-launch)*(1-launch));}
            if(!shot.relaunch&&!shot.store&&!shot.overflow&&t<.13f)size*=.94f;body.sizeDelta=Vector2.one*(size*(art.surfaceStyle?1.4f:1.2f));body.anchoredPosition=new Vector2(Mathf.Floor(p.x+.5f),-Mathf.Floor(p.y+.5f)-size/20);
            for(int i=0;i<trails.Length;i++)
            {
                trailImages[i].enabled=!reduced;if(reduced)continue;int j=i+1;var q=Position(time-j*.014f);trails[i].anchoredPosition=new Vector2(Mathf.Round(q.x),-Mathf.Round(q.y));trails[i].sizeDelta=Vector2.one*((9-j)*2);var c=RuneArtLibrary.Mid(color);c.a=(1-j/7f)*.45f;trailImages[i].color=c;
            }
        }
    }
}
