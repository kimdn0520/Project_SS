using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RuneLobbyIcon : MaskableGraphic
    {
        public string kind;
        public int pixelSize=3;
        public bool smooth;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(smooth){Smooth(vh);return;}string rows;
            switch(kind){
                case "check":rows="00000000/00000001/00000011/01000110/11101100/01111000/00110000/00000000";break;
                case "coin":rows="00111100/01111110/11011011/11010011/11001011/11011011/01111110/00111100";break;
                case "heart":rows="00000000/01100110/11111111/11111111/01111110/00111100/00011000/00000000";break;
                case "gift":rows="00100100/00111100/11111111/10011001/11111111/01011010/01011010/01111110";break;
                case "flag":rows="01000000/01111110/01011010/01111110/01000000/01000000/01000000/11100000";break;
                case "scroll":rows="00111110/01000001/11011101/01000010/01011110/01000010/01111110/00000000";break;
                case "calendar":rows="00100100/01111110/01000010/01111110/01010110/01101010/01010110/01111110";break;
                case "mail":rows="00000000/11111111/11000011/10100101/10011001/10000001/11111111/00000000";break;
                case "star":rows="00011000/00011000/11111111/01111110/00111100/01111110/01100110/11000011";break;
                case "shop":rows="00111100/01000010/11111111/10101001/11111111/01000010/01011010/01111110";break;
                case "leaderboard":rows="01111110/11011011/11011011/01111110/00111100/00011000/00111100/01111110";break;
                case "home":rows="00011000/00111100/01111110/11111111/01000010/01011010/01011010/01111110";break;
                case "team":rows="00111100/00111100/00000000/01111110/11011011/11011011/01011010/00000000";break;
                case "modes":rows="000000010000/000001110000/000001000000/001111111100/010000000010/100100001001/101110010101/100100001001/100001100001/011110011110";break;
                default:rows="11111000/10001110/10101011/10001001/11111001/00100001/00111111/00000000";break;
            }
            var lines=rows.Split('/');int height=lines.Length,width=lines[0].Length;
            var r=rectTransform.rect;float left=r.center.x-width*pixelSize*.5f,top=r.center.y+height*pixelSize*.5f;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(lines[y][x]=='1')RuneUiMesh.Quad(vh,left+x*pixelSize,top-y*pixelSize,pixelSize,pixelSize,color);
        }
        private Vector2 P(float x,float y){var r=rectTransform.rect;float s=Mathf.Min(r.width,r.height)/32;return r.center+new Vector2(x-16,16-y)*s;}
        private void Stroke(VertexHelper vh,float x1,float y1,float x2,float y2,float width=2)
        {
            float s=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/32;RuneUiMesh.Line(vh,P(x1,y1),P(x2,y2),width*s,color);Dot(vh,x1,y1,width/2);Dot(vh,x2,y2,width/2);
        }
        private void Dot(VertexHelper vh,float x,float y,float radius)
        {
            int k=vh.currentVertCount;vh.AddVert(P(x,y),color,Vector2.zero);for(int i=0;i<20;i++){float a=i*Mathf.PI/10;vh.AddVert(P(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),color,Vector2.zero);}for(int i=0;i<20;i++)vh.AddTriangle(k,k+1+i,k+1+(i+1)%20);
        }
        private void Ring(VertexHelper vh,float x,float y,float radius,float width=2)
        {
            for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;RuneUiMesh.Line(vh,P(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),P(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),width*Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/32,color);}
        }
        private void Box(VertexHelper vh,float x,float y,float w,float h)
        {
            Stroke(vh,x+2,y,x+w-2,y);Stroke(vh,x+2,y+h,x+w-2,y+h);Stroke(vh,x,y+2,x,y+h-2);Stroke(vh,x+w,y+2,x+w,y+h-2);
            Stroke(vh,x+2,y,x,y+2);Stroke(vh,x+w-2,y,x+w,y+2);Stroke(vh,x,y+h-2,x+2,y+h);Stroke(vh,x+w,y+h-2,x+w-2,y+h);
        }
        private void Smooth(VertexHelper vh)
        {
            switch(kind)
            {
                case "home":Stroke(vh,4,15,16,5,2.6f);Stroke(vh,16,5,28,15,2.6f);Stroke(vh,8,14,8,27);Stroke(vh,24,14,24,27);Stroke(vh,8,27,24,27);Stroke(vh,13,27,13,20);Stroke(vh,13,20,19,20);Stroke(vh,19,20,19,27);break;
                case "shop":Box(vh,6,12,20,15);Stroke(vh,10,12,10,8);Stroke(vh,10,8,16,5);Stroke(vh,16,5,22,8);Stroke(vh,22,8,22,12);Dot(vh,11,16,1.4f);Dot(vh,21,16,1.4f);break;
                case "leaderboard":Stroke(vh,9,5,23,5);Stroke(vh,9,5,10,16);Stroke(vh,23,5,22,16);Stroke(vh,10,16,16,20);Stroke(vh,22,16,16,20);Stroke(vh,16,20,16,26);Stroke(vh,11,27,21,27);Stroke(vh,9,8,4,8);Stroke(vh,4,8,5,14);Stroke(vh,5,14,11,17);Stroke(vh,23,8,28,8);Stroke(vh,28,8,27,14);Stroke(vh,27,14,21,17);break;
                case "team":Ring(vh,16,10,4);Ring(vh,6,13,3);Ring(vh,26,13,3);Stroke(vh,10,26,10,22);Stroke(vh,10,22,13,18);Stroke(vh,13,18,19,18);Stroke(vh,19,18,22,22);Stroke(vh,22,22,22,26);Stroke(vh,3,26,3,21);Stroke(vh,3,21,7,19);Stroke(vh,29,26,29,21);Stroke(vh,29,21,25,19);break;
                case "modes":Box(vh,3,10,26,15);Stroke(vh,16,10,16,5);Stroke(vh,16,5,21,4);Stroke(vh,7,17,13,17,2.5f);Stroke(vh,10,14,10,20,2.5f);Dot(vh,23,15,1.7f);Dot(vh,20,20,1.7f);break;
                case "music":Stroke(vh,13,23,13,7,2.5f);Stroke(vh,13,7,26,4,3);Stroke(vh,26,4,26,20,2.5f);Dot(vh,9,24,4);Dot(vh,22,21,4);break;
                case "sound":Stroke(vh,5,13,11,13,3);Stroke(vh,5,13,5,21,3);Stroke(vh,5,21,11,21,3);Stroke(vh,11,13,18,7,3);Stroke(vh,18,7,18,27,3);Stroke(vh,18,27,11,21,3);Stroke(vh,23,12,26,16,2.4f);Stroke(vh,26,16,23,21,2.4f);break;
                case "coin":Ring(vh,16,16,11,2.3f);Ring(vh,16,16,7,.9f);Stroke(vh,18,10,13,10);Stroke(vh,13,10,12,15);Stroke(vh,12,15,19,17);Stroke(vh,19,17,18,22);Stroke(vh,18,22,13,22);Stroke(vh,16,8,16,24,1.4f);break;
                case "heart":Stroke(vh,16,27,5,15,4);Stroke(vh,16,27,27,15,4);Ring(vh,10,11,6,3);Ring(vh,22,11,6,3);Stroke(vh,10,11,16,20,6);Stroke(vh,22,11,16,20,6);break;
                case "gift":Box(vh,5,13,22,14);Stroke(vh,3,12,29,12,3);Stroke(vh,16,8,16,27,2.5f);Ring(vh,11,8,4);Ring(vh,21,8,4);break;
                case "flag":Stroke(vh,7,4,7,28,2.4f);Stroke(vh,7,6,26,6);Stroke(vh,26,6,22,12);Stroke(vh,22,12,26,18);Stroke(vh,26,18,7,18);Stroke(vh,3,28,13,28);break;
                case "scroll":Box(vh,7,4,19,24);Stroke(vh,11,10,22,10);Stroke(vh,11,15,22,15);Stroke(vh,11,20,18,20);break;
                case "calendar":Box(vh,5,7,22,21);Stroke(vh,5,13,27,13);Stroke(vh,11,4,11,10,2.5f);Stroke(vh,21,4,21,10,2.5f);Stroke(vh,11,21,15,24,2.5f);Stroke(vh,15,24,22,17,2.5f);break;
                case "mail":Box(vh,3,7,26,19);Stroke(vh,4,9,16,18);Stroke(vh,16,18,28,9);break;
                case "settings":Ring(vh,16,16,8,2.6f);Ring(vh,16,16,3,2);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Stroke(vh,16+Mathf.Cos(a)*9,16+Mathf.Sin(a)*9,16+Mathf.Cos(a)*12,16+Mathf.Sin(a)*12,3);}break;
                case "star":for(int i=0;i<10;i++){float a=-Mathf.PI/2+i*Mathf.PI/5,b=a+Mathf.PI/5,r=i%2==0?12:5,s=i%2==0?5:12;Stroke(vh,16+Mathf.Cos(a)*r,16+Mathf.Sin(a)*r,16+Mathf.Cos(b)*s,16+Mathf.Sin(b)*s,1.8f);}break;
                default:Box(vh,6,6,20,20);Stroke(vh,11,21,16,11);Stroke(vh,16,11,21,21);break;
            }
        }
    }
}
