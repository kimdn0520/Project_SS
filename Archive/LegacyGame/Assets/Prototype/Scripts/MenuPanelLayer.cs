using UnityEngine;
namespace ProjectSS.Expedition
{
    [ExecuteAlways]
    public sealed class MenuPanelLayer : MonoBehaviour
    {
        public PlayPage page;
        public RectTransform composition;
        public GameObject curtain;
        public CanvasGroup group;
        public void BindCamera(Camera camera)
        {
            var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;canvas.planeDistance=10f;
        }
        private void LateUpdate()
        {
            var canvas=GetComponent<Canvas>();
            if(canvas.worldCamera==null)BindCamera(page!=null&&page.Canvas.worldCamera!=null?page.Canvas.worldCamera:Camera.main);
            bool visible=false;
            if(page!=null)foreach(var panel in page.menuPanels)visible|=panel!=null&&panel.activeSelf;
            curtain.SetActive(visible);group.alpha=visible?1:0;group.interactable=group.blocksRaycasts=visible;
            if(Screen.width<=0||Screen.height<=0)return;
            var area=Screen.safeArea;var root=(RectTransform)transform;
            if(canvas.worldCamera!=null){var view=canvas.worldCamera.pixelRect;area=Rect.MinMaxRect(Mathf.Max(area.xMin,view.xMin),Mathf.Max(area.yMin,view.yMin),Mathf.Min(area.xMax,view.xMax),Mathf.Min(area.yMax,view.yMax));}
            float scale=Mathf.Min(area.width/720f,area.height/1280f)/canvas.scaleFactor;
            composition.localScale=Vector3.one*scale;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,area.center,canvas.worldCamera,out var point);
            composition.anchoredPosition=point;
        }
        public void Close(){if(Application.isPlaying&&!PopupManager.IsOpenAny)page.OpenMenu(0);}
    }
}
