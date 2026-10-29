using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RuneBreak
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed partial class RuneFxGraphic : MaskableGraphic
    {
        public RuneArtLibrary art;
        private Color Light(string color)=>art!=null?art.LightTone(color):RuneArtLibrary.Light(color);
        private Color Mid(string color)=>art!=null?art.MidTone(color):RuneArtLibrary.Mid(color);
        private Color Dark(string color)=>art!=null?art.DarkTone(color):RuneArtLibrary.Dark(color);
        public TMP_Text[] captionLabels=System.Array.Empty<TMP_Text>();
        public TMP_Text[] captionShadows=System.Array.Empty<TMP_Text>();
        public RectTransform[] captionRects=System.Array.Empty<RectTransform>();
        public TMP_Text chainLabel;
        private struct Particle { public Vector2 p,v;public float age,life,size;public Color color; public bool link; }
        private struct Shard {public Vector2 p,v;public float age,life,size,angle,spin,aspect;public Color face,light,dark;}
        // Authored graphic, fixed storage: fragments never create GameObjects or meshes.
        private readonly Shard[] shards=new Shard[96];
        private int shardCursor;
        public int ActiveShards {get{int count=depthCount;for(int i=0;i<shards.Length;i++)if(shards[i].age<shards[i].life)count++;return count;}}
        private struct Ray {public Vector2 from,to;public float age,life;public Color color;}
        private struct Wave { public Vector2 p;public float age,life;public bool big;public Color color; }
        private struct Ricochet {public Vector2 p,inward;public float age,life;public bool reduced;public Color color;}
        private readonly Ricochet[] ricochets=new Ricochet[14];
        private int ricochetCursor;
        public void Rebound(Vector2 point,string direction,string color,bool reduced)
        {
            RuneRules.Direction(direction,out int dx,out int dy);
            ricochets[ricochetCursor]=new Ricochet{p=new Vector2(point.x,-point.y),inward=new Vector2(-dx,dy),life=reduced?.13f:.30f,reduced=reduced,color=Light(color)};ricochetCursor=(ricochetCursor+1)%ricochets.Length;
        }
        private struct Caption {public Vector2 p;public string text;public float age,life;public bool big;public Color color;}
        private readonly Particle[] particles=new Particle[650];
        private readonly Wave[] waves=new Wave[48];
        private readonly Caption[] captions=new Caption[24];
        private readonly Ray[] rays=new Ray[32];
        private int rayCursor;
        private int cursor,waveCursor,captionCursor,chain;
        private float chainAge,storageFlash;
        public void Clear(){ClearReward();depthCount=0;System.Array.Clear(shards,0,shards.Length);System.Array.Clear(ricochets,0,ricochets.Length);System.Array.Clear(particles,0,particles.Length);System.Array.Clear(waves,0,waves.Length);System.Array.Clear(captions,0,captions.Length);System.Array.Clear(rays,0,rays.Length);chain=0;storageFlash=0;for(int i=0;i<captionLabels.Length;i++){captionLabels[i].enabled=false;captionShadows[i].enabled=false;}if(chainLabel!=null)chainLabel.enabled=false;SetVerticesDirty();}
        public void ShatterBlock(Vector2 point,string color,bool layerBreak,bool reduced)
        {
            int count=reduced?4:layerBreak?18:12;
            for(int i=0;i<count;i++)
            {
                float angle=(i+.3f+Random.value*.4f)*Mathf.PI*2/count;
                var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                shards[shardCursor]=new Shard{p=point+direction*Random.Range(3f,16f),v=reduced?direction*35:direction*Random.Range(95f,205f)+new Vector2(0,-45),life=reduced?.30f:Random.Range(.62f,.95f),size=reduced?7:Random.Range(9f,18f),aspect=Random.Range(.55f,1.1f),angle=angle,spin=reduced?0:Random.Range(-9f,9f),face=Mid(color),light=Light(color),dark=Dark(color)};
                shardCursor=(shardCursor+1)%shards.Length;
            }
            AddWave(point,color,layerBreak);SetVerticesDirty();
        }
        public void Label(string text,Vector2 point,string color,bool big,float life,float delay=0)
        {
            if(captionLabels.Length==0)return;int i=captionCursor;
            captions[i]=new Caption{text=text,p=point,color=Light(color),big=big,life=life,age=-delay};
            captionLabels[i].text=captionShadows[i].text=text;captionLabels[i].fontSize=captionShadows[i].fontSize=big?(art!=null&&art.surfaceStyle?42:24):12;
            captionLabels[i].enabled=captionShadows[i].enabled=false;captionCursor=(i+1)%captionLabels.Length;
        }
        public void Chain(int value){chain=value;chainAge=0;if(chainLabel!=null)chainLabel.SetText("{0} CHAIN!",value);}
        public void Overflow()=>storageFlash=1;
        public void Emit(Vector2 point,string color,bool big,bool reduced,int side=-1)
        {
            if(art!=null&&art.surfaceStyle){EmitDepth(point,color,big,reduced,side);AddWave(point,color,big);return;}
            int count=reduced?(big?10:5):(big?66:26);
            for(int i=0;i<count;i++)
            {
                float a=Random.value*Mathf.PI*2,speed=(big?125:85)+Random.value*(big?250:160);
                particles[cursor]=new Particle {p=point,v=new Vector2(Mathf.Cos(a)*speed,Mathf.Sin(a)*speed-70),life=(big?.48f:.3f)+Random.value*.48f,size=Random.value<.2f?5:2+Random.Range(0,3),color=i%4==0?Light(color):i%4==1?Mid(color):i%4==2?Dark(color):RuneArtLibrary.Hex("f7ebc1")};cursor=(cursor+1)%particles.Length;
            }AddWave(point,color,big);
        }
        public void AddWave(Vector2 point,string color,bool big){waves[waveCursor]=new Wave {p=point,color=Light(color),big=big,life=art!=null&&art.surfaceStyle?(big?.26f:.16f):(big?.4f:.24f)};waveCursor=(waveCursor+1)%waves.Length;}
        public void ReleaseChain(Vector2 point,Vector2 source,string color,bool reduced)
        {
            rays[rayCursor]=new Ray{from=source,to=point,life=reduced?.14f:.32f,color=Light(color)};rayCursor=(rayCursor+1)%rays.Length;
            int count=reduced?4:16;
            for(int i=0;i<count;i++)
            {
                float a=(i+.5f)/count*Mathf.PI*2;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                particles[cursor]=new Particle{p=point+d*19,v=d*(95+Random.value*100)+Vector2.up*50,life=.45f+Random.value*.3f,size=5,color=i%3==0?Light(color):RuneArtLibrary.Hex("a3afa0"),link=true};cursor=(cursor+1)%particles.Length;
            }
            AddWave(point,color,true);
        }
        public void Reflect(Vector2 point,string color,bool reduced)
        {
            int count=reduced?4:14;for(int i=0;i<count;i++)
            {
                float a=(i%4)*Mathf.PI*.5f+Mathf.PI*.25f;float speed=80+Random.value*100;
                particles[cursor]=new Particle{p=point,v=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*speed,life=.17f+Random.value*.16f,size=i%3==0?4:2,color=i%2==0?Light(color):RuneArtLibrary.Hex("f7ebc1")};cursor=(cursor+1)%particles.Length;
            }
            AddWave(point,color,false);
        }
        public void Reveal(Vector2 point,string color,bool reduced)
        {
            AddWave(point,color,true);int count=reduced?4:12;
            for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;particles[cursor]=new Particle{p=point+new Vector2(Mathf.Cos(a)*17,Mathf.Sin(a)*12),v=new Vector2(Mathf.Cos(a)*35,-65-Random.value*65),life=.35f+Random.value*.22f,size=i%3==0?4:2,color=Light(color)};cursor=(cursor+1)%particles.Length;}
        }
        public void Tick(float dt)
        {
            TickReward(dt);
            TickDepth(dt);
            for(int i=0;i<shards.Length;i++){ref var s=ref shards[i];if(s.age>=s.life)continue;s.age+=dt;s.p+=s.v*dt;s.v.y+=380*dt;s.v.x*=Mathf.Pow(.985f,dt*60);s.angle+=s.spin*dt;}
            for(int i=0;i<ricochets.Length;i++)ricochets[i].age+=dt;
            for(int i=0;i<particles.Length;i++){ref var p=ref particles[i];if(p.age>=p.life)continue;p.age+=dt;p.p+=p.v*dt;p.v.y+=340*dt;p.v.x*=Mathf.Pow(.97f,dt*60);}
            for(int i=0;i<waves.Length;i++)waves[i].age+=dt;for(int i=0;i<captions.Length;i++)captions[i].age+=dt;for(int i=0;i<rays.Length;i++)rays[i].age+=dt;chainAge+=dt;storageFlash=Mathf.Max(0,storageFlash-dt*2);SetVerticesDirty();
            for(int i=0;i<captionLabels.Length;i++)
            {
                var l=captions[i];bool visible=l.age>=0&&l.age<l.life;captionLabels[i].enabled=captionShadows[i].enabled=visible;if(!visible)continue;
                float q=l.age/l.life;var c=l.color;c.a=Mathf.Min(1,(1-q)*2);var shadow=RuneArtLibrary.Hex("14252e");shadow.a=c.a;
                captionRects[i].anchoredPosition=new Vector2(Mathf.Round(l.p.x),-Mathf.Round(l.p.y-q*22));captionLabels[i].color=c;captionShadows[i].color=shadow;
                if(art!=null&&art.surfaceStyle&&l.big){float t=Mathf.Clamp01(l.age/.18f),scale=RunePreferences.Reduced?1:1+2.70158f*Mathf.Pow(t-1,3)+1.70158f*Mathf.Pow(t-1,2);captionRects[i].localScale=Vector3.one*scale;captionRects[i].localRotation=Quaternion.Euler(0,0,-7);captionLabels[i].color=new Color(1,.97f,.88f,c.a);}
            }
            if(chainLabel!=null){chainLabel.enabled=chain>1&&chainAge<.8f;var c=RuneArtLibrary.Hex("f7dda2");c.a=Mathf.Clamp01((.8f-chainAge)*3);chainLabel.color=c;}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            DrawReward(vh);
            DrawDepth(vh);
            foreach(var r in ricochets)
            {
                if(r.age>=r.life)continue;float u=Mathf.Clamp01(r.age/r.life);var v=r.inward;var perpendicular=new Vector2(-v.y,v.x);var c=r.color;c.a=(1-u)*.8f;
                var center=r.p+v*u*11;float rx=5+u*(r.reduced?8:23),ry=3+u*10;
                for(int j=0;j<32;j++){float a=j*Mathf.PI/16,b=(j+1)*Mathf.PI/16;Line(vh,center+perpendicular*Mathf.Cos(a)*rx+v*Mathf.Sin(a)*ry,center+perpendicular*Mathf.Cos(b)*rx+v*Mathf.Sin(b)*ry,r.reduced?1.1f:2.5f*(1-u)+.6f,c);}
                if(r.reduced)continue;
                if(r.age<.08f)
                {
                    float q=r.age/.08f;var flash=RuneArtLibrary.Hex("fff9e8");flash.a=(1-q)*.9f;int start=vh.currentVertCount;vh.AddVert(r.p,flash,Vector2.zero);
                    for(int j=0;j<16;j++){float a=j*Mathf.PI/8,rad=(j%2==0?16:5)*(1+q*.55f);vh.AddVert(r.p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rad,flash,Vector2.zero);}
                    for(int j=0;j<16;j++)vh.AddTriangle(start,start+1+j,start+1+(j+1)%16);
                }
                for(int j=0;j<7;j++){float a=-.95f+j*.31f;var d=v*Mathf.Cos(a)+perpendicular*Mathf.Sin(a);float distance=7+u*(37+j%3*10),tail=7*(1-u)+3;var spark=j%2==0?r.color:RuneArtLibrary.Hex("fff1c8");spark.a=(1-u)*.8f;Line(vh,r.p+d*distance,r.p+d*(distance+tail),j%2==0?2.6f:1.6f,spark);}
                if(r.age<.11f){var crease=RuneArtLibrary.Hex("fff9e8");crease.a=(1-r.age/.11f)*.55f;Line(vh,r.p-perpendicular*14,r.p+perpendicular*14,3,crease);}
            }
            foreach(var ray in rays)
            {
                if(ray.age>=ray.life)continue;float t=ray.age/ray.life;var c=ray.color;c.a=(1-t)*.8f;var d=ray.to-ray.from;
                for(int i=0;i<30;i++){float f=i/30f;var p=ray.from+d*f;float w=1+Mathf.Max(0,1-Mathf.Abs(f-t*1.3f)*6)*4;Rect(vh,p.x-w/2,p.y-w/2,w,w,c);}
            }
            foreach(var p in particles)
            {
                if(p.age>=p.life)continue;var c=p.color;c.a=Mathf.Max(0,1-p.age/p.life);
                if(p.link){Rect(vh,p.p.x,p.p.y,7,2,c);Rect(vh,p.p.x,p.p.y+2,2,3,c);Rect(vh,p.p.x+5,p.p.y+2,2,3,c);Rect(vh,p.p.x,p.p.y+5,7,2,c);}
                else RuneUiMesh.Quad(vh,Mathf.Round(p.p.x),-Mathf.Round(p.p.y),p.size,p.size,c);
            }
            foreach(var s in shards)
            {
                if(s.age>=s.life)continue;
                float q=s.age/s.life,alpha=Mathf.Clamp01((1-q)*4),size=s.size*(1-.3f*q);
                var x=new Vector2(Mathf.Cos(s.angle),Mathf.Sin(s.angle))*size*.5f;
                var y=new Vector2(-x.y,x.x)*s.aspect;var p=new Vector2(s.p.x,-s.p.y);
                var a=p-x+y*.7f;var b=p+x*.65f+y;var c=p+x-y;var d=p-x*.8f-y*.6f;
                var light=s.light;light.a=alpha;var face=s.face;face.a=alpha;var dark=s.dark;dark.a=alpha;
                var depth=new Vector2(1.5f,-2.4f);var shadow=new Color(.02f,.02f,.07f,alpha*.4f);
                ShardFace(vh,a+depth*2,b+depth*2,c+depth*2,d+depth*2,shadow,shadow);
                ShardFace(vh,b,c,c+depth,b+depth,dark,dark);ShardFace(vh,c,d,d+depth,c+depth,dark,dark);
                ShardFace(vh,a,b,c,d,light,face);
                var facet=Color.Lerp(face,dark,.25f);facet.a=alpha;int v=vh.currentVertCount;
                vh.AddVert(p,face,Vector2.zero);vh.AddVert(c,facet,Vector2.zero);vh.AddVert(d,facet,Vector2.zero);vh.AddTriangle(v,v+1,v+2);
                Line(vh,a,b,1.35f,light);Line(vh,a,d,1,light);
            }
            foreach(var w in waves)
            {
                if(art!=null&&art.surfaceStyle){DrawSurfaceWave(vh,w);continue;}
                if(w.age>=w.life)continue;float t=w.age/w.life,r=5+t*(w.big?42:23);var c=w.color;c.a=(1-t)*.8f;var p=new Vector2(w.p.x,-w.p.y);
                Line(vh,p+Vector2.up*r,p+Vector2.right*r,w.big?3:2,c);Line(vh,p+Vector2.right*r,p+Vector2.down*r,w.big?3:2,c);Line(vh,p+Vector2.down*r,p+Vector2.left*r,w.big?3:2,c);Line(vh,p+Vector2.left*r,p+Vector2.up*r,w.big?3:2,c);
            }
            if(chain>1&&chainAge<.8f)
            {
                float alpha=Mathf.Min(1,(.8f-chainAge)*3);var fill=RuneArtLibrary.Hex("14292f");var edge=RuneArtLibrary.Hex("b99c6b");var text=RuneArtLibrary.Hex("f7dda2");fill.a=edge.a=text.a=alpha;
                Box(vh,176,20,128,24,fill,edge,3);
            }
            if(storageFlash>0){var c=RuneArtLibrary.Hex("ef987a");c.a=storageFlash*.25f;if(art!=null&&art.surfaceStyle)Box(vh,59,535,362,94,c,c,6);else Box(vh,24,450,432,108,c,c,6);}
        }
        private static void ShardFace(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color top,Color bottom)
        {
            int i=vh.currentVertCount;vh.AddVert(a,top,Vector2.zero);vh.AddVert(b,top,Vector2.zero);vh.AddVert(c,bottom,Vector2.zero);vh.AddVert(d,bottom,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
        private static void Rect(VertexHelper vh,float x,float y,float w,float h,Color c)=>RuneUiMesh.Quad(vh,Mathf.Floor(x+.5f),-Mathf.Floor(y+.5f),Mathf.Floor(w+.5f),Mathf.Floor(h+.5f),c);
        private static void Box(VertexHelper vh,float x,float y,float w,float h,Color fill,Color edge,int cut)
        {
            Rect(vh,x+cut,y,w-cut*2,h,edge);Rect(vh,x,y+cut,w,h-cut*2,edge);Rect(vh,x+2+cut,y+2,w-4-cut*2,h-4,fill);Rect(vh,x+2,y+2+cut,w-4,h-4-cut*2,fill);
        }
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;int i=vh.currentVertCount;vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddVert(a-n,c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
