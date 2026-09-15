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
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var area=Screen.safeArea;
            var root=(RectTransform)transform;
            root.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);
            root.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            root.offsetMin=root.offsetMax=Vector2.zero;
            var window=transform.Find("Window");
            window.localScale=Vector3.one*Mathf.Min(1,root.rect.width/720f,root.rect.height/1280f);
        }
    }
}
