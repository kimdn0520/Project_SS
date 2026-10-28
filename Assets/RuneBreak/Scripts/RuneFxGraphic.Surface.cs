using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // DepthFX from block-play-surface-buttons.html. The existing effect graphic owns
    // every fragment; fixed arrays and one UI mesh keep the authored hierarchy stable.
    public sealed partial class RuneFxGraphic
    {
        private struct DepthShard
        {
            public Vector3 position,velocity,rotation,spin,size;
            public float age,life;
            public int bounces,mesh;
            public Color rgb;
        }
        private readonly DepthShard[] depthShards=new DepthShard[190];
        private readonly int[] depthOrder=new int[190],faceOrder=new int[26];
        private readonly float[] faceDepth=new float[26];
        private readonly Color[] faceColors=new Color[26];
        private readonly Vector3[,] faceVertices=new Vector3[26,4];
        private int depthCount;
        private uint depthSeed=1847;
        private static readonly Vector3 depthLight=new Vector3(-.46f,-.58f,.72f).normalized,depthHalf=new Vector3(-.46f,-.58f,1.72f).normalized;
        private static readonly Vector3[][][] depthMeshes={BuildBevel(),BuildChip()};
        private float DepthRandom(){depthSeed=unchecked(depthSeed*1664525u+1013904223u);return (float)(depthSeed/4294967296.0);}
        private void EmitDepth(Vector2 point,string color,bool big,bool reduced,int side)
        {
            if(big&&side>=0)
            {
                int heroes=reduced?3:10;
                for(int i=0;i<heroes;i++){float u=(i+.5f)/heroes;AddDepth(side==0?new Vector2(72+336*u,74):new Vector2(434,100+336*u),color,true,side,reduced,true);}
            }
            int count=reduced?(big?6:3):(big?32:16);
            for(int i=0;i<count;i++)AddDepth(point,color,big,side,reduced,false);
        }
        private void AddDepth(Vector2 point,string color,bool big,int side,bool reduced,bool hero)
        {
            float angle=DepthRandom()*Mathf.PI*2,speed=(big?55:30)+DepthRandom()*(big?105:70);
            float size=hero?5+DepthRandom()*2.7f:DepthRandom()<.22f?4.8f+DepthRandom()*2.5f:2.1f+DepthRandom()*3.2f;
            var p=new DepthShard();p.position=new Vector3(point.x,point.y,2+DepthRandom()*6);
            p.velocity=new Vector3(Mathf.Cos(angle)*speed+(side==1?-(big?65:30):0),Mathf.Sin(angle)*speed*.52f+(side==0?(big?42:24):0),reduced?40:125+DepthRandom()*120);
            p.life=reduced?.28f:.7f+DepthRandom()*.46f;
            p.rotation=new Vector3(DepthRandom()*5,DepthRandom()*5,DepthRandom()*6.28f);
            p.spin=new Vector3((DepthRandom()-.5f)*9,(DepthRandom()-.5f)*11,(DepthRandom()-.5f)*8);
            p.size=new Vector3(size*(hero?1.4f:.75f+DepthRandom()*.4f),size*(.65f+DepthRandom()*.5f),size*(hero?.38f:.48f+DepthRandom()*.38f));
            p.rgb=Mid(color);p.mesh=DepthRandom()<.62f?0:1;
            if(depthCount==depthShards.Length){Array.Copy(depthShards,1,depthShards,0,depthCount-1);depthCount--;}
            depthShards[depthCount++]=p;
        }
        private void TickDepth(float dt)
        {
            int count=0;
            for(int i=0;i<depthCount;i++)
            {
                var p=depthShards[i];p.age+=dt;p.position+=p.velocity*dt;p.velocity.z-=610*dt;
                float drag=Mathf.Pow(.987f,dt*60);p.velocity.x*=drag;p.velocity.y*=drag;p.rotation+=p.spin*dt;
                if(p.position.z<0)
                {
                    p.position.z=0;
                    if(p.bounces<2&&Mathf.Abs(p.velocity.z)>35){p.velocity.z=-p.velocity.z*.28f;p.velocity.x*=.63f;p.velocity.y*=.63f;p.spin.x*=.65f;p.spin.y*=.65f;p.bounces++;}
                    else{p.velocity.z=0;p.velocity.x*=.82f;p.velocity.y*=.82f;p.spin.x*=.8f;p.spin.y*=.8f;}
                }
                if(p.age<p.life)depthShards[count++]=p;
            }
            depthCount=count;
        }
        private static Vector3 RotateDepth(Vector3 v,Vector3 r)
        {
            float y=v.y*Mathf.Cos(r.x)-v.z*Mathf.Sin(r.x),z=v.y*Mathf.Sin(r.x)+v.z*Mathf.Cos(r.x);
            float x=v.x*Mathf.Cos(r.y)+z*Mathf.Sin(r.y),zz=-v.x*Mathf.Sin(r.y)+z*Mathf.Cos(r.y);
            return new Vector3(x*Mathf.Cos(r.z)-y*Mathf.Sin(r.z),x*Mathf.Sin(r.z)+y*Mathf.Cos(r.z),zz);
        }
        private static Vector3[] Outward(Vector3[] face)
        {
            Vector3 center=Vector3.zero;foreach(var v in face)center+=v;
            if(Vector3.Dot(Vector3.Cross(face[1]-face[0],face[2]-face[0]),center)<0)Array.Reverse(face);return face;
        }
        private static Vector3[][] BuildBevel()
        {
            const float q=.76f;var faces=new List<Vector3[]>();var square=new[]{new Vector2(-q,-q),new Vector2(q,-q),new Vector2(q,q),new Vector2(-q,q)};
            for(int a=0;a<3;a++)for(int s=-1;s<=1;s+=2){var face=new Vector3[4];for(int i=0;i<4;i++){face[i][a]=s;face[i][(a+1)%3]=square[i].x;face[i][(a+2)%3]=square[i].y;}faces.Add(Outward(face));}
            var edge=new[]{new Vector3(1,q,-q),new Vector3(q,1,-q),new Vector3(q,1,q),new Vector3(1,q,q)};
            for(int a=0;a<3;a++)for(int b=a+1;b<3;b++)for(int sa=-1;sa<=1;sa+=2)for(int sb=-1;sb<=1;sb+=2)
            {var face=new Vector3[4];for(int i=0;i<4;i++){face[i][a]=sa*edge[i].x;face[i][b]=sb*edge[i].y;face[i][3-a-b]=edge[i].z;}faces.Add(Outward(face));}
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)faces.Add(Outward(new[]{new Vector3(x,y*q,z*q),new Vector3(x*q,y,z*q),new Vector3(x*q,y*q,z)}));
            return faces.ToArray();
        }
        private static Vector3[][] BuildChip()
        {
            var v=new[]{new Vector3(-1,-.8f,-.75f),new Vector3(.9f,-.8f,-.75f),new Vector3(1,.8f,-.6f),new Vector3(-.65f,1,-.7f),new Vector3(-.55f,-.6f,1),new Vector3(.7f,-.4f,.8f),new Vector3(.6f,.6f,.65f),new Vector3(-.6f,.6f,.8f)};
            var ids=new[]{new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,1,5,4},new[]{1,2,6,5},new[]{2,3,7,6},new[]{3,0,4,7}};var faces=new Vector3[6][];
            for(int i=0;i<6;i++)faces[i]=Outward(new[]{v[ids[i][0]],v[ids[i][1]],v[ids[i][2]],v[ids[i][3]]});return faces;
        }
        private void DrawDepth(VertexHelper vh)
        {
            for(int i=0;i<depthCount;i++)
            {
                var p=depthShards[i];float fade=Mathf.Min(1,(p.life-p.age)/.25f);var shadow=RuneArtLibrary.Hex("05091c");shadow.a=fade*.18f/(1+p.position.z/36);
                Ellipse(vh,new Vector2(p.position.x+2+p.position.z*.07f,-p.position.y-3),Mathf.Max(1.5f,p.size.x*(1+p.position.z*.009f)),Mathf.Max(1,p.size.y*.45f),shadow);
                int at=i;float key=p.position.y+p.position.z*.15f;
                while(at>0){var old=depthShards[depthOrder[at-1]];if(old.position.y+old.position.z*.15f<=key)break;depthOrder[at]=depthOrder[at-1];at--;}depthOrder[at]=i;
            }
            for(int order=0;order<depthCount;order++)
            {
                var p=depthShards[depthOrder[order]];var mesh=depthMeshes[p.mesh];int count=0;
                for(int f=0;f<mesh.Length;f++)
                {
                    float depth=0;for(int j=0;j<mesh[f].Length;j++){var v=RotateDepth(Vector3.Scale(mesh[f][j],p.size),p.rotation);faceVertices[f,j]=v;depth+=v.z;}
                    var n=Vector3.Cross(faceVertices[f,1]-faceVertices[f,0],faceVertices[f,2]-faceVertices[f,0]).normalized;if(n.z<=.025f)continue;
                    float diffuse=Mathf.Max(0,Vector3.Dot(n,depthLight)),spec=Mathf.Pow(Mathf.Max(0,Vector3.Dot(n,depthHalf)),32);
                    var c=p.rgb;for(int ch=0;ch<3;ch++)c[ch]=Mathf.Clamp(Mathf.Floor(c[ch]*255*(.38f+.63f*diffuse)+spec*78+.5f),0,255)/255;
                    c.a=Mathf.Min(1,(p.life-p.age)/.23f);faceColors[f]=c;faceDepth[f]=depth/mesh[f].Length;
                    int at=count++;while(at>0&&faceDepth[faceOrder[at-1]]>faceDepth[f]){faceOrder[at]=faceOrder[at-1];at--;}faceOrder[at]=f;
                }
                for(int fi=0;fi<count;fi++)
                {
                    int f=faceOrder[fi],start=vh.currentVertCount;
                    for(int j=0;j<mesh[f].Length;j++){var v=faceVertices[f,j];float perspective=1/(1-v.z*.016f);vh.AddVert(new Vector2(p.position.x+v.x*perspective,-p.position.y+p.position.z-v.y*perspective),faceColors[f],Vector2.zero);}
                    for(int j=1;j<mesh[f].Length-1;j++)vh.AddTriangle(start,start+j,start+j+1);
                }
            }
        }
        private static void Ellipse(VertexHelper vh,Vector2 center,float rx,float ry,Color color)
        {
            const int segments=16;int start=vh.currentVertCount;vh.AddVert(center,color,Vector2.zero);
            for(int j=0;j<segments;j++){float a=j*Mathf.PI*2/segments;vh.AddVert(center+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),color,Vector2.zero);}
            for(int j=0;j<segments;j++)vh.AddTriangle(start,start+1+j,start+1+(j+1)%segments);
        }
        private static void DrawSurfaceWave(VertexHelper vh,Wave w)
        {
            if(w.age>=w.life)return;float t=w.age/w.life,r=5+t*(w.big?32:18);var c=w.color;c.a=(1-t)*.46f;var p=new Vector2(w.p.x,-w.p.y);
            for(int j=0;j<32;j++){float a=j*Mathf.PI/16,b=(j+1)*Mathf.PI/16;Line(vh,p+new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.6f),p+new Vector2(Mathf.Cos(b)*r,Mathf.Sin(b)*r*.6f),w.big?2:1.3f,c);}
        }
    }
}
