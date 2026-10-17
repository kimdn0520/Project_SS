using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    // Exact 5x7 glyph masks from RuneArt.pixText, rendered as a single UI mesh.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RunePixelText : MaskableGraphic
    {
        public RuneArtLibrary art;
        [TextArea] [SerializeField] private string value="";
        public int pixelSize=1;
        public TextAnchor alignment=TextAnchor.MiddleCenter;
        public string Text {get=>value;set {if(this.value==value)return;this.value=value;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(art==null||string.IsNullOrEmpty(value))return;
            float width=-pixelSize;foreach(char ch in value)width+=(art.Glyph(ch)[0].Length+1)*pixelSize;
            var r=rectTransform.rect;float x=alignment==TextAnchor.MiddleLeft?r.xMin:alignment==TextAnchor.MiddleRight?r.xMax-width:r.center.x-width/2;
            float y=r.center.y+3.5f*pixelSize;
            foreach(char ch in value){var rows=art.Glyph(ch);for(int row=0;row<rows.Length;row++)for(int col=0;col<rows[row].Length;col++)if(rows[row][col]=='1')Quad(vh,Mathf.Floor(x+col*pixelSize+.5f),Mathf.Floor(y-row*pixelSize+.5f),pixelSize,pixelSize,color);x+=(rows[0].Length+1)*pixelSize;}
        }
        public static void Quad(VertexHelper vh,float x,float y,float w,float h,Color color)
        {
            int i=vh.currentVertCount;vh.AddVert(new Vector3(x,y),color,Vector2.zero);vh.AddVert(new Vector3(x+w,y),color,Vector2.zero);vh.AddVert(new Vector3(x+w,y-h),color,Vector2.zero);vh.AddVert(new Vector3(x,y-h),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
