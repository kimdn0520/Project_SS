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
        private void LateUpdate()
        {
            bool visible=false;
            if(page!=null)foreach(var panel in page.menuPanels)visible|=panel!=null&&panel.activeSelf;
            curtain.SetActive(visible);group.alpha=visible?1:0;group.interactable=group.blocksRaycasts=visible;
            if(Screen.width<=0||Screen.height<=0)return;
            var area=Screen.safeArea;var root=(RectTransform)transform;
            float scale=Mathf.Min(area.width/720f,area.height/1280f)/GetComponent<Canvas>().scaleFactor;
            composition.localScale=Vector3.one*scale;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,area.center,null,out var point);
            composition.anchoredPosition=point;
        }
        public void Close(){if(Application.isPlaying&&!PopupManager.IsOpenAny)page.OpenMenu(0);}
    }
}
