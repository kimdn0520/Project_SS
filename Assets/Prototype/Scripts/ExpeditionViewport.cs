using UnityEngine;

namespace ProjectSS.Expedition
{
    /// <summary>Keep world and camera-space UI on the same portrait composition inside the safe area.</summary>
    [ExecuteAlways]
    public sealed class ExpeditionViewport : MonoBehaviour
    {
        [SerializeField] private Camera renderCamera;
        private Rect previousSafe;
        private int width, height;
        private Camera backdrop;
        private void OnEnable() { Refresh(); }
        private void Update()
        {
            if (width != Screen.width || height != Screen.height || previousSafe != Screen.safeArea) Refresh();
        }
        public void Refresh()
        {
            if (renderCamera == null || Screen.width <= 0 || Screen.height <= 0) return;
            width = Screen.width; height = Screen.height;
            // Clear the full display before the letterboxed world camera. Otherwise overlay
            // controls leave stale pixels outside its viewport when the resolution changes.
            if (Application.isPlaying && backdrop == null)
            {
                var go = new GameObject("ViewportBackdrop", typeof(Camera));
                go.transform.SetParent(transform, false);
                backdrop = go.GetComponent<Camera>();
                backdrop.cullingMask = 0;
                backdrop.clearFlags = CameraClearFlags.SolidColor;
                backdrop.depth = renderCamera.depth - 1;
                backdrop.backgroundColor = renderCamera.backgroundColor;
            }
            previousSafe = Screen.safeArea;
            Rect area = previousSafe;
            if (area.width <= 0 || area.height <= 0) area = new Rect(0, 0, width, height);
            float target = 720f / 1280f;
            if (area.width / area.height > target)
            {
                float fitted = area.height * target;
                area.x += (area.width - fitted) * 0.5f; area.width = fitted;
            }
            else
            {
                float fitted = area.width / target;
                area.y += (area.height - fitted) * 0.5f; area.height = fitted;
            }
            renderCamera.rect = new Rect(area.x / width, area.y / height, area.width / width, area.height / height);
        }
        private void OnDisable() { if(backdrop!=null) { Destroy(backdrop.gameObject);backdrop=null; } }
    }
}
