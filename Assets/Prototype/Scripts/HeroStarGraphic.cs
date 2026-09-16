using UnityEngine;using UnityEngine.UI;
namespace ProjectSS.Expedition
{
 [RequireComponent(typeof(CanvasRenderer))]
 public sealed class HeroStarGraphic : MaskableGraphic
 {
  // Rounded tips, warm outline and a light-to-gold face, drawn without a bitmap.
  protected override void OnPopulateMesh(VertexHelper vh)
  {
   vh.Clear();var rect=GetPixelAdjustedRect();float radius=Mathf.Min(rect.width,rect.height)*.47f;
   var points=new Vector2[10];for(int i=0;i<10;i++){float angle=(90+i*36)*Mathf.Deg2Rad;float r=radius*(i%2==0?1:.48f);points[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r;}
   var contour=new Vector2[40];for(int i=0;i<10;i++){var p=points[i];var a=Vector2.Lerp(p,points[(i+9)%10],.13f);var b=Vector2.Lerp(p,points[(i+1)%10],.13f);for(int s=0;s<4;s++){float t=s/3f;contour[i*4+s]=(1-t)*(1-t)*a+2*(1-t)*t*p+t*t*b;}}
   Face(vh,contour,rect.center+new Vector2(0,-.7f),1,new Color(.40f,.22f,.06f),new Color(.55f,.31f,.07f),radius);
   Face(vh,contour,rect.center,.89f,new Color(1,.64f,.13f),new Color(1,.94f,.62f),radius);
   Face(vh,contour,rect.center+new Vector2(0,.45f),.70f,new Color(1,.72f,.21f),new Color(1,.98f,.76f),radius);
  }
  static void Face(VertexHelper vh,Vector2[] contour,Vector2 center,float scale,Color bottom,Color top,float radius)
  {
   int start=vh.currentVertCount;vh.AddVert(center,Color.Lerp(bottom,top,.55f),Vector2.zero);
   foreach(var point in contour)vh.AddVert(center+point*scale,Color.Lerp(bottom,top,Mathf.InverseLerp(-radius,radius,point.y)),Vector2.zero);
   for(int i=0;i<contour.Length;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%contour.Length);
  }
 }
}
