using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    /// <summary>Decorative sky extends above the safe world viewport, including the notch.</summary>
    [ExecuteAlways]
    public sealed class BattleSkyExtension : MonoBehaviour
    {
        public PlayPage page;
        public RawImage sky;
        private void OnEnable(){LateUpdate();}
        private void LateUpdate()
        {
            if(sky==null)return;
            var camera=page!=null?page.Canvas.worldCamera:null;
            if(camera==null)camera=Camera.main;
            if(camera==null){sky.enabled=false;return;}
            var r=sky.rectTransform;
            r.anchorMin=new Vector2(0,camera.rect.yMax);r.anchorMax=Vector2.one;
            r.offsetMin=r.offsetMax=Vector2.zero;
            sky.enabled=r.anchorMin.y<.9999f;
        }
        private void OnDisable(){if(sky!=null)sky.enabled=false;}
    }
}
