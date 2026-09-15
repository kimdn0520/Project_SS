using UnityEngine;
namespace ProjectSS.Expedition
{
    // Overlay canvas uses the whole screen while the world retains its portrait viewport.
    [ExecuteAlways]
    public sealed class ExpeditionSidebarLayout : MonoBehaviour
    {
        public PlayPage page;
        public RectTransform safeArea;
        float nextRefresh;
        void LateUpdate()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            safeArea.gameObject.SetActive(page != null && page.minePanel.activeInHierarchy);
            if(Application.isPlaying&&Time.unscaledTime>=nextRefresh)
            {
                nextRefresh=Time.unscaledTime+1;
                foreach(var icon in safeArea.GetComponentsInChildren<BaseSidebarIcon>(true))icon.RefreshVisibility();
            }
        }
        public void ApplySafeArea(Rect area, Vector2 screen)
        {
            if(screen.x<=0||screen.y<=0)return;
            safeArea.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            safeArea.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
    }
}
