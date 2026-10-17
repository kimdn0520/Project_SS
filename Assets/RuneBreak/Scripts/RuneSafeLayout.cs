using UnityEngine;

namespace RuneBreak
{
    [ExecuteAlways]
    public sealed class RuneSafeLayout : MonoBehaviour
    {
        [System.Serializable] public class Pin {public RectTransform target;public Vector2 position;public bool top,bottom,right;}
        public RectTransform canvasRect,content;
        public Pin[] pins=System.Array.Empty<Pin>();
        private Vector2 lastSize;
        private Rect lastSafe;
        private void OnValidate(){lastSize=Vector2.zero;}
        private void LateUpdate()
        {
            if(canvasRect==null||content==null||Screen.width==0||Screen.height==0)return;
            Vector2 size=canvasRect.rect.size;Rect safe=Application.isPlaying?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
            if(size==lastSize&&safe==lastSafe)return;lastSize=size;lastSafe=safe;
            float sx=size.x/Screen.width,sy=size.y/Screen.height;
            float scale=Mathf.Min(safe.width*sx/540,safe.height*sy/960);
            content.localScale=Vector3.one*scale;
            content.anchoredPosition=new Vector2((safe.center.x-Screen.width*.5f)*sx,(safe.center.y-Screen.height*.5f)*sy);
            float extraY=(safe.height*sy/scale-960)*.5f,extraX=(safe.width*sx/scale-540)*.5f;
            foreach(var pin in pins)if(pin.target!=null)pin.target.anchoredPosition=pin.position+new Vector2(pin.right?extraX:0,pin.top?extraY:pin.bottom?-extraY:0);
        }
    }
}
