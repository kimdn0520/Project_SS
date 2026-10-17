using UnityEngine;
namespace ProjectSS.Expedition
{
    [ExecuteAlways]
    public sealed class PopupSafeLayout : MonoBehaviour
    {
        void OnEnable() { LateUpdate(); }
        void LateUpdate()
        {
            var canvas=GetComponentInParent<Canvas>();
            if(canvas==null||Screen.width<=0||Screen.height<=0)return;
            var area=Screen.safeArea;
            var viewport=canvas.worldCamera!=null?canvas.worldCamera.pixelRect:new Rect(0,0,Screen.width,Screen.height);
            var root=(RectTransform)transform;
            root.anchorMin=new Vector2(Mathf.Clamp01((area.xMin-viewport.xMin)/viewport.width),Mathf.Clamp01((area.yMin-viewport.yMin)/viewport.height));
            root.anchorMax=new Vector2(Mathf.Clamp01((area.xMax-viewport.xMin)/viewport.width),Mathf.Clamp01((area.yMax-viewport.yMin)/viewport.height));
            root.offsetMin=root.offsetMax=Vector2.zero;
            var window=transform.Find("Window");
            window.localScale=Vector3.one*Mathf.Min(1,root.rect.width/720f,root.rect.height/1280f);
        }
    }
}
