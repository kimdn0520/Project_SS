using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RuneFxGraphic : MaskableGraphic
    {
        public RuneArtLibrary art;
        private struct Particle { public Vector2 p,v;public float age,life,size;public Color color; }
        private struct Wave { public Vector2 p;public float age,life;public bool big;public Color color; }
        private struct Caption {public Vector2 p;public string text;public float age,life;public bool big;public Color color;}
        private readonly Particle[] particles=new Particle[650];
        private readonly Wave[] waves=new Wave[48];
        private readonly Caption[] captions=new Caption[24];
        private int cursor,waveCursor,captionCursor,chain;
        private float chainAge,storageFlash;
        public void Clear(){System.Array.Clear(particles,0,particles.Length);System.Array.Clear(waves,0,waves.Length);System.Array.Clear(captions,0,captions.Length);chain=0;storageFlash=0;SetVerticesDirty();}
        public void Label(string text,Vector2 point,string color,bool big,float life,float delay=0){captions[captionCursor]=new Caption{text=text,p=point,color=RuneArtLibrary.Light(color),big=big,life=life,age=-delay};captionCursor=(captionCursor+1)%captions.Length;}
        public void Chain(int value){chain=value;chainAge=0;}
        public void Overflow()=>storageFlash=1;
        public void Emit(Vector2 point,string color,bool big,bool reduced)
        {
            int count=reduced?(big?10:5):(big?66:26);
            for(int i=0;i<count;i++)
            {
                float a=Random.value*Mathf.PI*2,speed=(big?125:85)+Random.value*(big?250:160);
                particles[cursor]=new Particle {p=point,v=new Vector2(Mathf.Cos(a)*speed,Mathf.Sin(a)*speed-70),life=(big?.48f:.3f)+Random.value*.48f,size=Random.value<.2f?5:2+Random.Range(0,3),color=i%4==0?RuneArtLibrary.Light(color):i%4==1?RuneArtLibrary.Mid(color):i%4==2?RuneArtLibrary.Dark(color):RuneArtLibrary.Hex("f7ebc1")};cursor=(cursor+1)%particles.Length;
            }AddWave(point,color,big);
        }
        public void AddWave(Vector2 point,string color,bool big){waves[waveCursor]=new Wave {p=point,color=RuneArtLibrary.Light(color),big=big,life=big?.4f:.24f};waveCursor=(waveCursor+1)%waves.Length;}
        public void Tick(float dt)
        {
            for(int i=0;i<particles.Length;i++){ref var p=ref particles[i];if(p.age>=p.life)continue;p.age+=dt;p.p+=p.v*dt;p.v.y+=340*dt;p.v.x*=Mathf.Pow(.97f,dt*60);}
            for(int i=0;i<waves.Length;i++)waves[i].age+=dt;for(int i=0;i<captions.Length;i++)captions[i].age+=dt;chainAge+=dt;storageFlash=Mathf.Max(0,storageFlash-dt*2);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();foreach(var p in particles){if(p.age>=p.life)continue;var c=p.color;c.a=Mathf.Max(0,1-p.age/p.life);RunePixelText.Quad(vh,Mathf.Round(p.p.x),-Mathf.Round(p.p.y),p.size,p.size,c);}
            foreach(var w in waves)
            {
                if(w.age>=w.life)continue;float t=w.age/w.life,r=5+t*(w.big?42:23);var c=w.color;c.a=(1-t)*.8f;var p=new Vector2(w.p.x,-w.p.y);
                Line(vh,p+Vector2.up*r,p+Vector2.right*r,w.big?3:2,c);Line(vh,p+Vector2.right*r,p+Vector2.down*r,w.big?3:2,c);Line(vh,p+Vector2.down*r,p+Vector2.left*r,w.big?3:2,c);Line(vh,p+Vector2.left*r,p+Vector2.up*r,w.big?3:2,c);
            }
            foreach(var l in captions)
            {
                if(l.age>=l.life)continue;float q=l.age/l.life;var c=l.color;c.a=Mathf.Min(1,(1-q)*2);var shadow=RuneArtLibrary.Hex("14252e");shadow.a=c.a;
                Text(vh,l.text,l.p.x+1,l.p.y-q*22+1,l.big?2:1,shadow);Text(vh,l.text,l.p.x,l.p.y-q*22,l.big?2:1,c);
            }
            if(chain>1&&chainAge<.8f)
            {
                float alpha=Mathf.Min(1,(.8f-chainAge)*3);var fill=RuneArtLibrary.Hex("14292f");var edge=RuneArtLibrary.Hex("b99c6b");var text=RuneArtLibrary.Hex("f7dda2");fill.a=edge.a=text.a=alpha;
                Box(vh,176,20,128,24,fill,edge,3);Text(vh,chain+" CHAIN!",240,28,1,text);
            }
            if(storageFlash>0){var c=RuneArtLibrary.Hex("ef987a");c.a=storageFlash*.25f;Box(vh,24,450,432,108,c,c,6);}
        }
        private void Text(VertexHelper vh,string value,float x,float y,int size,Color c)
        {
            if(art==null||value==null)return;float width=-size;foreach(char ch in value)width+=(art.Glyph(ch)[0].Length+1)*size;x-=width*.5f;
            foreach(char ch in value){var g=art.Glyph(ch);for(int r=0;r<g.Length;r++)for(int col=0;col<g[r].Length;col++)if(g[r][col]=='1')Rect(vh,x+col*size,y+r*size,size,size,c);x+=(g[0].Length+1)*size;}
        }
        private static void Rect(VertexHelper vh,float x,float y,float w,float h,Color c)=>RunePixelText.Quad(vh,Mathf.Floor(x+.5f),-Mathf.Floor(y+.5f),Mathf.Floor(w+.5f),Mathf.Floor(h+.5f),c);
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
