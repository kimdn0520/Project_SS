using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RuneLobbyIcon : MaskableGraphic
    {
        public string kind;
        public int pixelSize=3;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();string rows;
            switch(kind){
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
                default:rows="11111000/10001110/10101011/10001001/11111001/00100001/00111111/00000000";break;
            }
            var lines=rows.Split('/');var r=rectTransform.rect;float left=r.center.x-4*pixelSize,top=r.center.y+4*pixelSize;
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(lines[y][x]=='1')RuneUiMesh.Quad(vh,left+x*pixelSize,top-y*pixelSize,pixelSize,pixelSize,color);
        }
    }
}
